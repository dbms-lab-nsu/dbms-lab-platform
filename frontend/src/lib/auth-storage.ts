const TOKEN_KEY = 'dbmslab.accessToken';
const EXPIRES_KEY = 'dbmslab.expiresAtUtc';

export function saveSession(token: string, expiresAtUtc: string) {
  if (typeof window === 'undefined') return;
  window.sessionStorage.setItem(TOKEN_KEY, token);
  window.sessionStorage.setItem(EXPIRES_KEY, expiresAtUtc);
}

export function readToken(): string | null {
  if (typeof window === 'undefined') return null;
  const token = window.sessionStorage.getItem(TOKEN_KEY);
  const expiresAt = window.sessionStorage.getItem(EXPIRES_KEY);
  if (!token) return null;
  if (expiresAt && Date.parse(expiresAt) <= Date.now()) {
    clearSession();
    return null;
  }
  return token;
}

export function clearSession() {
  if (typeof window === 'undefined') return;
  window.sessionStorage.removeItem(TOKEN_KEY);
  window.sessionStorage.removeItem(EXPIRES_KEY);
}