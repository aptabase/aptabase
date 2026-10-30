# One-time app retention migration

Apply [app_retention.patch](app_retention.patch) once to each legacy workspace: dev, then EU and US. Complete this schema upgrade before deploying the retention feature's backend. Ordinary CI uses the committed `SELECT *` forward queries and never applies this patch.

This is a terminal deployment using the files in this PR. Do not run the `SELECT` statements in Tinybird's SQL console: they are copy instructions used by the deployment, not commands that add columns.

## Exact terminal steps

Start at the repository root, on this PR branch, with Tinybird CLI 4.6.18 installed. Do this for dev first, then EU, then US. Each workspace has its own token; dev and EU use `https://api.tinybird.co`, and US uses `https://api.us-east.aws.tinybird.co`.

Set `TB_HOST` and `TB_TOKEN` for the workspace, then check the ordinary deployment:

```sh
cd etc/tinybird
tb --cloud --host "${TB_HOST:?Set TB_HOST}" --token "${TB_TOKEN:?Set TB_TOKEN}" deploy --check
cd ../..
```

If it reports **No changes**, the workspace already matches the PR: skip its migration. If it reports **FORWARD_QUERY columns don't match datasource schema**, it still needs the initial upgrade. For any other error, inspect that error before proceeding.

For a legacy workspace, apply the temporary patch and inspect the migration plan:

```sh
git apply --check etc/tinybird/migrations/app_retention.patch
git apply etc/tinybird/migrations/app_retention.patch
cd etc/tinybird
tb --cloud --host "$TB_HOST" --token "$TB_TOKEN" deploy --check
```

The plan must copy `events_daily_v3_mv`, `event_props_daily_v2_mv`, and `monthly_usage_v1` **From live deployment using Forward Query**. Then deploy and wait for it to finish:

```sh
tb --cloud --host "$TB_HOST" --token "$TB_TOKEN" deploy
```

Remove the temporary patch and verify the normal files now match the server:

```sh
cd ../..
git apply --reverse etc/tinybird/migrations/app_retention.patch
cd etc/tinybird
tb --cloud --host "$TB_HOST" --token "$TB_TOKEN" deploy --check
cd ../..
```

The last check must report **No changes**. Repeat with the next workspace's host/token. Do not commit the temporary patch. Remove it after a failed deployment too, and inspect the failed deployment before retrying.

Once all three workspaces match the ordinary definitions, CI can deploy the same files without repeating the legacy copy. If the upgrade is skipped, normal CI checks fail on the legacy forward-query column mismatch and block the affected backend deployment. That failure was reproduced in Tinybird Local with the CLI version used by CI.

The patch adds these explicit forward queries to the three aggregate datasources for the initial upgrade:

```sql
-- events_daily_v3_mv and event_props_daily_v2_mv
SELECT *, period + toIntervalDay(1500) AS expires_at

-- monthly_usage_v1
SELECT *, toDateTime(0) AS expires_at, toUInt8(0) AS retention_managed
```

The daily query preserves the former 1500-day TTL. The monthly query preserves legacy counts without expiry: flag 0 excludes them from the new TTL rule. All existing aggregate states are copied directly, including history whose raw events have already expired. The new materializations assign expiry from raw `ttl` to subsequently ingested events; the existing backend already supplies that column.

## Apply to a legacy workspace

Use a checkout of this PR with Tinybird CLI 4.6.18. Before applying, confirm that both daily targets still use `period + toIntervalDay(1500)`, monthly usage has no TTL, and all three lack `expires_at` (monthly also lacks `retention_managed`). This patch is for that legacy schema only. Do not reapply it to upgraded workspaces: its fixed defaults could reset assigned deadlines during a later rewrite. An upgraded workspace uses the ordinary committed definitions.

Inspect the live datasource schemas in Tinybird before applying the patch. This is a required operator check: `deploy --check` can report no changes on an upgraded workspace even with the temporary forward queries applied. It does not prove that replaying this patch is safe. If the schemas are partially upgraded, stop and inspect the previous deployment rather than applying legacy defaults to all three targets.

Set `TB_HOST` and `TB_TOKEN` to the specific workspace. From the repository root:

```sh
git apply --check etc/tinybird/migrations/app_retention.patch
git apply etc/tinybird/migrations/app_retention.patch
```

From `etc/tinybird`, inspect the deployment plan and then deploy:

```sh
tb --cloud --host "${TB_HOST:?Set TB_HOST}" --token "${TB_TOKEN:?Set TB_TOKEN}" deploy --check
tb --cloud --host "$TB_HOST" --token "$TB_TOKEN" deploy
```

The plan must copy all three targets **from the live deployment using Forward Query**. Do not proceed with a plan that repopulates them from raw events. The deployment must complete and be promoted before moving to the next workspace. Do not enable destructive operations.

After success or failure, remove the temporary patch from the repository root:

```sh
git apply --reverse etc/tinybird/migrations/app_retention.patch
```

Do not commit the patched datasource definitions. If the deployment failed, inspect its state before retrying. If it succeeded, verify ordinary deployment checks from `etc/tinybird`:

```sh
tb --cloud --host "$TB_HOST" --token "$TB_TOKEN" deploy --check
```

Compare counts and legacy deadlines before/after migration, including aggregate-only history. Rehearse continuous ingestion in Cloud dev before approving production, and check backfill/storage capacity as described in [the audit](../retention_audit.md). CI's live-workspace checks run at deployment time and block deployment until each workspace has completed this upgrade.
