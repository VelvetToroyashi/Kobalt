use diesel::prelude::*;
use chrono::NaiveDateTime;
use diesel::FromSqlRow;
use serde::{Serialize, Deserialize};
use crate::schema::images;

#[derive(Serialize, Deserialize, Queryable)]
//#[table_name = "images"]
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