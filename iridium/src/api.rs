use crate::discord::DiscordUser;
use crate::models::Image;
use axum::extract::{Query, State};
use axum::{routing::post, Json, Router};
use axum_macros::debug_handler;
use diesel::{
    r2d2,
    r2d2::ConnectionManager,
    sql_types::{Bytea, Float},
    FromSqlRow, PgConnection, RunQueryDsl,
};
use hyper::StatusCode;
use img_hash::{image, HasherConfig, ImageHash};
use serde::Serialize;
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
    added_by: String,
    added_at: String,
}

// Write an API endpoint (POST /check/image) that takes an avatar hash as a query parameter, and requires authentication.
#[debug_handler]
async fn check_image(
    State(pg): State<Config>,
    Query(query): Query<HashMap<String, String>>,
    _user: DiscordUser,
) -> Result<Json<FoundHashResponse>, StatusCode> {
    let hash = query.get("hash").ok_or(StatusCode::BAD_REQUEST)?;

    let id = query.get("id").ok_or(StatusCode::BAD_REQUEST)?;

    let threshold = query
        .get("threshold")
        .and_then(|t| t.parse::<f32>().ok())
        .unwrap_or(0.95);

    let fetch = reqwest::get(format!(
        "https://cdn.discordapp.com/avatars/{}/{}.png?size=256",
        id, hash
    ))
    .await;

    let image = fetch
        .map_err(|_| StatusCode::BAD_REQUEST)?
        .bytes()
        .await
        .map_err(|_| StatusCode::INTERNAL_SERVER_ERROR)?;

    let image = compute_hash(image.to_vec()).ok_or(StatusCode::BAD_REQUEST)?;

    let hash_bytes = image.as_bytes().to_vec();

    let pg_connection = &mut pg.db_pool.clone().lock().unwrap().get().unwrap();

    Err(StatusCode::NOT_FOUND)
}

fn get_most_similar_image(
    bytes: Vec<u8>,
    threshold: f32,
    pg_conn: &mut PooledConnection,
) -> Option<Image> {
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

    image
}

fn compute_hash(image: Vec<u8>) -> Option<ImageHash> {
    let image = match image::load_from_memory(&image) {
        Ok(image) => image,
        Err(_) => return None,
    };

    let config = HasherConfig::new().to_hasher();

    Some(config.hash_image(&image))
}
