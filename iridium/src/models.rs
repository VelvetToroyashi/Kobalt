use diesel::prelude::*;
use chrono::NaiveDateTime;
use serde::{Deserialize};
use crate::schema::images;

#[derive(Queryable)]
pub struct Image {
    pub id: i32,
    pub category: String,
    pub source: String,
    pub added: NaiveDateTime,
    pub md5_hash: Option<String>,
    pub phash: Vec<u8>,
}

#[derive(Deserialize, Insertable)]
#[table_name = "images"]
pub struct NewImage<'a> {
    pub category: &'a str,
    pub source: &'a str,
    pub md5_hash: Option<&'a str>,
    pub phash: &'a [u8],
}