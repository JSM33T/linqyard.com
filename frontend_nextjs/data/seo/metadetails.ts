// data/seo/metadetails.ts
export const siteBase = {
  name: "Linqyard",
  baseUrl: process.env.NEXT_PUBLIC_SITE_URL || "https://linqyard.com",
};

/* ────────────────────────────── Status Page ────────────────────────────── */
export const statusMeta = {
  title: "Linqyard Service Status & Uptime Monitor",
  description:
    "Check real-time service health, uptime, and maintenance updates for all Linqyard systems and APIs.",
  path: "/status",
};

/* ────────────────────────────── Documentation ────────────────────────────── */
export const docsMeta = {
  title: "Linqyard Documentation & Developer Guides",
  description:
    "Browse official Linqyard docs — setup guides, API references, and integration examples for developers and teams.",
  path: "/docs",
};

/* ────────────────────────────── Home Page ────────────────────────────── */
export const homeMeta = {
  title: "Linqyard — Smart Link Management & Creator Tools",
  description:
    "Build your professional link hub with Linqyard. Organize all your social links, CTAs, and analytics in one beautiful, privacy-first page.",
  path: "/",
};

/* ────────────────────────────── About Page ────────────────────────────── */
export const aboutMeta = {
  title: "About Linqyard | Link Platform for Creators & Businesses",
  description:
    "Learn how Linqyard helps creators, startups, and brands centralize links, track engagement, and grow audiences with privacy-focused analytics.",
  path: "/about",
};

/* ────────────────────────────── Export ────────────────────────────── */
const metaDetails = {
  siteBase,
  status: statusMeta,
  docs: docsMeta,
  home: homeMeta,
  about: aboutMeta,
};

export default metaDetails;
