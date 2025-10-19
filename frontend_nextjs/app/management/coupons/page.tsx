"use client";

import type { JSX } from "react";
import type { JSX } from "react";
import { useMemo, useState } from "react";
import { toast } from "sonner";
import { BadgePercent, Loader2, Pencil, Plus, Trash2, X, Check } from "lucide-react";
import AccessDenied from "@/components/AccessDenied";
import { useUser } from "@/contexts/UserContext";
import { useGet, usePost, useApi } from "@/hooks/useApi";
import { ApiError, CouponAdmin, TierAdminDetails } from "@/hooks/types";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Switch } from "@/components/ui/switch";
import { Badge } from "@/components/ui/badge";
import { Separator } from "@/components/ui/separator";
import { ConfirmDialog } from "@/components/modals/ConfirmDialog";

type ApiEnvelope<T> = { data: T; meta?: any };

type CouponFormState = {
  code: string;
  discountPercentage: string;
  description: string;
  tierId: string;
  maxRedemptions: string;
  validFrom: string;
  validUntil: string;
  isActive: boolean;
};

type EditState = {
  couponId: string;
  form: CouponFormState;
};

type DeleteState = {
  couponId: string;
  code: string;
  error?: string;
};

const getApiErrorMessage = (error: unknown, fallback: string) => {
  const apiError = error as ApiError | undefined;
  if (!apiError) return fallback;

  const data = apiError.data as Record<string, unknown> | undefined;
  const detail =
    (typeof data?.detail === "string" && data.detail) ||
    (typeof data?.title === "string" && data.title) ||
    (typeof data?.message === "string" && data.message);

  return detail || apiError.message || fallback;
};

const EMPTY_FORM: CouponFormState = {
  code: "",
  discountPercentage: "",
  description: "",
  tierId: "",
  maxRedemptions: "",
  validFrom: "",
  validUntil: "",
  isActive: true,
};

const extractData = <T,>(payload: ApiEnvelope<T> | null | undefined): T | null =>
  payload && "data" in payload ? payload.data : null;

const formatDateForInput = (value?: string | null): string => {
  if (!value) return "";
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return "";
  return date.toISOString().slice(0, 10);
};

const formatDateTime = (value: string) => {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return value;
  return date.toLocaleString();
};

const parsePercentage = (value: string): number => {
  const numeric = Number.parseFloat(value);
  if (!Number.isFinite(numeric) || numeric <= 0 || numeric > 100) {
    throw new Error("Enter a discount between 0 and 100.");
  }
  return Number(numeric.toFixed(2));
};

const parseOptionalInt = (value: string): number | null => {
  if (!value.trim()) return null;
  const numeric = Number.parseInt(value, 10);
  if (!Number.isFinite(numeric) || numeric < 1) {
    throw new Error("Max redemptions must be a positive integer.");
  }
  return numeric;
};

const parseDate = (value: string): string | null => {
  if (!value.trim()) return null;
  return `${value}T00:00:00Z`;
};

const makeFormState = (coupon: CouponAdmin): CouponFormState => ({
  code: coupon.code,
  discountPercentage: coupon.discountPercentage.toString(),
  description: coupon.description ?? "",
  tierId: coupon.tierId ? String(coupon.tierId) : "",
  maxRedemptions: coupon.maxRedemptions ? String(coupon.maxRedemptions) : "",
  validFrom: formatDateForInput(coupon.validFrom),
  validUntil: formatDateForInput(coupon.validUntil),
  isActive: coupon.isActive,
});

