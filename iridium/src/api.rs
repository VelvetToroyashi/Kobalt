use rocket::{get, put, delete, Rocket, routes};

pub struct Api;

impl Api {
    pub fn new() -> Api {
        Api
    }

    pub async fn run(&self) -> Result<(), Box<dyn std::error::Error>> {
        let res = rocket::build()
            .mount("/", routes![])
            .launch()
            .await?;

        Ok(())
    }
}