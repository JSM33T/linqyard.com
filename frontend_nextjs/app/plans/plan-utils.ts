import { TierBillingCycle, TierDetails } from "@/hooks/types";

export const PLAN_FEATURES: Record<string, string[]> = {
  plus: [
    "Unlimited links & groups",
    "Advanced analytics dashboard",
    "Priority email support",
    "Custom themes & branding",
  ],
  pro: [
    "Everything in Plus",
    "Team collaboration tools",
    "Export analytics & data sync",
    "Dedicated success manager",
  ],
};

export function extractData<T>(payload: any): T | null {
  if (!payload) return null;
  if (Array.isArray(payload)) return (payload as unknown) as T;
  if (typeof payload === "object") {
    if ("data" in payload) {
      return payload.data as T;
    }
  }
  return payload as T;
}

export function formatCurrency(amountInMinorUnits: number, currency: string): string {
  try {
    return new Intl.NumberFormat("en-IN", {
      style: "currency",
      currency,
      minimumFractionDigits: currency === "JPY" ? 0 : 2,
    }).format(amountInMinorUnits / 100);
  } catch {
    return `${currency} ${(amountInMinorUnits / 100).toFixed(2)}`;
  }
}

export function formatDurationLabel(durationMonths: number): string {
  if (durationMonths <= 0) return "One-time";
  if (durationMonths === 1) return "Monthly";
  if (durationMonths === 12) return "Yearly";
  return `${durationMonths}-month`;
}

export function sortPlans(tier: TierDetails) {
  return [...tier.plans].sort((a, b) => a.durationMonths - b.durationMonths);
}

export function formatBillingPeriodLabel(billingPeriod: string) {
  if (!billingPeriod) return "Flexible";
  const normalized = billingPeriod.trim().toLowerCase();
  if (normalized === "monthly") return "Monthly";
  if (normalized === "yearly") return "Yearly";
  return normalized.charAt(0).toUpperCase() + normalized.slice(1);
}

export function calculateUpgradeCredit(
  tiers: TierDetails[],
  activeTierName: string | undefined,
  targetTierName: string,
  targetPlan: TierBillingCycle,
): number {
  if (!activeTierName) return 0;

  const sourceTierSlug = activeTierName.trim().toLowerCase();
  const targetTierSlug = targetTierName.trim().toLowerCase();
  if (!sourceTierSlug || sourceTierSlug === "free" || sourceTierSlug === targetTierSlug) {
    return 0;
  }

  const sourceTier = tiers.find((tier) => tier.name.toLowerCase() === sourceTierSlug);
  if (!sourceTier) {
    return 0;
  }

  const normalizedBilling = targetPlan.billingPeriod.trim().toLowerCase();
  const matchingPlan =
    sourceTier.plans.find(
      (plan) => plan.billingPeriod.trim().toLowerCase() === normalizedBilling,
    ) ?? sourceTier.plans.find((plan) => plan.durationMonths === targetPlan.durationMonths);

  return matchingPlan?.amount ?? 0;
}
