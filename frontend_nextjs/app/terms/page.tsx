
// ---- Metadata (SSR) ----
import { createMetadata } from "@/app/lib/seo";
import TermsClient from "./page.client";

const termsMeta = {
	title: "Terms & Conditions",
	description: "Terms & Conditions for Linqyard",
	path: "/terms-and-conditions",
};

export const metadata = createMetadata(termsMeta);

export default function TermsPage() {
	return <TermsClient />;
}
