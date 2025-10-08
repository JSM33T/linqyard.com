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
import {
  Accordion,
  AccordionContent,
  AccordionItem,
  AccordionTrigger,
} from "@/components/ui/accordion";
import {
  Shield,
  Users,
  Sparkles,
  Globe,
  Lock,
  Code2,
  Server,
  Target,
  Mail,
  TrendingUp,
  CheckCircle,
  ExternalLink,
} from "lucide-react";

const containerVariants = {
  hidden: { opacity: 0 },
  visible: {
    opacity: 1,
    transition: { delayChildren: 0.2, staggerChildren: 0.15 },
  },
};

const itemVariants = {
  hidden: { y: 20, opacity: 0 },
  visible: { y: 0, opacity: 1 },
};

const principles = [
  {
    icon: <Shield className="h-6 w-6" />,
    title: "Privacy‑aware (demo)",
    desc:
      "Designed with sensible defaults. This demo is for evaluation—avoid uploading production‑sensitive data.",
  },
  {
    icon: <Target className="h-6 w-6" />,
    title: "Clarity over hype",
    desc: "Straightforward features, clear copy, and predictable behavior.",
  },
  {
    icon: <Users className="h-6 w-6" />,
    title: "Creator‑friendly",
    desc: "Manage links and CTAs without clutter. Start simple; grow gradually.",
  },
  {
    icon: <Globe className="h-6 w-6" />,
    title: "Works where you share",
    desc: "Responsive layouts for modern devices and browsers.",
  },
];

const tech = [
  {
    icon: <Code2 className="h-5 w-5" />,
    label: "Frontend",
    value: "Next.js • shadcn/ui • Framer Motion • Lucide",
  },
  {
    icon: <Lock className="h-5 w-5" />,
    label: "Auth (platform)",
    value: "JWT‑based sessions, optional Google Sign‑In",
  },
  {
    icon: <Server className="h-5 w-5" />,
    label: "Backend (platform)",
    value: ".NET API with conventional logging and CORS policies",
  },
];

const roadmap = [
  {
    when: "Now",
    items: [
      "Create & reorder links/CTAs",
      "Lightweight click counts (optional)",
      "Theme & layout presets",
    ],
  },
  {
    when: "Soon",
    items: [
      "Basic teams & roles",
      "Import from common socials",
      "Improved analytics summaries",
    ],
  },
  {
    when: "Exploring",
    items: [
      "UTM helpers",
      "QR sharing",
      "Bulk actions",
    ],
  },
];

