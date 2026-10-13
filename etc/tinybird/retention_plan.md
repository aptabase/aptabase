# Aptabase retention for future events

Owners can choose 5 years, 2 years, 1 year, or 6 months when creating an app or in General Settings. The default is 5 years. Each ingestion request captures the current policy and assigns its events a fixed UTC expiry. Later setting changes affect new requests; existing and buffered events keep their assigned deadlines. Native ClickHouse and Tinybird TTL handle expiry.

## Event admission

PostgreSQL stores `apps.retention_months`, constrained to 6, 12, 24, or 60. Existing apps receive 60. App creation accepts the setting; editing can omit it without resetting the existing value. Only the owner can change it, while shared readers receive it in app metadata.

The app-status cache remains available for cheap rejection of unknown and locked keys. Current retention is read separately once per eligible ingestion request, including batches, so a saved change takes effect across replicas without waiting for the 30-minute status cache. The request gets its own immutable app snapshot rather than mutating the cached object. This adds one small indexed PostgreSQL read per request, not per individual event.

`TrackingEvent` carries that snapshot into the buffer. `EventRow` computes expiry using calendar months from the UTC event timestamp. SDK payloads and the existing raw `ttl` column stay compatible. Debug events keep the earlier of the selected period and the existing 182-day cap; errors keep their 180-day cap. Retrying a serialized batch does not recalculate its deadlines.

## Aggregate expiry

Daily event and property materializations derive `expires_at = toStartOfDay(ttl)`, include it in their grouping and sorting keys, and use `ENGINE_TTL "expires_at"`. Different policies can coexist within one app/day and expire independently. Endpoint queries continue merging the surviving aggregate states.

Rounding expiry down to UTC midnight can remove an aggregate contribution less than one day before its raw event. Grouping by the day instead of the exact event deadline preserves aggregation efficiency. A stable policy generally adds one expiry bucket per existing daily group.

Monthly usage also groups new count states by expiry day and sets `retention_managed = 1`. Its conditional TTL is `expires_at DELETE WHERE retention_managed = 1`. Legacy states use flag 0 and retain their previous no-expiry behavior. Existing app/month queries merge the surviving contributions. New monthly data grows to roughly one count state per contributing day; the minimum six-month retention preserves current-month quota accounting.

The session datasource keeps its existing, shorter 30-day TTL. Self-hosted ClickHouse's usage and session objects are ordinary views over raw events, so raw expiry controls their results automatically.

## Migration and deployment

The daily targets' sorting-key changes require a one-time rewrite. Copy their existing aggregate states instead of repopulating them from raw events, which may already have expired. Legacy daily expiry remains `period + 1,500 days`; legacy monthly counts remain unmanaged. Existing raw expiry timestamps are unchanged.

Run deployment checks and deployments through [deploy_tinybird.py](../../scripts/deploy_tinybird.py) from `etc/tinybird`:

```sh
python3 ../../scripts/deploy_tinybird.py --cloud --host "$TB_HOST" --token "$TB_TOKEN" --check
python3 ../../scripts/deploy_tinybird.py --cloud --host "$TB_HOST" --token "$TB_TOKEN"
```

CI uses this helper for dev, EU, and US. It first checks the normal definitions, which use identity `FORWARD_QUERY SELECT *`. If the pinned CLI reports missing retention columns in a legacy datasource, the helper temporarily supplies the explicit history-preserving mapping for that datasource, validates again, and deploys. It restores the source definitions on exit. Other deployment errors remain failures, and destructive operations are not enabled.

Tinybird rejects leftover custom mappings once the columns exist. Supplying them only for the initial upgrade allows later schema rewrites to preserve already assigned deadlines using the identity query. Each region's schema deploy still precedes its backend deploy and retains the existing production approval gate. Check production backfill size and available storage before promotion.

## Validation and limits

Backend tests cover all supported values, creation defaults, omitted update values, invalid settings, owner/shared-reader authorization, UTC calendar boundaries, debug caps, and changing policy while events are buffered. Browser checks cover the first-app form, new-app modal, and edit form.

Tinybird Local migration checks preserve legacy aggregate states after raw events are removed, retain legacy monthly counts, and assign separate deadlines to new one-year and two-year contributions. Deployment checks and subsequent rewrites verify that assigned deadlines are retained. Native expiry behavior was also tested in a disposable ClickHouse 23.8.4.69 instance.

TTL cleanup runs during background merges. This feature relies on that behavior and does not add a purge worker or a read-time filter. Mixed retention values can require rewriting shared parts; monitor PostgreSQL request load, aggregate row growth, and merge backlog during rollout.

This future-only setting does not shorten the customer's already stored events to 25 months. Those existing events keep their prior deadlines.
