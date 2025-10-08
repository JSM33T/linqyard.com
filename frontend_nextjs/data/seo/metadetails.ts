// data/seo/metadetails.ts
export const siteBase = {
  name: "Linqyard",
  baseUrl: process.env.NEXT_PUBLIC_SITE_URL || "https://linqyard.com",
};

export const statusMeta = {
  title: "Service Status",
  description: "Real-time monitoring of Linqyard services",
  path: "/status",
};

export const docsMeta = {
  title: "Docs",
  description: "Linqyard documentation",
  path: "/docs",
};

export const homeMeta = {
  title: "Home",
  description: "Linqyard : All your links in one place.",
  path: "/",
};

export const aboutMeta = {
  title: "About",
  description: "Learn more about Linqyard",
  path: "/about",
};

const metaDetails = {
  siteBase,
  status: statusMeta,
  docs: docsMeta,
};


export default metaDetails;

