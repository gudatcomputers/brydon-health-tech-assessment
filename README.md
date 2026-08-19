# Brydon Health Tech Assessment

A simulated multi-tenant EHR ecosystem: independent `single-tenant` instances (one per provider office) each report their patient directory to a shared `tenant-router`, which `patient-portal` and `marketing-website` both query to provide the same unified 2-step login (username, then password, proxied to whichever tenant the username belongs to) across all of them.


## Architecture Decisions
1. Use .NET's [Minimal APIs](https://learn.microsoft.com/en-us/aspnet/core/tutorials/min-web-api?view=aspnetcore-10.0&tabs=visual-studio)
1. Use [PostgreSQL](https://www.postgresql.org/) for persistence
1. Use [React](https://react.dev/) with a [vite](https://vite.dev/) build system
1. [12 factor configuration](https://12factor.net/config)
1. use [EntityFramework](https://learn.microsoft.com/en-us/ef/) for database migrations and ORM
1. `patient-portal` owns no data - acts as orchestrator
1. `patient-portal/server` exists to allow secure service to service communication that doesn't require exposing "internal" services and any shared secrets to the `client` codebase
1. `tenant-router` is a microservice that supports collection of data from tenant instances to facilitate `patient-portal` and `marketing-website` unified login
1. `single-tenant/server` reports (non-blocking) all unsynchronized users to `tenant-router` on startup
1. `single-tenant/server` reports new user registrations to `tenant-router`
1. `patient-portal/client` and `marketing-website/client` expose identical login orchestration for differing purposes to demonstrate the consumption of the shared `tenant-router` service
1. `tenant-router` takes on the risk of persisting hashed usernames alongside their tenant membership

## Additional Problem Introduced By The Approach
Patient/Provider membership in multiple tenants - With each tenant reporting its membership to `tenant-router` there exists a possibility that the same username exists in multiple tenants.  To combat this, the login orhestration in `patient-portal` and `marketing-website` expect an [HTTP 300](https://developer.mozilla.org/en-US/docs/Web/HTTP/Reference/Status/300) status that reutrns a list of valid tenants for a user only when multiple memberships exist.  This **does** leak information, but only in the case of multiple memberships and the username being known by the actor.



## With More Time..
In a normal working environment I would be test driving these implementations. Given the PoC nature of demonstrating this approach I did not include tests.  I would advocate for a strong unit test base (95% coverage) with an integration test suite to demonstrate each vertical integration, but not exhaustive of all edge cases covered in the unit tests.  I would also include a set of end to end tests that exercise the full browser flow via [Playwright](https://playwright.dev/) or [Cypress](https://www.cypress.io/)

I'm ignoring the visual aspect of the implementation for now as that would be driven by design.

I would also ensure [WCAG 2.2](https://www.w3.org/TR/WCAG22/) guidelines are met  for accessibility.

I would utilizie something like [Tanstack Query](https://tanstack.com/query/latest/docs/framework/react/overview) for client API interaction

I would add Swagger/OpenAPI documentation.

I would add [OpenTelemtry](https://opentelemetry.io/) for observability

I would add some form of APM.

<hr>

See [CLAUDE.md](CLAUDE.md) for the full architecture writeup 

<hr/>


## Starting the environment

This spins up 5 independent provider-office instances (their own client, server, and Postgres database), `tenant-router` (the shared directory they report to), and both `patient-portal` and `marketing-website` (two independent front doors into the same login flow) — 16 containers total, fully containerized, nothing to install locally beyond Docker itself.


```bash
docker compose -f docker-compose.multi-tenant.yml up --build
```

First run builds every image, which takes a few minutes; subsequent runs are much faster (Docker layer caching). Once everything reports healthy:

| Service | URL | What it is |
|---|---|---|
| `patient-portal-client` | http://localhost:5591 | Unified 2-step login, entry point #1 |
| `marketing-website-client` | http://localhost:5593 | Placeholder marketing site + `/login`, entry point #2 |
| `tenant1-client` … `tenant5-client` | http://localhost:5401 … `:5405` | Each provider office's own app — where a successful login actually lands |
| `tenant1-server` … `tenant5-server` | http://localhost:5301 … `:5305` | Each provider office's own API |
| `tenant-router` | http://localhost:5595 | Internal only — the shared directory service, no UI |
| `patient-portal-server` | http://localhost:5590 | Internal-facing API behind the patient-portal client |
| `marketing-website-server` | http://localhost:5592 | Internal-facing API behind the marketing-website client |

## Technologies required

| Tool | Version | Needed for |
|---|---|---|
| [Docker](https://docs.docker.com/get-docker/) + Docker Compose | Compose v2 (bundled with modern Docker Desktop/Engine) | The multi-tenant simulation, and normal single-instance local dev's Postgres containers |
| [.NET SDK](https://dotnet.microsoft.com/download) | 10.0 | Building/running any `*/server` project outside Docker |
| [Node.js](https://nodejs.org/) | `^20.19.0` or `>=22.12.0` | Building/running any `*/client` project outside Docker (Vite's requirement) |
| npm | bundled with Node | Client package installs |

Everything else (PostgreSQL 16) runs as a Docker container — you don't need Postgres installed natively unless you're doing something unusual.


### Trying it out

Each tenant starts up with 15 seeded random users (`patient-<8 hex chars>`), all sharing the password `password123`. Find a real username to log in with:

```bash
docker compose -f docker-compose.multi-tenant.yml exec postgres \
  psql -U simuser -d tenant1 -c 'SELECT username FROM users LIMIT 5;'
```

Then go to either http://localhost:5591 or http://localhost:5593/login, enter that username, then `password123`. You'll land on that tenant's own app (e.g. `tenant1-client`), already signed in — login is proxied server-to-server through `patient-portal`/`marketing-website` and tenant-router, with only the resulting token ever reaching your browser.

To check the shared directory tenant-router has built up:

```bash
docker compose -f docker-compose.multi-tenant.yml exec postgres \
  psql -U simuser -d tenant_router -c \
  'SELECT s."Name", count(*) FROM tenant_usernames t
   JOIN subdomains s ON s."Id" = t."SubdomainId" GROUP BY s."Name"'
```

### Stopping / resetting

```bash
docker compose -f docker-compose.multi-tenant.yml down       # stop, keep data
docker compose -f docker-compose.multi-tenant.yml down -v    # stop, wipe all data
```

## Running a single instance (non-Docker local dev)

For working on one piece at a time without the full simulation. Bring up just the two Postgres databases:

```bash
docker compose up -d
```

Then run whichever services you need directly:

```bash
# each in its own terminal, from the relevant directory
cd single-tenant/server && dotnet run    # http://localhost:5251
cd single-tenant/client && npm install && npm run dev    # http://localhost:5173
cd tenant-router && dotnet run           # http://localhost:5236
cd patient-portal/server && dotnet run   # http://localhost:5009
cd patient-portal/client && npm install && npm run dev    # http://localhost:5174
cd marketing-website/server && dotnet run   # http://localhost:5026
cd marketing-website/client && npm install && npm run dev  # http://localhost:5175
```

Copy `.env.example` to `.env` (or export the vars in your shell) and adjust as needed — it documents every variable each service reads, including the shared `TENANT_REPORT_SECRET` that must match across `single-tenant`, `tenant-router`, `patient-portal`, and `marketing-website`.

A single `single-tenant` instance seeds one `demo` / `password123` user by default (override with `DEMO_USERNAME`/`DEMO_PASSWORD`).

### Known local dev issue

`npm install` in any client can fail on macOS with `Cannot find native binding` (a known npm/rolldown optional-dependency bug, unrelated to this repo). Fix:

```bash
npm install @rolldown/binding-darwin-x64@1.2.4 --no-save
```

Doesn't happen inside Docker builds — only local macOS `npm install`.
