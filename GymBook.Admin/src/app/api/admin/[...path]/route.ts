import { NextResponse } from "next/server";
import { ApiError, apiRequest, freshSession } from "@/lib/api";

// Forwards the pages' calls to the API's /api/admin/* endpoints with the admin's token, which stays on this server.
// The API checks the role on every call, so this only has to attach the token.

type Context = { params: Promise<{ path: string[] }> };

async function forward(req: Request, { params }: Context) {
  const session = await freshSession();
  if (!session) return NextResponse.json({ title: "Your session has ended. Please sign in again." }, { status: 401 });

  const { path } = await params;
  const search = new URL(req.url).search;
  const body = req.method === "GET" || req.method === "DELETE" ? undefined : await req.text();

  try {
    const res = await apiRequest(`/api/admin/${path.map(encodeURIComponent).join("/")}${search}`, {
      method: req.method,
      body: body || undefined,
      token: session.accessToken,
    });
    if (res.status === 204) return new NextResponse(null, { status: 204 });
    return new NextResponse(await res.text(), { status: res.status, headers: { "Content-Type": "application/json" } });
  } catch (e) {
    if (e instanceof ApiError) return NextResponse.json({ title: e.message }, { status: e.status });
    return NextResponse.json({ title: "Couldn't reach the Gym Book server." }, { status: 502 });
  }
}

export const GET = forward;
export const POST = forward;
export const DELETE = forward;
