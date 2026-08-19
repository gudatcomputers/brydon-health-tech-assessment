# single-tenant/server

ASP.NET Core 10 minimal API. Example EHR backend for one provider office. Namespace `BrydonServer`, project `BrydonServer.csproj`. EF Core + Npgsql against Postgres.

See the root `CLAUDE.md` for how this fits into the wider system (subdomain-per-deployment, reporting to tenant-router, shared conventions). This file is specific to what's in this directory.

## Folders

- `Auth/` — `User` entity, `UserStore`, `PasswordHasher` (PBKDF2, random salt per user — verification only, not for hashing usernames), `TokenService` (issues JWTs), `JwtBearerConfiguration` (validation setup, including the token-version revocation check), `JwtOptions`, `AuthEndpoints` (login/register/logout).
- `Data/` — `AppDbContext`, `DatabaseCredentials` (value object, see root conventions), `DbSeeder`, `Migrations/`.
- `Hosting/` — `DeploymentOrigin` (subdomain-derived identity, see root `CLAUDE.md`). Exposes `BaseUrl` (public identity — JWT issuer/audience, CORS), `SelfUrl` (how *other backends* reach this one directly; defaults to `BaseUrl`, override with `SELF_URL` when they diverge, e.g. a Docker container hostname), and `ClientOrigin` (how a *browser* reaches this tenant's client; reads `CLIENT_ORIGIN`, the same var the CORS allow-list already uses — one env var, two jobs, since "where the client lives" is exactly what both CORS and a redirect target need). `SelfUrl` is reported to tenant-router as `ServerUrl` (for patient-portal's login-proxy call); `ClientOrigin` is reported as-is (for patient-portal's post-login redirect) — don't use `BaseUrl` for either, it's the wrong audience for both.
- `Sync/` — everything related to reporting to tenant-router: `TenantRouterReportingOptions`, `TenantRouterReportingService`, `TenantRouterRetryPolicy` (Polly), `TenantUserSyncTrigger` + `TenantUserSyncHostedService`, `UsernameHasher` (deterministic HMAC-SHA256, distinct from `PasswordHasher`).

## Auth model

JWTs carry `sub` (user id), `unique_name`, `jti`, and `ver` (the user's `TokenVersion` at issuance). Logout atomically increments `User.TokenVersion`; `OnTokenValidated` in `JwtBearerConfiguration` rejects any token whose `ver` claim doesn't match the current DB value. This means logout invalidates *every* outstanding token for that user at once (all devices/sessions), by design — there's no separate revoked-token table.

`options.MapInboundClaims = false` is set in `JwtBearerConfiguration.Configure`. Don't remove it — without it, `sub` gets silently remapped to `ClaimTypes.NameIdentifier` on the way in and `FindFirstValue(JwtRegisteredClaimNames.Sub)` returns null.

## Endpoints

- `POST /api/auth/login` — anonymous. `{ username, password }` → `{ token, expiresAt }`.
- `POST /api/auth/register` — anonymous. `{ username, password }` → `{ token, expiresAt }` (auto-login on success). Password must be ≥8 chars. Duplicate username returns `400` (deliberately not `409`, to avoid leaking whether a username exists) from the pre-check; the rare concurrent-registration race that slips past the pre-check and hits the DB's unique constraint returns `409` from the catch block — that asymmetry is intentional-ish, not a bug, but worth knowing about if it ever needs to be made consistent. Fires `TenantUserSyncTrigger.RunAsync()` (unawaited) after creating the user.
- `POST /api/auth/logout` — requires auth. Increments `TokenVersion`.
- `GET /health` — anonymous.

## Reporting to tenant-router

Two triggers, same underlying `TenantRouterReportingService.SynchronizeTenantUsersAsync()` (queries all users where `ReportedToTenantRouter = false`, hashes usernames, POSTs, marks reported on success — never throws, logs and leaves users unreported to retry later on any failure):
1. **Startup** — `TenantUserSyncHostedService : BackgroundService`, runs once, doesn't block the app from accepting requests (`BackgroundService.StartAsync` returns before `ExecuteAsync` finishes).
2. **Per-registration** — `AuthEndpoints` calls `TenantUserSyncTrigger.RunAsync()` without awaiting it, right after a new user is created.

Both go through `TenantUserSyncTrigger`, which opens its own DI scope (so it can outlive a disposed request scope) and catches everything. Don't inline `TenantRouterReportingService` calls anywhere else — always go through the trigger.

HTTP calls to tenant-router are wrapped in Polly (`TenantRouterRetryPolicy`): 3 retries with exponential backoff, then a circuit breaker (opens after 5 consecutive failures, 30s cooldown). `TenantRouterReportingService` catches both `HttpRequestException` and `Polly.CircuitBreaker.BrokenCircuitException`.

Note: `patient-portal` used to be both the report *recipient* and the login-proxy orchestrator in one service — it isn't anymore. This service still reports to whatever `TENANT_ROUTER_URL` points at (now `tenant-router`, not `patient-portal`); patient-portal calls tenant-router separately to resolve logins. See the root `CLAUDE.md` and `tenant-router/CLAUDE.md`.

## Env vars

Required: `DB_HOST`, `DB_PORT`, `DB_NAME`, `DB_USER`, `DB_PASSWORD`, `TENANT_ROUTER_URL`, `TENANT_REPORT_SECRET`.
Optional: `SUBDOMAIN` + `APP_DOMAIN` (both or neither), `SELF_URL` (see `DeploymentOrigin` above), `CLIENT_ORIGIN` (CORS, default `http://localhost:5173`), `DEMO_USERNAME`/`DEMO_PASSWORD` (seed override), `SEED_RANDOM_USER_COUNT` (seeds N random `patient-<hex>` users instead of the one demo user — used by the multi-tenant simulation).

## Testing against multiple local instances

If you ever need two `dotnet run` instances of this project pointed at different databases (e.g. testing patient-portal's login proxy across tenants), pass `--no-launch-profile` — otherwise `launchSettings.json`'s hardcoded `DB_NAME` (and other listed vars) silently overrides whatever you exported in the shell, and both instances end up writing to the same database. Cost real debugging time to track down once.

## Running locally

`dotnet run` (port `5251` per `launchSettings.json`). Needs its Postgres up first: `docker compose up -d` from the repo root (or the multi-tenant compose file for the 5-instance simulation). Migrations and seeding run automatically on startup, before the background sync fires.

`dotnet ef migrations add <Name> -o Data/Migrations` from this directory for schema changes — see the root `CLAUDE.md`'s note on never trusting the auto-scaffold blindly when data could be lost.
