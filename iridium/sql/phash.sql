SELECT * FROM images
    ORDER BY phash <-> $1 ASC
    LIMIT 1