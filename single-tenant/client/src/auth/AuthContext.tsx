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

  const value = useMemo<AuthContextValue>(
    () => ({ token, isAuthenticated: token !== null, login, register, logout }),
    [token, login, register, logout],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
