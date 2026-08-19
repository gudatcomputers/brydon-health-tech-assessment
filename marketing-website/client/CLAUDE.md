# marketing-website/client

React 19 + TypeScript + Vite SPA. `/` is still a placeholder — `HomePage.tsx` renders "Marketing Website / Coming soon" plus a "Sign in" link. `/login` has the same 2-step login flow as `patient-portal/client`.

See the root `CLAUDE.md` for how the login flow fits into the wider system — the marketing-site placeholder itself doesn't, it's a standalone informational page.

## History

Initialized from `patient-portal/client`'s tooling (same `package.json` conventions, `eslint.config.js`, `tsconfig*.json`, `vite.config.ts`, `Dockerfile`/`nginx.conf`, shared `index.css` design tokens), originally *without* its login code — `react-router-dom` wasn't even a dependency at first. The login flow was added later, on request, as a deliberate duplication of `patient-portal/client`'s: `src/api/auth.ts` and `src/pages/LoginPage.tsx` are near-identical copies (same request/response shapes, same redirect-on-success behavior), not a shared import — this codebase's convention throughout is independent per-service copies of infra-shape code (see `UsernameHasher` on the backend side), not a shared frontend package either.

## Structure

- `src/api/auth.ts` — `login(username, password, subdomain?)`, returning a discriminated `LoginResult`: `{status: "success", token, expiresAt, clientOrigin}` or `{status: "multiple", subdomains}` (the server's `300 Multiple Choices`). `AuthError` for the thrown, user-facing failure cases.
- `src/pages/HomePage.tsx` — the real home page (`/`), placeholder content plus a link to `/login`.
- `src/pages/LoginPage.tsx` — the 2-step wizard (username, then password, with a subdomain picker inserted if the username matches more than one tenant). On success, redirects the browser away entirely — see "What happens on success" below. No session is ever held here, same as `patient-portal/client`.

No `src/auth/` folder, no protected routes — same reasoning as `patient-portal/client`: there's no session here to protect, login always ends in a redirect to somewhere else.

## What happens on success

Identical to `patient-portal/client`'s — see that project's `CLAUDE.md` for the full explanation (why it's a real cross-origin `window.location.href` redirect and not client-side routing, why the token goes in a URL fragment and not a query string, why this doesn't reopen the username-enumeration problem the removed `/api/auth/identify` endpoint was about). The receiving end is the same `/handoff` route on `single-tenant/client` that `patient-portal/client`'s redirect already lands on — nothing there needed to change to support a second caller.

Same debugging note applies too: nginx serves a content-hashed JS filename per Docker build, and a stale browser cache can make a real fix look like a no-op. See `patient-portal/client/CLAUDE.md`'s note on confirming the actually-loaded bundle before concluding code is wrong.

## Env vars

`VITE_API_BASE_URL` — this service's own server's origin, e.g. `http://localhost:5026`. Build-time (Vite), not runtime — see `single-tenant/client`'s `CLAUDE.md` for what that means for Docker.

## Running locally

`npm run dev` (port `5175` per `.claude/launch.json`). Needs `marketing-website/server` running for `/login` to do anything (`/` works standalone). Same npm/rolldown native-binding caveat as the other clients (see `single-tenant/client`'s `CLAUDE.md`) if `npm install` fails with "Cannot find native binding".

`Dockerfile` builds and serves via nginx (same pattern as the other clients). Wired into `docker-compose.multi-tenant.yml` (`marketing-website-client`, port `5593`, depends on `marketing-website-server` at `5592`) but not `docker-compose.yml` — that file is single-tenant + tenant-router Postgres only, and this still has no database to need it.
