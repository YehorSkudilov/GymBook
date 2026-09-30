import "server-only";
import { decodeJwt } from "jose";
import { clearSession, readSession, writeSession, type Role, type Session } from "./session";

// Talks to GymBook.Api from the server. The API answers errors with RFC 7807 problem details ({ title, errors }).

export class ApiError extends Error {
  constructor(
    public status: number,
    message: string,
  ) {
    super(message);
  }
}

type AuthResponse = {
  userId: string;
  email: string;
  accessToken: string;
  accessTokenExpiresAt: string;
  refreshToken: string;
  refreshTokenExpiresAt: string;
};

function baseUrl() {
  const url = process.env.API_BASE_URL;
  if (!url) throw new Error("API_BASE_URL is not set.");
  return url.replace(/\/+$/, "");
}

async function errorMessage(res: Response) {
  try {
    const body = await res.json();
    const firstError = body?.errors && Object.values(body.errors as Record<string, string[]>)[0]?.[0];
    return (body?.title as string) || firstError || res.statusText;
  } catch {
    return res.statusText || `Request failed (${res.status})`;
  }
}

export async function apiRequest(path: string, init: RequestInit & { token?: string } = {}) {
  const { token, ...rest } = init;
  const res = await fetch(`${baseUrl()}${path}`, {
    ...rest,
    cache: "no-store",
    headers: {
      Accept: "application/json",
      ...(rest.body ? { "Content-Type": "application/json" } : {}),
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...rest.headers,
    },
  });
  if (!res.ok) throw new ApiError(res.status, await errorMessage(res));
  return res;
}

/** The admin role in an access token ("role" claim), or null for everyone else. */
export function roleOf(accessToken: string): Role | null {
  const role = decodeJwt(accessToken).role;
  return role === "Admin" || role === "SuperAdmin" ? role : null;
}

export function toSession(auth: AuthResponse, role: Role): Session {
  return {
    userId: auth.userId,
    email: auth.email,
    role,
    accessToken: auth.accessToken,
    accessExpires: Date.parse(auth.accessTokenExpiresAt),
    refreshToken: auth.refreshToken,
    refreshExpires: Date.parse(auth.refreshTokenExpiresAt),
  };
}

export async function login(email: string, password: string) {
  const res = await apiRequest("/api/auth/login", { method: "POST", body: JSON.stringify({ email, password }) });
  return (await res.json()) as AuthResponse;
}

export async function logout(refreshToken: string) {
  await apiRequest("/api/auth/logout", { method: "POST", body: JSON.stringify({ refreshToken }) }).catch(() => {});
}

/**
 * The session with an access token good for at least 30 more seconds, refreshing it (and the cookie) when needed.
 * Null when signed out, the session expired, or the account lost its admin role. Refresh tokens rotate, so only call
 * this from route handlers, which can write the new cookie.
 */
export async function freshSession(): Promise<Session | null> {
  const session = await readSession();
  if (!session) return null;
  if (session.accessExpires - 30_000 > Date.now()) return session;

  const next = await refreshOnce(session.refreshToken);
  if (next) await writeSession(next);
  else await clearSession();
  return next;
}

// Pages fire several requests at once, all carrying the same expired cookie. The API treats a refresh token used twice
// as stolen and ends the whole session, so requests with the same token share one refresh (kept a minute for stragglers).
const refreshes = new Map<string, Promise<Session | null>>();

function refreshOnce(refreshToken: string) {
  let pending = refreshes.get(refreshToken);
  if (!pending) {
    pending = refresh(refreshToken);
    refreshes.set(refreshToken, pending);
    setTimeout(() => refreshes.delete(refreshToken), 60_000);
  }
  return pending;
}

async function refresh(refreshToken: string): Promise<Session | null> {
  try {
    const res = await apiRequest("/api/auth/refresh", { method: "POST", body: JSON.stringify({ refreshToken }) });
    const auth = (await res.json()) as AuthResponse;
    const role = roleOf(auth.accessToken);
    if (!role) {
      await logout(auth.refreshToken);
      return null;
    }
    return toSession(auth, role);
  } catch {
    return null;
  }
}
