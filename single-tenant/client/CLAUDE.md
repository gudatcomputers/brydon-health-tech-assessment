# single-tenant/client

React 19 + TypeScript + Vite SPA for the single-tenant EHR example. Talks to `single-tenant/server`.

See the root `CLAUDE.md` for how this fits into the wider system. This file is specific to what's in this directory.

## Structure

- `src/api/auth.ts` — `login`, `register`, `logout` calls to the server. `AuthError` is the error type both throw (renamed from `LoginError` once `register` was added — it's not login-specific).
- `src/auth/` — `AuthContext.tsx` (provider, stores the JWT in `localStorage`), `auth-context.ts` (context + `AuthContextValue` type), `useAuth.ts` (hook — kept in its own file, not exported from `AuthContext.tsx`, so Vite's fast-refresh lint rule doesn't complain about a file exporting both a component and a non-component value), `ProtectedRoute.tsx`.
- `src/pages/` — `LoginPage.tsx`, `RegisterPage.tsx` (cross-link to each other), `WelcomePage.tsx` (protected, has the logout button).
- Routing in `App.tsx`: `/` redirects based on auth state, `/login`, `/register`, `/welcome` (wrapped in `ProtectedRoute`).

## Styling convention

`LoginPage` and `RegisterPage` share the `.auth-page` / `.auth-error` classes in `App.css` rather than each having their own — when adding a new auth-adjacent page, reuse those classes instead of duplicating the CSS.

## Env vars

`VITE_API_BASE_URL` — the server's origin, e.g. `http://localhost:5251`. **Baked in at build time** (Vite convention), not read at runtime. This matters for Docker: `Dockerfile` accepts it as a build `ARG`, and each containerized client that needs to point at a different server needs its own image build with a different `--build-arg`, not a shared image with a runtime env var.

## Known local dev issue

`npm install` here can hit a known npm/rolldown optional-dependency bug on this machine (`Cannot find native binding`). Fix: `npm install @rolldown/binding-darwin-x64@1.2.4 --no-save`. Only affects local macOS `npm install` — the Docker build (Debian-based Node image, chosen specifically to avoid Alpine's musl libc compatibility issues with native bindings) doesn't hit it.

## Running locally

`npm run dev` (port `5173`). Needs `single-tenant/server` running (default `http://localhost:5251`) for anything beyond the static pages to work.