export default function AboutClient() {
  return (
    <div className="min-h-screen bg-background">
      {/* Header / Intro */}
      <motion.section
        className="container mx-auto px-4 py-20 text-center"
        variants={containerVariants}
        initial="hidden"
        animate="visible"
      >
        <motion.div className="space-y-6 max-w-4xl mx-auto" variants={itemVariants}>
          <Badge variant="secondary" className="text-sm px-4 py-2">
            <Sparkles className="h-4 w-4 mr-2" /> About linqyard
          </Badge>

          <motion.h1
            className="text-4xl md:text-6xl font-bold tracking-tight"
            variants={itemVariants}
          >
            What is linqyard?
          </motion.h1>

          <motion.p
            className="text-lg md:text-xl text-muted-foreground"
            variants={itemVariants}
          >
            A focused way to organize all your social links and CTAs on a single page.
            Share one link. Update in minutes. Keep things clear and privacy‑aware.
          </motion.p>

          <motion.div
            className="flex flex-col sm:flex-row gap-3 justify-center items-center"
            variants={itemVariants}
          >
            <Button asChild size="lg">
              <Link href="/docs" className="inline-flex items-center">
                Read docs <ExternalLink className="ml-2 h-4 w-4" />
              </Link>
            </Button>
            <Button asChild variant="outline" size="lg">
              <a href="mailto:mail@linqyard.com" className="inline-flex items-center">
                Email support <Mail className="ml-2 h-4 w-4" />
              </a>
            </Button>
          </motion.div>

          <motion.div
            className="flex items-center justify-center gap-2 text-sm text-muted-foreground"
            variants={itemVariants}
          >
            <CheckCircle className="h-4 w-4" /> Neutral copy
            <CheckCircle className="h-4 w-4 ml-4" /> No bold claims
            <CheckCircle className="h-4 w-4 ml-4" /> Demo‑friendly
          </motion.div>
        </motion.div>
      </motion.section>

      {/* Principles */}
      <motion.section
        className="container mx-auto px-4 py-14"
        variants={containerVariants}
        initial="hidden"
        whileInView="visible"
        viewport={{ once: true }}
      >
        <motion.div className="text-center mb-12" variants={itemVariants}>
          <h2 className="text-3xl md:text-5xl font-bold">Principles</h2>
          <p className="mt-3 text-muted-foreground max-w-2xl mx-auto">
            A few guardrails that shape product decisions.
          </p>
        </motion.div>

        <div className="grid md:grid-cols-2 lg:grid-cols-4 gap-6">
          {principles.map((p, i) => (
            <motion.div
              key={i}
              initial={{ opacity: 0, y: 30 }}
              whileInView={{ opacity: 1, y: 0 }}
              transition={{ duration: 0.5, delay: i * 0.08 }}
              viewport={{ once: true }}
            >
              <Card className="h-full">
                <CardHeader>
                  <div className="text-primary mb-2">{p.icon}</div>
                  <CardTitle className="leading-tight">{p.title}</CardTitle>
                </CardHeader>
                <CardContent>
                  <CardDescription>{p.desc}</CardDescription>
                </CardContent>
              </Card>
            </motion.div>
          ))}
        </div>
      </motion.section>

      {/* How it works / Tech snapshot */}
      <motion.section
        className="container mx-auto px-4 py-14"
        initial={{ opacity: 0 }}
        whileInView={{ opacity: 1 }}
        transition={{ duration: 0.6 }}
        viewport={{ once: true }}
      >
        <div className="grid lg:grid-cols-2 gap-8 items-stretch">
          <Card className="shadow-lg">
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <Sparkles className="h-5 w-5 text-primary" /> How it works
              </CardTitle>
              <CardDescription>
                A lightweight flow designed to stay out of your way.
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="flex items-start gap-3">
                <span className="mt-1">1.</span>
                <div>
                  <p className="font-medium">Create your page</p>
                  <p className="text-sm text-muted-foreground">Add links and CTAs you want to share.</p>
                </div>
              </div>
              <Separator />
              <div className="flex items-start gap-3">
                <span className="mt-1">2.</span>
                <div>
                  <p className="font-medium">Share one URL</p>
                  <p className="text-sm text-muted-foreground">Post it on social profiles, bios, and campaigns.</p>
                </div>
              </div>
              <Separator />
              <div className="flex items-start gap-3">
                <span className="mt-1">3.</span>
                <div>
                  <p className="font-medium">Tweak in minutes</p>
                  <p className="text-sm text-muted-foreground">Reorder, toggle, or edit without heavy setup.</p>
                </div>
              </div>
            </CardContent>
          </Card>

          <Card className="shadow-lg">
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <Server className="h-5 w-5 text-primary" /> Tech snapshot
              </CardTitle>
              <CardDescription>
                An overview of the stack used in this demo and the broader platform.
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              {tech.map((t, i) => (
                <div key={i} className="flex items-start gap-3">
                  <div className="mt-0.5 text-muted-foreground">{t.icon}</div>
                  <div>
                    <p className="font-medium">{t.label}</p>
                    <p className="text-sm text-muted-foreground">{t.value}</p>
                  </div>
                </div>
              ))}
              <p className="text-xs text-muted-foreground/80 mt-2">
                Notes: This page reflects a demo configuration. Features and
                integrations may change.
              </p>
            </CardContent>
          </Card>
        </div>
      </motion.section>

      {/* Roadmap */}
      <motion.section
        className="container mx-auto px-4 py-14"
        initial={{ opacity: 0 }}
        whileInView={{ opacity: 1 }}
        transition={{ duration: 0.6 }}
        viewport={{ once: true }}
      >
        <div className="text-center mb-10">
          <h2 className="text-3xl md:text-5xl font-bold">Roadmap (indicative)</h2>
          <p className="mt-3 text-muted-foreground max-w-2xl mx-auto">
            A simple view of what exists now and what might come next.
          </p>
        </div>
        <div className="grid md:grid-cols-3 gap-6">
          {roadmap.map((group, gi) => (
            <Card key={gi} className="h-full">
              <CardHeader>
                <div className="flex items-center gap-2">
                  <Badge variant={gi === 0 ? "default" : "secondary"}>{group.when}</Badge>
                  {gi === 0 ? (
                    <TrendingUp className="h-4 w-4 text-primary" />
                  ) : (
                    <Sparkles className="h-4 w-4 text-muted-foreground" />
                  )}
                </div>
                <CardTitle className="sr-only">{group.when}</CardTitle>
              </CardHeader>
              <CardContent className="space-y-3">
                {group.items.map((it, ii) => (
                  <div key={ii} className="flex items-start gap-2">
                    <CheckCircle className="h-4 w-4 mt-0.5" />
                    <span className="text-sm">{it}</span>
                  </div>
                ))}
              </CardContent>
            </Card>
          ))}
        </div>
        <p className="text-xs text-muted-foreground text-center mt-6">
          Timelines and scope may shift. This section is informational, not a promise.
        </p>
      </motion.section>

      {/* FAQ */}
      <motion.section
        className="container mx-auto px-4 py-14"
        initial={{ opacity: 0 }}
        whileInView={{ opacity: 1 }}
        transition={{ duration: 0.6 }}
        viewport={{ once: true }}
      >
        <div className="text-center mb-8">
          <h2 className="text-3xl md:text-5xl font-bold">FAQ</h2>
          <p className="mt-3 text-muted-foreground max-w-2xl mx-auto">
            Quick answers in a neutral tone.
          </p>
        </div>

        <Accordion type="single" collapsible className="max-w-3xl mx-auto">
          <AccordionItem value="item-1">
            <AccordionTrigger>Is this the production app?</AccordionTrigger>
            <AccordionContent>
              This is a demo experience for evaluation. Functionality and copy are
              intentionally conservative and may change.
            </AccordionContent>
          </AccordionItem>
          <AccordionItem value="item-2">
            <AccordionTrigger>Do I need a credit card to try it?</AccordionTrigger>
            <AccordionContent>
              No credit card is required for the demo. If a paid plan is offered later,
              details would appear in the docs first.
            </AccordionContent>
          </AccordionItem>
          <AccordionItem value="item-3">
            <AccordionTrigger>How is data handled here?</AccordionTrigger>
            <AccordionContent>
              Data handling in this demo follows sensible defaults. Avoid uploading
              production‑sensitive information. For specifics, see the docs.
            </AccordionContent>
          </AccordionItem>
          <AccordionItem value="item-4">
            <AccordionTrigger>Where can I ask questions?</AccordionTrigger>
            <AccordionContent>
              Email <a className="underline" href="mailto:support@linqyard.com">support@linqyard.com</a> or check the docs page for short guides.
            </AccordionContent>
          </AccordionItem>
        </Accordion>
      </motion.section>

      {/* Footer */}
      <footer className="container mx-auto px-4 pb-10 text-center text-muted-foreground">
        <p>
          © {new Date().getFullYear()} linqyard. This page is a demo for illustrative purposes only.
        </p>
      </footer>
    </div>
  );
}
