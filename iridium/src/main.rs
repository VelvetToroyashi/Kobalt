use sqlx::postgres::PgPool;
use std::error::Error;
use sqlx::migrate::Migrator;

static MIGRATOR: Migrator = sqlx::migrate!();

type Err = Box<dyn Error>;
#[tokio::main]
async fn main() -> Result<(), Err> {


    Ok(())
}
