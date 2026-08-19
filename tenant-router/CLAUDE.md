# tenant-router

ASP.NET Core 10 minimal API. Namespace/project `TenantRouter`. EF Core + Npgsql against its own Postgres (separate from both `single-tenant`'s and `patient-portal`'s — independently deployable).

See the root `CLAUDE.md` for how this fits into the wider system. This file is specific to what's in this directory.

**Owns the tenant directory, nothing else.** This service exists to answer one question — "which tenant(s) does this username belong to?" — for whichever caller needs it. It has no login flow, no password checking, no JWT, no client. `patient-portal/server` calls it over HTTP for every login attempt; it holds no HTTP client of its own and never calls out to anyone.

## Folders

- `Tenants/` — `Subdomain` + `SubdomainStore` (get-or-create/update by name), `TenantUsername` (the directory row), `TenantReportEndpoints` (the intake endpoint), `TenantLookupEndpoints` (the query endpoint), `TenantReportSecret` (value object for the shared secret — also used as the HMAC key for both endpoints), `UsernameHasher` (must stay byte-identical to `single-tenant`'s copy — see that project's `CLAUDE.md`).
- `Data/` — `TenantRouterDbContext`, `DatabaseCredentials` (same value-object pattern as the other two services, `TENANT_ROUTER_DB_*` prefix), `TenantRouterDbContextFactory` (design-time only, falls back to `localhost:5435`).

This directory used to be `patient-portal/server`'s `Tenants/` + `Data/` folders, moved here verbatim (see git history) when patient-portal was split into a stateless orchestrator over this service. `TenantReportEndpoints.IsAuthorized` is `internal` (not `private`) specifically so `TenantLookupEndpoints` can reuse the same constant-time secret check without duplicating it.

## Data model

Normalized, not denormalized: `subdomains` (`Id`, `Name` unique, `ServerUrl` nullable, `ClientOrigin` nullable) + `tenant_usernames` (`SubdomainId` FK, `UsernameHash`, composite PK) — a hash doesn't repeat the subdomain text on every row, and the same hash can legitimately appear under multiple subdomains (a patient who's a patient at more than one office). If you're tempted to re-denormalize for a query, don't; extend `SubdomainStore`/join instead.

`Subdomain.ServerUrl` and `Subdomain.ClientOrigin` are two distinct addresses for the same tenant, both reported by `single-tenant` alongside its username hashes, both nullable because rows created before they existed won't have them:
- `ServerUrl` (`DeploymentOrigin.SelfUrl`) is where *another backend* reaches this tenant's **server** — patient-portal uses it to proxy the credential check.
- `ClientOrigin` (`DeploymentOrigin.ClientOrigin`) is where a **browser** reaches this tenant's **client** — patient-portal uses it to redirect the user's browser there after a successful login, since patient-portal never hosts a session itself.

Neither is the tenant's public `BaseUrl` (the JWT-issuer identity) — all three diverge in Docker and local dev, where server, client, and public origin can be three different addresses. tenant-router itself never calls either; it just relays them to whichever caller asked.

`tenant_usernames.updated_on` (`timestamp without time zone` — column name comes from the `UseSnakeCaseNamingConvention()` convention configured in `Program.cs`/`TenantRouterDbContextFactory`, not a per-property `.HasColumnName()`, see root `CLAUDE.md`) is stamped with `DateTime.UtcNow` on every insert *and* update by `TenantRouterDbContext.SaveChanges`/`SaveChangesAsync` — not set by hand anywhere, don't set `TenantUsername.UpdatedOn` directly. In practice this only ever fires on insert today, since `/api/tenants/report` never updates an existing row (see above); the update-path handling exists for whenever that changes, not dead code to remove. Two things worth knowing if you touch this: Npgsql rejects a `DateTimeKind.Utc` value against `timestamp without time zone` outright, so the stamped value is `DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified)` — same instant, Kind tag stripped, not a genuine "unspecified" timezone. And the column also carries a DB-level `DEFAULT timezone('utc', now())`, which is what backfilled all existing rows when this column was added via migration — the app-layer stamp is what actually keeps it current for real writes, the default is just a safety net for anything written outside EF Core.

## Endpoints

Both are internal/service-to-service only — neither is reachable from a browser, so neither needs patient-portal's public-facing anti-enumeration caution (uniform `400`, etc.). Both authenticate the same way: header `X-Tenant-Report-Key` compared constant-time against `TENANT_REPORT_SECRET`, via the shared `TenantReportEndpoints.IsAuthorized`.

`POST /api/tenants/report` — body `{ subdomain, serverUrl, clientOrigin, usernameHashes }`. Behavior:
- **Additive only.** Inserts `(subdomain, hash)` pairs that don't already exist; never deletes. A "full refresh" resend of an already-known set is a cheap no-op (one bounded `SELECT`, zero inserts) — don't reintroduce a full-replace/delete-then-insert approach here, that was tried and explicitly reverted.
- Auto-creates the `subdomains` row on first report for a new name (and keeps `ServerUrl`/`ClientOrigin` current on every report) via `SubdomainStore.GetOrCreateIdAsync` — same insert-if-not-exists-with-unique-violation-catch pattern as the hash inserts (see root `CLAUDE.md`).
- The existence check is bounded to the incoming batch (`WHERE UsernameHash = ANY(@incoming)`), not the tenant's whole existing set — don't change this back to loading everything into memory just to diff it.
- Per-hash inserts are saved individually (not one batched `SaveChangesAsync`) so a unique-violation race on one hash can't roll back other legitimately-new hashes in the same request.

`POST /api/tenants/lookup` — body `{ username }` → `{ matches: [{ subdomain, serverUrl, clientOrigin }] }`. Hashes the username and returns every match, filtering out any subdomain missing *either* `ServerUrl` or `ClientOrigin` — a match patient-portal can't proxy the credential check to, or can't redirect the browser to afterward, isn't a usable match. Unlike `patient-portal`'s public login endpoint, this deliberately does **not** hide whether a username exists — an empty `matches` array for an unknown username is a perfectly fine, honest response, since the only caller is another backend already holding the shared secret, not an anonymous browser. Don't add username-enumeration protections here; that concern belongs entirely to whichever public-facing service calls this one.

`GET /health` — anonymous, used as the Docker healthcheck target (see the multi-tenant compose file's `depends_on: condition: service_healthy` ordering — both `patient-portal-server` and every `tenantN-server` wait on this service being healthy before starting).

## Env vars

Required: `TENANT_ROUTER_DB_HOST`, `TENANT_ROUTER_DB_PORT`, `TENANT_ROUTER_DB_NAME`, `TENANT_ROUTER_DB_USER`, `TENANT_ROUTER_DB_PASSWORD`, `TENANT_REPORT_SECRET`.

No `CLIENT_ORIGIN` / CORS — this service has no browser client and never will.

## Running locally

`dotnet run` (port `5236` per `launchSettings.json`). Needs its Postgres up first: `docker compose up -d` from the repo root brings up `tenant_router_postgres` (port `5435`) alongside single-tenant's. Migrations run automatically on startup — no seeding here (nothing to seed; all data arrives via `/api/tenants/report`).

`dotnet ef migrations add <Name> -o Data/Migrations` from this directory for schema changes — see the root `CLAUDE.md`'s note on never trusting the auto-scaffold blindly when data could be lost.

If testing against local (non-Docker) `single-tenant`/`patient-portal` instances, see `single-tenant/server/CLAUDE.md`'s note on `--no-launch-profile`.
