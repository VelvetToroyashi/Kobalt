// @generated automatically by Diesel CLI.

diesel::table! {
    images (id) {
        id -> Int4,
        category -> Text,
        source -> Text,
        added -> Timestamp,
        md5_hash -> Nullable<Bpchar>,
        phash -> Bytea,
    }
}
