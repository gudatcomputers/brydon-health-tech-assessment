import { useCallback, useMemo, useState, type ReactNode } from "react";
import { login as loginRequest, logout as logoutRequest, register as registerRequest } from "../api/auth";
import { AuthContext, type AuthContextValue } from "./auth-context";

const TOKEN_STORAGE_KEY = "brydon.auth.token";

export function AuthProvider({ children }: { children: ReactNode }) {
  const [token, setToken] = useState<string | null>(() =>
    localStorage.getItem(TOKEN_STORAGE_KEY),
  );

  const login = useCallback(async (username: string, password: string) => {
    const response = await loginRequest(username, password);
    localStorage.setItem(TOKEN_STORAGE_KEY, response.token);
    setToken(response.token);
  }, []);

  const register = useCallback(async (username: string, password: string) => {
    const response = await registerRequest(username, password);
    localStorage.setItem(TOKEN_STORAGE_KEY, response.token);
    setToken(response.token);
  }, []);

  const logout = useCallback(async () => {
    try {
      if (token) {
        await logoutRequest(token);
      }
    } finally {
      localStorage.removeItem(TOKEN_STORAGE_KEY);
      setToken(null);
    }
  }, [token]);

  // Accepts a token that was already verified elsewhere — patient-portal
  // proxying a login to this tenant, then handing the browser off with the
  // resulting token. No credentials involved, so no API call here, just the
  // same storage/state update login() does after its own call succeeds.
  const acceptToken = useCallback((newToken: string) => {
    localStorage.setItem(TOKEN_STORAGE_KEY, newToken);
    setToken(newToken);
  }, []);

  const value = useMemo<AuthContextValue>(
    () => ({ token, isAuthenticated: token !== null, login, register, logout, acceptToken }),
    [token, login, register, logout, acceptToken],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
