use rocket::{put, delete, Rocket, State, routes};
use std::sync::Mutex;

pub fn create_rocket() -> Rocket {
    Rocket::ignite()
        .mount("/", routes![put, delete])
        .manage(Mutex::new(0))
}