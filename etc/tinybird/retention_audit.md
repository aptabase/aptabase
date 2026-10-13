# Retention audit — 13 October 2026

Scope: [PR #197](https://github.com/aptabase/aptabase/pull/197), including app creation/editing, authorization, ingestion, buffering/retries, raw TTL, every persisted analytics aggregate, endpoint aggregation, schema evolution, and deployment ordering. This is a review of the retention change and its affected paths, not a security certification of the entire application.

The implementation is functionally sound in the verified cases after the fixes below. Production promotion still requires a Cloud migration rehearsal under ingestion and capacity checks. No production schema, app setting, or customer event was changed during this audit.

## Findings fixed in the PR

| Severity | Finding | Fix and evidence |
| --- | --- | --- |
| High | Backend deployment accepted a skipped regional schema job even when a failed Tinybird build/check caused the skip. The new backend could therefore run against the old aggregate schema. | Require successful change detection and either a successful regional schema deployment or an explicit `changed == false`. Production schema jobs also depend on successful Cloud dev deployment. Workflow validation passes. |
| High | App editing checked ownership before an icon upload, then updated by app ID alone. Ownership transfer or deletion during the upload could let the previous owner change retention. | Repeat owner and active-app predicates in the atomic update. `RETURNING retention_months` reports the committed value when another edit changes policy. Integration tests reproduce transfer, deletion, and concurrent policy edits during upload. |
| Medium | The fresh policy read ran before parsing/validation and on error reporting, including the cached exhausted-quota path. Malformed traffic with valid public app keys could add unnecessary database load. | Resolve current retention after event validation and only before buffering events. Empty/oversized/invalid batches perform no policy read; a valid batch performs one. Error reporting keeps its independent 180-day cap without this read. |
| Medium | Existing timestamp handling compared a local `DateTime` with UTC before normalization. A past offset timestamp was clamped to receipt time on the non-UTC development host, changing its expiry. | Normalize to UTC before future-date clamping. The failing offset test now passes for release/debug events; unit tests cover local timestamps and positive/negative JSON offsets. |
| Medium | Non-400 save failures could be treated as successful JSON responses, and failed form submissions left processing state stuck. A name-only save also resent the loaded policy and could undo another tab's policy change. | App mutations reject non-success responses. All three forms recover from failures, preserve selected values, and prevent duplicate submissions while processing. Editing sends retention only when the selector was changed. Browser checks cover 403, 500, network failure, and successful retries. |
| Medium | Deployment targeting and initial migration logic were hidden inside a general deployment wrapper. | Remove the wrapper. CI uses ordinary Tinybird commands with required host/token values and GitHub's secret masking. The initial upgrade uses a readable SQL patch applied once per legacy workspace and removed after migration. Regular CI never edits datasource definitions. |

## Verified data behavior

The database accepts only 6, 12, 24, or 60 months and defaults existing/new apps to 60. The API validates the same choices. Owners can set them during creation or editing; foreign users and shared readers cannot edit. Omitted or null update values preserve the saved policy. App IDs, expiry timestamps, and retention fields in an SDK payload cannot override the header-resolved app and server policy.

Each accepted request reads the committed policy once from PostgreSQL, snapshots it, and uses it for every event in its batch. Existing, in-flight, buffered, and serialized events keep their assigned policy. Setting changes do not rewrite history. Expiry uses calendar months from the normalized UTC event timestamp, including leap days and end-of-month clamping. Debug events never exceed their 182-day cap; errors remain capped at 180 days and session aggregates at 30 days.

Daily event/property targets include expiry day in their grouping and sorting keys. Monthly usage also separates managed new states from unmanaged legacy states. These keys prevent a shorter-lived contribution from merging irreversibly with a longer-lived contribution. All relevant endpoints merge count/min/max/quantile/sum states across surviving buckets, and unique sessions/users are not counted once per bucket. Native cleanup tests verify counts, durations, numeric properties, and an unaffected second app after forcing TTL cleanup.

Migration copies aggregate states rather than reconstructing them from raw rows that may already have expired. Legacy daily deadlines remain `period + 1500 days`; legacy monthly states retain no expiry. The [one-time patch](migrations/README.md) supplies those defaults with plain SQL; normal deployments keep identity forward queries. Static Local upgrades and subsequent schema rewrites preserved legacy history and assigned deadlines. Existing raw `ttl` values are untouched. Tinybird's documented [forward-query backfill](https://www.tinybird.co/docs/forward/guides/evolve-data-source) supports this approach.

## Performance and operational limits

The remaining ingestion cost is one indexed PostgreSQL read per valid event request, including batches, with request cancellation passed through. The local query plan uses the app primary-key index; the observed 0.045 ms execution time is not a network or production benchmark. At 10,000 events/second, full 25-event batches mean roughly 400 additional reads/second; single-event requests mean 10,000. These are workload calculations, not measured production capacity. Immediate cross-replica policy changes deliberately avoid a retention cache. Monitor round-trip latency, pool waits, database CPU, and ingestion error rate at actual peak traffic.

Expiry is grouped by day to preserve aggregation efficiency. A stable policy generally adds one expiry bucket to each existing daily group. New monthly usage has roughly one state per contributing day instead of one per month, with additional buckets if policies change. Default five-year daily/property history is also longer than the previous 1500-day retention: approximately 22% more retained days at steady state, before compression and workload effects. The extra columns/index entries, retained history, and one-time rewrite require storage and backfill capacity estimates.

Different lifetimes share parts. Keep `ttl_only_drop_parts = 0`; enabling it can retain expired short-lived rows until every row in a part expires. This is Tinybird's documented [default](https://www.tinybird.co/docs/sql-reference/engines/engine-settings), but production settings must be checked. Partial-part TTL cleanup may rewrite surviving rows and compete with ingestion and queries.

Aggregate expiry rounds down to midnight and can precede raw expiry by less than a day. Native [TTL cleanup runs during merges](https://clickhouse.com/docs/concepts/features/operations/delete/ttl); expired rows can remain readable until cleanup. Four hours is the default merge delay setting, not a guaranteed deletion deadline. This feature implements the agreed future-only native-TTL behavior and does not establish an exact deletion SLA or change backup retention.

## Required before production promotion

1. Inspect each live regional schema and engine settings against the assumed legacy definitions, especially the old 1500-day aggregate TTL, monthly no-TTL behavior, and `ttl_only_drop_parts`. Estimate bytes/rows to copy and temporary storage needed. Apply the explicit migration once per legacy workspace before deploying the feature; regular CI does not perform the initial upgrade. Live-workspace compatibility checks run in the deployment jobs.
2. Rehearse the upgrade in Cloud dev with continuous ingestion. Compare acknowledged event counts and aggregate counts before/after promotion; include legacy aggregate-only history and mixed assigned expiries. Repeat a deployment check and later rewrite to confirm deadlines stay unchanged.
3. Run ingestion load checks at representative peak request rates and batch sizes, and check aggregate query latency and merge/storage growth. Local functional tests are not a production throughput benchmark.
4. After regional schema deployment, complete backend rollout before relying on the new setting. A legacy backend still stamps its old default TTL; rolling back to it would stop honoring saved shorter policies for subsequent events. Leave the PostgreSQL column and aggregate expiry metadata intact during rollback, and use a backend revision that still honors retention.
5. Smoke-test create/edit, raw expiry, daily/property/monthly expiry, debug caps, and shared-reader denial in each region, then monitor ingestion failures, pool waits, quarantine counts, backfill jobs, and TTL merge backlog.

GitHub's existing Tinybird EU/US and Aptabase EU/US environments were checked through the API: all four have required reviewers and deployment branch rules. No deployment approval was submitted.

The concurrent Local rehearsal could not complete: `monthly_usage_v1` promotion hit `movePartitionToTable ... only for ReplicatedMergeTree ... Got AggregatingMergeTree (NOT_IMPLEMENTED)`. Local reported a failed deployment and failed cleanup job. Its live legacy tables retained all 80 concurrently ingested events and their daily/property/monthly contributions, plus the seeded aggregate-only history. Source definitions were restored. This establishes preservation in that failed Local attempt, not successful concurrent Cloud promotion; the Cloud rehearsal remains required.

## Validation

- 222 .NET unit tests and 88 integration tests, including actual persisted event deadlines and the ownership/deletion races.
- 4 native materialization/TTL/migration tests using the checked-in schemas, SQL, and one-time patch. They verify copied aggregate-only history, old daily deadlines, unmanaged monthly counts, and independent expiry. CI runs them on ClickHouse 23.3; local verification used 23.8.4.69.
- TypeScript/Vite production build and browser flows for first-app creation, modal creation, editing, and HTTP/network failure recovery.
- Tinybird Local fresh build, static legacy migration, later rewrite preserving expiry metadata, and idempotent deployment check. Concurrent promotion has the limitation described above.
- `actionlint` workflow validation and review of success/failure/skipped deployment paths.

The original customer's already-stored data is unaffected. Selecting two years does not retroactively delete their older events. A related pre-existing lifecycle gap remains outside this PR: app deletion soft-deletes PostgreSQL metadata, and account deletion removes PostgreSQL records, without immediately purging analytics tables. Retention controls should not be presented as resolving that separate deletion behavior or the customer's retrospective instruction.
