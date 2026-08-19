const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5026";

// clientOrigin is the tenant's own browser-reachable client — this site never
// hosts a session itself, on success the caller redirects the browser there
// with the token, rather than staying here.
export type LoginResult =
  | { status: "success"; token: string; expiresAt: string; clientOrigin: string }
  | { status: "multiple"; subdomains: string[] }

export class AuthError extends Error {}

// No separate "does this username exist" lookup — that was an unauthenticated
// existence oracle. Figuring out which tenant(s) a username belongs to now
// requires submitting a password. Omit subdomain on the first attempt; if the
// result is "multiple", resubmit with the same username/password plus one of
// the returned subdomains.
export async function login(username: string, password: string, subdomain?: string): Promise<LoginResult> {
  const response = await fetch(`${API_BASE_URL}/api/auth/login`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ username, password, subdomain: subdomain ?? null }),
  });

  if (response.status === 300) {
    const body = (await response.json()) as { subdomains: string[] };
    return { status: "multiple", subdomains: body.subdomains };
  }

  if (response.status === 400) {
    // Covers "no such user", "wrong subdomain", and "wrong password" — the
    // server deliberately returns the same status for all three so this
    // can't be used to tell whether a username exists.
    throw new AuthError("Invalid username or password");
  }

  if (response.status === 502) {
    throw new AuthError("That provider isn't available right now");
  }

  if (!response.ok) {
    throw new AuthError("Something went wrong, please try again");
  }

  const body = (await response.json()) as { token: string; expiresAt: string; clientOrigin: string };
  return { status: "success", ...body };
}
