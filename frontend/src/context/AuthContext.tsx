"use client";

import { createContext, useCallback, useContext, useEffect, useMemo, useState } from "react";
import { api, type ApiError, type RegisterPayload, type UserResponse } from "@/lib/api";
import { clearSession, readToken, saveSession } from "@/lib/auth-storage";

type AuthStatus = "loading" | "guest" | "authenticated";

interface AuthContextValue {
  status: AuthStatus;
  user: UserResponse | null;
  register: (payload: RegisterPayload) => Promise<void>;
  login: (email: string, password: string) => Promise<void>;
  logout: () => void;
  refreshMe: () => Promise<void>;
}

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [status, setStatus] = useState<AuthStatus>("loading");
  const [user, setUser] = useState<UserResponse | null>(null);
  const refreshMe = useCallback(async () => {
    const token = readToken();
    if (!token) {
      setUser(null);
      setStatus("guest");
      return;
    }
    try {
      const me = await api.me(token);
      setUser(me);
      setStatus("authenticated");
    } catch (err) {
      const apiErr = err as ApiError;
      if (apiErr.kind === "http" && apiErr.status === 401) {
        clearSession();
      }
      setUser(null);
      setStatus("guest");
    }
  }, []);

  useEffect(() => {
    refreshMe();
  }, [refreshMe]);

  const login = useCallback(async (email: string, password: string) => {
    const res = await api.login({ email, password });
    saveSession(res.accessToken, res.expiresAtUtc);
    try {
      const me = await api.me(res.accessToken);
      setUser(me);
      setStatus("authenticated");
    } catch (err) {
      clearSession();
      setUser(null);
      setStatus("guest");
      throw err;
    }
  }, []);

  const register = useCallback(async (payload: RegisterPayload) => {
    await api.register(payload);
  }, []);

  const logout = useCallback(() => {
    clearSession();
    setUser(null);
    setStatus("guest");
  }, []);

  const value = useMemo<AuthContextValue>(
    () => ({ status, user, register, login, logout, refreshMe }),
    [status, user, register, login, logout, refreshMe],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used inside <AuthProvider>");
  return ctx;
}