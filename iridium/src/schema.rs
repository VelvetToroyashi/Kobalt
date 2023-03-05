// @generated automatically by Diesel CLI.

diesel::table! {
    images (id) {
        id -> Int4,
        category -> Text,
        source -> Text,
        added_by -> Int8,
        added -> Timestamp,
        md5_hash -> Nullable<Text>,
        phash -> Bytea,
    }
}
