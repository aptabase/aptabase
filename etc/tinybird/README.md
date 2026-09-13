# Tinybird project

This folder is a [Tinybird Forward](https://www.tinybird.co/docs/forward) project and the
single source of truth for the analytics schema used by the **managed cloud** regions
(EU and US). Self-hosted installs and local development use ClickHouse instead, whose
schema lives in `etc/clickhouse/` and ships inside the Docker image.

Every Tinybird endpoint in `endpoints/` must have a ClickHouse twin with the same name in
`etc/clickhouse/queries/*.liquid`. CI fails if one is missing.

## Layout

| Folder              | Contents                                                        |
| ------------------- | --------------------------------------------------------------- |
| `datasources/`      | Landing tables (`events`, `error_events`) and MV target tables  |
| `endpoints/`        | API endpoints called by the backend, named `<name>__v<N>.pipe`  |
| `materializations/` | Materialized pipes that populate the aggregation datasources    |

Rules that Forward enforces:

- A node name must differ from every pipe and datasource name. Convention: `NODE endpoint`
  for endpoints, `NODE materialize` for materialized pipes.
- Token grants are declared in the datafiles (`TOKEN "aptabase-api" APPEND` / `READ`) and are
  reconciled on deploy. Tokens that are not declared get removed.
- Deploys are full-state: a resource missing from this folder is removed from the workspace.

## Workspaces

| Workspace          | Host                                    | Deployed by                       |
| ------------------ | --------------------------------------- | --------------------------------- |
| `aptabase_dev`     | `https://api.tinybird.co`               | CI on every push to `main`        |
| `aptabase_eu_prod` | `https://api.tinybird.co`               | CI, "Aptabase EU" environment gate |
| `aptabase_us_prod` | `https://api.us-east.aws.tinybird.co`   | CI, "Aptabase US" environment gate |

## CI/CD flow

1. **Every push**: `tinybird-check` runs the ClickHouse parity check and
   `tb --cloud deploy --check` against `aptabase_dev`.
2. **Push to `main`**: `deploy-tinybird-dev` deploys to `aptabase_dev` right after tests pass.
3. **Push to `main`**, after environment approval: `deploy-us` and `deploy-eu` deploy the
   Tinybird project **before** pushing the backend image to ECR, so the schema is live before
   App Runner rolls out the new backend.

A deploy with no pending changes is a no-op and exits 0, so the steps are safe to re-run.

### Secrets

| Scope                 | Name                 | Value                                        |
| --------------------- | -------------------- | -------------------------------------------- |
| Repository            | `TINYBIRD_DEV_TOKEN` | `aptabase_dev` token with `WORKSPACE:DEPLOY` |
| Environment (EU, US)  | `TINYBIRD_HOST`      | API host from the table above                |
| Environment (EU, US)  | `TINYBIRD_TOKEN`     | Workspace token with `WORKSPACE:DEPLOY`      |

Use a dedicated static token with the `WORKSPACE:DEPLOY` scope rather than the workspace admin
token. It can create and check deployments but cannot read or manage tokens, secrets or data.
Create one per workspace while logged in to it, then copy the value:

```sh
tb --cloud token create static ci_deploy --scope WORKSPACE:DEPLOY
tb --cloud token copy ci_deploy
```

Static tokens created this way are not declared in datafiles and are **not** removed by
deployments. Only resource-scoped tokens (`TOKEN "..." READ` lines) are deployment-managed.

## Manual operations

CI never passes `--allow-destructive-operations`, so removing a datasource, materialized view
or connection fails in CI on purpose. To ship such a change:

```sh
cd etc/tinybird
tb --cloud --host <host> --token <workspace admin token> deploy --check --allow-destructive-operations
tb --cloud --host <host> --token <workspace admin token> deploy --allow-destructive-operations
```

Run it per workspace, then merge. CI will report "no changes" for the already-deployed
workspaces.

Schema changes that Forward considers incompatible (for example adding a column to `events`
in a workspace that lacks it) require a temporary `FORWARD_QUERY` in the datasource file.
Deploy with it, then remove it in a follow-up before deploying anywhere else.

## Local usage

```sh
cd etc/tinybird
tb login --host https://api.tinybird.co --workspace aptabase_dev   # writes .tinyb (gitignored)
tb --cloud deploy --check
```

Always pass `--host` and `--token` explicitly when targeting production, so the target is
visible in the command.