export default function CouponManagementPage() {
  const { user, isAuthenticated, isInitialized } = useUser();
  const role = (user?.role ?? "").toLowerCase();
  const isAdmin = role === "admin";
  const api = useApi();

  const enabledConfig = useMemo(
    () => ({
      enabled: isInitialized && isAuthenticated && isAdmin,
    }),
    [isInitialized, isAuthenticated, isAdmin],
  );

  const {
    data: couponsPayload,
    loading: couponsLoading,
    error: couponsError,
    refetch: refetchCoupons,
  } = useGet<ApiEnvelope<CouponAdmin[]>>("/admin/coupons", enabledConfig);

  const { data: tiersPayload } = useGet<ApiEnvelope<TierAdminDetails[]>>("/admin/tiers", enabledConfig);
  const createCoupon = usePost<ApiEnvelope<CouponAdmin>>("/admin/coupons");

  const coupons = useMemo(() => extractData(couponsPayload) ?? [], [couponsPayload]);
  const tiers = useMemo(() => extractData(tiersPayload) ?? [], [tiersPayload]);

  const [editState, setEditState] = useState<EditState | null>(null);
  const [createState, setCreateState] = useState<CouponFormState | null>(null);
  const [deleteState, setDeleteState] = useState<DeleteState | null>(null);
  const [isMutating, setIsMutating] = useState(false);

  if (!isInitialized) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-background text-muted-foreground">
        <Loader2 className="mr-2 h-5 w-5 animate-spin" />
        Preparing management tools…
      </div>
    );
  }

  if (!isAuthenticated || !isAdmin) {
    return <AccessDenied />;
  }

  const beginCreate = () => {
    setEditState(null);
    setDeleteState(null);
    setCreateState({ ...EMPTY_FORM });
  };

  const handleCreateChange = (field: keyof CouponFormState, value: string | boolean) => {
    setCreateState((prev) =>
      prev
        ? {
            ...prev,
            [field]: value as CouponFormState[typeof field],
          }
        : prev,
    );
  };

  const handleEdit = (coupon: CouponAdmin) => {
    setCreateState(null);
    setDeleteState(null);
    setEditState({
      couponId: coupon.id,
      form: makeFormState(coupon),
    });
  };

  const handleEditChange = (field: keyof CouponFormState, value: string | boolean) => {
    setEditState((prev) =>
      prev
        ? {
            ...prev,
            form: {
              ...prev.form,
              [field]: value as CouponFormState[typeof field],
            },
          }
        : prev,
    );
  };

  const submitCreate = async () => {
    if (!createState) return;
    try {
      setIsMutating(true);
      const discount = parsePercentage(createState.discountPercentage);
      const maxRedemptions = parseOptionalInt(createState.maxRedemptions);

      await createCoupon.mutate({
        code: createState.code.trim(),
        discountPercentage: discount,
        description: createState.description.trim() || null,
        tierId: createState.tierId ? Number.parseInt(createState.tierId, 10) : null,
        maxRedemptions,
        validFrom: parseDate(createState.validFrom),
        validUntil: parseDate(createState.validUntil),
        isActive: createState.isActive,
      });

      toast.success("Coupon created.");
      setCreateState(null);
      await refetchCoupons();
    } catch (err) {
      console.error(err);
      toast.error(getApiErrorMessage(err, "Failed to create coupon."));
    } finally {
      setIsMutating(false);
    }
  };

  const submitEdit = async () => {
    if (!editState) return;
    try {
      setIsMutating(true);
      const discount = parsePercentage(editState.form.discountPercentage);
      const maxRedemptions = parseOptionalInt(editState.form.maxRedemptions);

      await api.put<ApiEnvelope<CouponAdmin>>(`/admin/coupons/${editState.couponId}`, {
        discountPercentage: discount,
        description: editState.form.description.trim() || null,
        tierId: editState.form.tierId ? Number.parseInt(editState.form.tierId, 10) : null,
        maxRedemptions,
        validFrom: parseDate(editState.form.validFrom),
        validUntil: parseDate(editState.form.validUntil),
        isActive: editState.form.isActive,
      });

      toast.success("Coupon updated.");
      setEditState(null);
      await refetchCoupons();
    } catch (err) {
      console.error(err);
      toast.error(getApiErrorMessage(err, "Failed to update coupon."));
    } finally {
      setIsMutating(false);
    }
  };

  const beginDelete = (coupon: CouponAdmin) => {
    setEditState(null);
    setCreateState(null);
    setDeleteState({ couponId: coupon.id, code: coupon.code });
  };

  const cancelDelete = () => setDeleteState(null);

  const confirmDelete = async () => {
    if (!deleteState) return;
    try {
      setIsMutating(true);
      setDeleteState((prev) => (prev ? { ...prev, error: undefined } : prev));
      await api.delete(`/admin/coupons/${deleteState.couponId}`);
      toast.success("Coupon deleted.");
      setDeleteState(null);
      await refetchCoupons();
    } catch (err) {
      console.error(err);
      const message = getApiErrorMessage(err, "Failed to delete coupon.");
      toast.error(message);
      setDeleteState((prev) => (prev ? { ...prev, error: message } : prev));
    } finally {
      setIsMutating(false);
    }
  };

  const isBusy = couponsLoading || createCoupon.loading || isMutating;

  return (
    <div className="container mx-auto min-h-screen px-4 py-10">
      <header className="mb-8 space-y-2">
        <p className="text-sm text-muted-foreground uppercase tracking-wide">Management · Promotions</p>
        <h1 className="text-3xl font-semibold tracking-tight">Coupon management</h1>
        <p className="text-muted-foreground">
          Create and maintain coupon codes for marketing campaigns and targeted promotions.
        </p>
      </header>

      {couponsError && (
        <div className="mb-6 rounded-lg border border-destructive/40 bg-destructive/10 p-4 text-sm text-destructive">
          {getApiErrorMessage(couponsError, "Unable to load coupons.")}
        </div>
      )}

      <Card className="mb-8">
        <CardHeader>
          <div className="flex items-center justify-between">
            <CardTitle className="flex items-center gap-2">
              <BadgePercent className="h-5 w-5 text-primary" />
              Coupons
            </CardTitle>
            {!createState ? (
              <Button type="button" size="sm" onClick={beginCreate} disabled={isBusy}>
                <Plus className="mr-1 h-4 w-4" />
                New coupon
              </Button>
            ) : null}
          </div>
          <CardDescription>Coupons are case-insensitive and can target a specific tier.</CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          {createState ? (
            <div className="space-y-3 rounded-lg border border-dashed p-4">
              <div className="grid gap-3 sm:grid-cols-2">
                <div className="space-y-1">
                  <label className="text-xs font-medium text-muted-foreground">Coupon code</label>
                  <Input
                    value={createState.code}
                    onChange={(event) => handleCreateChange("code", event.target.value.toUpperCase())}
                    placeholder="WELCOME10"
                  />
                </div>
                <div className="space-y-1">
                  <label className="text-xs font-medium text-muted-foreground">Discount (%)</label>
                  <Input
                    value={createState.discountPercentage}
                    onChange={(event) => handleCreateChange("discountPercentage", event.target.value)}
                    placeholder="10"
                  />
                </div>
              </div>
              <div className="grid gap-3 sm:grid-cols-2">
                <div className="space-y-1">
                  <label className="text-xs font-medium text-muted-foreground">Tier limitation</label>
                  <select
                    className="h-10 w-full rounded-md border border-input bg-background px-3 text-sm"
                    value={createState.tierId}
                    onChange={(event) => handleCreateChange("tierId", event.target.value)}
                    disabled={isBusy}
                  >
                    <option value="">All tiers</option>
                    {tiers.map((tier) => (
                      <option key={tier.tierId} value={tier.tierId}>
                        {tier.name}
                      </option>
                    ))}
                  </select>
                </div>
                <div className="space-y-1">
                  <label className="text-xs font-medium text-muted-foreground">Max redemptions</label>
                  <Input
                    value={createState.maxRedemptions}
                    onChange={(event) => handleCreateChange("maxRedemptions", event.target.value)}
                    placeholder="500"
                  />
                </div>
              </div>
              <div className="grid gap-3 sm:grid-cols-2">
                <div className="space-y-1">
                  <label className="text-xs font-medium text-muted-foreground">Valid from</label>
                  <Input
                    type="date"
                    value={createState.validFrom}
                    onChange={(event) => handleCreateChange("validFrom", event.target.value)}
                  />
                </div>
                <div className="space-y-1">
                  <label className="text-xs font-medium text-muted-foreground">Valid until</label>
                  <Input
                    type="date"
                    value={createState.validUntil}
                    onChange={(event) => handleCreateChange("validUntil", event.target.value)}
                  />
                </div>
              </div>
              <div className="space-y-1">
                <label className="text-xs font-medium text-muted-foreground">Description</label>
                <Input
                  value={createState.description}
                  onChange={(event) => handleCreateChange("description", event.target.value)}
                  placeholder="Optional description"
                />
              </div>
              <div className="flex h-10 items-center gap-2 rounded-md border px-3">
                <Switch
                  checked={createState.isActive}
                  onCheckedChange={(checked) => handleCreateChange("isActive", checked)}
                  disabled={isBusy}
                />
                <span className="text-xs text-muted-foreground">
                  {createState.isActive ? "Active" : "Inactive"}
                </span>
              </div>
              <div className="flex items-center justify-end gap-2">
                <Button type="button" variant="ghost" onClick={() => setCreateState(null)} disabled={isBusy}>
                  <X className="mr-1 h-4 w-4" />
                  Cancel
                </Button>
                <Button type="button" onClick={submitCreate} disabled={isBusy}>
                  {isBusy ? (
                    <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                  ) : (
                    <Check className="mr-2 h-4 w-4" />
                  )}
                  Save
                </Button>
              </div>
            </div>
          ) : null}

          <Separator />

          <div className="space-y-3">
            {coupons.map((coupon) => {
              const isEditing = editState?.couponId === coupon.id;
              const isDeleting = deleteState?.couponId === coupon.id;

              let body: JSX.Element;
              if (isEditing) {
                body = (
                  <div className="space-y-3">
                    <div className="grid gap-3 sm:grid-cols-2">
                      <div className="space-y-1">
                        <label className="text-xs font-medium text-muted-foreground">Coupon code</label>
                        <Input value={editState.form.code} readOnly />
                      </div>
                      <div className="space-y-1">
                        <label className="text-xs font-medium text-muted-foreground">Discount (%)</label>
                        <Input
                          value={editState.form.discountPercentage}
                          onChange={(event) => handleEditChange("discountPercentage", event.target.value)}
                        />
                      </div>
                    </div>
                    <div className="grid gap-3 sm:grid-cols-2">
                      <div className="space-y-1">
                        <label className="text-xs font-medium text-muted-foreground">Tier limitation</label>
                        <select
                          className="h-10 w-full rounded-md border border-input bg-background px-3 text-sm"
                          value={editState.form.tierId}
                          onChange={(event) => handleEditChange("tierId", event.target.value)}
                          disabled={isBusy}
                        >
                          <option value="">All tiers</option>
                          {tiers.map((tier) => (
                            <option key={tier.tierId} value={tier.tierId}>
                              {tier.name}
                            </option>
                          ))}
                        </select>
                      </div>
                      <div className="space-y-1">
                        <label className="text-xs font-medium text-muted-foreground">Max redemptions</label>
                        <Input
                          value={editState.form.maxRedemptions}
                          onChange={(event) => handleEditChange("maxRedemptions", event.target.value)}
                        />
                      </div>
                    </div>
                    <div className="grid gap-3 sm:grid-cols-2">
                      <div className="space-y-1">
                        <label className="text-xs font-medium text-muted-foreground">Valid from</label>
                        <Input
                          type="date"
                          value={editState.form.validFrom}
                          onChange={(event) => handleEditChange("validFrom", event.target.value)}
                        />
                      </div>
                      <div className="space-y-1">
                        <label className="text-xs font-medium text-muted-foreground">Valid until</label>
                        <Input
                          type="date"
                          value={editState.form.validUntil}
                          onChange={(event) => handleEditChange("validUntil", event.target.value)}
                        />
                      </div>
                    </div>
                    <div className="space-y-1">
                      <label className="text-xs font-medium text-muted-foreground">Description</label>
                      <Input
                        value={editState.form.description}
                        onChange={(event) => handleEditChange("description", event.target.value)}
                      />
                    </div>
                    <div className="flex h-10 items-center gap-2 rounded-md border px-3">
                      <Switch
                        checked={editState.form.isActive}
                        onCheckedChange={(checked) => handleEditChange("isActive", checked)}
                        disabled={isBusy}
                      />
                      <span className="text-xs text-muted-foreground">
                        {editState.form.isActive ? "Active" : "Inactive"}
                      </span>
                    </div>
                    <div className="flex items-center justify-end gap-2">
                      <Button type="button" variant="ghost" onClick={() => setEditState(null)} disabled={isBusy}>
                        <X className="mr-1 h-4 w-4" />
                        Cancel
                      </Button>
                      <Button type="button" onClick={submitEdit} disabled={isBusy}>
                        {isBusy ? (
                          <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                        ) : (
                          <Check className="mr-2 h-4 w-4" />
                        )}
                        Save
                      </Button>
                    </div>
                  </div>
                );
              } else if (isDeleting) {
                body = (
                  <div className="space-y-2 text-xs text-muted-foreground">
                    <p>Confirm deletion in the dialog to remove this coupon.</p>
                    {deleteState?.error ? <p className="text-destructive">{deleteState.error}</p> : null}
                  </div>
                );
              } else {
                body = (
                  <div className="space-y-3">
                    <div className="flex flex-wrap items-center justify-between gap-2">
                      <h3 className="text-sm font-semibold uppercase tracking-wide">{coupon.code}</h3>
                      <Badge variant={coupon.isActive ? "default" : "outline"}>
                        {coupon.isActive ? "Active" : "Inactive"}
                      </Badge>
                    </div>
                    <p className="text-xs text-muted-foreground">
                      {coupon.discountPercentage}% off · {coupon.tierName ?? "All tiers"}
                    </p>
                    <div className="grid gap-2 text-xs text-muted-foreground sm:grid-cols-2">
                      <div>
                        <span className="font-medium">Valid:</span>{" "}
                        {coupon.validFrom ? formatDateForInput(coupon.validFrom) : "Anytime"} →{" "}
                        {coupon.validUntil ? formatDateForInput(coupon.validUntil) : "No expiry"}
                      </div>
                      <div>
                        <span className="font-medium">Redemptions:</span> {coupon.redemptionCount}
                        {coupon.maxRedemptions ? ` / ${coupon.maxRedemptions}` : ""}
                      </div>
                    </div>
                    {coupon.description ? (
                      <p className="text-xs text-muted-foreground">{coupon.description}</p>
                    ) : null}
                    <div className="grid gap-2 text-xs text-muted-foreground sm:grid-cols-2">
                      <div>
                        <span className="font-medium">Created:</span> {formatDateTime(coupon.createdAt)}
                      </div>
                      <div>
                        <span className="font-medium">Updated:</span> {formatDateTime(coupon.updatedAt)}
                      </div>
                    </div>
                    <div className="flex items-center gap-2">
                      <Button size="sm" variant="outline" onClick={() => handleEdit(coupon)} disabled={isBusy}>
                        <Pencil className="mr-1 h-4 w-4" />
                        Edit
                      </Button>
                      <Button size="sm" variant="ghost" onClick={() => beginDelete(coupon)} disabled={isBusy}>
                        <Trash2 className="mr-1 h-4 w-4" />
                        Delete
                      </Button>
                    </div>
                  </div>
                );
              }

              return (
                <div key={coupon.id} className="rounded-lg border p-4">
                  {body}
                </div>
              );
            })}
          </div>

          {couponsLoading && coupons.length === 0 && (
            <div className="flex items-center justify-center text-sm text-muted-foreground">
              <Loader2 className="mr-2 h-4 w-4 animate-spin" />
              Loading coupons…
            </div>
          )}

      {!couponsLoading && coupons.length === 0 && !createState ? (
        <div className="rounded-lg border border-dashed p-6 text-center text-sm text-muted-foreground">
          No coupons have been created yet.
        </div>
      ) : null}
    </CardContent>
    </Card>

    <ConfirmDialog
      open={Boolean(deleteState)}
      title="Delete coupon?"
      description={
        deleteState ? `Remove the coupon ${deleteState.code}? This action cannot be undone.` : undefined
      }
      confirmLabel="Delete"
      destructive
      busy={isMutating}
      busyLabel="Deleting…"
      onCancel={cancelDelete}
      onConfirm={confirmDelete}
    />
  </div>
);
}
