import { NextResponse } from "next/server";
import { ApiError, freshSession, login, logout, roleOf, toSession } from "@/lib/api";
import { clearSession, readSession, writeSession } from "@/lib/session";

// Sign in, see who's signed in, and sign out. Only Admins and SuperAdmins get a session.

export async function GET() {
  const session = await freshSession();
  if (!session) return NextResponse.json({ title: "Not signed in." }, { status: 401 });
  return NextResponse.json({ userId: session.userId, email: session.email, role: session.role });
}

export async function POST(req: Request) {
  const { email, password } = (await req.json().catch(() => ({}))) as { email?: string; password?: string };
  if (!email || !password) return NextResponse.json({ title: "Enter your email and password." }, { status: 400 });

  try {
    const auth = await login(email, password);
    const role = roleOf(auth.accessToken);
    if (!role) {
      // A real account, but not one that runs Gym Book: don't leave its new session lying around.
      await logout(auth.refreshToken);
      return NextResponse.json({ title: "This account doesn't have admin access." }, { status: 403 });
    }
    const session = toSession(auth, role);
    await writeSession(session);
    return NextResponse.json({ userId: session.userId, email: session.email, role: session.role });
  } catch (e) {
    if (e instanceof ApiError) return NextResponse.json({ title: e.message }, { status: e.status });
    return NextResponse.json({ title: "Couldn't reach the Gym Book server." }, { status: 502 });
  }
}

export async function DELETE() {
  const session = await readSession();
  if (session) await logout(session.refreshToken);
  await clearSession();
  return new NextResponse(null, { status: 204 });
}
