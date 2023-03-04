use std::error::Error;
use axum::{async_trait, http};
use axum::extract::{FromRequest, FromRequestParts};
use axum::http::Request;
use axum::http::request::Parts;
use hyper::client::HttpConnector;
use hyper::{StatusCode, Uri};
use hyper_tls::HttpsConnector;
use reqwest::{Client};
use serde::{Deserialize};
use crate::api::Config;

const DISCORD_API_OAUTH2_URL: &str = "https://discord.com/api/v10/users/@me";
const DISCORD_API_APPLICATION_URL: &str = "https://discord.com/api/v10/oauth2/applications/@me";

#[derive(Debug, Clone, Deserialize)]
struct DiscordApplication {
    pub id: String,
    pub owner: DiscordUser,
    pub team: Option<DiscordTeam>,
}

#[derive(Debug, Clone, Deserialize)]
pub(crate) struct DiscordUser {
    pub id: String,
    pub username: String,
}

#[derive(Debug, Clone, Deserialize)]
pub(crate) struct DiscordTeam {
    pub id: String,
    pub name: String,
    pub members: Vec<DiscordUser>,
}

struct DiscordAPI;

#[derive(Debug)]
pub enum OAuth2Error {
    RequestError,
    InvalidToken,
    IncorrectUser
}

impl PartialEq for DiscordUser {
    fn eq(&self, other: &Self) -> bool {
        self.id == other.id
    }
}


impl DiscordAPI {
    async fn validate_token(token: String) -> Result<Client, OAuth2Error> {
        let client = Client::new();

        client
            .get(DISCORD_API_OAUTH2_URL)
            .bearer_auth(token)
            .send()
            .await
            .map_err(|_| OAuth2Error::RequestError)?;

        Ok(client)
    }

    async fn get_user_info(client: &Client, token: String) -> Result<DiscordUser, OAuth2Error> {

        let user = client
            .get(DISCORD_API_OAUTH2_URL)
            .bearer_auth(token)
            .send()
            .await
            .map_err(|_| OAuth2Error::RequestError)?
            .json::<DiscordUser>()
            .await
            .unwrap();//.map_err(|_| OAuth2Error::RequestError)?;

        Ok(user)
    }

    async fn get_owners(client: &Client, token: String) -> Result<Vec<DiscordUser>, OAuth2Error>
    {
        let owners = client
            .get(DISCORD_API_APPLICATION_URL)
            .header("Authorization", format!("Bot {}", token))
            .send()
            .await
            .map_err(|_| OAuth2Error::RequestError)?
            .json::<DiscordApplication>()
            .await
            .map(|app| app.team.map(|team| team.members).unwrap_or(vec![app.owner]))
            .map_err(|_| OAuth2Error::RequestError)?;

        Ok(owners)
    }
}


#[async_trait]
impl FromRequestParts<Config> for DiscordUser
{
    type Rejection = StatusCode;

    async fn from_request_parts(parts: &mut Parts, state: &Config) -> Result<Self, Self::Rejection> {
        let token = parts
            .headers
            .get("Authorization")
            .ok_or(StatusCode::UNAUTHORIZED)?
            .to_str()
            .map_err(|_| StatusCode::BAD_REQUEST)?
            .to_string();

        let token = token.strip_prefix("Bearer ").ok_or(StatusCode::BAD_REQUEST)?.to_string();

        let client = DiscordAPI::validate_token(token.to_owned()).await.map_err(|_| StatusCode::UNAUTHORIZED)?;
        let user = DiscordAPI::get_user_info(&client, token).await.map_err(|_| StatusCode::UNAUTHORIZED)?;

        let owners = DiscordAPI::get_owners(&client, state.bot_token.to_string()).await.map_err(|_| StatusCode::INTERNAL_SERVER_ERROR)?;

        if !owners.contains(&user) {
            return Err(StatusCode::FORBIDDEN);
        }

        Ok(user)
    }
}