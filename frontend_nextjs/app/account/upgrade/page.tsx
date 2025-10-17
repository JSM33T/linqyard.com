"use client";

import { useCallback, useMemo, useState } from "react";
import Link from "next/link";
import { toast } from "sonner";
import { ArrowLeft, Check, Loader2, ShieldCheck, Star, Clock } from "lucide-react";
import AccessDenied from "@/components/AccessDenied";
import { useUser, userHelpers } from "@/contexts/UserContext";
import { useGet, usePost } from "@/hooks/useApi";
import {
  ApiError,
  TierBillingCycle,
  TierDetails,
  TierOrderData,
  TierUpgradeConfirmationData,
  UserTierInfo,
} from "@/hooks/types";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Separator } from "@/components/ui/separator";
import { cn } from "@/lib/utils";

declare global {
  interface Window {
    Razorpay?: new (options: RazorpayCheckoutOptions) => RazorpayInstance;
  }
}

interface RazorpayInstance {
  open(): void;
  close(): void;
  on(event: "payment.failed", handler: (response: RazorpayFailureResponse) => void): void;
}

interface RazorpayCheckoutOptions {
  key: string;
  amount: number;
  currency: string;
  name?: string;
  description?: string;
  image?: string;
  order_id: string;
  notes?: Record<string, any>;
  prefill?: {
    name?: string;
    email?: string;
  };
  handler: (response: RazorpaySuccessResponse) => void;
  modal?: {
    ondismiss?: () => void;
  };
  theme?: {
    color?: string;
  };
}

interface RazorpaySuccessResponse {
  razorpay_payment_id: string;
  razorpay_order_id: string;
  razorpay_signature: string;
}

interface RazorpayFailureResponse {
  error: {
    code: string;
    description: string;
    source: string;
    step: string;
    reason: string;
    metadata?: Record<string, any>;
  };
}

type ApiEnvelope<T> = {
  data: T;
  meta?: any;
};

