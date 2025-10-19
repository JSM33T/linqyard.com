"use client";

import dynamic from "next/dynamic";
import { Suspense, useMemo } from "react";
import { motion } from "framer-motion";
import Link from "next/link";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Separator } from "@/components/ui/separator";
import { Check, CheckCircle, Loader2, Sparkles } from "lucide-react";
import { useUser } from "@/contexts/UserContext";
import { useGet } from "@/hooks/useApi";
import { ApiError, TierDetails } from "@/hooks/types";
import {
  PLAN_FEATURES,
  extractData,
  formatBillingPeriodLabel,
  formatCurrency,
  formatDurationLabel,
  sortPlans,
} from "./plan-utils";

const TierUpgradePage = dynamic(() => import("../account/upgrade/page"), {
  ssr: false,
  loading: () => (
    <div className="flex items-center justify-center py-16 text-muted-foreground">
      <Loader2 className="mr-3 h-5 w-5 animate-spin" />
      Preparing your personalized plan&hellip;
    </div>
  ),
});

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

type TierResponseEnvelope = { data: TierDetails[] };

function MarketingPlans({
  tiers,
  loading,
  error,
}: {
  tiers: TierDetails[];
  loading: boolean;
  error: ApiError | null;
}) {
  const sortedTiers = useMemo(() => [...tiers].sort((a, b) => a.tierId - b.tierId), [tiers]);

  return (
    <div className="min-h-screen bg-background">
      <motion.section
        className="container mx-auto px-4 py-20 text-center"
        variants={containerVariants}
        initial="hidden"
        animate="visible"
      >
        <motion.div className="mx-auto max-w-4xl space-y-6" variants={itemVariants}>
          <Badge variant="secondary" className="px-4 py-2 text-sm">
            <Sparkles className="mr-2 h-4 w-4" />
            Pricing
          </Badge>

          <motion.h1 className="text-4xl font-bold tracking-tight md:text-6xl" variants={itemVariants}>
            Simple, predictable pricing
          </motion.h1>

          <motion.p className="text-lg text-muted-foreground md:text-xl" variants={itemVariants}>
            Start free and upgrade when you&apos;re ready. Monthly or yearly billing, no hidden fees, cancel anytime.
          </motion.p>

          <motion.div className="flex flex-wrap items-center justify-center gap-4 text-sm text-muted-foreground" variants={itemVariants}>
            <span className="flex items-center gap-2">
              <CheckCircle className="h-4 w-4" />
              Cancel anytime
            </span>
            <span className="flex items-center gap-2">
              <CheckCircle className="h-4 w-4" />
              30-day money-back guarantee
            </span>
            <span className="flex items-center gap-2">
              <CheckCircle className="h-4 w-4" />
              Secure Razorpay payments
            </span>
          </motion.div>
        </motion.div>
      </motion.section>

      <motion.section
        className="container mx-auto px-4 pb-20"
        variants={containerVariants}
        initial="hidden"
        whileInView="visible"
        viewport={{ once: true }}
      >
        <div className="mx-auto grid max-w-5xl gap-6 md:grid-cols-3">
          {sortedTiers.map((tier, index) => {
            const tierName = tier.name.toLowerCase();
            const isFree = tierName === "free";
            const plans = sortPlans(tier);
            const spotlightPlan = plans[0];
            const priceLabel =
              spotlightPlan && spotlightPlan.amount > 0 ? formatCurrency(spotlightPlan.amount, tier.currency) : "Free";
            const durationLabel = spotlightPlan ? formatDurationLabel(spotlightPlan.durationMonths) : "Flexible";
            const cta = isFree
              ? { label: "Get started", href: "/account/signup" }
              : { label: "Log in to upgrade", href: "/account/login" };
            const highlight = tierName === "plus";

            return (
              <motion.div
                key={tier.tierId}
                initial={{ opacity: 0, y: 30 }}
                whileInView={{ opacity: 1, y: 0 }}
                transition={{ duration: 0.5, delay: index * 0.08 }}
                viewport={{ once: true }}
              >
                <Card className={highlight ? "relative z-10 h-full border-primary shadow-2xl" : "h-full"}>
                  <CardHeader className="text-center space-y-6">
                    <div className="flex items-center justify-center gap-2">
                      <Badge variant={highlight ? "secondary" : "outline"} className="capitalize">
                        {tier.name}
                      </Badge>
                    </div>
                    {highlight && (
                      <div className="absolute -top-3 left-1/2 -translate-x-1/2 transform">
                        <Badge variant="secondary" className="px-3 py-1 text-sm">
                          Most popular
                        </Badge>
                      </div>
                    )}
                    <div className="space-y-2">
                      <CardTitle className="text-3xl">
                        {priceLabel}
                        {!isFree && spotlightPlan ? (
                          <span className="text-base font-normal text-muted-foreground">
                            {" "}
                            / {durationLabel.toLowerCase()}
                          </span>
                        ) : null}
                      </CardTitle>
                      <CardDescription>
                        {tier.description ??
                          (isFree
                            ? "Everything you need to publish your profile and start sharing instantly."
                            : "Unlock deeper analytics, personalization, and workflow automation to grow faster.")}
                      </CardDescription>
                    </div>
                  </CardHeader>

                  <CardContent className="space-y-6">
                    <div className="space-y-3 text-sm">
                      {plans.length === 0 ? (
                        <div className="rounded-lg border border-dashed px-4 py-3 text-muted-foreground">
                          Pricing configuration coming soon.
                        </div>
                      ) : (
                        plans.map((plan) => {
                          const isFeatured = plan === spotlightPlan && !isFree;
                          const billingLabel = formatBillingPeriodLabel(plan.billingPeriod);
                          return (
                            <div
                              key={`${tier.tierId}-${plan.billingPeriod}`}
                              className={`rounded-lg border px-4 py-3 text-left transition ${
                                isFeatured ? "border-primary bg-primary/5" : "border-border"
                              }`}
                            >
                              <div className="flex items-center justify-between gap-3">
                                <div>
                                  <div className="text-lg font-semibold">
                                    {formatCurrency(plan.amount, tier.currency)}
                                  </div>
                                  <div className="text-xs text-muted-foreground">
                                    {billingLabel} · {formatDurationLabel(plan.durationMonths).toLowerCase()} billing
                                  </div>
                                </div>
                                {isFeatured && (
                                  <Badge variant="secondary" className="text-xs uppercase tracking-wide">
                                    Best value
                                  </Badge>
                                )}
                              </div>
                              {plan.description ? (
                                <p className="mt-2 text-xs text-muted-foreground">{plan.description}</p>
                              ) : null}
                            </div>
                          );
                        })
                      )}
                    </div>

                    {plans.length > 0 && <Separator />}

                    <ul className="space-y-2 text-sm">
                      {(PLAN_FEATURES[tierName] ?? PLAN_FEATURES.plus ?? []).map((feature) => (
                        <li key={feature} className="flex items-start gap-2">
                          <Check className={`mt-0.5 h-4 w-4 ${highlight ? "text-primary" : "text-muted-foreground"}`} />
                          <span>{feature}</span>
                        </li>
                      ))}
                      {!isFree && spotlightPlan ? (
                        <li className="flex items-start gap-2 text-sm text-muted-foreground">
                          <Check className="mt-0.5 h-4 w-4 text-primary" />
                          Coupon-ready Razorpay checkout once you activate
                        </li>
                      ) : null}
                    </ul>

                    <div className="pt-2">
                      <Button asChild size="lg" className="w-full">
                        <Link href={cta.href} aria-label={cta.label}>
                          {cta.label}
                        </Link>
                      </Button>
                    </div>
                  </CardContent>
                </Card>
              </motion.div>
            );
          })}
        </div>

        {error && (
          <div className="mx-auto mt-6 max-w-2xl rounded-lg border border-destructive/40 bg-destructive/10 p-4 text-center text-sm text-destructive">
            {error.message || "Unable to load plans right now. Please try again shortly."}
          </div>
        )}

        {loading && (
          <div className="flex items-center justify-center py-10 text-muted-foreground">
            <Loader2 className="mr-2 h-5 w-5 animate-spin" />
            Loading plans…
          </div>
        )}
      </motion.section>
    </div>
  );
}

export default function PlansClient() {
  const { isAuthenticated, isInitialized } = useUser();
  const shouldLoadMarketing = isInitialized && !isAuthenticated;

  const { data: tiersEnvelope, loading, error } = useGet<TierResponseEnvelope>(
    "/tiers",
    useMemo(() => ({ enabled: shouldLoadMarketing }), [shouldLoadMarketing]),
  );

  const tiers = useMemo(() => {
    const payload = extractData<TierDetails[]>(tiersEnvelope);
    return Array.isArray(payload) ? payload : [];
  }, [tiersEnvelope]);

  if (!isInitialized) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-background text-muted-foreground">
        <Loader2 className="mr-2 h-5 w-5 animate-spin" />
        Loading&hellip;
      </div>
    );
  }

  if (isAuthenticated) {
    return (
      <Suspense
        fallback={
          <div className="flex items-center justify-center py-16 text-muted-foreground">
            <Loader2 className="mr-3 h-5 w-5 animate-spin" />
            Preparing your personalized plan&hellip;
          </div>
        }
      >
        <TierUpgradePage />
      </Suspense>
    );
  }

  return <MarketingPlans tiers={tiers} loading={loading} error={error} />;
}
