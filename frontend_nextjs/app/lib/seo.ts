// app/lib/seo.ts
import type { Metadata } from "next";

const SITE_NAME = "Linqyard";
const BASE_URL = process.env.NEXT_PUBLIC_SITE_URL || "https://linqyard.com";

const absolute = (path = "/") =>
  new URL(path.startsWith("/") ? path : `/${path}`, BASE_URL);

type MetaOpts = {
  title?: string;
  description?: string;
  path?: string; // e.g., "/status"
  robotsIndex?: boolean; // default true
  image?: string; // relative or absolute
};

export function createMetadata(opts: MetaOpts = {}): Metadata {
  const {
    title = SITE_NAME,
    description = "Build, connect, and monitor your Linqyard services.",
    path = "/",
    robotsIndex = true,
    image,
  } = opts;

  // Return the raw title. The root layout defines a `title.template` which
  // will compose the page-specific title with the site name. Avoid
  // pre-composing here to prevent duplication (e.g. "Linqyard • Linqyard").
  const finalTitle = title;
  const url = absolute(path).toString();
  const ogImage = image
    ? (image.startsWith("http") ? image : absolute(image).toString())
    : absolute("/og-default.png").toString();

  return {
    metadataBase: new URL(BASE_URL),
  title: finalTitle,
    description,
    alternates: { canonical: url },
    robots: { index: robotsIndex, follow: true },
    openGraph: {
      siteName: SITE_NAME,
      type: "website",
      url,
  title: finalTitle,
      description,
      images: [{ url: ogImage }],
    },
    twitter: {
      card: "summary_large_image",
  title: finalTitle,
      description,
      images: [ogImage],
    },
    icons: {
      icon: "/favicon.ico",
      apple: "/apple-touch-icon.png",
    },
  };
}
