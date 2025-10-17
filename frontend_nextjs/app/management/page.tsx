"use client";

import Link from "next/link";
import { useMemo } from "react";
import { useUser } from "@/contexts/UserContext";
import AccessDenied from "@/components/AccessDenied";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { ShieldCheck, Users as UsersIcon, Layers, Hammer } from "lucide-react";

type ToolCard = {
  title: string;
  description: string;
  href?: string;
  icon: React.ElementType;
  badge?: string;
  disabled?: boolean;
};

export default function ManagementHomePage() {
  const { user, isAuthenticated, isInitialized } = useUser();
  const role = (user?.role ?? "").toLowerCase();
  const isAdmin = role === "admin";
  const isModerator = role === "mod" || isAdmin;

  const availableTools = useMemo<ToolCard[]>(() => {
    const tools: ToolCard[] = [];

    if (isAdmin) {
      tools.push({
        title: "User Directory",
        description: "Search, audit, and update user profiles, roles, and account status.",
        href: "/management/users",
        icon: UsersIcon,
        badge: "Admin",
      });
    }

    if (isModerator) {
      tools.push({
        title: "Content Moderation",
        description: "Review reports and keep the community safe. (Coming soon)",
        icon: Hammer,
        badge: isAdmin ? "Shared" : "Moderator",
        disabled: true,
      });
    }

    tools.push({
      title: "System Status",
      description: "Quick links to infrastructure dashboards and uptime monitoring.",
      href: "/status",
      icon: Layers,
    });

    return tools;
  }, [isAdmin, isModerator]);

  if (!isInitialized) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-background">
        <div className="flex flex-col items-center space-y-4">
          <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary"></div>
          <p className="text-sm text-muted-foreground">Loading management tools…</p>
        </div>
      </div>
    );
  }

  if (!isAuthenticated) {
    return <AccessDenied />;
  }

  if (!isModerator) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-background px-4">
        <Card className="max-w-lg">
          <CardHeader>
            <CardTitle>Restricted Area</CardTitle>
            <CardDescription>
              Only moderators and administrators can access the management console.
            </CardDescription>
          </CardHeader>
          <CardContent className="flex items-center gap-3 text-sm text-muted-foreground">
            <ShieldCheck className="h-5 w-5 text-primary" />
            <span>Contact an administrator if you believe you should have access.</span>
          </CardContent>
        </Card>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-background py-12">
      <div className="container mx-auto px-4 space-y-10">
        <header className="space-y-2">
          <p className="text-sm text-muted-foreground uppercase tracking-wide">Management Console</p>
          <h1 className="text-3xl font-semibold tracking-tight">
            Welcome back, {user?.firstName || user?.username || "team member"}
          </h1>
          <p className="text-muted-foreground">
            Use the tools below to keep Linqyard healthy. Your current role:{" "}
            <Badge variant="secondary" className="uppercase">
              {role || "user"}
            </Badge>
          </p>
        </header>

        <section className="grid gap-6 md:grid-cols-2 xl:grid-cols-3">
          {availableTools.map((tool) => {
            const Icon = tool.icon;
            const cardContent = (
              <Card
                key={tool.title}
                className={`transition hover:border-primary ${tool.disabled ? "opacity-60 pointer-events-none" : ""}`}
              >
                <CardHeader>
                  <div className="flex items-center justify-between">
                    <Icon className="h-5 w-5 text-primary" />
                    {tool.badge ? <Badge variant="outline">{tool.badge}</Badge> : null}
                  </div>
                  <CardTitle className="mt-4">{tool.title}</CardTitle>
                  <CardDescription>{tool.description}</CardDescription>
                </CardHeader>
                <CardContent>
                  <Button variant="outline" disabled={tool.disabled}>
                    {tool.disabled ? "Coming soon" : "Open"}
                  </Button>
                </CardContent>
              </Card>
            );

            return tool.href && !tool.disabled ? (
              <Link key={tool.title} href={tool.href} className="block">
                {cardContent}
              </Link>
            ) : (
              cardContent
            );
          })}
        </section>
      </div>
    </div>
  );
}
