import { NextRequest, NextResponse } from "next/server";

type MetadataResponse = {
  description: string | null;
};

const FETCH_TIMEOUT_MS = 7000;

const META_TAG_PATTERNS = [
  /<meta\s[^>]*name=["']description["'][^>]*>/i,
  /<meta\s[^>]*property=["']og:description["'][^>]*>/i,
  /<meta\s[^>]*name=["']twitter:description["'][^>]*>/i,
];

const ENTITY_MAP: Record<string, string> = {
  amp: "&",
  lt: "<",
  gt: ">",
  quot: "\"",
  apos: "'",
  nbsp: " ",
};

function decodeHtmlEntities(input: string): string {
  return input.replace(/&(#x?[0-9a-fA-F]+|[a-zA-Z]+);/g, (match, entity) => {
    if (entity[0] === "#") {
      const codePoint =
        entity[1] === "x" || entity[1] === "X"
          ? parseInt(entity.slice(2), 16)
          : parseInt(entity.slice(1), 10);

      if (Number.isNaN(codePoint)) {
        return match;
      }

      try {
        return String.fromCodePoint(codePoint);
      } catch {
        return match;
      }
    }

    const normalized = entity.toLowerCase();
    return ENTITY_MAP[normalized] ?? match;
  });
}

function extractMetaContent(tag: string): string | null {
  const contentMatch =
    tag.match(/content\s*=\s*"([^"]*?)"/i) ??
    tag.match(/content\s*=\s*'([^']*?)'/i);

  if (!contentMatch) {
    return null;
  }

  const raw = contentMatch[1].trim();
  if (!raw) {
    return null;
  }

  return decodeHtmlEntities(raw);
}

function extractDescription(html: string): string | null {
  for (const pattern of META_TAG_PATTERNS) {
    const match = html.match(pattern);
    if (match?.[0]) {
      const content = extractMetaContent(match[0]);
      if (content) {
        return content;
      }
    }
  }
  return null;
}

function sanitizeUrl(rawUrl: string): URL | null {
  try {
    const parsed = new URL(rawUrl);
    if (!["http:", "https:"].includes(parsed.protocol)) {
      return null;
    }
    return parsed;
  } catch {
    return null;
  }
}

async function fetchWithTimeout(url: string, externalSignal?: AbortSignal) {
  const controller = new AbortController();
  const timeout = setTimeout(() => controller.abort(), FETCH_TIMEOUT_MS);
  const abortFromExternal = () => controller.abort();

  try {
    if (externalSignal) {
      if (externalSignal.aborted) {
        throw new DOMException("Aborted", "AbortError");
      }
      externalSignal.addEventListener("abort", abortFromExternal, { once: true });
    }

    return await fetch(url, {
      method: "GET",
      redirect: "follow",
      signal: controller.signal,
      headers: {
        "User-Agent": "LinqyardLinkPreview/1.0 (+https://linqyard.com)",
        Accept: "text/html,application/xhtml+xml",
      },
    });
  } finally {
    clearTimeout(timeout);
    if (externalSignal) {
      externalSignal.removeEventListener("abort", abortFromExternal);
    }
  }
}

export async function POST(request: NextRequest) {
  try {
    let body: any;
    try {
      body = await request.json();
    } catch {
      return NextResponse.json<MetadataResponse>({ description: null }, { status: 400 });
    }

    const urlInput = typeof body?.url === "string" ? body.url.trim() : "";

    const parsedUrl = sanitizeUrl(urlInput);
    if (!parsedUrl) {
      return NextResponse.json<MetadataResponse>(
        { description: null },
        { status: 400 },
      );
    }

    const requestSignal = (request as any).signal as AbortSignal | undefined;

    let response: Response;
    try {
      response = await fetchWithTimeout(parsedUrl.toString(), requestSignal);
    } catch (error) {
      if ((error as Error).name === "AbortError") {
        return NextResponse.json<MetadataResponse>({ description: null }, { status: 504 });
      }
      console.warn("Failed to fetch metadata:", error);
      return NextResponse.json<MetadataResponse>({ description: null }, { status: 200 });
    }

    if (!response.ok) {
      return NextResponse.json<MetadataResponse>({ description: null }, { status: 200 });
    }

    const contentType = response.headers.get("content-type") ?? "";
    if (!contentType.toLowerCase().includes("text/html")) {
      return NextResponse.json<MetadataResponse>({ description: null }, { status: 200 });
    }

    const html = (await response.text()).slice(0, 20000);
    const description = extractDescription(html);

    return NextResponse.json<MetadataResponse>({ description: description ?? null });
  } catch (error) {
    console.error("link-metadata route error:", error);
    return NextResponse.json<MetadataResponse>({ description: null }, { status: 500 });
  }
}
