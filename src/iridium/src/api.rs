use crate::discord::DiscordUser;
use crate::models::{Image, NewImage};
use axum::extract::{Query, State};
use axum::{
    routing::{post, put},
    Json, Router,
};

use chrono::NaiveDateTime;
use diesel::{
    r2d2,
    r2d2::ConnectionManager,
    sql_types::{Bytea, Float},
    FromSqlRow, PgConnection, RunQueryDsl,
};
use hyper::StatusCode;
use image::ImageFormat;
use image_hasher::{HasherConfig, ImageHash};
use serde::{Deserialize, Serialize};
use std::{
    collections::HashMap,
    ops::DerefMut,
    sync::{Arc, Mutex},
};

pub struct Api;

type SharedConnectionManager = Arc<Mutex<r2d2::Pool<ConnectionManager<PgConnection>>>>;
type PooledConnection = r2d2::PooledConnection<ConnectionManager<PgConnection>>;

#[derive(Clone)]
pub(crate) struct Config {
    pub bot_token: String,
    pub db_pool: SharedConnectionManager,
}

impl Api {
    pub async fn run() -> Result<(), Box<dyn std::error::Error>> {
        let bot_token = std::env::var("BOT_TOKEN").expect("BOT_TOKEN must be set");
        let db_url = std::env::var("DATABASE_URL").expect("DATABASE_URL must be set");

        let db_manager = ConnectionManager::<PgConnection>::new(db_url.clone());

        let db_pool = r2d2::Pool::builder()
            .build(db_manager)
            .expect("Failed to create pool.");

        let db_pool = Arc::new(Mutex::new(db_pool));

        let app = Router::new()
            .route("/phishing/check/image", post(check_image))
            .route("/phishing/submit/image", put(create_image))
            .with_state(Config { bot_token, db_pool });

        axum::Server::bind(&"127.0.0.1:7000".parse().unwrap())
            .serve(app.into_make_service())
            .await?;

        Ok(())
    }
}

#[derive(Serialize, FromSqlRow)]
struct FoundHashResponse {
    category: String,
    score: f32,
    added_by: i64,
    added_at: NaiveDateTime,
}

#[derive(Deserialize)]
struct SubmitImageResponse {
    url: String,
    category: String,
    added_by: i64,
    md5: Option<String>,
}

async fn create_image(
    State(config): State<Config>,
    //_user: DiscordUser,
    Json(mut body): Json<SubmitImageResponse>,
) -> Result<StatusCode, StatusCode> {
    let pg_connection = &mut config.db_pool.clone().lock().unwrap().get().unwrap();

    if body.url.contains("discord") {
        let idx = body.url.find("?size=");

        if let Some(idx) = idx {
            let url = body.url[..idx].to_string() + "?size=1024";
            body.url = url;
        }
    }

    let res = reqwest::get(&body.url).await.unwrap();

    let bytes = res.bytes().await.map_err(|_| StatusCode::BAD_REQUEST)?;

    let image = image::load_from_memory_with_format(&bytes, ImageFormat::Png).unwrap();

    let hasher = HasherConfig::new().to_hasher();
    let hash = hasher.hash_image(&image).as_bytes().to_vec();

    let image = NewImage {
        source: body.url.as_str(),
        category: body.category.as_str(),
        added_by: body.added_by,
        md5_hash: body.md5.as_ref().map(|s| s.as_str()),
        phash: hash,
    };

    diesel::insert_into(crate::schema::images::table)
        .values(&image)
        .execute(pg_connection)
        .map_err(|_| StatusCode::INTERNAL_SERVER_ERROR)
        .map(|c| {
            if c > 0 {
                StatusCode::CREATED
            } else {
                StatusCode::BAD_REQUEST
            }
        })
}

// Write an API endpoint (POST /check/image) that takes an avatar hash as a query parameter, and requires authentication.
async fn check_image(
    State(pg): State<Config>,
    Query(query): Query<HashMap<String, String>>,
    //_user: DiscordUser,
) -> Result<Json<FoundHashResponse>, StatusCode> {
    let hash = query.get("hash").ok_or(StatusCode::NOT_FOUND)?;

    let id = query.get("id").ok_or(StatusCode::NOT_FOUND)?;

    let threshold = query
        .get("threshold")
        .and_then(|t| t.parse::<f32>().ok())
        .unwrap_or(0.95);

    let image = reqwest::get(format!(
        "https://cdn.discordapp.com/avatars/{}/{}.png?size=256",
        id, hash
    ))
    .await
    .map_or(Err(StatusCode::BAD_REQUEST), |r| {
        if r.status().is_success() {
            Ok(r)
        } else {
            Err(StatusCode::BAD_REQUEST)
        }
    })?
    .bytes()
    .await
    .map_err(|_| StatusCode::INTERNAL_SERVER_ERROR)?;

    let image = compute_hash(image.to_vec()).ok_or(StatusCode::BAD_REQUEST)?;

    let hash_bytes = image.as_bytes().to_vec();

    let pg_connection = &mut pg.db_pool.clone().lock().unwrap().get().unwrap();

    get_most_similar_image(hash_bytes, threshold, pg_connection)
        .ok_or(StatusCode::NOT_FOUND)
        .map(|(image, score)| {
            Json(FoundHashResponse {
                category: image.category,
                score,
                added_by: image.added_by,
                added_at: image.added,
            })
        })
}

fn get_most_similar_image(
    bytes: Vec<u8>,
    threshold: f32,
    pg_conn: &mut PooledConnection,
) -> Option<(Image, f32)> {
    use diesel::dsl::*;

    let conn = pg_conn.deref_mut();

    // Find the most similar image in the database using the <-> operator
    let image = sql_query(include_str!("../sql/phash.sql"))
        .bind::<Bytea, _>(&bytes)
        .bind::<Float, _>(threshold)
        .load::<Image>(conn)
        .ok()?
        .first_mut()
        .map(|i| i.clone());

    if let Some(img) = image {
        use bitvec::prelude::*;

        let dist_vec = img
            .phash
            .iter()
            .zip(bytes)
            .map(|(a, b)| *a ^ b)
            .collect::<Vec<u8>>();

        let dist = dist_vec.as_bits::<Msb0>().count_ones();

        // Normalize the distance between 1 and 0

        let dist_normalized = 1.0 - (dist as f32 / dist_vec.len() as f32);

        return Some((img, dist_normalized));
    }

    None
}

fn compute_hash(image: Vec<u8>) -> Option<ImageHash> {
    let image = match image::load_from_memory(&image) {
        Ok(image) => image,
        Err(_) => return None,
    };

    let config = HasherConfig::new().to_hasher();

    Some(config.hash_image(&image))
}
