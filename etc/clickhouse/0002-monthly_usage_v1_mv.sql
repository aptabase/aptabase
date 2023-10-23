CREATE OR REPLACE VIEW monthly_usage_v1
AS
SELECT
    app_id,
    toStartOfMonth(timestamp) AS period,
    countState() AS events
FROM events
GROUP BY app_id, period