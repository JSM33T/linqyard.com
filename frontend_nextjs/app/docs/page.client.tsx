"use client";

import { motion } from "framer-motion";
import Link from "next/link";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Separator } from "@/components/ui/separator";
import { TableOfContents, type TocHeading } from "@/components/blog/table-of-contents";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import {
  BookOpen,
  Link2,
  Shield,
  ExternalLink,
  UserCheck,
  Globe,
  BarChart3,
  Palette,
  QrCode,
} from "lucide-react";

const sections = [
  { id: "onboarding", title: "Onboarding", icon: <UserCheck className="h-4 w-4" /> },
  { id: "account-security", title: "Account & Security", icon: <Shield className="h-4 w-4" /> },
  { id: "account-recovery", title: "Account Recovery", icon: <Shield className="h-4 w-4" /> },
  { id: "build-your-page", title: "Build Your Page", icon: <Link2 className="h-4 w-4" /> },
  { id: "customization", title: "Customization by Plan", icon: <Palette className="h-4 w-4" /> },
  { id: "analytics", title: "Analytics by Plan", icon: <BarChart3 className="h-4 w-4" /> },
  { id: "share-subdomains", title: "Sharing & Subdomains", icon: <Globe className="h-4 w-4" /> },
  { id: "tips-support", title: "Tips & Support", icon: <BookOpen className="h-4 w-4" /> },
];

// Motion variants
const headerVariants = {
  hidden: { y: -10, opacity: 0 },
  visible: { y: 0, opacity: 1, transition: { duration: 0.45 } },
};

const sectionVariants = {
  hidden: { y: 20, opacity: 0 },
  visible: { y: 0, opacity: 1, transition: { duration: 0.45 } },
};

const footerVariants = {
  hidden: { opacity: 0, y: 8 },
  visible: { opacity: 1, y: 0, transition: { duration: 0.45, delay: 0.1 } },
};

function SidebarNav() {
  const tocHeadings: TocHeading[] = sections.map((section) => ({
    id: section.id,
    title: section.title,
    depth: 2,
    label: (
      <span className="flex items-center gap-2">
        {section.icon}
        <span>{section.title}</span>
      </span>
    ),
  }));

  return (
    <div className="flex h-full flex-col rounded-md border border-primary/40 bg-background/80">
      <div className="px-3 py-3">
        <Badge variant="secondary" className="px-2">Docs</Badge>
        <h2 className="mt-2 text-lg font-semibold">LinqYard</h2>
        <p className="text-sm text-muted-foreground">Product documentation</p>
      </div>
      <Separator />
      <div className="flex-1 overflow-auto p-3">
        <TableOfContents
          headings={tocHeadings}
          className="border-0 bg-transparent p-0 text-sm [&_p]:hidden [&_ul]:mt-2"
        />
      </div>
      <Separator />
      <div className="p-3 text-xs text-muted-foreground">
        Need help? <a href="mailto:mail@jsm33t.com" className="underline">Email support</a>
      </div>
    </div>
  );
}

