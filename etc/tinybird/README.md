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

| Workspace          | Host                                    | Deployed by                        |
| ------------------ | --------------------------------------- | ---------------------------------- |
| `aptabase_dev`     | `https://api.tinybird.co`               | CI on every push to `main`         |
| `aptabase_eu_prod` | `https://api.tinybird.co`               | CI, "Tinybird EU" environment gate |
| `aptabase_us_prod` | `https://api.us-east.aws.tinybird.co`   | CI, "Tinybird US" environment gate |

## CI/CD flow

1. **Every push**: `tinybird-check` runs the ClickHouse parity check, a token-free
   `tb --local build` against a Tinybird Local container, and `tb --cloud deploy --check`
   against `aptabase_dev` (the last one only when the dev token is available).
2. **Push to `main`**: `deploy-tinybird-dev` deploys to `aptabase_dev` right after tests pass.
3. **Push to `main`**, behind their own approvals: `deploy-tinybird-us` and `deploy-tinybird-eu`
   deploy the project to production. Together with the existing backend gates that makes up to
   four approvals per release: Tinybird US, Tinybird EU, Aptabase US, Aptabase EU.
4. The backend deploy of each region (`deploy-us`, `deploy-eu`) **depends on** its Tinybird
   deploy, so the schema is always live before App Runner rolls out the new backend image.

The Tinybird deploy jobs only run when the push changed `etc/tinybird` (`tinybird-check` compares
the push's before and after commits). A backend-only release therefore shows just the two
backend approvals. A failed or rejected Tinybird deploy still blocks the matching backend deploy.
If the base commit is unknown (new branch, force push) the jobs run and act as a no-op.

A deploy with no pending changes is a no-op and exits 0, so the steps are safe to re-run.

### Environments and secrets

The "Tinybird US" and "Tinybird EU" environments must exist with required reviewers **before**
the workflow first runs on `main`. GitHub auto-creates a missing environment with no protection
rules, which would skip the approval.

| Scope                        | Kind     | Name                 | Value                                            |
| ---------------------------- | -------- | -------------------- | ------------------------------------------------ |
| Repository                   | secret   | `TINYBIRD_DEV_TOKEN` | `aptabase_dev` token with `WORKSPACE:DEPLOY`     |
| Environment "Tinybird EU"    | variable | `TINYBIRD_HOST`      | `https://api.tinybird.co`                        |
| Environment "Tinybird EU"    | secret   | `TINYBIRD_TOKEN`     | `aptabase_eu_prod` token with `WORKSPACE:DEPLOY` |
| Environment "Tinybird US"    | variable | `TINYBIRD_HOST`      | `https://api.us-east.aws.tinybird.co`            |
| Environment "Tinybird US"    | secret   | `TINYBIRD_TOKEN`     | `aptabase_us_prod` token with `WORKSPACE:DEPLOY` |

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
visible in the command. Be aware that the CLI can persist the last token it authenticated
with into the folder's `.tinyb`, replacing the stored login. Run `--token` commands from a
scratch folder if you want to keep a folder's login intact.
