"use client";

import { motion } from "framer-motion";
import Link from "next/link";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { CheckCircle, Sparkles } from "lucide-react";
import { useUser } from "@/contexts/UserContext";

const containerVariants = {
  hidden: { opacity: 0 },
  visible: {
    opacity: 1,
    transition: { delayChildren: 0.2, staggerChildren: 0.12 },
  },
};

const itemVariants = {
  hidden: { y: 20, opacity: 0 },
  visible: { y: 0, opacity: 1 },
};

const tiers = [
  {
    id: "free",
    name: "Free",
    price: "$0",
    desc: "Great for getting started — 12 links, 2 groups, basic analytics, and 2 theme presets.",
    features: ["12 links","2 groups","Basic analytics","Custom Subdomain", "Mobile-optimized", "Email support"],
    cta: "/account",
    variant: "default",
  },
  {
    id: "plus",
    name: "Plus",
    price: "$2/mo",
    desc: "For creators who want more customization and improved analytics.",
    features: ["Unlimited Links","Unlimited Groups", "Advanced analytics", "Priority email support"],
    cta: "/account/upgrade",
    variant: "secondary",
  },
  {
    id: "pro",
    name: "Pro",
    price: "$5/mo",
    desc: "Designed for teams and power users — full analytics and team management.",
    features: ["All Plus features", "Advanced analytics and more dashboards", "Data export"],
    cta: "/account/upgrade",
    variant: "outline",
  },
];

export default function PlansClient() {
  const { isAuthenticated, isInitialized } = useUser();
  return (
    <div className="min-h-screen bg-background">
      <motion.section
        className="container mx-auto px-4 py-20 text-center"
        variants={containerVariants}
        initial="hidden"
        animate="visible"
      >
        <motion.div className="space-y-6 max-w-4xl mx-auto" variants={itemVariants}>
          <Badge variant="secondary" className="text-sm px-4 py-2">
            <Sparkles className="h-4 w-4 mr-2" /> Pricing
          </Badge>

          <motion.h1 className="text-4xl md:text-6xl font-bold tracking-tight" variants={itemVariants}>
            Simple, predictable pricing
          </motion.h1>

          <motion.p className="text-lg md:text-xl text-muted-foreground" variants={itemVariants}>
            Choose a plan that fits your needs — start free and upgrade as you grow. Monthly billing with no surprise fees.
          </motion.p>

          <motion.div className="flex items-center justify-center gap-2 text-sm text-muted-foreground" variants={itemVariants}>
            <CheckCircle className="h-4 w-4" /> Cancel anytime
            <CheckCircle className="h-4 w-4 ml-4" /> 30 day money-back (for paid tiers)
            <CheckCircle className="h-4 w-4 ml-4" /> Secure payments
          </motion.div>
        </motion.div>
      </motion.section>

      <motion.section className="container mx-auto px-4 py-14" variants={containerVariants} initial="hidden" whileInView="visible" viewport={{ once: true }}>
        <div className="max-w-5xl mx-auto">
          <div className="grid md:grid-cols-3 gap-6">
            {tiers.map((t, i) => (
              <motion.div key={t.id} initial={{ opacity: 0, y: 30 }} whileInView={{ opacity: 1, y: 0 }} transition={{ duration: 0.5, delay: i * 0.08 }} viewport={{ once: true }}>
                {/* outer wrapper made relative so badges or ribbons can be absolutely positioned */}
                <div className={`relative ${t.id === "plus" ? "z-10 transform scale-105" : ""}`}>
                  <Card className={`h-full ${t.id === "plus" ? "shadow-2xl border-2 border-primary" : ""}`}>
                    <CardHeader className="text-center">
                      <div className="flex items-center justify-center gap-2">
                        <Badge variant={t.variant as "default" | "destructive" | "outline" | "secondary" | null | undefined}>{t.name}</Badge>
                      </div>

                      {/* Highlight badge for the Plus plan */}
                      {t.id === "plus" && (
                        <div className="absolute -top-3 left-1/2 transform -translate-x-1/2">
                          <Badge variant="secondary" className="px-3 py-1 text-sm">Most feasible</Badge>
                        </div>
                      )}

                      <CardTitle className="mt-6 text-2xl">{t.price}</CardTitle>
                      <CardDescription className="mt-2">{t.desc}</CardDescription>
                    </CardHeader>
                    <CardContent className="space-y-4">
                      <ul className="text-sm space-y-2">
                        {t.features.map((f, ii) => (
                          <li key={ii} className="flex items-start gap-2">
                            <CheckCircle className={`h-4 w-4 mt-1 ${t.id === "plus" ? "text-primary" : "text-primary"}`} />
                            <span>{f}</span>
                          </li>
                        ))}
                      </ul>

                      <div className="pt-2">
                        {t.id === "free" ? (
                          // Auth-aware CTA: show 'Active' when logged in, otherwise link to signup.
                          !isInitialized ? (
                            <button
                              className="inline-flex items-center justify-center w-full rounded-md px-4 py-3 text-sm font-medium opacity-80 bg-muted text-muted-foreground"
                              aria-disabled={true}
                              disabled
                            >
                              Loading...
                            </button>
                          ) : isAuthenticated ? (
                            <button
                              className="inline-flex items-center justify-center w-full rounded-md px-4 py-3 text-sm font-medium bg-primary text-primary-foreground cursor-default"
                              aria-disabled={true}
                              disabled
                            >
                              Active
                            </button>
                          ) : (
                            <Button asChild size="lg">
                                <Link href="/account/signup" className="inline-flex items-center justify-center w-full" aria-label="Sign up">
                                  Get Started (Free)
                                </Link>
                            </Button>
                          )
                        ) : (
                          // Paid tiers are coming soon: render a faded, disabled button
                          <button
                            className="inline-flex items-center justify-center w-full rounded-md px-4 py-3 text-sm font-medium opacity-60 cursor-not-allowed bg-muted text-muted-foreground"
                            aria-disabled={true}
                            disabled
                          >
                            Coming soon
                          </button>
                        )}
                      </div>
                    </CardContent>
                  </Card>
                </div>
              </motion.div>
            ))}
          </div>
        </div>
      </motion.section>
    </div>
  );
}
