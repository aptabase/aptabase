ALTER TABLE error_events
ADD COLUMN IF NOT EXISTS severity LowCardinality(String),
ADD COLUMN IF NOT EXISTS kind LowCardinality(String);
