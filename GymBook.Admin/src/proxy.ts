import { NextResponse, type NextRequest } from "next/server";
import { SESSION_COOKIE, unsealSession } from "@/lib/seal";

// Sends anyone without a session to the sign-in page. A session that's merely expired is caught by the API routes,
// which answer 401 and the pages come back here.
export async function proxy(request: NextRequest) {
  const session = await unsealSession(request.cookies.get(SESSION_COOKIE)?.value);
  const isLogin = request.nextUrl.pathname === "/login";

  if (!session && !isLogin) return NextResponse.redirect(new URL("/login", request.url));
  if (session && isLogin) return NextResponse.redirect(new URL("/", request.url));
  return NextResponse.next();
}

export const config = {
  // Pages only: the API routes answer 401 themselves, and assets must load on the sign-in page.
  matcher: ["/((?!api|_next/static|_next/image|icon.svg|logo.svg|favicon.ico).*)"],
};
