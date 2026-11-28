const API_PREFIX = "";

export type ApiError =
  | { kind: "network" } | { kind: "http"; status: number; message: string; fieldErrors?: string[] };

export interface RegisterPayload {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
}

export interface LoginResponse {
  accessToken: string;
  tokenType: string;
  expiresAtUtc: string;
}

export interface UserResponse {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  roles: string[];
}

type UnauthorizedHandler = () => void;

let onUnauthorized: UnauthorizedHandler | null = null;

export function setUnauthorizedHandler(handler: UnauthorizedHandler | null) {
  onUnauthorized = handler;
}

async function parseError(res: Response): Promise<ApiError> {
  let body: unknown = null;
  try {
    const text = await res.text();
    body = text ? JSON.parse(text) : null;
  } catch {
    body = null;
  }

  const b = (body ?? {}) as Record<string, unknown>;
  let message = `Ошибка сервера (${res.status})`;
  if (typeof b.message === "string") message = b.message;
  else if (typeof b.error === "string") message = b.error;

  let fieldErrors: string[] | undefined;
  if (Array.isArray(b.errors)) {
    fieldErrors = b.errors.filter((e): e is string => typeof e === "string");
  }

  return { kind: "http", status: res.status, message, fieldErrors };
}

async function request<T>(
  path: string,
  init: RequestInit & { token?: string | null } = {},
): Promise<T> {
  const { token, headers, ...rest } = init;
  const finalHeaders = new Headers(headers);
  finalHeaders.set("Content-Type", "application/json");
  if (token) finalHeaders.set("Authorization", `Bearer ${token}`);

  let res: Response;
  try {
    res = await fetch(`${API_PREFIX}${path}`, {
      ...rest,
      headers: finalHeaders,
      cache: "no-store",
    });
  } catch {
    const networkError: ApiError = { kind: "network" };
    throw networkError;
  }

  if (res.status === 401) {
    onUnauthorized?.();
  }

  if (!res.ok) {
    throw await parseError(res);
  }
  if (res.status === 204) return undefined as T;
  return (await res.json()) as T;
}

export const api = {
  register: (body: RegisterPayload) =>
    request<{ id: string; message: string }>("/api/auth/register", {
      method: "POST",
      body: JSON.stringify(body),
    }),

  login: (body: { email: string; password: string }) =>
    request<LoginResponse>("/api/auth/login", {
      method: "POST",
      body: JSON.stringify(body),
    }),

  me: (token: string) => request<UserResponse>("/api/auth/me", { token }),
};