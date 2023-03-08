-- Your SQL goes here

-- Create a postgres table called "images" that has an auto-incrementing ID, a category, a hash, and when it was updated
CREATE EXTENSION pg_trgm; -- For fuzzy string matching

CREATE TABLE images (
    id SERIAL PRIMARY KEY,
    category TEXT NOT NULL, -- "discord_modmail_nuggies", or the likes
    source TEXT NOT NULL, -- Discord, Twitter, etc.
    added_by BIGINT NOT NULL, -- Discord ID of the person who added it
    added TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
    md5_hash TEXT UNIQUE, -- Discord uses MD5 hashes for their image URLs
    phash bytea UNIQUE NOT NULL -- If that fails, we'll use perceptual hashes
);

