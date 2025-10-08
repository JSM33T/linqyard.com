"use client";

import { motion } from "framer-motion";
import Link from "next/link";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Separator } from "@/components/ui/separator";
import { ScrollArea } from "@/components/ui/scroll-area";
import {
  ArrowRight,
  CheckCircle,
  Sparkles,
  Shield,
  Users,
  Globe,
  Wand2,
  Link2,
  BarChart3,
  Mail,
  ExternalLink,
} from "lucide-react";

// ---- Motion presets ----
const container = {
  hidden: { opacity: 0 },
  visible: {
    opacity: 1,
    transition: { delayChildren: 0.2, staggerChildren: 0.12 },
  },
};
const item = { hidden: { y: 16, opacity: 0 }, visible: { y: 0, opacity: 1 } };

// ---- Sample data ----
const features = [
  {
    icon: <Shield className="h-6 w-6" />,
    title: "Security‑first (demo)",
    desc: "Sensible defaults. Avoid uploading production‑sensitive data to this demo.",
  },
  {
    icon: <Users className="h-6 w-6" />,
    title: "Simple link management",
    desc: "Create, edit, and reorder links and CTAs without clutter.",
  },
  {
    icon: <BarChart3 className="h-6 w-6" />,
    title: "Basic insights (optional)",
    desc: "Lightweight click counts to understand engagement.",
  },
  {
    icon: <Globe className="h-6 w-6" />,
    title: "Works across devices",
    desc: "Responsive layouts for modern browsers and screen sizes.",
  },
  {
    icon: <Wand2 className="h-6 w-6" />,
    title: "Customizable",
    desc: "Choose themes, layouts, and CTA types to match your brand.",
  },
  {
    icon: <Mail className="h-6 w-6" />,
    title: "Docs & email support",
    desc: "Short guides to get started, with friendly email support.",
  },
];

// ---- Mobile Mockup ----
function DeviceMockup() {
  return (
    <motion.div
      className="relative mx-auto w-full max-w-[320px] md:max-w-[360px]"
      initial={{ y: 10, opacity: 0 }}
      animate={{ y: 0, opacity: 1 }}
      transition={{ duration: 0.8, ease: "easeOut" }}
    >
      {/* phone frame */}
      <div className="relative rounded-[3rem] border bg-gradient-to-b from-background/40 to-background/70 shadow-2xl backdrop-blur supports-[backdrop-filter]:bg-background/60">
        {/* top bar / notch */}
        <div className="absolute left-1/2 top-2 z-20 h-6 w-36 -translate-x-1/2 rounded-full bg-muted" />
        <div className="p-4 pt-8">
          <div className="rounded-[2.2rem] border overflow-hidden">
            {/* screen */}
            <div className="relative bg-gradient-to-b from-muted/50 to-background">
              <div className="p-4">
                {/* profile header */}
                <div className="flex items-center gap-3">
                  <div className="h-12 w-12 shrink-0 rounded-full bg-primary/10 grid place-items-center">
                    <Sparkles className="h-5 w-5 text-primary" />
                  </div>
                  <div>
                    <p className="text-sm text-muted-foreground">linqyard</p>
                    <p className="text-base font-semibold">@yourname</p>
                  </div>
                </div>

                <Separator className="my-4" />

                {/* links list */}
                <ScrollArea className="h-[360px] pr-2">
                  <div className="space-y-3">
                    {[
                      { label: "Portfolio", icon: <ExternalLink className="h-4 w-4" /> },
                      { label: "YouTube", icon: <ExternalLink className="h-4 w-4" /> },
                      { label: "Instagram", icon: <ExternalLink className="h-4 w-4" /> },
                      { label: "Newsletter", icon: <ExternalLink className="h-4 w-4" /> },
                      { label: "Contact", icon: <ExternalLink className="h-4 w-4" /> },
                      { label: "Latest project", icon: <ExternalLink className="h-4 w-4" /> },
                      { label: "Speaking", icon: <ExternalLink className="h-4 w-4" /> },
                    ].map((l, i) => (
                      <button
                        key={i}
                        className="group w-full rounded-xl border bg-background px-4 py-3 text-left shadow-sm transition-all hover:shadow-md focus-visible:outline-none"
                      >
                        <div className="flex items-center justify-between">
                          <span className="inline-flex items-center gap-2 font-medium">
                            <Link2 className="h-4 w-4 text-muted-foreground" />
                            {l.label}
                          </span>
                          <span className="opacity-60 transition-opacity group-hover:opacity-100">
                            {l.icon}
                          </span>
                        </div>
                      </button>
                    ))}
                  </div>
                </ScrollArea>

                <Separator className="my-4" />

                {/* foot chips */}
                <div className="flex flex-wrap gap-2 text-xs">
                  <Badge variant="secondary">Demo</Badge>
                  <Badge variant="secondary">Less chaos</Badge>
                  <Badge variant="secondary">More Clicks</Badge>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* subtle glow */}
      <div className="pointer-events-none absolute -inset-10 -z-10 blur-2xl opacity-50" style={{
        background: "radial-gradient(400px 200px at 50% 100%, hsl(var(--primary)/0.25), transparent)",
      }} />
    </motion.div>
  );
}

