export type CacheBustVersion = string | number | Date | null | undefined;

/**
 * Adds or replaces a `v=` query parameter on the provided URL so browsers treat the resource
 * as unique whenever the `version` changes. Works with absolute and relative URLs and preserves
 * existing query/hash fragments.
 */
export function applyCacheBustingParam(url?: string | null, version?: CacheBustVersion): string | undefined {
  if (!url) {
    return undefined;
  }

  if (version === null || version === undefined || version === '') {
    return url;
  }

  const resolvedVersion = normalizeVersion(version);
  if (!resolvedVersion) {
    return url;
  }

  try {
    const hasScheme = /^[a-zA-Z][a-zA-Z\d+\-.]*:/.test(url);
    const base = hasScheme
      ? undefined
      : typeof window !== 'undefined'
        ? window.location.origin
        : 'http://localhost';

    const parsed = new URL(url, base);
    parsed.searchParams.set('v', resolvedVersion);

    const result = parsed.toString();
    if (!hasScheme && base) {
      return result.replace(base, '');
    }

    return result;
  }
  catch {
    return appendManually(url, resolvedVersion);
  }
}

function normalizeVersion(version: CacheBustVersion): string | undefined {
  if (version instanceof Date) {
    return version.getTime().toString();
  }

  if (typeof version === 'number') {
    if (!Number.isFinite(version)) {
      return undefined;
    }
    return Math.trunc(version).toString();
  }

  if (typeof version === 'string') {
    const trimmed = version.trim();
    return trimmed.length > 0 ? trimmed : undefined;
  }

  return undefined;
}

function appendManually(url: string, version: string): string {
  const [withoutHash, hash] = url.split('#');
  const cleaned = withoutHash.replace(/([?&])v=[^&]*(&?)/, (_match, prefix, suffix) => (suffix ? prefix : ''));
  const separator = cleaned.includes('?') ? '&' : '?';
  const next = `${cleaned}${separator}v=${encodeURIComponent(version)}`;
  return hash ? `${next}#${hash}` : next;
}
