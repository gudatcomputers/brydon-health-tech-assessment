# patient-portal/client

React 19 + TypeScript + Vite SPA. **Currently a placeholder** — `App.tsx` just renders "Patient Portal / Coming soon", no routing, no auth, no API calls.

See the root `CLAUDE.md` for how this fits into the wider system. This file is specific to what's in this directory.

## History worth knowing before touching this

This directory started as a literal copy of `single-tenant/client`'s full auth flow (login/register/welcome, `AuthContext`, protected routes, react-router-dom). It was then deliberately stripped back to a blank placeholder on request, because patient-portal's server had no login flow of its own yet and a dangling copy of `single-tenant`'s login UI didn't make sense here. `react-router-dom` was removed from `package.json` at the same time since nothing used it anymore.

**When login is built here, it should not just be a re-copy of `single-tenant/client`'s login page.** The whole point of `patient-portal` (per the root `CLAUDE.md`) is a *unified* login that isn't scoped to one tenant — a patient logs in once here and gets routed to whichever `single-tenant` subdomain(s) their account exists on, using the hashed-username directory `patient-portal/server` already collects via `/api/tenants/report`. That redirect logic doesn't exist yet on either side.

## Env vars

None currently — there's nothing to configure until API calls exist.

## Running locally

`npm run dev` (port `5174` per `.claude/launch.json`). Same npm/rolldown native-binding caveat as `single-tenant/client` applies here (see its `CLAUDE.md`) if `npm install` fails with "Cannot find native binding".
