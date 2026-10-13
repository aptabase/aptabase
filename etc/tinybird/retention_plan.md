# Aptabase retention for future events

Owners can choose 5 years, 2 years, 1 year, or 6 months when creating an app or in General Settings. The default is 5 years. Each ingestion request uses the cached app policy and assigns its events a fixed UTC expiry. Saved changes take effect as the existing app cache expires, within 30 minutes; existing and buffered events keep their assigned deadlines. Native ClickHouse and Tinybird TTL handle expiry.

## Event admission

PostgreSQL stores `apps.retention_months`, constrained to 6, 12, 24, or 60. Existing apps receive 60. App creation accepts the setting; editing can omit it without resetting the existing value. Only the owner can change it, while shared readers receive it in app metadata.

Retention is loaded with the existing app metadata query and stored in `CachedApplication`. The existing 30-minute success cache, five-minute negative cache, and coalesced cold lookups are unchanged. Cache hits perform no PostgreSQL query. Saving a policy does not invalidate entries: each instance picks it up when its entry expires, within 30 minutes. Requests copy the cached policy into each `TrackingEvent`, so refreshing the cache never changes an in-flight or buffered event's deadline. The edit UI explains this delay.

`TrackingEvent` carries that snapshot into the buffer. `EventRow` computes expiry using calendar months from the UTC event timestamp. Timestamps are normalized to UTC before future-date clamping. SDK payloads and the existing raw `ttl` column stay compatible; SDK-supplied expiry or retention fields cannot override the server policy. Debug events keep the earlier of the selected period and the existing 182-day cap; errors keep their independent 180-day cap. Retrying a serialized batch does not recalculate its deadlines.

## Aggregate expiry

Daily event and property materializations derive `expires_at = toStartOfDay(toTimeZone(ttl, 'UTC'))`, include it in their grouping and sorting keys, and use `ENGINE_TTL "expires_at"`. Different policies can coexist within one app/day and expire independently. Endpoint queries continue merging the surviving aggregate states.

Rounding expiry down to UTC midnight can remove an aggregate contribution less than one day before its raw event. Grouping by the day instead of the exact event deadline preserves aggregation efficiency. A stable policy generally adds one expiry bucket per existing daily group.

Monthly usage also groups new count states by expiry day and sets `retention_managed = 1`. Its conditional TTL is `expires_at DELETE WHERE retention_managed = 1`. Legacy states use flag 0 and retain their previous no-expiry behavior. Existing app/month queries merge the surviving contributions. New monthly data grows to roughly one count state per contributing day; the minimum six-month retention preserves current-month quota accounting.

The session datasource keeps its existing, shorter 30-day TTL. Self-hosted ClickHouse's usage and session objects are ordinary views over raw events, so raw expiry controls their results automatically.

## Migration and deployment

The daily targets' sorting-key changes require a one-time rewrite. Copy their existing aggregate states instead of repopulating them from raw events, which may already have expired. Legacy daily expiry remains `period + 1,500 days`; legacy monthly counts remain unmanaged. Existing raw expiry timestamps are unchanged.

Apply the [explicit one-time migration](migrations/README.md) to each legacy workspace before the feature's first backend deployment. Its patch supplies plain SQL: daily states receive their former `period + 1500 days` deadline; monthly states receive flag 0 and preserve their no-expiry behavior. Remove the patch after migration. It is not applied by CI and contains no schema detection or conditional column selectors.

Normal deployment checks and deployments use the Tinybird CLI from `etc/tinybird`:

```sh
tb --cloud --host "${TB_HOST:?Set TB_HOST}" --token "${TB_TOKEN:?Set TB_TOKEN}" deploy --check
tb --cloud --host "$TB_HOST" --token "$TB_TOKEN" deploy
```

The committed definitions use identity `FORWARD_QUERY SELECT *`, preserving existing expiry metadata on subsequent rewrites. CI validates the project against a fresh Local workspace; its live-workspace checks run in the deployment jobs. A legacy workspace must complete the one-time migration before those deployment checks pass. No automatic migration or destructive operation is enabled.

The legacy patch is used only during the initial upgrade; it must not remain applied after promotion. Production schema jobs wait for the Cloud dev deployment. Each region's schema deploy precedes its backend deploy and retains the existing production approval gate. Failed schema checks, builds, dev deployments, or regional deployments block the backend; a skipped schema job permits a backend deployment only when change detection succeeded and explicitly reported no schema changes. Check production backfill size and available storage before promotion.

On main, change detection compares the release with the last successful main push workflow, rather than just the previous push. This keeps failed or still-pending schema changes in the next release's deployment requirements. Missing or unrelated release history requires a schema deployment; a GitHub API failure blocks release. Feature branches use the normal previous-push comparison. Cancel or drain older queued deployments and complete backend rollout before relying on retention settings. A legacy replica cannot honor the new setting.

## Validation and limits

Backend tests cover all supported values, creation defaults, omitted update values, invalid settings, owner/shared-reader authorization, concurrent ownership transfer/deletion during edits, UTC calendar boundaries and offsets, debug caps, SDK override attempts, and changing policy while events are buffered. Browser checks cover the first-app form, new-app modal, and edit form, including recovery from HTTP and network failures.

Tinybird Local migration checks preserve legacy aggregate states after raw events are removed, retain legacy monthly counts, and assign separate deadlines to new one-year and two-year contributions. Deployment checks and subsequent rewrites verify that assigned deadlines are retained. CI exercises the checked-in materialization SQL and target schemas on ClickHouse 23.3, forcing TTL cleanup to verify independent expiry, merged counts/durations/property statistics, tenant isolation, and preservation of legacy monthly counts.

A Local migration with concurrent ingestion failed because Local's non-replicated engine does not support the partition move used during promotion. The live legacy tables preserved all 80 ingested events and their aggregate states, but this does not establish that a concurrent Cloud promotion succeeds. Rehearse the upgrade under ingestion in Cloud dev before approving production. See [the retention audit](retention_audit.md) for the findings and release requirements.

TTL cleanup runs during background merges. This feature relies on that behavior and does not add a purge worker or a read-time filter. Mixed retention values can require rewriting shared parts; monitor aggregate row growth and merge backlog during rollout. Ingestion keeps the existing cached metadata lookup and adds no database round-trip on cache hits.

This future-only setting does not shorten the customer's already stored events to 25 months. Those existing events keep their prior deadlines.
