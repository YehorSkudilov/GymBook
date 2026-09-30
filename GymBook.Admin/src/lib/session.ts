import "server-only";
import { cookies } from "next/headers";
import { sealSession, SESSION_COOKIE, unsealSession, type Session } from "./seal";

// The admin's session: the API tokens, encrypted into an httpOnly cookie. The browser never sees the tokens - it only
// calls this app's own /api routes, which attach them (see api.ts).

export type { Role, Session } from "./seal";

export async function readSession() {
  return unsealSession((await cookies()).get(SESSION_COOKIE)?.value);
}

/** Only from route handlers and server actions: server components can't set cookies. */
export async function writeSession(session: Session) {
  (await cookies()).set(SESSION_COOKIE, await sealSession(session), {
    httpOnly: true,
    // Plain http only for local development; behind the reverse proxy the site is always https.
    secure: process.env.NODE_ENV === "production" && process.env.SESSION_COOKIE_INSECURE !== "true",
    sameSite: "lax",
    path: "/",
    expires: new Date(session.refreshExpires),
  });
}

export async function clearSession() {
  (await cookies()).delete(SESSION_COOKIE);
}
