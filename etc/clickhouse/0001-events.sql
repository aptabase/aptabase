CREATE TABLE IF NOT EXISTS events
(
    `app_id` String,
    `timestamp` DateTime,
    `event_name` String,
    `user_id` String,
    `session_id` String,
    `os_name` LowCardinality(String),
    `os_version` String,
    `locale` LowCardinality(String),
    `app_version` String,
    `app_build_number` String,
    `engine_name` LowCardinality(String),
    `engine_version` String,
    `sdk_version` String,
    `country_code` LowCardinality(String),
    `region_name` LowCardinality(String),
    `city` String,
    `string_props` String,
    `numeric_props` String,
    `ttl` DateTime
)
ENGINE = MergeTree()
PARTITION BY toYYYYMM(timestamp)
ORDER BY (app_id, timestamp, event_name)
TTL ttl;