import PrivacyClient from "./page.client";
// ---- Metadata (SSR) ----
import { createMetadata } from "@/app/lib/seo";

const privacyMeta = {
  title: "Privacy Policy",
  description: "Privacy Policy for Linqyard",
  path: "/privacy-policy",
};

export const metadata = createMetadata(privacyMeta);

export default function PrivacyPage() {
  return <PrivacyClient />;
}
