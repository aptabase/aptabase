Future improvements for sessions and events tables:

- Review https://clickhouse.com/blog/common-getting-started-issues-with-clickhouse
- Should sorting key be toDate(timestamp) instead of timestamp? See https://kb.altinity.com/engines/mergetree-table-engine-family/pick-keys/, maybe add both toStartOfDay(timestamp) and timestamp, see https://github.com/ClickHouse/ClickHouse/issues/33056
- Add user_id and session_id to sorting key?
- Use Codecs to compress data
- Use `UInt64` for app_id, user_id and session_id
- country code should be `LowCardinality(FixedString(2))`
- add `LowCardinality` to os_version, sdk_version, engine_version
- string_props and numeric_props should be a dictionary (??)
- add sampling key

- Create a sessions table that is managed by the app (not MV, probably using CollapsingMergeTree)
  app_id - available in request
  timestamp - can be cached (static)
  duration - can be cached (dynamic)
  events - has to increment
  user_id - available in request
  session_id - available in request
  os_name - available in request
  os_version - available in request
  locale - available in request
  app_version - available in request
  app_build_number - available in request
  engine_name - available in request
  engine_version - available in request
  sdk_version - available in request
  country_code - available in request
  region_name - available in request
  ttl - timestamp + days (depends on DEBUG / RELEASE)

Cache Key: App ID + Session ID
Cache Value: (timestamp, duration, events)
