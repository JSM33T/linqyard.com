import { createMetadata } from "@/app/lib/seo";
import ContactClient from "./page.client";

const contactMeta = {
  title: "Contact Linqyard Support",
  description:
    "Reach the Linqyard team for product support, partnership opportunities, and general questions.",
  path: "/contact",
};

export const metadata = createMetadata(contactMeta);

export default function ContactPage() {
  return <ContactClient />;
}