const PLAN_FEATURES: Record<string, string[]> = {
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

const RAZORPAY_SCRIPT_URL = "https://checkout.razorpay.com/v1/checkout.js";
let razorpayScriptPromise: Promise<void> | null = null;

async function ensureRazorpayScript(): Promise<void> {
  if (typeof window === "undefined") {
    throw new Error("Razorpay is only available in the browser.");
  }
  if (window.Razorpay) {
    return;
  }
  if (!razorpayScriptPromise) {
    razorpayScriptPromise = new Promise<void>((resolve, reject) => {
      const existingScript = document.querySelector<HTMLScriptElement>(`script[src="${RAZORPAY_SCRIPT_URL}"]`);
      if (existingScript) {
        existingScript.addEventListener("load", () => resolve(), { once: true });
        existingScript.addEventListener("error", () => reject(new Error("Failed to load Razorpay script.")), {
          once: true,
        });
        return;
      }

      const script = document.createElement("script");
      script.src = RAZORPAY_SCRIPT_URL;
      script.async = true;
      script.onload = () => resolve();
      script.onerror = () => reject(new Error("Failed to load Razorpay script."));
      document.body.appendChild(script);
    });
  }
  await razorpayScriptPromise;
  if (!window.Razorpay) {
    throw new Error("Razorpay SDK did not initialize.");
  }
}

function extractData<T>(payload: any): T | null {
  if (!payload) return null;
  if (Array.isArray(payload)) return (payload as unknown) as T;
  if (typeof payload === "object") {
    if ("data" in payload) {
      return payload.data as T;
    }
  }
  return payload as T;
}

function formatCurrency(amountInMinorUnits: number, currency: string): string {
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

function formatDurationLabel(durationMonths: number): string {
  if (durationMonths <= 0) return "One-time";
  if (durationMonths === 1) return "Monthly";
  if (durationMonths === 12) return "Yearly";
  return `${durationMonths}-month`;
}

function extractErrorMessage(error: ApiError | null | undefined): string {
  if (!error) return "Something went wrong. Please try again.";
  const detail = (error.data && (error.data.detail || error.data.message)) || error.message;
  return detail || "Unable to process your request right now.";
}

export default function TierUpgradePage() {
  const { user, isAuthenticated, updateUser } = useUser();

  const {
    data: tiersEnvelope,
    loading: tiersLoading,
    error: tiersError,
    refetch: refetchTiers,
  } = useGet<ApiEnvelope<TierDetails[]>>("/tiers");

  const myTierRequestConfig = useMemo(() => ({ enabled: isAuthenticated }), [isAuthenticated]);

  const {
    data: myTierEnvelope,
    loading: myTierLoading,
    refetch: refetchMyTier,
  } = useGet<ApiEnvelope<UserTierInfo | null>>("/tiers/me", myTierRequestConfig);

  const createOrder = usePost<ApiEnvelope<TierOrderData>>("/tiers/upgrade/order");
  const confirmUpgrade = usePost<ApiEnvelope<TierUpgradeConfirmationData>>("/tiers/upgrade/confirm");

  const [isPaymentInProgress, setIsPaymentInProgress] = useState(false);
  const [selectedOption, setSelectedOption] = useState<{ tierName: string; billingPeriod: string } | null>(null);

  const tiers = useMemo(() => {
    const payload = extractData<TierDetails[]>(tiersEnvelope);
    return Array.isArray(payload) ? payload : [];
  }, [tiersEnvelope]);

  const activeTier = useMemo(() => {
    const payload = extractData<UserTierInfo | null>(myTierEnvelope);
    return payload ?? null;
  }, [myTierEnvelope]);

  const paidTiers = useMemo(() => {
    return tiers
      .filter((tier) => tier.name.toLowerCase() !== "free")
      .sort((a, b) => a.tierId - b.tierId);
  }, [tiers]);

  const currentTierName = userHelpers.getTierName(user).toLowerCase();
  const currentTierDisplay = userHelpers.getTierDisplayName(user);

  const isBusy = tiersLoading || myTierLoading || createOrder.loading || confirmUpgrade.loading || isPaymentInProgress;

  const handleUpgrade = useCallback(
    async (tier: TierDetails, plan: TierBillingCycle) => {
      const tierName = tier.name.toLowerCase();
      const billingPeriod = plan.billingPeriod;
      const prettyTierName = tier.name.charAt(0).toUpperCase() + tier.name.slice(1);

      if (!isAuthenticated) {
        toast.info("Please sign in to upgrade your plan.");
        return;
      }

      try {
        setSelectedOption({ tierName, billingPeriod });
        setIsPaymentInProgress(true);

        const orderResponse = await createOrder.mutate({
          tierName,
          billingPeriod,
        });

        const orderData = extractData<TierOrderData>(orderResponse.data);
        if (!orderData) {
          throw new Error("Unable to create payment order.");
        }

        await ensureRazorpayScript();

        const onPaymentSuccess = async (response: RazorpaySuccessResponse) => {
          try {
            const confirmationResponse = await confirmUpgrade.mutate({
              tierName,
              billingPeriod,
              razorpayOrderId: response.razorpay_order_id,
              razorpayPaymentId: response.razorpay_payment_id,
              razorpaySignature: response.razorpay_signature,
            });

            const confirmation = extractData<TierUpgradeConfirmationData>(confirmationResponse.data);
            if (confirmation?.tier) {
              updateUser({
                tierId: confirmation.tier.tierId,
                tierName: confirmation.tier.name,
              });
            }

            toast.success(confirmation?.message ?? "Tier upgraded successfully!");
            await Promise.allSettled([refetchTiers(), refetchMyTier()]);
          } catch (error) {
            console.error("Tier confirmation error", error);
            toast.error(extractErrorMessage(error as ApiError));
          } finally {
            setIsPaymentInProgress(false);
            setSelectedOption(null);
          }
        };

        const onPaymentFailure = (failure: RazorpayFailureResponse) => {
          console.warn("Razorpay payment failed", failure);
          const reason =
            failure?.error?.description ||
            failure?.error?.reason ||
            "Payment was not completed. You were not charged.";
          toast.error(reason);
          setIsPaymentInProgress(false);
          setSelectedOption(null);
        };

        const onModalDismiss = () => {
          toast.info("Payment cancelled.");
          setIsPaymentInProgress(false);
          setSelectedOption(null);
        };

        const prefillName = `${user?.firstName ?? ""} ${user?.lastName ?? ""}`.trim() || undefined;
        const prefillEmail = user?.email ?? undefined;

        const razorpayOptions: RazorpayCheckoutOptions = {
          key: orderData.razorpayKeyId,
          amount: orderData.amount,
          currency: orderData.currency,
          name: "Linqyard",
          description: `Upgrade to ${prettyTierName} (${formatDurationLabel(plan.durationMonths)})`,
          order_id: orderData.orderId,
          notes: {
            tierName: orderData.tierName,
            billingPeriod: orderData.billingPeriod,
          },
          prefill: {
            name: prefillName,
            email: prefillEmail,
          },
          handler: onPaymentSuccess,
          modal: {
            ondismiss: onModalDismiss,
          },
          theme: {
            color: "#2563eb",
          },
        };

        const razorpay = new window.Razorpay!(razorpayOptions);
        razorpay.on("payment.failed", onPaymentFailure);
        razorpay.open();
      } catch (error) {
        console.error("Tier upgrade initiation failed", error);
        toast.error(extractErrorMessage(error as ApiError));
        setIsPaymentInProgress(false);
        setSelectedOption(null);
      }
    },
    [isAuthenticated, createOrder, confirmUpgrade, updateUser, refetchTiers, refetchMyTier, user?.firstName, user?.lastName, user?.email],
  );

  if (!isAuthenticated) {
    return <AccessDenied />;
  }

  const activeTierName = activeTier?.name ?? currentTierName;
  const activeTierPretty = activeTier ? activeTier.name.charAt(0).toUpperCase() + activeTier.name.slice(1) : currentTierDisplay;

  return (
    <div className="max-w-6xl mx-auto px-4 py-10 space-y-8">
      <div className="flex items-center justify-between flex-wrap gap-4">
        <div className="flex items-center gap-3">
          <Button variant="ghost" size="sm" asChild>
            <Link href="/account">
              <ArrowLeft className="h-4 w-4 mr-2" />
              Back to account
            </Link>
          </Button>
          <h1 className="text-2xl md:text-3xl font-semibold tracking-tight">Manage your plan</h1>
        </div>
        <Badge variant="secondary" className="flex items-center gap-1">
          <ShieldCheck className="h-3.5 w-3.5" />
          Secure Razorpay checkout
        </Badge>
      </div>

      <Card>
        <CardHeader className="flex flex-col md:flex-row md:items-center md:justify-between gap-4">
          <div>
            <CardTitle className="text-xl">Current subscription</CardTitle>
            <CardDescription>Review your active tier and explore upgrade options below.</CardDescription>
          </div>
          <Badge variant="default" className="text-base px-4 py-1">
            {activeTierPretty}
          </Badge>
        </CardHeader>
        <CardContent className="grid gap-4 md:grid-cols-3">
          <div className="rounded-lg border p-4">
            <div className="text-sm text-muted-foreground">Plan</div>
            <div className="text-lg font-semibold capitalize">{activeTierPretty}</div>
          </div>
          <div className="rounded-lg border p-4">
            <div className="text-sm text-muted-foreground">Started</div>
            <div className="text-lg font-medium">
              {activeTier?.activeFrom ? new Date(activeTier.activeFrom).toLocaleDateString() : "—"}
            </div>
          </div>
          <div className="rounded-lg border p-4">
            <div className="text-sm text-muted-foreground">Renews / Expires</div>
            <div className="text-lg font-medium">
              {activeTier?.activeUntil ? new Date(activeTier.activeUntil).toLocaleDateString() : "Until cancelled"}
            </div>
          </div>
        </CardContent>
      </Card>

      <section className="space-y-6">
        <div>
          <h2 className="text-xl font-semibold">Upgrade options</h2>
          <p className="text-sm text-muted-foreground">
            Choose a billing cycle that works for you. Amounts are shown in your billing currency.
          </p>
        </div>

        {tiersError && (
          <div className="rounded-lg border border-destructive/40 bg-destructive/10 p-4 text-sm text-destructive">
            {extractErrorMessage(tiersError)}
          </div>
        )}

        <div className="grid gap-6 md:grid-cols-2">
          {paidTiers.map((tier) => {
            const tierName = tier.name.toLowerCase();
            const isCurrent = tierName === activeTierName?.toLowerCase();
            const plans = tier.plans.sort((a, b) => a.durationMonths - b.durationMonths);

            return (
              <Card key={tier.tierId} className={cn("h-full", isCurrent && "border-primary shadow-lg")}>
                <CardHeader>
                  <div className="flex items-center justify-between">
                    <CardTitle className="text-2xl capitalize">{tier.name}</CardTitle>
                    {tierName === "plus" && (
                      <Badge variant="secondary" className="flex items-center gap-1">
                        <Star className="h-3.5 w-3.5 text-primary" />
                        Popular
                      </Badge>
                    )}
                  </div>
                  <CardDescription>{tier.description ?? "Premium features tailored for growing creators."}</CardDescription>
                </CardHeader>
                <CardContent className="space-y-5">
                  <div className="space-y-3">
                    {plans.map((plan) => {
                      const formattedPrice = formatCurrency(plan.amount, tier.currency);
                      const periodLabel = formatDurationLabel(plan.durationMonths);
                      const effectiveMonthly =
                        plan.durationMonths > 1 ? formatCurrency(Math.round(plan.amount / plan.durationMonths), tier.currency) : null;
                      const optionActive =
                        selectedOption?.tierName === tierName && selectedOption?.billingPeriod === plan.billingPeriod && isPaymentInProgress;
                      const planDisabled = isCurrent || optionActive || isBusy;

                      return (
                        <div key={plan.billingPeriod} className="rounded-lg border p-4 space-y-3">
                          <div className="flex items-center justify-between">
                            <div>
                              <div className="text-lg font-semibold">{formattedPrice}</div>
                              <div className="text-xs text-muted-foreground">
                                {periodLabel} billing {effectiveMonthly ? `· ~${effectiveMonthly}/month` : ""}
                              </div>
                            </div>
                            <Badge variant="outline" className="flex items-center gap-1">
                              <Clock className="h-3.5 w-3.5" />
                              {plan.durationMonths <= 0 ? "Flexible" : `${plan.durationMonths} mo`}
                            </Badge>
                          </div>

                          <p className="text-sm text-muted-foreground">
                            {plan.description ?? tier.description ?? "Enjoy more control, personalization, and insights."}
                          </p>

                          <Button
                            className="w-full"
                            disabled={planDisabled}
                            onClick={() => handleUpgrade(tier, plan)}
                          >
                            {optionActive ? (
                              <>
                                <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                                Securing checkout…
                              </>
                            ) : isCurrent ? (
                              <>
                                <Check className="mr-2 h-4 w-4" />
                                Active plan
                              </>
                            ) : (
                              `Upgrade (${periodLabel.toLowerCase()})`
                            )}
                          </Button>
                        </div>
                      );
                    })}
                  </div>

                  <Separator />

                  <div className="space-y-2">
                    <h3 className="text-sm font-medium text-muted-foreground">What&apos;s included</h3>
                    <ul className="space-y-2 text-sm">
                      {(PLAN_FEATURES[tierName] ?? PLAN_FEATURES.plus).map((feature) => (
                        <li key={feature} className="flex items-start gap-2">
                          <Check className="h-4 w-4 mt-0.5 text-primary" />
                          <span>{feature}</span>
                        </li>
                      ))}
                    </ul>
                  </div>
                </CardContent>
              </Card>
            );
          })}
        </div>

        {paidTiers.length === 0 && !tiersLoading && (
          <Card>
            <CardContent className="py-10 text-center text-muted-foreground">
              No paid plans are currently available. Please check back later.
            </CardContent>
          </Card>
        )}

        {tiersLoading && (
          <div className="flex items-center justify-center py-10 text-muted-foreground">
            <Loader2 className="mr-2 h-5 w-5 animate-spin" />
            Loading plans…
          </div>
        )}
      </section>
    </div>
  );
}
