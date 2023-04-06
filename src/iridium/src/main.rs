//#![feature(decl_macro)]
extern crate core;

mod api;
mod discord;
pub mod models;
pub mod schema;

use diesel::backend::Backend;
use diesel::{Connection, PgConnection};
use diesel_migrations::{embed_migrations, EmbeddedMigrations, MigrationHarness};
use std::error::Error;

pub const MIGRATIONS: EmbeddedMigrations = embed_migrations!("migrations");

#[allow(dead_code)]
fn run_migrations<DB: Backend>(
    connection: &mut impl MigrationHarness<DB>,
) -> Result<(), Box<dyn Error + Send + Sync + 'static>> {
    // This will run the necessary migrations.
    //
    // See the documentation for `MigrationHarness` for
    // all available methods.
    connection.run_pending_migrations(MIGRATIONS)?;

    Ok(())
}

type Err = Box<dyn Error>;
#[tokio::main]
async fn main() -> Result<(), Err> {
    let db_url = std::env::var("DATABASE_URL").expect("DATABASE_URL must be set");
    let mut connection = PgConnection::establish(&db_url)?;
    run_migrations(&mut connection).map_err(|e| e as Err)?;

    api::Api::run().await?;

    Ok(())
}
