"use client";

import { useCallback, useEffect, useState } from "react";

// The pages' side: they call this app's /api routes, never the API directly.

export class RequestError extends Error {
  constructor(
    public status: number,
    message: string,
  ) {
    super(message);
  }
}

async function request<T>(url: string, init?: RequestInit): Promise<T> {
  const res = await fetch(url, {
    ...init,
    headers: init?.body ? { "Content-Type": "application/json" } : undefined,
  });
  if (res.status === 401) {
    // The session ended (expired, signed out elsewhere, or the admin role was taken away).
    window.location.href = "/login";
    throw new RequestError(401, "Signed out");
  }
  if (!res.ok) {
    const body = await res.json().catch(() => null);
    throw new RequestError(res.status, body?.title ?? `Something went wrong (${res.status}).`);
  }
  return (res.status === 204 ? undefined : await res.json()) as T;
}

export const adminGet = <T>(path: string) => request<T>(`/api/admin/${path}`);

export const adminPost = (path: string, body: unknown = {}) =>
  request<void>(`/api/admin/${path}`, { method: "POST", body: JSON.stringify(body) });

export const adminDelete = (path: string) => request<void>(`/api/admin/${path}`, { method: "DELETE" });

/** Loads an admin endpoint; call reload() after changing something. */
export function useAdmin<T>(path: string | null) {
  const [data, setData] = useState<T | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  const reload = useCallback(async () => {
    if (!path) return;
    setLoading(true);
    try {
      setData(await adminGet<T>(path));
      setError(null);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Something went wrong.");
    } finally {
      setLoading(false);
    }
  }, [path]);

  useEffect(() => {
    // Fetching on mount and whenever the path changes is the point of this hook.
    // eslint-disable-next-line react-hooks/set-state-in-effect
    reload();
  }, [reload]);

  return { data, error, loading, reload };
}

export { request };
