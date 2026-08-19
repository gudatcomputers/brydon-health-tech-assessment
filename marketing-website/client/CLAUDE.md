# marketing-website/client

React 19 + TypeScript + Vite SPA. Currently a placeholder — `App.tsx` just renders "Marketing Website / Coming soon". No routing, no auth, no API calls yet. `marketing-website/server` exists (see its `CLAUDE.md`) but this client doesn't call it yet — there's nothing to call beyond `/health`.

See the root `CLAUDE.md` for how this fits into the wider system — it doesn't, really; this is a standalone informational site, not part of the tenant/patient-portal login flow.

## History

Initialized from `patient-portal/client`'s tooling (same `package.json` conventions, `eslint.config.js`, `tsconfig*.json`, `vite.config.ts`, `Dockerfile`/`nginx.conf`, shared `index.css` design tokens) but *not* its code — `patient-portal/client` already has a full 2-step login flow (`react-router-dom`, `AuthContext`, pages) that has nothing to do with a marketing site, so none of that was copied. `react-router-dom` isn't a dependency here; add it if/when this becomes more than one page.

## Env vars

None — no API to point at.

## Running locally

`npm run dev` (port `5175` per `.claude/launch.json`). Same npm/rolldown native-binding caveat as the other clients (see `single-tenant/client`'s `CLAUDE.md`) if `npm install` fails with "Cannot find native binding".

`Dockerfile` builds and serves via nginx (same pattern as the other clients) but isn't wired into either `docker-compose.yml` or `docker-compose.multi-tenant.yml` yet — add it there if/when this needs to run alongside the rest of the stack.
