// app/docs/page.tsx — server wrapper
// Keep metadata and SSR-safe values here; UI lives in the client component.
import { createMetadata } from "@/app/lib/seo";
import { docsMeta } from "@/data/seo/metadetails";
import DocsPageClient from "./page.client";

export const metadata = createMetadata(docsMeta);

export default function DocsPage() {
  // Server component: render the client component for interactive UI.
  return <DocsPageClient />;
}
