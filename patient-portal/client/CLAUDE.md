# patient-portal/client

React 19 + TypeScript + Vite SPA. The unified 2-step login: username first, then password (with a tenant picker in between if the username matches more than one). On success, hands the browser off to the resolved tenant's own client — this app holds no session of its own.

See the root `CLAUDE.md` for how this fits into the wider system. This file is specific to what's in this directory.

## History worth knowing before touching this

This directory started as a literal copy of `single-tenant/client`'s full auth flow, then was deliberately stripped back to a blank placeholder while `patient-portal/server` had nothing to call, then rebuilt once the server's login-proxy endpoint existed. It is **not** a re-copy of `single-tenant/client`'s login page — the flow, the API, and the redirect-on-success behavior are all shaped around patient-portal's specific job (figure out the tenant, proxy the password, then get out of the way), not a single tenant's login.

There used to be a separate `identify(username)` call/endpoint for step 1 — it was a free, unauthenticated way to check whether any username existed, and was removed (see `patient-portal/server`'s `CLAUDE.md`). **Step 1 on this client is now purely local UI state** — it doesn't call the server at all. The real work happens in one call on step 2's submit, which includes the password.

This client also used to hold its own session — an `AuthContext`, a `ProtectedRoute`, a `WelcomePage` showing "signed in to X" with its own logout button that proxied through patient-portal's server. All of that was deliberately removed: patient-portal has no business hosting a generic post-login experience, since each tenant's client could be a completely different codebase. If you find yourself wanting to add session state back here, re-read the root `CLAUDE.md`'s "traffic cop, not a destination" framing first.

## Structure

- `src/api/auth.ts` — `login(username, password, subdomain?)`, returning a discriminated `LoginResult`: `{status: "success", token, expiresAt, clientOrigin}` or `{status: "multiple", subdomains}` (the server's `300 Multiple Choices`). `AuthError` for the thrown, user-facing failure cases — `400` (every kind of login failure, deliberately indistinguishable server-side — see `patient-portal/server`'s `CLAUDE.md`) and `502` (tenant unreachable).
- `src/pages/LoginPage.tsx` — the 2-step wizard as **one component with internal step state** (`1 | 2`), not two separate routes, not two server round-trips. Step 1 just collects the username locally and advances — no fetch. Step 2 collects the password and calls `login(username, password)` (no subdomain the first time); if the result is `"multiple"`, the *same* step-2 form re-renders with a subdomain radio-picker added (password stays filled in, not cleared) and resubmits with the chosen subdomain. On `"success"`, calls `login()` directly (no context indirection — there's no session to update) and redirects, see below.

No `src/auth/` folder, no protected routes — there's no session here to protect.

## What happens on success

Not a client-side navigation to a "welcome" page — a real, cross-origin browser redirect to the resolved tenant's own client: `window.location.href = \`${clientOrigin}/handoff#token=${encodeURIComponent(token)}\``. `clientOrigin` comes straight from the server response (ultimately `tenant-router`'s `Subdomain.ClientOrigin`, reported by `single-tenant`). That tenant's client — not this one — owns everything from there: welcome page, logout, whatever it wants.

The token goes in the URL **fragment**, not a query string — fragments aren't sent to a server or written to access logs, and this project's own safety rules explicitly forbid putting sensitive data in query strings. The receiving end is `HandoffPage` on `single-tenant/client` (see its `CLAUDE.md`), which reads the fragment and starts a normal session with it, no credentials re-entered.

This doesn't reopen the username-enumeration problem `/api/auth/identify`'s removal was about: the redirect only happens *after* a successful password check, never from the username alone.

## Debugging note: stale Docker bundle cache

Easy to lose time on this. nginx serves a content-hashed JS filename per build (e.g. `index-Bp1EC03a.js`), and a browser that already has an *older* bundle cached under the old-but-still-valid `index.html` won't refetch it just because you rebuilt the container. If a fix to this client seems to have no effect when clicking around — including things that plainly *should* have changed, like a network call that should now fire and doesn't — confirm the actually-loaded `<script src>` in the page matches what the current image actually serves (`docker exec <container> ls /usr/share/nginx/html/assets/`) before concluding the code itself is wrong. A cache-busting reload (`?cb=<anything>`) or a hard refresh resolves it.

## Env vars

`VITE_API_BASE_URL` — patient-portal server's origin, e.g. `http://localhost:5009`. Build-time (Vite), not runtime — see `single-tenant/client`'s `CLAUDE.md` for what that means for Docker.

## Running locally

`npm run dev` (port `5174` per `.claude/launch.json`). Needs `patient-portal/server` running for `/login` to do anything. Same npm/rolldown native-binding caveat as `single-tenant/client` applies here (see its `CLAUDE.md`) if `npm install` fails with "Cannot find native binding".
