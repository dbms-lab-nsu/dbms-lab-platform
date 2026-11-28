"use client";

import { createContext, useCallback, useContext, useEffect, useMemo, useRef, useState } from "react";
import { api, setUnauthorizedHandler, type ApiError, type RegisterPayload, type UserResponse } from "@/lib/api";
import { clearSession, readExpiresAt, readToken, saveSession } from "@/lib/auth-storage";

type AuthStatus = "loading" | "guest" | "authenticated" | "error";

interface AuthContextValue {
  status: AuthStatus;
  user: UserResponse | null;
  authError: ApiError | null;
  register: (payload: RegisterPayload) => Promise<void>;
  login: (email: string, password: string) => Promise<void>;
  logout: () => void;
  refreshMe: () => Promise<void>;
}

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [status, setStatus] = useState<AuthStatus>("loading");
  const [user, setUser] = useState<UserResponse | null>(null);
  const [authError, setAuthError] = useState<ApiError | null>(null);
  const expiryTimer = useRef<ReturnType<typeof setTimeout> | null>(null);

  const clearExpiryTimer = useCallback(() => {
    if (expiryTimer.current) {
      clearTimeout(expiryTimer.current);
      expiryTimer.current = null;
    }
  }, []);

  const resetToGuest = useCallback(() => {
    clearExpiryTimer();
    clearSession();
    setUser(null);
    setStatus("guest");
    setAuthError(null);
  }, [clearExpiryTimer]);

  const scheduleExpiry = useCallback(
    (expiresAtUtc: string | null) => {
      clearExpiryTimer();
      if (!expiresAtUtc) return;
      const target = Date.parse(expiresAtUtc);
      if (Number.isNaN(target)) return;
      const delay = target - Date.now();
      if (delay <= 0) {
        resetToGuest();
        return;
      }
      expiryTimer.current = setTimeout(() => {
        resetToGuest();
      }, delay);
    },
    [clearExpiryTimer, resetToGuest],
  );

  const applyMe = useCallback(
    (me: UserResponse) => {
      setUser(me);
      setStatus("authenticated");
      setAuthError(null);
      scheduleExpiry(readExpiresAt());
    },
    [scheduleExpiry],
  );

  const applyMeError = useCallback((err: unknown) => {
    const apiErr = err as ApiError;
    if (apiErr.kind === "http" && apiErr.status === 401) return;
    setAuthError(apiErr);
    setStatus("error");
  }, []);

  const refreshMe = useCallback(async () => {
    const token = readToken();
    if (!token) {
      resetToGuest();
      return;
    }
    try {
      applyMe(await api.me(token));
    } catch (err) {
      applyMeError(err);
    }
  }, [applyMe, applyMeError, resetToGuest]);

  useEffect(() => {
    const token = readToken();
    if (!token) {
      queueMicrotask(() => resetToGuest());
      return;
    }
    let cancelled = false;
    api.me(token).then(
      (me) => {
        if (!cancelled) applyMe(me);
      },
      (err) => {
        if (!cancelled) applyMeError(err);
      },
    );
    return () => {
      cancelled = true;
    };
  }, [applyMe, applyMeError, resetToGuest]);

  useEffect(() => {
    setUnauthorizedHandler(() => {
      clearExpiryTimer();
      clearSession();
      setUser(null);
      setStatus("guest");
      setAuthError(null);
    });
    return () => setUnauthorizedHandler(null);
  }, [clearExpiryTimer]);

  useEffect(() => {
    return () => {
      clearExpiryTimer();
    };
  }, [clearExpiryTimer]);

  const login = useCallback(
    async (email: string, password: string) => {
      const res = await api.login({ email, password });
      saveSession(res.accessToken, res.expiresAtUtc);
      try {
        const me = await api.me(res.accessToken);
        setUser(me);
        setStatus("authenticated");
        setAuthError(null);
        scheduleExpiry(res.expiresAtUtc);
      } catch (err) {
        resetToGuest();
        throw err;
      }
    },
    [resetToGuest, scheduleExpiry],
  );

  const register = useCallback(async (payload: RegisterPayload) => {
    await api.register(payload);
  }, []);

  const logout = useCallback(() => {
    resetToGuest();
  }, [resetToGuest]);

  const value = useMemo<AuthContextValue>(
    () => ({ status, user, authError, register, login, logout, refreshMe }),
    [status, user, authError, register, login, logout, refreshMe],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used inside <AuthProvider>");
  return ctx;
}