-- Your SQL goes here

-- Create a postgres table called "images" that has an auto-incrementing ID, a category, a hash, and when it was updated
CREATE TABLE images (
    id SERIAL PRIMARY KEY,
    category TEXT NOT NULL,
    source TEXT NOT NULL,
    added TIMESTAMP NOT NULL DEFAULT NOW(),
    md5_hash CHAR(32) UNIQUE,
    phash bytea NOT NULL
);

