import { NextResponse, type NextRequest } from "next/server";
import { jwtVerify } from "jose";

const SECRET = new TextEncoder().encode(
  process.env.JWT_SECRET || "tatovpn-secret-jwt-key-super-secure-token-9988"
);

const PUBLIC_PATHS = ["/login", "/api/auth/login"];

export async function middleware(request: NextRequest) {
  const { pathname } = request.nextUrl;

  // Permitir archivos estáticos y rutas públicas
  if (
    pathname.startsWith("/_next") ||
    pathname.startsWith("/static") ||
    pathname.includes(".") ||
    PUBLIC_PATHS.some((path) => pathname === path)
  ) {
    return NextResponse.next();
  }

  const token = request.cookies.get("tatovpn_session")?.value;

  if (!token) {
    // Redirigir a login si no hay token
    const url = request.nextUrl.clone();
    url.pathname = "/login";
    url.searchParams.set("redirect", pathname);
    return NextResponse.redirect(url);
  }

  try {
    await jwtVerify(token, SECRET);
    return NextResponse.next();
  } catch {
    const url = request.nextUrl.clone();
    url.pathname = "/login";
    return NextResponse.redirect(url);
  }
}

export const config = {
  matcher: ["/((?!api/auth/login|_next/static|_next/image|favicon.ico).*)"],
};
