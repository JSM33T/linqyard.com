"use client";

import { motion } from "framer-motion";
import Link from "next/link";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Separator } from "@/components/ui/separator";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { BookOpen, Link2, Shield, ExternalLink } from "lucide-react";

const sections = [
  { id: "getting-started", title: "Getting Started", icon: <BookOpen className="h-4 w-4" /> },
  { id: "managing-links", title: "Managing Links & CTAs", icon: <Link2 className="h-4 w-4" /> },
  { id: "insights-privacy", title: "Insights & Privacy", icon: <Shield className="h-4 w-4" /> },
];

// Motion variants
const headerVariants = {
  hidden: { y: -10, opacity: 0 },
  visible: { y: 0, opacity: 1, transition: { duration: 0.45 } },
};

const sidebarList = {
  hidden: {},
  visible: {
    transition: { staggerChildren: 0.08, delayChildren: 0.12 },
  },
};

const sidebarItem = {
  hidden: { x: -12, opacity: 0 },
  visible: { x: 0, opacity: 1, transition: { type: "spring", stiffness: 300, damping: 24 } },
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
  return (
    <div className="h-full flex flex-col">
      <div className="px-3 py-3">
        <Badge variant="secondary" className="px-2">Docs</Badge>
        <h2 className="mt-2 text-lg font-semibold">linqyard</h2>
        <p className="text-sm text-muted-foreground">Demo documentation</p>
      </div>
      <Separator />
      <div className="flex-1 overflow-auto">
        <motion.nav className="p-2" initial="hidden" whileInView="visible" viewport={{ once: true }} variants={sidebarList}>
          {sections.map((s) => (
            <motion.a
              key={s.id}
              href={`#${s.id}`}
              className="flex items-center gap-2 rounded-md px-3 py-2 text-sm hover:bg-muted transition-colors"
              variants={sidebarItem as any}
            >
              <span className="text-muted-foreground">{s.icon}</span>
              <span>{s.title}</span>
            </motion.a>
          ))}
        </motion.nav>
      </div>
      <Separator />
      <div className="p-3 text-xs text-muted-foreground">
        Need help? <a href="mailto:support@linqyard.com" className="underline">Email support</a>
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
            <Link href="/" className="font-semibold">linqyard</Link>
            <span className="mx-2 text-muted-foreground">/</span>
            <span className="text-muted-foreground">Docs</span>
          </div>
          <div className="hidden lg:block">
            <Badge variant="secondary">Demo • Neutral copy</Badge>
          </div>
        </div>
      </motion.header>

      {/* Body */}
      <div className="container mx-auto grid lg:grid-cols-[260px_1fr] gap-6 px-4 py-8">
        {/* Desktop sidebar */}
        <aside className="hidden lg:block sticky top-[68px] h-[calc(100vh-76px)] border rounded-md">
          <SidebarNav />
        </aside>

        {/* Content */}
        <main className="min-w-0">
          {/* Mobile sidebar substitute */}
          <div className="lg:hidden mb-6 border rounded-md">
            <SidebarNav />
          </div>

            {/* Intro */}
            <motion.div className="mb-6" initial="hidden" whileInView="visible" viewport={{ once: true }} variants={sectionVariants}>
              <h1 className="text-3xl md:text-5xl font-bold tracking-tight">Documentation</h1>
              <p className="mt-2 text-muted-foreground max-w-2xl">
                Short, practical guidance to get started with the linqyard demo.
                The docs intentionally use neutral tone—no bold claims.
              </p>
            </motion.div>

          <div className="space-y-10">
            {/* Getting Started */}
            <motion.section id="getting-started" className="scroll-mt-28" initial="hidden" whileInView="visible" viewport={{ once: true }} variants={sectionVariants}>
              <Card className="shadow-sm">
                <CardHeader>
                  <CardTitle className="flex items-center gap-2">
                    <BookOpen className="h-5 w-5 text-primary" /> Getting Started
                  </CardTitle>
                  <CardDescription>Set up a page and share your single link.</CardDescription>
                </CardHeader>
                <CardContent className="space-y-4">
                  <ol className="list-decimal pl-5 space-y-2 text-sm">
                    <li>Open the demo and create your profile page.</li>
                    <li>Add a few links and CTAs (e.g., portfolio, contact, latest project).</li>
                    <li>Copy your page URL and share it across socials.</li>
                  </ol>
                  <Separator />
                  <div>
                    <p className="mb-2 text-sm font-medium">Example URL structure</p>
                    <pre className="rounded-md border bg-muted/30 p-3 text-xs overflow-x-auto">
                      <code>https://linqyard.com/u/&lt;your-handle&gt;</code>
                    </pre>
                  </div>
                  <div className="text-xs text-muted-foreground">
                    Note: This is a demo. Features and copy may change.
                  </div>
                </CardContent>
              </Card>
            </motion.section>

            {/* Managing Links & CTAs */}
            <motion.section id="managing-links" className="scroll-mt-28" initial="hidden" whileInView="visible" viewport={{ once: true }} variants={sectionVariants}>
              <Card className="shadow-sm">
                <CardHeader>
                  <CardTitle className="flex items-center gap-2">
                    <Link2 className="h-5 w-5 text-primary" /> Managing Links & CTAs
                  </CardTitle>
                  <CardDescription>Keep things tidy and easy to update.</CardDescription>
                </CardHeader>
                <CardContent className="space-y-3 text-sm">
                  <ul className="list-disc pl-5 space-y-1">
                    <li>Reorder items to prioritize what matters.</li>
                    <li>Toggle visibility for seasonal campaigns.</li>
                    <li>Use concise titles; add a short description if needed.</li>
                  </ul>
                  <Separator />
                  <div>
                    <p className="mb-2 font-medium">Tip</p>
                    <p className="text-muted-foreground">
                      Group related links (e.g., “Projects”) to reduce clutter.
                    </p>
                  </div>
                </CardContent>
              </Card>
            </motion.section>

            {/* Insights & Privacy */}
            <motion.section id="insights-privacy" className="scroll-mt-28" initial="hidden" whileInView="visible" viewport={{ once: true }} variants={sectionVariants}>
              <Card className="shadow-sm">
                <CardHeader>
                  <CardTitle className="flex items-center gap-2">
                    <Shield className="h-5 w-5 text-primary" /> Insights & Privacy
                  </CardTitle>
                  <CardDescription>
                    Understand clicks with a privacy-aware mindset.
                  </CardDescription>
                </CardHeader>
                <CardContent className="space-y-3 text-sm">
                  <ul className="list-disc pl-5 space-y-1">
                    <li>Basic click counts can be enabled to see engagement.</li>
                    <li>For this demo, avoid uploading production-sensitive data.</li>
                    <li>Check the About page and docs for updates as features evolve.</li>
                  </ul>
                  <Separator />
                  <div className="flex items-center gap-2">
                    <Link className="inline-flex items-center text-sm underline" href="/about">
                      Learn more on About <ExternalLink className="ml-1 h-3.5 w-3.5" />
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
                      <h3 className="text-xl md:text-2xl font-bold">Ready to try the demo?</h3>
                      <p className="mt-1 text-primary-foreground/80 text-sm">
                        Create a page, add links, and share one URL.
                      </p>
                    </div>
                    <div className="flex gap-3">
                      <Button size="lg" variant="secondary">Open demo</Button>
                      <Button asChild size="lg" variant="outline" className="bg-transparent text-primary-foreground border-primary-foreground/40 hover:bg-primary-foreground/10">
                        <Link href="/about">About</Link>
                      </Button>
                    </div>
                  </div>
                  <p className="mt-3 text-xs text-primary-foreground/80">
                    This is an indicative demo. Features and scope may change.
                  </p>
                </CardContent>
              </Card>
            </motion.section>
          </div>
        </main>
      </div>

      {/* Page footer */}
      <motion.footer className="container mx-auto px-4 pb-8 text-center text-muted-foreground" initial="hidden" whileInView="visible" viewport={{ once: true }} variants={footerVariants}>
        <p>© {new Date().getFullYear()} linqyard. Demo documentation for illustrative purposes only.</p>
      </motion.footer>
    </div>
  );
}
