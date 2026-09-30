import { EncryptJWT, jwtDecrypt } from "jose";

// Encrypts the session into its cookie value. Kept apart from session.ts (which needs next/headers) so proxy.ts can
// check a session too.

export const SESSION_COOKIE = "gymbook_admin";

export type Role = "Admin" | "SuperAdmin";

export type Session = {
  userId: string;
  email: string;
  role: Role;
  accessToken: string;
  /** Epoch milliseconds. */
  accessExpires: number;
  refreshToken: string;
  /** Epoch milliseconds. */
  refreshExpires: number;
};

async function key() {
  const secret = process.env.SESSION_SECRET;
  if (!secret || secret.length < 32) throw new Error("SESSION_SECRET must be set to at least 32 characters.");
  return new Uint8Array(await crypto.subtle.digest("SHA-256", new TextEncoder().encode(secret)));
}

export async function sealSession(session: Session) {
  return new EncryptJWT({ ...session })
    .setProtectedHeader({ alg: "dir", enc: "A256GCM" })
    .setIssuedAt()
    .setExpirationTime(Math.floor(session.refreshExpires / 1000))
    .encrypt(await key());
}

export async function unsealSession(value: string | undefined): Promise<Session | null> {
  if (!value) return null;
  try {
    const { payload } = await jwtDecrypt(value, await key());
    return payload as unknown as Session;
  } catch {
    return null;
  }
}
