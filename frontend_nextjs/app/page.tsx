import { createMetadata } from "@/app/lib/seo";
import { homeMeta } from "@/data/seo/metadetails";
import HomeClient from "./page.client";

export const metadata = createMetadata(homeMeta);

export default function Home() {
  return <HomeClient />;
}
