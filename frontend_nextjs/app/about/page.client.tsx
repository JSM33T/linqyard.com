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

import {
  Shield,
  Users,
  Sparkles,
  Globe,
  Target,
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
    title: "Privacy‑first",
    desc:
      "Built with security in mind. Your data stays yours with transparent handling and minimal collection.",
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
            The professional way to organize all your social links and CTAs on one page.
            Share one link everywhere. Update instantly. Track what matters.
          </motion.p>

          <motion.div
            className="flex flex-col sm:flex-row gap-3 justify-center items-center"
            variants={itemVariants}
          >
            <Button asChild size="lg">
              <Link href="/account/signup" className="inline-flex items-center">
                Get Started Free <ExternalLink className="ml-2 h-4 w-4" />
              </Link>
            </Button>
            <Button asChild variant="outline" size="lg">
              <Link href="/docs" className="inline-flex items-center">
                View Documentation <ExternalLink className="ml-2 h-4 w-4" />
              </Link>
            </Button>
          </motion.div>

          <motion.div
            className="flex items-center justify-center gap-2 text-sm text-muted-foreground"
            variants={itemVariants}
          >
            <CheckCircle className="h-4 w-4" /> Free tier included
            <CheckCircle className="h-4 w-4 ml-4" /> Mobile optimized
            <CheckCircle className="h-4 w-4 ml-4" /> Analytics included
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
          <h2 className="text-3xl md:text-5xl font-bold">Why Choose Linqyard</h2>
          <p className="mt-3 text-muted-foreground max-w-2xl mx-auto">
            Built on principles that put your success and privacy first.
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

      {/* How it works */}
      <motion.section
        className="container mx-auto px-4 py-14"
        initial={{ opacity: 0 }}
        whileInView={{ opacity: 1 }}
        transition={{ duration: 0.6 }}
        viewport={{ once: true }}
      >
        <div className="max-w-4xl mx-auto">
          <Card className="shadow-lg">
            <CardHeader className="text-center">
              <CardTitle className="flex items-center justify-center gap-2 text-2xl">
                <Sparkles className="h-6 w-6 text-primary" /> How it works
              </CardTitle>
              <CardDescription className="text-lg">
                A simple process designed to get you started quickly.
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-6">
              <div className="grid md:grid-cols-3 gap-6">
                <div className="text-center">
                  <div className="w-12 h-12 bg-primary/10 rounded-full flex items-center justify-center mx-auto mb-4">
                    <span className="text-primary font-bold text-lg">1</span>
                  </div>
                  <h3 className="font-semibold mb-2">Create your page</h3>
                  <p className="text-sm text-muted-foreground">Add links and CTAs you want to share with your audience.</p>
                </div>
                <div className="text-center">
                  <div className="w-12 h-12 bg-primary/10 rounded-full flex items-center justify-center mx-auto mb-4">
                    <span className="text-primary font-bold text-lg">2</span>
                  </div>
                  <h3 className="font-semibold mb-2">Share one URL</h3>
                  <p className="text-sm text-muted-foreground">Post it on social profiles, bios, and campaigns.</p>
                </div>
                <div className="text-center">
                  <div className="w-12 h-12 bg-primary/10 rounded-full flex items-center justify-center mx-auto mb-4">
                    <span className="text-primary font-bold text-lg">3</span>
                  </div>
                  <h3 className="font-semibold mb-2">Track & optimize</h3>
                  <p className="text-sm text-muted-foreground">Monitor clicks, reorder links, and optimize your strategy.</p>
                </div>
              </div>
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
          <h2 className="text-3xl md:text-5xl font-bold">What&apos;s Coming Next</h2>
          <p className="mt-3 text-muted-foreground max-w-2xl mx-auto">
            Exciting features in development to make your link management even better.
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
      </motion.section>
    </div>
  );
}
