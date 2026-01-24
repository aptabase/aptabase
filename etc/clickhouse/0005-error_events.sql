CREATE TABLE IF NOT EXISTS error_events
(
    `error_id` String,
    `app_id` String,
    `timestamp` DateTime,
    `error_message` String,
    `error_type` String,
    `stack_trace` String,
    `platform` LowCardinality(String),
    `os_name` LowCardinality(String),
    `os_version` String,
    `app_version` String,
    `sdk_version` String,
    `session_id` String,
    `ttl` DateTime
)
ENGINE = MergeTree()
PARTITION BY toYYYYMM(timestamp)
ORDER BY (app_id, timestamp, error_type)
TTL ttl;
