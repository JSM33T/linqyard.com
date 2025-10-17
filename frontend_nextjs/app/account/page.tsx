import AccountClient from "./page.client";
import { createMetadata } from "@/app/lib/seo";

const accountMeta = {
	title: "Account — Linqyard",
	description: "Manage your Linqyard account: profile, links, security, and billing.",
	path: "/account",
};

export const metadata = createMetadata(accountMeta);

export default function AccountPage() {
	return <AccountClient />;
}
