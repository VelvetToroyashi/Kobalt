use std::collections::HashMap;
use axum::{Router, routing::post};
use axum::extract::Query;
use axum::response::IntoResponse;
use serde::Deserialize;
use crate::discord::DiscordUser;

pub struct Api;

#[derive(Clone)]
pub(crate) struct Config {
    pub bot_token: String,
}

impl Api {
    pub async fn run() -> Result<(), Box<dyn std::error::Error>> {
        let bot_token = std::env::var("BOT_TOKEN").expect("BOT_TOKEN must be set");

        let app = Router::new()
            .route("/phishing/check/image", post(check_image))
            .with_state(Config { bot_token });

        axum::Server::bind(&"127.0.0.1:3000".parse().unwrap())
            .serve(app.into_make_service())
            .await?;



        Ok(())
    }
}

// Write an API endpoint (POST /check/image) that takes an avatar hash as a query parameter, and requires authentication.
async fn check_image(Query(hash): Query<HashMap<String, String>>, _user: DiscordUser) -> impl IntoResponse {
    format!("Hello, world! {}", hash["hash"])
}