const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5251'

export interface LoginResponse {
  token: string
  expiresAt: string
}

export class LoginError extends Error {}

export async function login(username: string, password: string): Promise<LoginResponse> {
  const response = await fetch(`${API_BASE_URL}/api/auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ username, password }),
  })

  if (response.status === 401) {
    throw new LoginError('Invalid username or password')
  }

  if (!response.ok) {
    throw new LoginError('Something went wrong, please try again')
  }

  return response.json() as Promise<LoginResponse>
}

export async function logout(token: string): Promise<void> {
  await fetch(`${API_BASE_URL}/api/auth/logout`, {
    method: 'POST',
    headers: { Authorization: `Bearer ${token}` },
  })
}
