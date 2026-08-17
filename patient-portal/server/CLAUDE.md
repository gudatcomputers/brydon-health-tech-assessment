# patient-portal/server

ASP.NET Core 10 minimal API. Namespace/project `PatientPortalServer`. EF Core + Npgsql against its own Postgres (separate from `single-tenant`'s — these are meant to be independently deployable systems).

See the root `CLAUDE.md` for how this fits into the wider system. This file is specific to what's in this directory.

**Current scope: intake + directory only.** The "unified login and redirect" half of patient-portal's purpose (letting a patient log in once and get routed to the right `single-tenant` subdomain) hasn't been built yet — there's no auth/JWT here at all. This service currently just receives and stores what `single-tenant` instances report.

## Folders

- `Tenants/` — `Subdomain` + `SubdomainStore` (get-or-create by name), `TenantUsername` (the directory row), `TenantReportEndpoints` (the intake endpoint), `TenantReportSecret` (value object for the shared secret).
- `Data/` — `PatientPortalDbContext`, `DatabaseCredentials` (same value-object pattern as `single-tenant`, different env var prefix).

## Data model

Normalized, not denormalized: `subdomains` (`Id`, `Name` unique) + `tenant_usernames` (`SubdomainId` FK, `UsernameHash`, composite PK) — a hash doesn't repeat the subdomain text on every row, and the same hash can legitimately appear under multiple subdomains (a patient who's a patient at more than one office). This was a deliberate normalization pass (see the `NormalizeSubdomains` migration and the root `CLAUDE.md`'s note on migrations) — if you're tempted to re-denormalize for a query, don't; extend `SubdomainStore`/join instead.

## Endpoint

`POST /api/tenants/report` — body `{ subdomain, usernameHashes }`, header `X-Tenant-Report-Key` compared (constant-time) against `TENANT_REPORT_SECRET`. Behavior:
- **Additive only.** Inserts `(subdomain, hash)` pairs that don't already exist; never deletes. A "full refresh" resend of an already-known set is a cheap no-op (one bounded `SELECT`, zero inserts) — don't reintroduce a full-replace/delete-then-insert approach here, that was tried and explicitly reverted.
- Auto-creates the `subdomains` row on first report for a new name, via `SubdomainStore.GetOrCreateIdAsync` — same insert-if-not-exists-with-unique-violation-catch pattern as the hash inserts (see root `CLAUDE.md`).
- The existence check is bounded to the incoming batch (`WHERE UsernameHash = ANY(@incoming)`), not the tenant's whole existing set — don't change this back to loading everything into memory just to diff it; that was a deliberate scaling fix.
- Per-hash inserts are saved individually (not one batched `SaveChangesAsync`) so a unique-violation race on one hash can't roll back other legitimately-new hashes in the same request.

`GET /health` — anonymous, used as the Docker healthcheck target (see the multi-tenant compose file's `depends_on: condition: service_healthy` ordering).

## Env vars

Required: `PATIENT_PORTAL_DB_HOST`, `PATIENT_PORTAL_DB_PORT`, `PATIENT_PORTAL_DB_NAME`, `PATIENT_PORTAL_DB_USER`, `PATIENT_PORTAL_DB_PASSWORD`, `TENANT_REPORT_SECRET`.

## Running locally

`dotnet run` (port `5009` per `launchSettings.json`). Needs its Postgres up first (`docker compose up -d` from the repo root brings up both this and single-tenant's). Migrations run automatically on startup — no seeding here (nothing to seed; all data arrives via the report endpoint).