export default function HomeClient() {
  return (
    <div className="min-h-screen bg-background relative">
       <div
        aria-hidden
        className="pointer-events-none absolute inset-0 -z-10"
        style={{
          background:
            "linear-gradient(to bottom right, hsl(var(--primary)/0.05), transparent 70%)",
        }}
      />
      {/* decorative grid / gradient */}
      <div
        aria-hidden
        className="pointer-events-none absolute inset-0 -z-10"
        style={{
          background:
            "radial-gradient(60% 50% at 50% -10%, hsl(var(--primary)/0.10), transparent 70%), radial-gradient(30% 30% at 10% 20%, hsl(var(--primary)/0.08), transparent 60%), radial-gradient(30% 30% at 90% 20%, hsl(var(--primary)/0.08), transparent 60%)",
        }}
      />

      {/* Hero */}
      <motion.section
        className="container mx-auto px-4 pt-20 pb-10 md:py-24"
        variants={container}
        initial="hidden"
        animate="visible"
      >
        <div className="grid lg:grid-cols-2 gap-10 items-center">
          <motion.div className="space-y-6" variants={item}>
            <Badge variant="secondary" className="text-sm px-3 py-1.5 inline-flex items-center">
              <Link2 className="h-4 w-4 mr-2" /> Less Chaos • More Clicks
            </Badge>

            <motion.h1
              variants={item}
              className="text-4xl md:text-6xl lg:text-7xl font-bold tracking-tight"
            >
              Stop juggling links. Share one.
            </motion.h1>

            <motion.p variants={item} className="text-lg md:text-xl text-muted-foreground max-w-2xl">
              Share a single page for all your socials and actions. Update in minutes,
              keep things simple and privacy‑aware.
            </motion.p>

            <motion.div variants={item} className="flex flex-col sm:flex-row gap-3">
              <Button size="lg" className="text-base px-7">
                <Link className="inline-flex items-center" href={"/account/login"}>Join Now<ArrowRight className="ml-2 h-5 w-5" /></Link>
              </Button>
              <Button asChild variant="outline" size="lg" className="text-base px-7">
                <Link href="/about" className="inline-flex items-center">Learn more</Link>
              </Button>
            </motion.div>

            <motion.div variants={item} className="flex flex-wrap items-center gap-4 text-sm text-muted-foreground pt-2">
              <span className="inline-flex items-center gap-2"><CheckCircle className="h-4 w-4 text-green-600 dark:text-green-400" /> No credit card for demo</span>
              <span className="inline-flex items-center gap-2"><CheckCircle className="h-4 w-4 text-green-600 dark:text-green-400" /> Email support</span>
              <span className="inline-flex items-center gap-2"><CheckCircle className="h-4 w-4 text-green-600 dark:text-green-400" /> Sensible defaults</span>
            </motion.div>
          </motion.div>

          {/* phone mockup */}
          <DeviceMockup />
        </div>
      </motion.section>

      {/* Logos / social proof (neutral) */}
      <section className="container mx-auto px-4 pb-6 md:pb-10">
        <div className="flex flex-wrap items-center justify-center gap-3 md:gap-6 text-xs md:text-sm text-muted-foreground">
          <span className="opacity-70">Used in demos by</span>
          <span className="rounded-full border px-3 py-1">Design teams</span>
          <span className="rounded-full border px-3 py-1">Creators</span>
          <span className="rounded-full border px-3 py-1">Student groups</span>
          <span className="rounded-full border px-3 py-1">Small projects</span>
        </div>
      </section>

      {/* Feature grid */}
      <motion.section
        className="container mx-auto px-4 py-12 md:py-20"
        initial={{ opacity: 0 }}
        whileInView={{ opacity: 1 }}
        transition={{ duration: 0.6 }}
        viewport={{ once: true }}
      >
        <div className="text-center mb-12">
          <h2 className="text-3xl md:text-5xl font-bold">Practical basics</h2>
          <p className="mt-3 text-muted-foreground max-w-2xl mx-auto">
            Focused features that cover the essentials. No buzzwords—just what you need to get started.
          </p>
        </div>

        <div className="grid md:grid-cols-2 lg:grid-cols-3 gap-6">
          {features.map((f, i) => (
            <motion.div
              key={i}
              initial={{ opacity: 0, y: 24 }}
              whileInView={{ opacity: 1, y: 0 }}
              transition={{ duration: 0.45, delay: i * 0.08 }}
              viewport={{ once: true }}
              whileHover={{ y: -3 }}
              className="h-full"
            >
              <Card className="h-full shadow-md hover:shadow-lg transition-shadow">
                <CardHeader>
                  <div className="text-primary mb-3">{f.icon}</div>
                  <CardTitle>{f.title}</CardTitle>
                </CardHeader>
                <CardContent>
                  <CardDescription>{f.desc}</CardDescription>
                </CardContent>
              </Card>
            </motion.div>
          ))}
        </div>
      </motion.section>

      {/* CTA band */}
      <motion.section
        className="container mx-auto px-4 pb-16"
        initial={{ opacity: 0 }}
        whileInView={{ opacity: 1 }}
        transition={{ duration: 0.6 }}
        viewport={{ once: true }}
      >
        <Card className="shadow-2xl overflow-hidden bg-gradient-to-br from-background via-background to-background">
          <CardContent className="p-8 md:p-12">
            <div className="grid md:grid-cols-2 gap-8 items-center">
              <div>
                <h3 className="text-2xl md:text-4xl font-bold">Start with a demo</h3>
                <p className="mt-3 text-muted-foreground max-w-xl">
                  Explore the flow, edit links, and see how it feels to keep everything in one place.
                </p>
                <div className="mt-6 flex flex-col sm:flex-row gap-3">
                  <Button size="lg" className="text-base px-7">Try the demo</Button>
                  <Button asChild variant="outline" size="lg" className="text-base px-7">
                    <Link href="/docs">Read docs</Link>
                  </Button>
                </div>
                <p className="mt-4 text-xs text-muted-foreground">
                  This is a demo for evaluation. Features may change.
                </p>
              </div>

              <div className="relative">
                <div className="absolute -inset-6 -z-10 blur-2xl opacity-60" style={{
                  background: "radial-gradient(400px 200px at 50% 50%, hsl(var(--primary)/0.18), transparent)",
                }} />
                {/* something */}
              </div>
            </div>
          </CardContent>
        </Card>
      </motion.section>

      {/* Footer */}
      <footer className="container mx-auto px-4 pb-10 text-center text-muted-foreground">
        <p>© {new Date().getFullYear()} linqyard. This page is a demo for illustrative purposes only.</p>
      </footer>
    </div>
  );
}
