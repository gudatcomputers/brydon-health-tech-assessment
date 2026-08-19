# marketing-website/server

ASP.NET Core 10 minimal API. Namespace/project `MarketingWebsiteServer`. Still no database of its own — no EF Core, no Postgres, nothing persisted.

See the root `CLAUDE.md` for how this fits into the wider system.

## Scope

This was created as a bare scaffold matching `patient-portal/server`'s *infrastructure shape* (ASP.NET Core 10 minimal API, `GET /health`, CORS for its own client, Dockerfile with a `curl`-based healthcheck). It has since grown patient-portal's actual login-proxy behavior too, on request — a marketing site's "Sign in" link routes an existing customer into their own tenant's app without them needing to know which subdomain they're on.

**This is a deliberate duplication of `patient-portal/server`'s login flow, not a shared dependency on it.** `Auth/MarketingLoginEndpoints.cs`, `Tenants/TenantRouterOptions.cs`, and `Tenants/TenantRouterClient.cs` are near-identical copies of `patient-portal/server`'s equivalents — same request/response shapes, same anti-enumeration behavior, same everything, just renamed to fit this service. This matches the established precedent elsewhere in this codebase (`UsernameHasher` is independently copied into both `single-tenant/server` and `tenant-router` rather than shared, "must stay byte-identical" enforced by comments/discipline, not a package). If `patient-portal/server`'s login logic changes, check whether this needs the same change — there's no compiler to catch drift between them.

Still no database, still no EF Core — the login proxy doesn't need to persist anything, same as `patient-portal/server`'s own copy of this logic doesn't.

## How tenant resolution works

Identical to `patient-portal/server`'s — see that project's `CLAUDE.md` for the full write-up (lookup semantics, the `subdomain`-provided/omitted/multiple-match decision tree, the `300 Multiple Choices` flow). The only thing to know here: this is a **second, independent caller** of tenant-router's `/api/tenants/lookup` — tenant-router already trusts any caller holding `TENANT_REPORT_SECRET`, so adding this caller didn't require any change on tenant-router's side.

## Endpoints

`POST /api/auth/login` — anonymous, public-facing. Same contract as `patient-portal/server`'s: `{ username, password, subdomain? }` in, uniform `400` on any failure (wrong password, no such user, wrong subdomain — deliberately indistinguishable), `300 Multiple Choices` + `{ subdomains }` on an ambiguous username, `502` if tenant-router or the resolved tenant is unreachable, `200` with `{ token, expiresAt, clientOrigin }` on success. `clientOrigin` is where the client should redirect the browser next — see `patient-portal/client`'s `CLAUDE.md` for what happens with it (the token goes in a URL fragment, never a query string, and lands on the resolved tenant's own `/handoff` route).

`GET /health` — anonymous. `{"status": "healthy"}`.

## CORS

`CLIENT_ORIGIN` env var, defaults to `http://localhost:5175` (`marketing-website/client`'s dev port).

## Env vars

Required: `TENANT_ROUTER_URL`, `TENANT_REPORT_SECRET` (same values as `patient-portal/server`'s — same tenant-router instance).
Optional: `CLIENT_ORIGIN`.

## Running locally

`dotnet run` (port `5026`). Needs `tenant-router` reachable at `TENANT_ROUTER_URL` — see `patient-portal/server/CLAUDE.md`'s "Running locally" section, same requirement here.

Also wired into `docker-compose.multi-tenant.yml` as `marketing-website-server` (port `5592`), `depends_on: tenant-router: condition: service_healthy` — no longer standalone there now that it actually talks to tenant-router.
