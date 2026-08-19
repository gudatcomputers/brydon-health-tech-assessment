# patient-portal/server

ASP.NET Core 10 minimal API. Namespace/project `PatientPortalServer`. **No database of its own** — it's a stateless orchestrator that calls `tenant-router` over HTTP to resolve which tenant a username belongs to. The tenant directory (`subdomains` + `tenant_usernames`) used to live here; it was extracted to `tenant-router` when a login-proxy-only frontend to that data no longer needed to own it directly (see git history and `tenant-router/CLAUDE.md`).

See the root `CLAUDE.md` for how this fits into the wider system. This file is specific to what's in this directory.

**No auth/JWT of its own, and no session either.** patient-portal has a login flow (`Auth/PatientLoginEndpoints.cs`), but it's a pass-through, not its own identity system — it never issues or validates a JWT, and it never holds one either. It asks `tenant-router` which tenant(s) a username belongs to, proxies the actual credential check to that tenant's server, and on success hands the resulting token straight to the browser to carry away to that tenant's own client — see "What happens on success" below. This is deliberate: each tenant's client could be a completely different codebase, so patient-portal can't assume it's safe to host a generic post-login experience for all of them. It's a traffic cop, not a destination.

## Folders

- `Auth/` — `PatientLoginEndpoints` (`/api/auth/login` — resolve-and-proxy in one endpoint). No `User` entity, no `JwtBearerConfiguration` — there's nothing to authenticate against here.
- `Tenants/` — `TenantRouterOptions` (value object: tenant-router's base URL + the shared secret), `TenantRouterClient` (the HTTP client that calls tenant-router's `/api/tenants/lookup`).

No `Data/` folder — there's nothing to persist here.

## How tenant resolution works now

`TenantRouterClient.LookupAsync(username)` `POST`s `{ username }` to `{TENANT_ROUTER_URL}/api/tenants/lookup` with the `X-Tenant-Report-Key` header (same shared secret, same header name as the report endpoint — tenant-router reuses one auth check for both), and deserializes `{ matches: [{ subdomain, serverUrl, clientOrigin }] }` into a `List<TenantMatch>`. This is a synchronous, interactive call in the request path (a user is waiting) — deliberately **not** wrapped in Polly retry/circuit-breaker (see root `CLAUDE.md`'s note on background vs. interactive calls); a failed/unreachable tenant-router surfaces as a `502` to the client instead of hanging.

`PatientLoginEndpoints` still owns all the decision logic that used to run against the local DB directly — it just sources the candidate list from this HTTP call instead of a query:
- `subdomain` provided → filters the lookup result to that subdomain; no match → `BadRequest()`.
- `subdomain` omitted, zero matches → `BadRequest()`.
- `subdomain` omitted, exactly one match → resolves and proxies automatically, no extra round trip needed.
- `subdomain` omitted, more than one match → returns **`300 Multiple Choices`** with `{ subdomains: [...] }`, *without* checking the password against any of them. Client resubmits the same `{ username, password }` plus a chosen `subdomain`. Browsers' `fetch` does not auto-follow `300` (only 301/302/303/307/308 are auto-followed, and there's no `Location` header here anyway) — verified live, the response reaches client code intact.

## Endpoints

`POST /api/auth/login` — anonymous, public-facing. `{ username, password, subdomain? }`, `subdomain` optional. **There is no separate "does this username exist" endpoint** — an earlier version had a standalone `/api/auth/identify` that hashed a bare username and returned matching subdomains with no password required, which was a free unauthenticated username-existence oracle; it was folded into this endpoint and removed. This still holds true after the tenant-router extraction: tenant-router's own `/api/tenants/lookup` *would* answer that question honestly (it has no reason not to — it's server-to-server only, not reachable from a browser), but patient-portal never exposes that answer directly. It always requires a password before revealing anything, and even then only through the collapsed failure semantics below.

**Every failure path returns `400 BadRequest` with no body — deliberately the same status for all of them.** "No matching user at all," "wrong subdomain for a real user," and "correct user/subdomain but wrong password" (the tenant's own `401`, translated on the way back) all collapse to the identical `400`. This isn't a REST-purity choice — a first pass used `401` for these and it was flagged as still leaking user existence: if the tenant's genuine wrong-password response passed through as `401` while a local "no such user" short-circuit returned something else, the status code itself would tell an attacker whether an account exists. If you add any new early-return failure case here, it must also return plain `BadRequest()` — never a distinguishable status, and never a response body that varies by reason. A tenant-router lookup failure (unreachable, non-2xx) is the one exception: that's an infrastructure problem, not a "wrong login," so it returns `502` — safe to distinguish since it doesn't reveal anything about a specific account.

On any resolved match, proxies `{ username, password }` server-to-server to `{ServerUrl}/api/auth/login` (the `ServerUrl` tenant-router handed back) via the named `tenant-login-proxy` HttpClient (10s timeout, no retry/circuit-breaker — see root `CLAUDE.md`'s note on why background vs. interactive calls are treated differently). Tenant unreachable or errors (not a credentials rejection) → `502`. Tenant's `200` → `200` with `{ token, expiresAt, clientOrigin }` — the tenant's own JWT, plus where the browser should go next.

`GET /health` — anonymous, used as the Docker healthcheck target.

## What happens on success

patient-portal doesn't store the token or route the client to a "welcome" page of its own — that page doesn't exist here. `clientOrigin` in the response is the resolved tenant's browser-reachable client origin (tenant-router's `Subdomain.ClientOrigin`, reported by `single-tenant` — see `tenant-router/CLAUDE.md`). `patient-portal/client` does a real cross-origin navigation, `window.location.href = \`${clientOrigin}/handoff#token=${token}\``, and that tenant's own client takes over from there — its own welcome page, its own logout, its own everything.

This shape (token in the URL fragment, not a query string) matters: fragments aren't sent to a server or written to access logs, and the safety rules this project follows explicitly forbid putting sensitive data in query strings. `HandoffPage` on the receiving `single-tenant/client` end reads it back out client-side.

Because the redirect only fires after a *successful* password check (never from the username alone), this doesn't reintroduce the username-enumeration problem the `/api/auth/identify` removal was about — nothing about which tenant(s) a username belongs to is ever revealed without a correct password first.

## CORS

`CLIENT_ORIGIN` env var, defaults to `http://localhost:5174`.

## Env vars

Required: `TENANT_ROUTER_URL`, `TENANT_REPORT_SECRET`.
Optional: `CLIENT_ORIGIN` (CORS, default `http://localhost:5174`).

## Running locally

`dotnet run` (port `5009` per `launchSettings.json`). No database of its own to bring up — just needs `tenant-router` reachable at `TENANT_ROUTER_URL` (`docker compose up -d` from the repo root brings up tenant-router's Postgres; tenant-router itself still needs `dotnet run` separately, or run the whole thing via `docker-compose.multi-tenant.yml`).

If testing the login proxy against local (non-Docker) tenant instances, see `single-tenant/server/CLAUDE.md`'s note on `--no-launch-profile` — needed to run more than one tenant instance locally without them silently sharing a database.
