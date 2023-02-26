#![feature(decl_macro)]
pub mod models;
pub mod schema;
mod api;

use std::error::Error;
use diesel::{backend::Backend};
use diesel_migrations::{embed_migrations, EmbeddedMigrations, MigrationHarness};

pub const MIGRATIONS: EmbeddedMigrations = embed_migrations!("migrations");

#[allow(dead_code)]
fn run_migrations<DB: Backend>(connection: &mut impl MigrationHarness<DB>) -> Result<(), Box<dyn Error + Send + Sync + 'static>> {

    // This will run the necessary migrations.
    //
    // See the documentation for `MigrationHarness` for
    // all available methods.
    connection.run_pending_migrations(MIGRATIONS)?;

    Ok(())
}

type Err = Box<dyn Error>;
#[rocket::main]
async fn main() -> Result<(), Err> {
    api::Api::new().run().await?;


    Ok(())
}