export default function DocsPageClient() {
  return (
    <div className="min-h-screen bg-background">
      {/* Header */}
      <motion.header
        className="sticky top-0 z-20 border-b bg-background/80 backdrop-blur supports-[backdrop-filter]:bg-background/60"
        initial="hidden"
        animate="visible"
        variants={headerVariants}
      >
        <div className="container mx-auto flex items-center justify-between px-4 py-3">
          <div className="flex items-center gap-2">
            <Link href="/" className="font-semibold">LinqYard</Link>
            <span className="mx-2 text-muted-foreground">/</span>
            <span className="text-muted-foreground">Docs</span>
          </div>
          <div className="hidden lg:block">
            <Badge variant="secondary">Onboarding • Product Guide</Badge>
          </div>
        </div>
      </motion.header>

      {/* Body */}
      <div className="container mx-auto grid lg:grid-cols-[260px_1fr] gap-6 px-4 py-8">
        {/* Desktop sidebar */}
        <aside className="hidden lg:block sticky top-[68px] h-[calc(100vh-76px)]">
          <SidebarNav />
        </aside>

        {/* Content */}
        <main className="min-w-0">
          {/* Mobile sidebar substitute */}
          <div className="lg:hidden mb-6">
            <SidebarNav />
          </div>

          {/* Intro */}
          <motion.div className="mb-6" initial="hidden" whileInView="visible" viewport={{ once: true }} variants={sectionVariants}>
            <h1 className="text-3xl md:text-5xl font-bold tracking-tight">LinqYard Documentation</h1>
            <p className="mt-2 text-muted-foreground max-w-2xl">
              A concise, practical guide to help you onboard quickly: create an account, claim your subdomain,
              add links & groups, and start tracking performance.
            </p>
          </motion.div>

          <div className="space-y-10">
            {/* Onboarding */}
            <motion.section id="onboarding" className="scroll-mt-28" initial="hidden" whileInView="visible" viewport={{ once: true }} variants={sectionVariants}>
              <Card className="shadow-sm">
                <CardHeader>
                  <CardTitle className="flex items-center gap-2">
                    <UserCheck className="h-5 w-5 text-primary" /> Onboarding
                  </CardTitle>
                  <CardDescription>Account creation, verification, and first-time setup.</CardDescription>
                </CardHeader>
                <CardContent className="space-y-4 text-sm">
                  <ol className="list-decimal pl-5 space-y-2">
                    <li>
                      <span className="font-medium">Sign up</span> with email & password, or use Google or GitHub to sign in. Verify your email to secure your account.
                    </li>
                    <li>
                      <span className="font-medium">Choose your handle</span> (becomes your subdomain). You’ll get a unique URL like
                      <code className="ml-1 rounded bg-muted px-1">username.linqyard.com</code>.
                    </li>
                    <li>
                      <span className="font-medium">Complete profile</span>: add display name, avatar, and a short bio so visitors know it’s you.
                    </li>
                    <li>
                      <span className="font-medium">Add your first links</span>: create up to 12 links and 2 groups on the Free plan (unlimited on Plus). Drag to reorder; toggle visibility anytime.
                    </li>
                    <li>
                      <span className="font-medium">Share</span> your LinqYard URL on social profiles, email signatures, and bios. QR codes are coming soon.
                    </li>
                  </ol>
                  <Separator />
                  <div>
                    <p className="mb-2 text-sm font-medium">Example URLs</p>
                    <pre className="rounded-md border bg-muted/30 p-3 text-xs overflow-x-auto">
                      <code>https://username.linqyard.comhttps://linqyard.com/u/&lt;your-handle&gt;</code>
                    </pre>
                  </div>
                </CardContent>
              </Card>
            </motion.section>

            {/* Account & Security */}
            <motion.section id="account-security" className="scroll-mt-28" initial="hidden" whileInView="visible" viewport={{ once: true }} variants={sectionVariants}>
              <Card className="shadow-sm">
                <CardHeader>
                  <CardTitle className="flex items-center gap-2">
                    <Shield className="h-5 w-5 text-primary" /> Account & Security
                  </CardTitle>
                  <CardDescription>Best practices to keep your LinqYard secure.</CardDescription>
                </CardHeader>
                <CardContent className="space-y-3 text-sm">
                  <ul className="list-disc pl-5 space-y-1">
                    <li>Email verification is required to publish your page.</li>
                    <li>Use a strong password and update it if you suspect a compromise.</li>
                    <li>Two-Factor Authentication (2FA) is coming soon for additional protection.</li>
                    <li>Social logins: Google and GitHub are available; you can also sign in with email/password. Your links and analytics remain the same regardless of which sign-in method you use.</li>
                  </ul>
                </CardContent>
              </Card>
            </motion.section>

            {/* Account Recovery */}
            <motion.section id="account-recovery" className="scroll-mt-28" initial="hidden" whileInView="visible" viewport={{ once: true }} variants={sectionVariants}>
              <Card className="shadow-sm">
                <CardHeader>
                  <CardTitle className="flex items-center gap-2">
                    <Shield className="h-5 w-5 text-primary" /> Account Recovery
                  </CardTitle>
                  <CardDescription>Reset passwords and recover access to your account.</CardDescription>
                </CardHeader>
                <CardContent className="space-y-3 text-sm">
                  <ul className="list-disc pl-5 space-y-1">
                    <li>
                      <span className="font-medium">Forgot password</span>: Use the &quot;Forgot password&quot; link on the sign-in page to request a password reset email. Follow the instructions in the email to set a new password.
                    </li>
                    <li>
                      Reset links are one-time use and expire after a short period for security. If a link expires, request a new reset from the sign-in page.
                    </li>
                    <li>
                      If you signed up using Google or GitHub, account recovery is primarily handled by those providers—use their recovery flows or contact their support if needed.
                    </li>
                    <li>
                      Still stuck? Contact support at <a className="underline" href="mailto:mail@jsm33t.com">mail@jsm33t.com</a> and include as much detail as you can (email, handle, and what happened).
                    </li>
                  </ul>
                </CardContent>
              </Card>
            </motion.section>

            {/* Build Your Page */}
            <motion.section id="build-your-page" className="scroll-mt-28" initial="hidden" whileInView="visible" viewport={{ once: true }} variants={sectionVariants}>
              <Card className="shadow-sm">
                <CardHeader>
                  <CardTitle className="flex items-center gap-2">
                    <Link2 className="h-5 w-5 text-primary" /> Build Your Page
                  </CardTitle>
                  <CardDescription>Create, group, and organize links for a clean layout.</CardDescription>
                </CardHeader>
                <CardContent className="space-y-3 text-sm">
                  <ul className="list-disc pl-5 space-y-1">
                    <li>Create links and optional groups (e.g., Social, Work, Projects).</li>
                    <li>Drag-and-drop to reorder links and groups.</li>
                    <li>Toggle visibility without deleting to run seasonal promos.</li>
                    <li>Use concise titles; add short descriptions when needed.</li>
                  </ul>
                  <Separator />
                  <div className="flex items-center gap-2 text-sm">
                    <Link className="inline-flex items-center underline" href="/about">
                      Why LinqYard <ExternalLink className="ml-1 h-3.5 w-3.5" />
                    </Link>
                  </div>
                </CardContent>
              </Card>
            </motion.section>

            {/* Customization by Plan */}
            <motion.section id="customization" className="scroll-mt-28" initial="hidden" whileInView="visible" viewport={{ once: true }} variants={sectionVariants}>
              <Card className="shadow-sm">
                <CardHeader>
                  <CardTitle className="flex items-center gap-2">
                    <Palette className="h-5 w-5 text-primary" /> Customization by Plan
                  </CardTitle>
                  <CardDescription>Design options vary by plan.</CardDescription>
                </CardHeader>
                <CardContent className="space-y-3 text-sm">
                  <ul className="list-disc pl-5 space-y-1">
                    <li><span className="font-medium">Free</span>: 2 basic themes (coming soon). No custom theming.</li>
                    <li><span className="font-medium">Plus</span>: Custom backgrounds, gradients, and theme color options.</li>
                  </ul>
                  <div className="text-xs text-muted-foreground">Advanced card layouts (e.g., rectangular, square, 1:1 icons) are planned for a future tier.</div>
                </CardContent>
              </Card>
            </motion.section>

            {/* Analytics by Plan */}
            <motion.section id="analytics" className="scroll-mt-28" initial="hidden" whileInView="visible" viewport={{ once: true }} variants={sectionVariants}>
              <Card className="shadow-sm">
                <CardHeader>
                  <CardTitle className="flex items-center gap-2">
                    <BarChart3 className="h-5 w-5 text-primary" /> Analytics by Plan
                  </CardTitle>
                  <CardDescription>Understand performance and optimize your page.</CardDescription>
                </CardHeader>
                <CardContent className="space-y-3 text-sm">
                  <ul className="list-disc pl-5 space-y-1">
                    <li><span className="font-medium">Free</span>: Basic analytics — total clicks, per-link click counts, and top-performing links.</li>
                    <li><span className="font-medium">Plus</span>: Advanced analytics — device metrics, location insights, and engagement by time of day and day of week.</li>
                  </ul>
                  <p className="mt-2 text-sm">Analytics and your links remain consistent regardless of how you sign in (email, Google, or GitHub).</p>
                </CardContent>
              </Card>
            </motion.section>

            {/* Sharing & Subdomains */}
            <motion.section id="share-subdomains" className="scroll-mt-28" initial="hidden" whileInView="visible" viewport={{ once: true }} variants={sectionVariants}>
              <Card className="shadow-sm">
                <CardHeader>
                  <CardTitle className="flex items-center gap-2">
                    <Globe className="h-5 w-5 text-primary" /> Sharing & Subdomains
                  </CardTitle>
                  <CardDescription>Make your LinqYard easy to find and share.</CardDescription>
                </CardHeader>
                <CardContent className="space-y-4 text-sm">
                  <ul className="list-disc pl-5 space-y-1">
                    <li>Each profile gets a unique subdomain: <code className="rounded bg-muted px-1">username.linqyard.com</code>.</li>
                    <li>Copy and share your URL across social bios and posts.</li>
                    <li>QR codes for profiles are coming soon for quick offline sharing.</li>
                  </ul>
                  <Separator />
                  <div className="flex items-center gap-2 text-sm">
                    <Button asChild size="sm" variant="secondary">
                      <Link href="/account/signup">Create account</Link>
                    </Button>
                    <Button asChild size="sm" variant="outline">
                      <Link href="/plans">View plans</Link>
                    </Button>
                    <span className="inline-flex items-center gap-1 text-muted-foreground text-xs">
                      <QrCode className="h-3.5 w-3.5" /> QR sharing soon
                    </span>
                  </div>
                </CardContent>
              </Card>
            </motion.section>

            {/* Tips & Support */}
            <motion.section id="tips-support" className="scroll-mt-28" initial="hidden" whileInView="visible" viewport={{ once: true }} variants={sectionVariants}>
              <Card className="shadow-sm">
                <CardHeader>
                  <CardTitle className="flex items-center gap-2">
                    <BookOpen className="h-5 w-5 text-primary" /> Tips & Support
                  </CardTitle>
                  <CardDescription>Helpful pointers and where to get help.</CardDescription>
                </CardHeader>
                <CardContent className="space-y-3 text-sm">
                  <ul className="list-disc pl-5 space-y-1">
                    <li>Keep titles short and actionable (e.g., “Watch: New Demo”).</li>
                    <li>Put your most important links at the top; use groups to reduce clutter.</li>
                    <li>Review analytics weekly to refine your layout.</li>
                    <li>Avoid sensitive data; treat your public page as a storefront.</li>
                  </ul>
                  <Separator />
                  <div className="flex items-center gap-2">
                    <Link className="inline-flex items-center text-sm underline" href="mailto:mail@jsm33t.com">
                      Contact support <ExternalLink className="ml-1 h-3.5 w-3.5" />
                    </Link>
                  </div>
                </CardContent>
              </Card>
            </motion.section>

            {/* Footer callout */}
            <motion.section className="pb-10" initial="hidden" whileInView="visible" viewport={{ once: true }} variants={sectionVariants}>
              <Card className="bg-primary text-primary-foreground shadow-2xl overflow-hidden">
                <CardContent className="p-6 md:p-8">
                  <div className="flex flex-col md:flex-row items-start md:items-center justify-between gap-4">
                    <div>
                      <h3 className="text-xl md:text-2xl font-bold">Ready to create your LinqYard?</h3>
                      <p className="mt-1 text-primary-foreground/80 text-sm">
                        Sign up, claim your subdomain, add links & groups, and start sharing.
                      </p>
                    </div>
                    <div className="flex gap-3">
                      <Button asChild size="lg" variant="secondary">
                        <Link href="/account/signup">Create account</Link>
                      </Button>
                      <Button asChild size="lg" variant="outline" className="bg-transparent text-primary-foreground border-primary-foreground/40 hover:bg-primary-foreground/10">
                        <Link href="/plans">View plans</Link>
                      </Button>
                    </div>
                  </div>
                  <p className="mt-3 text-xs text-primary-foreground/80">
                    Features and availability may evolve as we iterate. Follow update notes in the dashboard.
                  </p>
                </CardContent>
              </Card>
            </motion.section>
          </div>
        </main>
      </div>
    </div>
  );
}
