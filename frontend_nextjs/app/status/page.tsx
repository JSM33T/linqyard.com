// app/status/page.tsx
import StatusClient from "./page.client";

type Status = "online" | "offline" | "checking";

interface ServiceBase {
  name: string;
  url: string;
  description: string;
  icon: "server" | "zap" | "database" | "globe";
}

interface ServiceStatus extends ServiceBase {
  status: Status;
  responseTime: number | null;
  lastChecked: string | null; // ISO string for serialization
  error?: string;
}

const services: ServiceBase[] = [
  { name: "Main API", url: "https://api.linqyard.com", description: "Core API services and authentication", icon: "server" },
  { name: "Utilities API", url: "https://util.linqyard.com", description: "Utility services and helper functions", icon: "zap" },
  { name: "Database", url: "https://api.linqyard.com", description: "Database connectivity and performance", icon: "database" },
  { name: "CDN", url: "https://cdn.jsm33t.com", description: "Content delivery network status", icon: "globe" },
];

// ---- Metadata (SSR) ----
import { createMetadata } from "@/app/lib/seo";
import { statusMeta } from "@/data/seo/metadetails";

export const metadata = createMetadata(statusMeta);

async function checkServiceStatus(service: ServiceBase): Promise<ServiceStatus> {
  const controller = new AbortController();
  const timeout = setTimeout(() => controller.abort(), 8000);
  const start = Date.now();

  try {
    await fetch(service.url, {
      method: "HEAD",
      cache: "no-store",
      signal: controller.signal,
    });
    const rt = Date.now() - start;
    return {
      ...service,
      status: "online",
      responseTime: rt,
      lastChecked: new Date().toISOString(),
    };
  } catch (err: any) {
    const rt = Date.now() - start;
    return {
      ...service,
      status: "offline",
      responseTime: rt > 7000 ? null : rt,
      lastChecked: new Date().toISOString(),
      error: err?.message ?? "Connection failed",
    };
  } finally {
    clearTimeout(timeout);
  }
}

export const dynamic = "force-dynamic"; // always re-check on request

export default async function StatusPage() {
  const results = await Promise.all(services.map(checkServiceStatus));

  return (
    <div className="min-h-screen bg-background">
      <div className="container mx-auto px-4 py-12 max-w-6xl">
        {/* Directly render client component with SSR data */}
        <StatusClient initialStatuses={results} />
      </div>
    </div>
  );
}
