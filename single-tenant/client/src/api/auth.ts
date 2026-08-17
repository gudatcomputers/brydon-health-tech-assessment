const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5251";

export interface LoginResponse {
  token: string
  expiresAt: string
}

export class AuthError extends Error {}

export async function login(username: string, password: string): Promise<LoginResponse> {
  const response = await fetch(`${API_BASE_URL}/api/auth/login`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ username, password }),
  });

  if (response.status === 401) {
    throw new AuthError("Invalid username or password");
  }

  if (!response.ok) {
    throw new AuthError("Something went wrong, please try again");
  }

  return response.json() as Promise<LoginResponse>;
}

export async function register(username: string, password: string): Promise<LoginResponse> {
  const response = await fetch(`${API_BASE_URL}/api/auth/register`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ username, password }),
  });

  if (response.status === 409) {
    throw new AuthError("That username is already taken");
  }

  if (response.status === 400) {
    throw new AuthError("Password must be at least 8 characters");
  }

  if (!response.ok) {
    throw new AuthError("Something went wrong, please try again");
  }

  return response.json() as Promise<LoginResponse>;
}

export async function logout(token: string): Promise<void> {
  await fetch(`${API_BASE_URL}/api/auth/logout`, {
    method: "POST",
    headers: { Authorization: `Bearer ${token}` },
  });
}
