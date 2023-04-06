SELECT *
    FROM
    (
        SELECT
            *,
            phash::TEXT <-> $1::TEXT AS sml
        FROM
            images
    )
AS sml
WHERE sml > $2
ORDER BY sml DESC
LIMIT 1;