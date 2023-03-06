SELECT *
    FROM images
    WHERE phash <-> $1 < $2
    ORDER BY phash <-> $1 DESC -- Is there a better way to do this?
    LIMIT 1;