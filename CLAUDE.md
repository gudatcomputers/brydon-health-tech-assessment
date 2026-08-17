# Brydon Health Tech Assessment

A simulated multi-tenant EHR ecosystem: independent `single-tenant` instances (one per provider office) each report their patient directory to one shared `patient-portal`, which is meant to become a unified login/redirect point across all of them.

## Repo layout

```
single-tenant/client   React 19 + TS + Vite SPA — login, register, welcome (protected)
single-tenant/server   ASP.NET Core 10 minimal API — JWT auth, Postgres, reports users to patient-portal
patient-portal/client  React 19 + TS + Vite SPA — currently a placeholder ("Coming soon")
patient-portal/server  ASP.NET Core 10 minimal API — tenant directory (subdomains + hashed usernames)

docker-compose.yml               single-tenant + patient-portal Postgres, one instance of each, for normal local dev
docker-compose.multi-tenant.yml  fully containerized: 5 single-tenant pairs + patient-portal, simulating 5 provider offices
db-init/multi-tenant-init.sql    creates the 6 databases the multi-tenant compose stack needs
.env.example                     documents every env var used across both systems
```

Each subsystem has its own `CLAUDE.md` with directory-specific detail — this file is cross-cutting architecture and conventions only.

## How the pieces fit together

- **Subdomain-per-deployment.** Each `single-tenant` instance is meant to run on its own subdomain of a shared domain (client and API reverse-proxied to the same origin in production). `SUBDOMAIN` + `APP_DOMAIN` env vars (see `Hosting/DeploymentOrigin.cs`) derive that origin, which is used as the JWT issuer/audience (so a token from one tenant can't validate against another) and folded into the CORS allow-list. Both are optional and fall back to `http://localhost:5251` for local dev.
- **Reporting to patient-portal.** Each `single-tenant` instance hashes its usernames (deterministic HMAC-SHA256, keyed by `TENANT_REPORT_SECRET` — see `Sync/UsernameHasher.cs`, *not* the same hashing as password storage) and `POST`s them to patient-portal's `/api/tenants/report`, authenticated via an `X-Tenant-Report-Key` header compared against the same secret. This happens twice: once at startup (all currently-unreported users) and once per new registration (fire-and-forget, so the HTTP call never blocks the caller) — see `Sync/TenantUserSyncTrigger.cs`, shared by both paths.
- **patient-portal's directory is additive, not a mirror.** `/api/tenants/report` never deletes rows — it inserts `(subdomain, hash)` pairs that don't already exist. A subdomain is auto-created on first report. Normalized: `subdomains` (id, name) + `tenant_usernames` (subdomain_id FK, hash) rather than repeating the subdomain text on every row.
- **patient-portal itself has no login flow yet.** The "unified login and redirect" piece described in the original ask hasn't been built — right now patient-portal is just the intake endpoint plus the normalized directory. `patient-portal/client` had a full login/register/welcome flow at one point (copied from `single-tenant/client`) but it was deliberately stripped back out since patient-portal wasn't ready to use it — see that subsystem's `CLAUDE.md`.

## Conventions established across this codebase

- **12-factor config via value objects.** Every piece of environment-specific config (DB credentials, deployment origin, shared secrets) is a `sealed record` with a *private* constructor and a static `FromConfiguration(IConfiguration)` factory that throws `InvalidOperationException` on a missing required var. See `DatabaseCredentials`, `DeploymentOrigin`, `TenantReportSecret`, `PatientPortalReportingOptions` for the pattern. Follow it for any new config.
- **Insert-if-not-exists, not raw SQL.** Preferred pattern: query for existence first, then insert, wrapped in `try/catch` for `DbUpdateException` where `ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation }` — treat the race as a success/no-op rather than propagating a 500. Used for tenant-username inserts, subdomain auto-creation, and username-registration races. Raw SQL (`ON CONFLICT DO NOTHING`) was tried and deliberately reverted in favor of this — it's less atomic under true concurrency but was explicitly preferred to keep the codebase free of raw SQL, and proven safe under concurrent load (10 simultaneous requests, one winner, rest handled cleanly, verified live).
- **EF Core migrations are never trusted blindly.** When a schema change could lose data, EF's auto-scaffolded migration is a starting point, not the final answer — inspect it and hand-edit for backfill logic. We hit this for real with the `NormalizeSubdomains` migration (EF wanted to drop the `Subdomain` text column before the replacement `subdomains` table + FK even existed, defaulting every row to a non-existent id). Always re-run against a database that actually has data in it, not just an empty one.
- **`options.MapInboundClaims = false` is required** in the JWT bearer config (`JwtBearerConfiguration.cs`). Without it, ASP.NET Core silently remaps well-known claim types on the way in (`sub` → `ClaimTypes.NameIdentifier`), so reading raw JWT claim names by their registered names returns null. Cost real debugging time once — don't remove it.
- **Nothing blocks on an external HTTP call in the startup or request path.** Calls from `single-tenant` to `patient-portal` go through a `BackgroundService` at startup (`TenantUserSyncHostedService`) or a fire-and-forget trigger with its own DI scope on the request path (`TenantUserSyncTrigger.RunAsync()`, deliberately not awaited by the caller). Never `await` `PatientPortalReportingService` directly inline in `Program.cs` startup or in an endpoint handler.
- **Resilience via Polly.** `single-tenant`'s HTTP calls to patient-portal are wrapped in retry (3 attempts, exponential backoff) then a circuit breaker (opens after 5 consecutive failures) — see `Sync/PatientPortalRetryPolicy.cs`. Retry wraps the breaker (Microsoft's documented ordering), so retries still respect an open circuit instead of hammering a known-down service.
- **Logout is version-based, not a revocation table.** `User.TokenVersion` is embedded in every issued JWT as the `ver` claim; logout increments it (atomic `ExecuteUpdateAsync`), and token validation compares the claim against the current DB value. This was a deliberate replacement for an earlier `revoked_tokens` table design that didn't scale with user count.
- **Vite bakes `VITE_API_BASE_URL` at build time, not runtime.** Matters for Docker: each client image that needs a different backend URl needs its own build with a different `--build-arg`, not a runtime env var substitution.

## Known environment quirks

- **npm/rolldown native-binding bug on this machine.** `npm install` in either client can fail with `Cannot find native binding` (a known npm optional-dependency bug, not specific to this repo). Fix: `npm install @rolldown/binding-darwin-x64@1.2.4 --no-save`. Doesn't happen inside Linux Docker builds — only hits local macOS `npm install`.
- **Ports, local dev (non-Docker, `dotnet run` / `npm run dev`):** single-tenant server `5251`, single-tenant client `5173`, patient-portal server `5009`, patient-portal client `5174` (per `.claude/launch.json`). single-tenant Postgres `5433`, patient-portal Postgres `5434` (both offset from `5432` to avoid clashing with a locally-installed native Postgres).
- **Ports, multi-tenant simulation (`docker-compose.multi-tenant.yml`):** entirely separate range — tenant servers `5301`-`5305`, tenant clients `5401`-`5405`, shared Postgres `5540`, patient-portal server `5590`. Designed to run alongside `docker-compose.yml` without collision.
- **`.claude/launch.json`** has preview configs named `single-tenant-client` and `patient-portal-client` for the Browser-pane preview tool.

## Verification habits worth keeping

Real bugs in this repo were only caught by actually running things, not by reading the code: the JWT claim-remapping issue, a Docker port-rebind race after renaming a compose service, and the migration data-loss issue above. When touching auth, migrations, or the reporting flow, prefer testing against the real running containers (or `dotnet run` + `curl`/a real browser) over trusting a build success alone.
