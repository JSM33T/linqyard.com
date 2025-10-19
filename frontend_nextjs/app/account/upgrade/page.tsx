"use client";

import { useCallback, useMemo, useState, type ChangeEvent } from "react";
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
  TierCouponPreviewData,
  UserTierInfo,
} from "@/hooks/types";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Separator } from "@/components/ui/separator";
import { cn } from "@/lib/utils";
import { PLAN_FEATURES, extractData, formatCurrency, formatDurationLabel, sortPlans } from "@/app/plans/plan-utils";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";

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
  const {
    mutate: previewCouponMutate,
    loading: isPreviewingCoupon,
    reset: resetCouponPreview,
  } = usePost<ApiEnvelope<TierCouponPreviewData>>("/tiers/coupons/preview");

  const [isPaymentInProgress, setIsPaymentInProgress] = useState(false);
  const [selectedOption, setSelectedOption] = useState<{ tierName: string; billingPeriod: string } | null>(null);
  const [checkoutContext, setCheckoutContext] = useState<{ tier: TierDetails; plan: TierBillingCycle } | null>(null);
  const [isCheckoutDialogOpen, setIsCheckoutDialogOpen] = useState(false);
  const [couponCode, setCouponCode] = useState("");
  const [appliedCoupon, setAppliedCoupon] = useState<TierCouponPreviewData | null>(null);
  const [couponError, setCouponError] = useState<string | null>(null);

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
  const isCheckoutBusy = isPaymentInProgress || createOrder.loading || confirmUpgrade.loading;

  const resetCouponState = useCallback(() => {
    setCouponCode("");
    setAppliedCoupon(null);
    setCouponError(null);
    resetCouponPreview();
  }, [resetCouponPreview]);

  const closeCheckoutDialog = useCallback(() => {
    setIsCheckoutDialogOpen(false);
    setCheckoutContext(null);
    setSelectedOption(null);
    setIsPaymentInProgress(false);
    resetCouponState();
  }, [resetCouponState]);

  const handleDialogOpenChange = useCallback(
    (open: boolean) => {
      if (!open) {
        if (isPaymentInProgress) {
          return;
        }
        closeCheckoutDialog();
      } else {
        setIsCheckoutDialogOpen(true);
      }
    },
    [closeCheckoutDialog, isPaymentInProgress],
  );

  const handleCouponInputChange = useCallback(
    (event: ChangeEvent<HTMLInputElement>) => {
      const value = event.target.value;
      setCouponCode(value);
      if (couponError) {
        setCouponError(null);
      }
      if (appliedCoupon && value.trim().toLowerCase() !== appliedCoupon.couponCode.toLowerCase()) {
        setAppliedCoupon(null);
      }
    },
    [couponError, appliedCoupon],
  );

  const handleApplyCoupon = useCallback(async () => {
    if (!checkoutContext) return;

    const code = couponCode.trim();
    if (!code) {
      setCouponError("Enter a coupon code to continue.");
      return;
    }

    try {
      setCouponError(null);
      const tierName = checkoutContext.tier.name.toLowerCase();
      const billingPeriod = checkoutContext.plan.billingPeriod;

      const response = await previewCouponMutate({
        tierName,
        billingPeriod,
        couponCode: code,
      });

      const preview = extractData<TierCouponPreviewData>(response.data);
      if (!preview) {
        throw new Error("Invalid coupon response.");
      }

      setAppliedCoupon(preview);
      setCouponCode(preview.couponCode);
      toast.success(`Coupon ${preview.couponCode} applied!`);
    } catch (error) {
      const message = extractErrorMessage(error as ApiError);
      setCouponError(message);
      setAppliedCoupon(null);
      console.error("Coupon apply error", error);
    }
  }, [checkoutContext, couponCode, previewCouponMutate]);

  const handleRemoveCoupon = useCallback(() => {
    setAppliedCoupon(null);
    setCouponError(null);
    setCouponCode("");
    resetCouponPreview();
  }, [resetCouponPreview]);

  const handleUpgrade = useCallback(
    (tier: TierDetails, plan: TierBillingCycle) => {
      if (!isAuthenticated) {
        toast.info("Please sign in to upgrade your plan.");
        return;
      }

      resetCouponState();
      setCheckoutContext({ tier, plan });
      setIsCheckoutDialogOpen(true);
      setSelectedOption(null);
      setIsPaymentInProgress(false);
    },
    [isAuthenticated, resetCouponState],
  );

  const handleCheckoutConfirm = useCallback(async () => {
    if (!checkoutContext) return;

    const { tier, plan } = checkoutContext;
    const tierName = tier.name.toLowerCase();
    const billingPeriod = plan.billingPeriod;
    const prettyTierName = tier.name.charAt(0).toUpperCase() + tier.name.slice(1);
    const couponToSend = appliedCoupon?.couponCode?.trim();

    try {
      setSelectedOption({ tierName, billingPeriod });
      setIsPaymentInProgress(true);

      const orderResponse = await createOrder.mutate({
        tierName,
        billingPeriod,
        couponCode: couponToSend,
      });

      const orderData = extractData<TierOrderData>(orderResponse.data);
      if (!orderData) {
        throw new Error("Unable to create payment order.");
      }

      await ensureRazorpayScript();

      const finalCouponCode = orderData.couponCode ?? couponToSend ?? undefined;

      const onPaymentSuccess = async (response: RazorpaySuccessResponse) => {
        try {
          const confirmationResponse = await confirmUpgrade.mutate({
            tierName,
            billingPeriod,
            razorpayOrderId: response.razorpay_order_id,
            razorpayPaymentId: response.razorpay_payment_id,
            razorpaySignature: response.razorpay_signature,
            couponCode: finalCouponCode,
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
          closeCheckoutDialog();
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
          subtotalAmount: orderData.subtotalAmount,
          discountAmount: orderData.discountAmount,
          couponCode: finalCouponCode,
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
  }, [
    checkoutContext,
    appliedCoupon,
    createOrder,
    confirmUpgrade,
    updateUser,
    refetchTiers,
    refetchMyTier,
    closeCheckoutDialog,
    user?.firstName,
    user?.lastName,
    user?.email,
  ]);

  const activeTierName = activeTier?.name ?? currentTierName;
  const activeTierPretty = activeTier ? activeTier.name.charAt(0).toUpperCase() + activeTier.name.slice(1) : currentTierDisplay;
  const checkoutSummary = useMemo(() => {
    if (!checkoutContext) {
      return null;
    }

    const { tier, plan } = checkoutContext;
    const subtotalAmount = appliedCoupon?.subtotalAmount ?? plan.amount;
    const discountAmount = appliedCoupon?.discountAmount ?? 0;
    const finalAmount = appliedCoupon?.finalAmount ?? plan.amount;
    const durationLabel = formatDurationLabel(plan.durationMonths);
    const currency = tier.currency;
    const effectiveMonthly =
      plan.durationMonths > 1 ? formatCurrency(Math.round(finalAmount / plan.durationMonths), currency) : null;
    const validUntil = appliedCoupon?.validUntil ? new Date(appliedCoupon.validUntil).toLocaleDateString() : null;

    return {
      tier,
      plan,
      subtotalAmount,
      discountAmount,
      finalAmount,
      durationLabel,
      currency,
      effectiveMonthly,
      validUntil,
    };
  }, [checkoutContext, appliedCoupon]);

  if (!isAuthenticated) {
    return <AccessDenied />;
  }

  return (
    <>
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
            const plans = sortPlans(tier);

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

      <Dialog open={isCheckoutDialogOpen} onOpenChange={handleDialogOpenChange}>
        <DialogContent className="max-w-xl space-y-4">
          {checkoutSummary ? (
            <>
              <DialogHeader>
                <DialogTitle>Confirm your upgrade</DialogTitle>
                <DialogDescription>
                  Review the pricing summary and apply a coupon before completing your secure Razorpay payment.
                </DialogDescription>
              </DialogHeader>

              <div className="space-y-3 rounded-lg border border-border bg-muted/10 p-4">
                <div className="flex items-center justify-between text-sm">
                  <span className="text-muted-foreground">Plan</span>
                  <span className="font-medium capitalize">{checkoutSummary.tier.name}</span>
                </div>
                <div className="flex items-center justify-between text-sm">
                  <span className="text-muted-foreground">Billing cycle</span>
                  <span className="font-medium">{checkoutSummary.durationLabel}</span>
                </div>
                <div className="space-y-2 pt-2 text-sm">
                  <div className="flex items-center justify-between">
                    <span>Subtotal</span>
                    <span>{formatCurrency(checkoutSummary.subtotalAmount, checkoutSummary.currency)}</span>
                  </div>
                  {checkoutSummary.discountAmount > 0 && (
                    <div className="flex items-center justify-between text-emerald-600">
                      <span>Coupon savings</span>
                      <span>-{formatCurrency(checkoutSummary.discountAmount, checkoutSummary.currency)}</span>
                    </div>
                  )}
                  <div className="flex items-center justify-between text-base font-semibold">
                    <span>Total due today</span>
                    <span>{formatCurrency(checkoutSummary.finalAmount, checkoutSummary.currency)}</span>
                  </div>
                </div>
                {checkoutSummary.effectiveMonthly && (
                  <p className="text-xs text-muted-foreground">
                    Works out to approximately {checkoutSummary.effectiveMonthly} per month.
                  </p>
                )}
              </div>

              <div className="space-y-2">
                <label className="text-sm font-medium" htmlFor="coupon-input">
                  Have a coupon?
                </label>
                <div className="flex flex-col gap-2 sm:flex-row sm:items-center">
                  <Input
                    id="coupon-input"
                    value={couponCode}
                    onChange={handleCouponInputChange}
                    placeholder="Enter coupon code"
                    autoComplete="off"
                    disabled={isPreviewingCoupon || isCheckoutBusy}
                    className="sm:flex-1 uppercase tracking-wide"
                  />
                  <div className="flex gap-2">
                    <Button
                      type="button"
                      variant="secondary"
                      onClick={handleApplyCoupon}
                      disabled={isPreviewingCoupon || isCheckoutBusy || couponCode.trim().length === 0}
                    >
                      {isPreviewingCoupon ? (
                        <>
                          <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                          Applying...
                        </>
                      ) : (
                        "Apply"
                      )}
                    </Button>
                    {appliedCoupon && (
                      <Button type="button" variant="ghost" onClick={handleRemoveCoupon} disabled={isCheckoutBusy}>
                        Remove
                      </Button>
                    )}
                  </div>
                </div>
                {couponError && <p className="text-sm text-destructive">{couponError}</p>}
                {appliedCoupon && (
                  <div className="space-y-1 text-sm text-emerald-600">
                    <p>
                      {appliedCoupon.couponCode} applied — {appliedCoupon.discountPercentage}% off.
                      {checkoutSummary.validUntil ? ` Valid until ${checkoutSummary.validUntil}.` : ""}
                    </p>
                    {appliedCoupon.description && (
                      <p className="text-xs text-emerald-700">{appliedCoupon.description}</p>
                    )}
                  </div>
                )}
              </div>

              <DialogFooter className="flex-col gap-2 sm:flex-row sm:justify-between">
                <Button type="button" variant="outline" onClick={closeCheckoutDialog} disabled={isCheckoutBusy}>
                  Cancel
                </Button>
                <Button
                  type="button"
                  onClick={handleCheckoutConfirm}
                  disabled={isCheckoutBusy}
                  className="w-full sm:w-auto"
                >
                  {isCheckoutBusy ? (
                    <>
                      <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                      Processing...
                    </>
                  ) : (
                    `Pay ${formatCurrency(checkoutSummary.finalAmount, checkoutSummary.currency)}`
                  )}
                </Button>
              </DialogFooter>
            </>
          ) : (
            <div className="py-8 text-center text-sm text-muted-foreground">
              Select a plan to review pricing and complete your upgrade.
            </div>
          )}
        </DialogContent>
      </Dialog>
    </>
  );
}
