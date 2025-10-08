"use client";

import { useEffect, useMemo, useState } from "react";
import { motion } from "framer-motion";
import {
  CheckCircle,
  XCircle,
  Clock,
  RefreshCw,
  Server,
  Database,
  Globe,
  Zap,
  Activity,
  AlertTriangle,
} from "lucide-react";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";

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
  lastChecked: string | null;
  error?: string;
}

function IconFor(kind: ServiceBase["icon"]) {
  const cls = "h-5 w-5";
  switch (kind) {
    case "server":
      return <Server className={cls} />;
    case "zap":
      return <Zap className={cls} />;
    case "database":
      return <Database className={cls} />;
    case "globe":
      return <Globe className={cls} />;
  }
}

async function clientCheck(service: ServiceBase): Promise<ServiceStatus> {
  const start = Date.now();
  try {
    // Client-side: use no-cors to just detect reachability without CORS errors
    await fetch(service.url, {
      method: "HEAD",
      mode: "no-cors",
      cache: "no-cache",
      signal: AbortSignal.timeout(8000),
    });
    const rt = Date.now() - start;
    return {
      ...service,
      status: "online",
      responseTime: rt,
      lastChecked: new Date().toISOString(),
    };
  } catch (e: any) {
    const rt = Date.now() - start;
    return {
      ...service,
      status: "offline",
      responseTime: rt > 7000 ? null : rt,
      lastChecked: new Date().toISOString(),
      error: e?.message ?? "Connection failed",
    };
  }
}

export default function StatusClient({ initialStatuses }: { initialStatuses: ServiceStatus[] }) {
  // derive the immutable service list from initial data
  const services = useMemo<ServiceBase[]>(
    () =>
      initialStatuses.map(({ name, url, description, icon }) => ({
        name,
        url,
        description,
        icon,
      })),
    [initialStatuses]
  );

  const [serviceStatuses, setServiceStatuses] = useState<ServiceStatus[]>(
    initialStatuses.map((s) => ({ ...s }))
  );
  const [isRefreshing, setIsRefreshing] = useState(false);
  const [lastUpdate, setLastUpdate] = useState<Date>(new Date());

  const getStatusIcon = (status: Status) => {
    switch (status) {
      case "online":
        return <CheckCircle className="h-5 w-5 text-green-600" />;
      case "offline":
        return <XCircle className="h-5 w-5 text-red-600" />;
      case "checking":
        return <Clock className="h-5 w-5 text-yellow-600 animate-spin" />;
      default:
        return <AlertTriangle className="h-5 w-5 text-gray-600" />;
    }
  };

  const getStatusBadge = (status: Status) => {
    switch (status) {
      case "online":
        return <Badge className="bg-green-100 text-green-800 hover:bg-green-100">Operational</Badge>;
      case "offline":
        return <Badge variant="destructive">Down</Badge>;
      case "checking":
        return <Badge variant="secondary">Checking...</Badge>;
      default:
        return <Badge variant="outline">Unknown</Badge>;
    }
  };

  const checkAllServices = async () => {
    setIsRefreshing(true);
    setServiceStatuses((prev) => prev.map((s) => ({ ...s, status: "checking" })));
    try {
      const results = await Promise.all(services.map((svc) => clientCheck(svc)));
      setServiceStatuses(results);
      setLastUpdate(new Date());
    } finally {
      setIsRefreshing(false);
    }
  };

  useEffect(() => {
    // auto-refresh every 30s
    const id = setInterval(checkAllServices, 30000);
    return () => clearInterval(id);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [services]);

  const overallStatus =
    serviceStatuses.every((s) => s.status === "online")
      ? "All Systems Operational"
      : serviceStatuses.some((s) => s.status === "offline")
      ? "Some Issues Detected"
      : "Checking Status...";

  const onlineCount = serviceStatuses.filter((s) => s.status === "online").length;
  const totalServices = serviceStatuses.length;

  return (
    <>
      {/* Header */}
      <motion.div
        className="text-center mb-12"
        initial={{ opacity: 0, y: 20 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.6 }}
      >
        <h1 className="text-4xl md:text-5xl font-bold text-foreground mb-4">System Status</h1>
        <p className="text-xl text-muted-foreground mb-6">Real-time monitoring of Linqyard services</p>

        <Card className="max-w-md mx-auto mb-8">
          <CardContent className="p-6">
            <div className="flex items-center justify-center gap-3 mb-2">
              {overallStatus === "All Systems Operational" ? (
                <CheckCircle className="h-8 w-8 text-green-600" />
              ) : (
                <AlertTriangle className="h-8 w-8 text-yellow-600" />
              )}
              <span className="text-2xl font-semibold text-foreground">{overallStatus}</span>
            </div>
            <p className="text-sm text-muted-foreground">
              {onlineCount} of {totalServices} services operational
            </p>
          </CardContent>
        </Card>

        <Button onClick={checkAllServices} disabled={isRefreshing} variant="outline" className="mb-8">
          <RefreshCw className={`h-4 w-4 mr-2 ${isRefreshing ? "animate-spin" : ""}`} />
          {isRefreshing ? "Checking..." : "Refresh Status"}
        </Button>
      </motion.div>

      {/* Services Grid */}
      <div className="grid md:grid-cols-2 gap-6 mb-12">
        {serviceStatuses.map((service, index) => (
          <motion.div
            key={service.name}
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.6, delay: index * 0.1 }}
          >
            <Card className="h-full hover:shadow-lg transition-all duration-300">
              <CardHeader>
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-3">
                    <div className="text-primary">{IconFor(service.icon)}</div>
                    <div>
                      <CardTitle className="text-lg">{service.name}</CardTitle>
                      <CardDescription className="text-sm">{service.description}</CardDescription>
                    </div>
                  </div>
                  {getStatusIcon(service.status)}
                </div>
              </CardHeader>
              <CardContent>
                <div className="space-y-3">
                  <div className="flex items-center justify-between">
                    <span className="text-sm font-medium">Status:</span>
                    {getStatusBadge(service.status)}
                  </div>

                  {service.responseTime !== null && (
                    <div className="flex items-center justify-between">
                      <span className="text-sm font-medium">Response Time:</span>
                      <span className="text-sm text-muted-foreground">{service.responseTime}ms</span>
                    </div>
                  )}

                  <div className="flex items-center justify-between">
                    <span className="text-sm font-medium">Endpoint:</span>
                    <code className="text-xs bg-muted px-2 py-1 rounded font-mono">{service.url}</code>
                  </div>

                  {service.lastChecked && (
                    <div className="flex items-center justify-between">
                      <span className="text-sm font-medium">Last Checked:</span>
                      <span className="text-xs text-muted-foreground">
                        {new Date(service.lastChecked).toLocaleTimeString()}
                      </span>
                    </div>
                  )}
                </div>
              </CardContent>
            </Card>
          </motion.div>
        ))}
      </div>

      {/* Footer Info */}
      <motion.div
        className="text-center text-sm text-muted-foreground"
        initial={{ opacity: 0 }}
        animate={{ opacity: 1 }}
        transition={{ duration: 0.6, delay: 0.8 }}
      >
        <div className="flex items-center justify-center gap-2 mb-2">
          <Activity className="h-4 w-4" />
          <span>Last updated: {lastUpdate.toLocaleString()}</span>
        </div>
        <p>Status checks run automatically every 30 seconds</p>
      </motion.div>
    </>
  );
}
