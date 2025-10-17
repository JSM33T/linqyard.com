"use client";

import React from "react";
import { useUser, userHelpers } from "@/contexts/UserContext";
import { Loader2 } from "lucide-react";
import { Users as UsersIcon, Link as LucideLink, Lock, Settings as SettingsIcon, BarChart3, CreditCard } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardHeader, CardTitle, CardContent, CardDescription } from "@/components/ui/card";
import Link from "next/link";

export default function AccountClient() {
  const { isInitialized, isAuthenticated, user } = useUser();

  if (!isInitialized) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-background text-muted-foreground">
        <Loader2 className="mr-2 h-5 w-5 animate-spin" />
        Loading&hellip;
      </div>
    );
  }

  if (!isAuthenticated) {
    return (
      <div className="min-h-screen bg-background py-20">
        <div className="container mx-auto px-4">
          <header className="mx-auto max-w-3xl text-center space-y-6">
            <Badge variant="secondary" className="px-4 py-2 text-sm inline-flex items-center justify-center">
              <UsersIcon className="mr-2 h-4 w-4" />
              Account
            </Badge>

            <h1 className="text-4xl font-bold tracking-tight md:text-5xl">Welcome to Linqyard</h1>

            <p className="text-lg text-muted-foreground">Sign in or create an account to manage your profile, links, and billing.</p>
          </header>

          <div className="mt-10 grid gap-6 md:grid-cols-2 lg:grid-cols-3">
            {(
              [
                {
                  title: "Sign up",
                  description: "Create a free account and start sharing links.",
                  href: "/account/signup",
                  icon: LucideLink,
                },
                {
                  title: "Log in",
                  description: "Already have an account? Sign in to access your dashboard.",
                  href: "/account/login",
                  icon: UsersIcon,
                },
                {
                  title: "Recover password",
                  description: "Forgot your password? Reset it quickly and securely.",
                  href: "/account/forgot-password",
                  icon: Lock,
                },
              ] as const
            ).map((action) => {
              const Icon = action.icon as React.ElementType;
              const card = (
                <Card key={action.title} className="transition hover:border-primary">
                  <CardHeader>
                    <div className="flex items-center justify-between">
                      <Icon className="h-5 w-5 text-primary" />
                    </div>
                    <CardTitle className="mt-4">{action.title}</CardTitle>
                    <CardDescription>{action.description}</CardDescription>
                  </CardHeader>
                  <CardContent>
                    <Button variant="outline">Open</Button>
                  </CardContent>
                </Card>
              );

              return (
                <Link key={action.title} href={action.href} className="block">
                  {card}
                </Link>
              );
            })}
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="container mx-auto py-12">
      <div className="mx-auto max-w-4xl">
        <h1 className="text-2xl font-semibold">Hello, {userHelpers.getDisplayName(user) ?? user?.email}</h1>
        <p className="mt-2 text-muted-foreground">Use the options below to manage your account.</p>

        <div className="mt-6 grid gap-6 md:grid-cols-2 lg:grid-cols-3">
          {(
            [
              {
                title: "Profile",
                description: "View and edit your profile information.",
                href: "/account/profile",
                icon: UsersIcon,
              },
              {
                title: "Links",
                description: "Manage your public links and groups.",
                href: "/account/links",
                icon: LucideLink,
              },
              {
                title: "Security",
                description: "Change password and manage sessions.",
                href: "/account/security",
                icon: Lock,
              },
              {
                title: "Settings",
                description: "Account preferences and integrations.",
                href: "/account/settings",
                icon: SettingsIcon,
              },
              {
                title: "Insights",
                description: "View analytics and engagement.",
                href: "/account/insights",
                icon: BarChart3,
              },
              {
                title: "Billing",
                description: "Manage plan and payment methods.",
                href: "/plans",
                icon: CreditCard,
              },
            ] as const
          ).map((tool) => {
            const Icon = tool.icon as React.ElementType;
            const card = (
              <Card key={tool.title} className="transition hover:border-primary">
                <CardHeader>
                  <div className="flex items-center justify-between">
                    <Icon className="h-5 w-5 text-primary" />
                    {/* no badge for these account options */}
                  </div>
                  <CardTitle className="mt-4">{tool.title}</CardTitle>
                  <CardDescription>{tool.description}</CardDescription>
                </CardHeader>
                <CardContent>
                  <Button variant="outline">Open</Button>
                </CardContent>
              </Card>
            );

            return tool.href ? (
              <Link key={tool.title} href={tool.href} className="block">
                {card}
              </Link>
            ) : (
              card
            );
          })}
        </div>
      </div>
    </div>
  );
}
