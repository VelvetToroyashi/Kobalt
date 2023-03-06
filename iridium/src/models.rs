use crate::schema::images;
use chrono::NaiveDateTime;
use diesel::prelude::*;
use serde::{Deserialize, Serialize};

#[derive(Serialize, Deserialize, QueryableByName, Clone)]
#[table_name = "images"]
pub struct Image {
    pub id: i32,
    pub category: String,
    pub source: String,
    pub added_by: i64,
    pub added: NaiveDateTime,
    pub md5_hash: Option<String>,
    pub phash: Vec<u8>,
}

#[derive(Serialize, Deserialize, Insertable)]
#[table_name = "images"]
pub struct NewImage<'a> {
    pub category: &'a str,
    pub source: &'a str,
    pub md5_hash: Option<&'a str>,
    pub phash: Vec<u8>,
}
