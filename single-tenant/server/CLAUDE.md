# single-tenant/server

ASP.NET Core 10 minimal API. Example EHR backend for one provider office. Namespace `BrydonServer`, project `BrydonServer.csproj`. EF Core + Npgsql against Postgres.

See the root `CLAUDE.md` for how this fits into the wider system (subdomain-per-deployment, reporting to patient-portal, shared conventions). This file is specific to what's in this directory.

## Folders

- `Auth/` — `User` entity, `UserStore`, `PasswordHasher` (PBKDF2, random salt per user — verification only, not for hashing usernames), `TokenService` (issues JWTs), `JwtBearerConfiguration` (validation setup, including the token-version revocation check), `JwtOptions`, `AuthEndpoints` (login/register/logout).
- `Data/` — `AppDbContext`, `DatabaseCredentials` (value object, see root conventions), `DbSeeder`, `Migrations/`.
- `Hosting/` — `DeploymentOrigin` (subdomain-derived identity, see root `CLAUDE.md`).
- `Sync/` — everything related to reporting to patient-portal: `PatientPortalReportingOptions`, `PatientPortalReportingService`, `PatientPortalRetryPolicy` (Polly), `TenantUserSyncTrigger` + `TenantUserSyncHostedService`, `UsernameHasher` (deterministic HMAC-SHA256, distinct from `PasswordHasher`).

## Auth model

JWTs carry `sub` (user id), `unique_name`, `jti`, and `ver` (the user's `TokenVersion` at issuance). Logout atomically increments `User.TokenVersion`; `OnTokenValidated` in `JwtBearerConfiguration` rejects any token whose `ver` claim doesn't match the current DB value. This means logout invalidates *every* outstanding token for that user at once (all devices/sessions), by design — there's no separate revoked-token table.

`options.MapInboundClaims = false` is set in `JwtBearerConfiguration.Configure`. Don't remove it — without it, `sub` gets silently remapped to `ClaimTypes.NameIdentifier` on the way in and `FindFirstValue(JwtRegisteredClaimNames.Sub)` returns null.

## Endpoints

- `POST /api/auth/login` — anonymous. `{ username, password }` → `{ token, expiresAt }`.
- `POST /api/auth/register` — anonymous. `{ username, password }` → `{ token, expiresAt }` (auto-login on success). Password must be ≥8 chars. Duplicate username returns `400` (deliberately not `409`, to avoid leaking whether a username exists) from the pre-check; the rare concurrent-registration race that slips past the pre-check and hits the DB's unique constraint returns `409` from the catch block — that asymmetry is intentional-ish, not a bug, but worth knowing about if it ever needs to be made consistent. Fires `TenantUserSyncTrigger.RunAsync()` (unawaited) after creating the user.
- `POST /api/auth/logout` — requires auth. Increments `TokenVersion`.
- `GET /health` — anonymous.

## Reporting to patient-portal

Two triggers, same underlying `PatientPortalReportingService.SynchronizeTenantUsersAsync()` (queries all users where `ReportedToPatientPortal = false`, hashes usernames, POSTs, marks reported on success — never throws, logs and leaves users unreported to retry later on any failure):
1. **Startup** — `TenantUserSyncHostedService : BackgroundService`, runs once, doesn't block the app from accepting requests (`BackgroundService.StartAsync` returns before `ExecuteAsync` finishes).
2. **Per-registration** — `AuthEndpoints` calls `TenantUserSyncTrigger.RunAsync()` without awaiting it, right after a new user is created.

Both go through `TenantUserSyncTrigger`, which opens its own DI scope (so it can outlive a disposed request scope) and catches everything. Don't inline `PatientPortalReportingService` calls anywhere else — always go through the trigger.

HTTP calls to patient-portal are wrapped in Polly (`PatientPortalRetryPolicy`): 3 retries with exponential backoff, then a circuit breaker (opens after 5 consecutive failures, 30s cooldown). `PatientPortalReportingService` catches both `HttpRequestException` and `Polly.CircuitBreaker.BrokenCircuitException`.

## Env vars

Required: `DB_HOST`, `DB_PORT`, `DB_NAME`, `DB_USER`, `DB_PASSWORD`, `PATIENT_PORTAL_URL`, `TENANT_REPORT_SECRET`.
Optional: `SUBDOMAIN` + `APP_DOMAIN` (both or neither), `CLIENT_ORIGIN` (CORS, default `http://localhost:5173`), `DEMO_USERNAME`/`DEMO_PASSWORD` (seed override), `SEED_RANDOM_USER_COUNT` (seeds N random `patient-<hex>` users instead of the one demo user — used by the multi-tenant simulation).

## Running locally

`dotnet run` (port `5251` per `launchSettings.json`). Needs its Postgres up first: `docker compose up -d` from the repo root (or the multi-tenant compose file for the 5-instance simulation). Migrations and seeding run automatically on startup, before the background sync fires.

`dotnet ef migrations add <Name> -o Data/Migrations` from this directory for schema changes — see the root `CLAUDE.md`'s note on never trusting the auto-scaffold blindly when data could be lost.
