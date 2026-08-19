# marketing-website/server

ASP.NET Core 10 minimal API. Namespace/project `MarketingWebsiteServer`.

See the root `CLAUDE.md` for how this fits into the wider system — it doesn't, really. This is intentionally a bare scaffold, not a smaller version of `patient-portal/server`'s business logic.

## Scope, and why it's this small

This was created on request to "behave just like `patient-portal/server`" — read as matching its *infrastructure shape* (ASP.NET Core 10 minimal API, `GET /health`, CORS for its own client, Dockerfile with a `curl`-based healthcheck), not literally copying its tenant-directory/login-proxy endpoints. Those exist because patient-portal is connective tissue between patients and `single-tenant` instances; a marketing site has no such relationship, so replicating that logic here would be nonsensical. If that reading is wrong and you actually want the tenant-report/login-proxy behavior here too, say so.

Concretely, this mirrors `patient-portal/server`'s state from *before* it grew a database or any endpoints beyond health — no EF Core, no Postgres, nothing persisted, because there's no domain model yet to justify one. Add a database the same way `patient-portal/server` did (a `DatabaseCredentials` value object per the root `CLAUDE.md`'s 12-factor-config convention, its own Postgres, migrations) once there's an actual reason to persist something.

## Endpoints

`GET /health` — anonymous. `{"status": "healthy"}`.

## CORS

`CLIENT_ORIGIN` env var, defaults to `http://localhost:5175` (`marketing-website/client`'s dev port). Same pattern as `patient-portal/server`.

## Env vars

None required. Optional: `CLIENT_ORIGIN`.

## Running locally

`dotnet run` (port `5026`, auto-assigned by `dotnet new webapi` at creation time — not manually chosen, same as how `patient-portal/server`'s `5009` came about).
