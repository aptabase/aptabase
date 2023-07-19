CREATE VIEW IF NOT EXISTS billing_usage_v1_mv
AS
SELECT
    app_id,
    toYear(timestamp) AS year,
    toMonth(timestamp) AS month,
    toDayOfMonth(timestamp) AS day,
    countState() AS events
FROM events
GROUP BY app_id, year, month, day