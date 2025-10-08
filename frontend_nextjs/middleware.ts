// middleware.ts
import type { NextRequest } from 'next/server'
import { NextResponse } from 'next/server'

const ROOT = process.env.NEXT_PUBLIC_ROOT_DOMAIN || 'linqyard.com'
// Add all infra subdomains you do NOT want treated as usernames
const RESERVED = new Set(['www', 'api', 'util', 'cdn', 'files'])

export function middleware(req: NextRequest) {
  const host = req.headers.get('host') || ''
  const url = req.nextUrl.clone()

  // Only handle subdomains of ROOT, and skip reserved subdomains
  if (host.endsWith(`.${ROOT}`)) {
    const sub = host.slice(0, -(`.${ROOT}`.length))
    if (sub && !RESERVED.has(sub)) {
      // Rewrite only the subdomain root to /links/<sub>
      if (url.pathname === '/' || url.pathname === '') {
        url.pathname = `/links/${sub}`
        return NextResponse.rewrite(url)
      }
      // (Optional) If you want every path to be scoped under /links/<sub>,
      // uncomment this block to support things like username.linqyard.com/about
      /*
      if (!url.pathname.startsWith('/_next')) {
        url.pathname = `/links/${sub}${url.pathname}`
        return NextResponse.rewrite(url)
      }
      */
    }
  }

  return NextResponse.next()
}

export const config = {
  matcher: [
    // Skip Next internals and common static assets
    '/((?!_next/static|_next/image|favicon.ico|robots.txt|sitemap.xml|assets/).*)',
  ],
}
