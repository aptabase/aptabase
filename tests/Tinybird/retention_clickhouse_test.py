"""Exercise the checked-in Tinybird materializations on a disposable local ClickHouse database.

Run with CLICKHOUSE_TEST_URL=http://localhost:8123 against the development/CI service.
Only loopback hosts are accepted; each run creates and drops its own audit database.
"""

import base64
import json
import os
import re
import unittest
import uuid
from datetime import datetime, timezone
from pathlib import Path
from urllib.error import HTTPError
from urllib.parse import urlencode, urlparse
from urllib.request import Request, urlopen


PROJECT = Path(__file__).resolve().parents[2] / "etc" / "tinybird"
TARGETS = {
    "events_daily_v3_mv": "key_metrics_daily_v3",
    "event_props_daily_v2_mv": "top_props_daily_v2",
    "monthly_usage_v1": "monthly_usage_v1_mv",
}


@unittest.skipUnless(os.environ.get("CLICKHOUSE_TEST_URL"), "requires a local ClickHouse test service")
class RetentionClickHouseTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.url = os.environ["CLICKHOUSE_TEST_URL"]
        if urlparse(cls.url).hostname not in ("localhost", "127.0.0.1", "::1"):
            raise ValueError("Retention tests must use a loopback ClickHouse test service")
        cls.database = "retention_audit_" + uuid.uuid4().hex
        cls.query(f"CREATE DATABASE {cls.database}", use_database=False)
        cls.addClassCleanup(lambda: cls.query(f"DROP DATABASE {cls.database}", use_database=False))
        for name in ("events", *TARGETS):
            definition = (PROJECT / "datasources" / f"{name}.datasource").read_text()
            schema = definition.split("SCHEMA >", 1)[1].split("\nENGINE", 1)[0].strip()
            schema = re.sub(r"\s+`json:[^`]+`", "", schema)
            engine = re.search(r'^ENGINE "([^"]+)"', definition, re.M)[1]
            sorting = re.search(r'^ENGINE_SORTING_KEY "([^"]+)"', definition, re.M)[1]
            ttl = re.search(r'^ENGINE_TTL "([^"]+)"', definition, re.M)[1]
            partition = re.search(r'^ENGINE_PARTITION_KEY "([^"]+)"', definition, re.M)
            partition_sql = f"PARTITION BY {partition[1]}" if partition else ""
            cls.query(f"CREATE TABLE {name} ({schema}) ENGINE = {engine} {partition_sql} ORDER BY ({sorting}) TTL {ttl}")
            # Make cleanup deterministic: only explicit TTL materialization should remove fixtures.
            cls.query(f"SYSTEM STOP TTL MERGES {name}")
        for target, name in TARGETS.items():
            pipe = (PROJECT / "materializations" / f"{name}.pipe").read_text()
            sql = pipe.split("SQL >", 1)[1].split("\nTYPE", 1)[0].strip()
            cls.query(f"CREATE MATERIALIZED VIEW {name}_audit TO {target} AS {sql}")

    @classmethod
    def query(cls, sql, use_database=True):
        params = {"database": cls.database} if use_database else {}
        request = Request(cls.url.rstrip("/") + "/?" + urlencode(params), data=sql.encode())
        user = os.environ.get("CLICKHOUSE_TEST_USER", "aptabase")
        password = os.environ.get("CLICKHOUSE_TEST_PASSWORD", "aptabase_pw")
        request.add_header("Authorization", "Basic " + base64.b64encode(f"{user}:{password}".encode()).decode())
        try:
            with urlopen(request, timeout=30) as response:
                return response.read().decode()
        except HTTPError as error:
            raise AssertionError(error.read().decode()) from error

    def rows(self, sql):
        return json.loads(self.query(sql + " FORMAT JSON"))["data"]

    def insert(self, app_id, timestamp, ttl, value):
        self.query(f"""INSERT INTO events (app_id, timestamp, ttl, event_name, user_id, session_id,
            string_props, numeric_props) VALUES ('{app_id}', {timestamp}, {ttl}, 'test', 'user', 'session',
            '{{"color":"blue"}}', '{{"score":{value}}}')""")

    def test_each_policy_has_its_own_aggregate_bucket(self):
        app = "policies"
        timestamp = "toStartOfDay(now()) + toIntervalHour(12)"
        for months in (6, 12, 24, 60):
            self.insert(app, timestamp, f"addMonths({timestamp}, {months})", months)
        for target in TARGETS:
            self.query(f"OPTIMIZE TABLE {target} FINAL")
            count_column = "events" if target == "monthly_usage_v1" else "events_count"
            rows = self.rows(f"SELECT expires_at, countMerge({count_column}) AS count FROM {target} WHERE app_id = '{app}' GROUP BY expires_at")
            self.assertEqual(len(rows), 4, target)
            self.assertTrue(all(int(row["count"]) == 1 for row in rows), target)

    def test_cleanup_keeps_longer_lived_states_and_legacy_monthly_counts(self):
        app = "cleanup"
        timestamp = "toStartOfDay(now()) - toIntervalDay(1) + toIntervalHour(12)"
        self.insert(app, timestamp, "now() - toIntervalDay(1)", 10)
        self.insert(app, timestamp + " + toIntervalMinute(1)", "now() + toIntervalDay(365)", 20)
        self.insert(app, timestamp + " + toIntervalMinute(2)", "now() + toIntervalDay(730)", 30)
        self.insert("other_app", timestamp, "now() + toIntervalDay(365)", 99)
        self.query(f"""INSERT INTO monthly_usage_v1 (app_id, period, events)
            SELECT '{app}', toStartOfMonth(now()), countState() FROM numbers(5)""")
        for target in TARGETS:
            self.query(f"OPTIMIZE TABLE {target} FINAL")

        # The endpoint's grouping must reunite one session across distinct expiry buckets.
        before = self.rows(f"""SELECT uniqExact(user_id) AS users, uniqExact(session_id) AS sessions,
            maxMerge(max_ts) - minMerge(min_ts) AS duration, countMerge(events_count) AS count
            FROM events_daily_v3_mv WHERE app_id = '{app}'""")[0]
        self.assertEqual((int(before["users"]), int(before["sessions"]), int(before["duration"]), int(before["count"])), (1, 1, 120, 3))

        self.query("ALTER TABLE events MATERIALIZE TTL SETTINGS mutations_sync = 2")
        self.assertEqual(int(self.rows(f"SELECT count() AS count FROM events WHERE app_id = '{app}'")[0]["count"]), 2)
        self.assertEqual(int(self.rows(f"SELECT countMerge(events_count) AS count FROM events_daily_v3_mv WHERE app_id = '{app}'")[0]["count"]), 3)
        for target in TARGETS:
            self.query(f"ALTER TABLE {target} MATERIALIZE TTL SETTINGS mutations_sync = 2")

        after = self.rows(f"""SELECT maxMerge(max_ts) - minMerge(min_ts) AS duration,
            countMerge(events_count) AS count FROM events_daily_v3_mv WHERE app_id = '{app}'""")[0]
        self.assertEqual((int(after["duration"]), int(after["count"])), (60, 2))
        props = self.rows(f"""SELECT countMerge(events_count) AS count, sumMerge(sum_value_state) AS sum,
            minMerge(min_value_state) AS min, maxMerge(max_value_state) AS max,
            medianMerge(numeric_value_state) AS median FROM event_props_daily_v2_mv WHERE app_id = '{app}'""")[0]
        self.assertEqual((int(props["count"]), props["sum"], props["min"], props["max"]), (2, 50, 20, 30))
        self.assertTrue(20 <= props["median"] <= 30)
        monthly = self.rows(f"SELECT retention_managed, countMerge(events) AS count FROM monthly_usage_v1 WHERE app_id = '{app}' GROUP BY retention_managed")
        self.assertEqual({int(row["retention_managed"]): int(row["count"]) for row in monthly}, {0: 5, 1: 2})
        self.assertEqual(int(self.rows("SELECT countMerge(events_count) AS count FROM events_daily_v3_mv WHERE app_id = 'other_app'")[0]["count"]), 1)

    def test_calendar_clamping_merges_monthly_counts_with_the_same_expiry(self):
        # Month-end timestamps can share an expiry day. Their monthly count states should merge.
        for day in (30, 31):
            timestamp = f"toDateTime('{datetime.now(timezone.utc).year}-08-{day} 12:00:00')"
            self.insert("month_end", timestamp, f"addMonths({timestamp}, 6)", 1)
        self.query("OPTIMIZE TABLE monthly_usage_v1 FINAL")
        rows = self.rows("SELECT expires_at, countMerge(events) AS count FROM monthly_usage_v1 WHERE app_id = 'month_end' GROUP BY expires_at")
        self.assertEqual(len(rows), 1)
        self.assertEqual(int(rows[0]["count"]), 2)

    def test_explicit_migration_preserves_aggregate_only_history(self):
        patch = (PROJECT / "migrations" / "app_retention.patch").read_text()
        sections = patch.split("diff --git ")[1:]
        timestamp = "toStartOfDay(now()) - toIntervalDay(500)"
        for days in (365, 730):
            self.insert("assigned_before_rewrite", "now()", f"now() + toIntervalDay({days})", 10)
        for section in sections:
            target = re.search(r"datasources/(\w+)\.datasource", section)[1]
            forward = "\n".join(line[1:] for line in section.splitlines() if line.startswith("+    ")).strip()
            definition = (PROJECT / "datasources" / f"{target}.datasource").read_text()
            # The legacy schema has the same aggregate columns, without the new expiry metadata.
            schema = definition.split("SCHEMA >", 1)[1].split("\nENGINE", 1)[0].split(",\n    `expires_at`", 1)[0].strip()
            legacy = "legacy_" + target
            copied = "copied_" + target
            self.query(f"CREATE TABLE {legacy} ({schema}) ENGINE = AggregatingMergeTree ORDER BY (app_id, period)")
            self.query(f"CREATE TABLE {copied} AS {target}")
            if target == "monthly_usage_v1":
                self.query(f"INSERT INTO {legacy} SELECT 'legacy_only', toStartOfMonth({timestamp}), countState() FROM numbers(3)")
            elif target == "events_daily_v3_mv":
                self.query(f"""INSERT INTO {legacy} (app_id, period, user_id, session_id, min_ts, max_ts, events_count)
                    SELECT 'legacy_only', {timestamp}, 'user', 'session', minState({timestamp}),
                    maxState({timestamp} + toIntervalMinute(number)), countState() FROM numbers(3)""")
            else:
                self.query(f"""INSERT INTO {legacy} (app_id, period, events_count, numeric_value_state,
                    min_value_state, max_value_state, sum_value_state)
                    SELECT 'legacy_only', {timestamp}, countState(), quantileState(toFloat32(10)),
                    minState(toFloat32(10)), maxState(toFloat32(10)), sumState(toFloat32(10)) FROM numbers(3)""")
            self.assertEqual(int(self.rows("SELECT count() AS count FROM events WHERE app_id = 'legacy_only'")[0]["count"]), 0)
            self.query(f"INSERT INTO {copied} {forward} FROM {legacy}")
            self.query(f"ALTER TABLE {copied} MATERIALIZE TTL SETTINGS mutations_sync = 2")
            count_column = "events" if target == "monthly_usage_v1" else "events_count"
            self.assertEqual(int(self.rows(f"SELECT countMerge({count_column}) AS count FROM {copied}")[0]["count"]), 3)
            if target == "monthly_usage_v1":
                row = self.rows(f"SELECT expires_at, retention_managed FROM {copied}")[0]
                self.assertEqual(row, {"expires_at": "1970-01-01 00:00:00", "retention_managed": 0})
            else:
                row = self.rows(f"SELECT expires_at = period + toIntervalDay(1500) AS preserved FROM {copied}")[0]
                self.assertEqual(row["preserved"], 1)
                if target == "event_props_daily_v2_mv":
                    row = self.rows(f"SELECT sumMerge(sum_value_state) AS sum, medianMerge(numeric_value_state) AS median FROM {copied}")[0]
                    self.assertEqual(row, {"sum": 30, "median": 10})
            # Once migrated, the ordinary forward query must preserve already assigned deadlines.
            # This also catches accidentally committing the one-time patch into regular definitions.
            rewrite = "rewrite_" + target
            self.query(f"CREATE TABLE {rewrite} AS {target}")
            normal_forward = definition.split("FORWARD_QUERY >", 1)[1].strip()
            self.query(f"INSERT INTO {rewrite} {normal_forward} FROM {target} WHERE app_id = 'assigned_before_rewrite'")
            columns = f"expires_at, countMerge({count_column}) AS count"
            groups = "expires_at"
            if target == "monthly_usage_v1":
                columns += ", retention_managed"
                groups += ", retention_managed"
            expected = self.rows(f"SELECT {columns} FROM {target} WHERE app_id = 'assigned_before_rewrite' GROUP BY {groups} ORDER BY expires_at")
            actual = self.rows(f"SELECT {columns} FROM {rewrite} GROUP BY {groups} ORDER BY expires_at")
            self.assertEqual(len(expected), 2)
            self.assertEqual(actual, expected)


if __name__ == "__main__":
    unittest.main()
