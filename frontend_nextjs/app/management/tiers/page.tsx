"use client";

import type { JSX } from "react";
import { useMemo, useState } from "react";
import { toast } from "sonner";
import { Loader2, Pencil, Plus, Trash2, X, Check } from "lucide-react";
import AccessDenied from "@/components/AccessDenied";
import { useUser } from "@/contexts/UserContext";
import { useGet, usePost, useApi } from "@/hooks/useApi";
import { ApiError, TierAdminDetails, TierAdminBillingCycle } from "@/hooks/types";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Switch } from "@/components/ui/switch";
import { Badge } from "@/components/ui/badge";
import { Separator } from "@/components/ui/separator";
import { formatCurrency } from "@/app/plans/plan-utils";
import { ConfirmDialog } from "@/components/modals/ConfirmDialog";

type ApiEnvelope<T> = { data: T; meta?: any };

type CycleFormState = {
  billingPeriod: string;
  amount: string;
  durationMonths: string;
  description: string;
  isActive: boolean;
};

type EditState = {
  tierId: number;
  cycleId: number;
  form: CycleFormState;
};

type CreateState = {
  tierId: number;
  form: CycleFormState;
};

type DeleteState = {
  tierId: number;
  cycleId: number;
  billingPeriod: string;
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

const EMPTY_FORM: CycleFormState = {
  billingPeriod: "",
  amount: "",
  durationMonths: "",
  description: "",
  isActive: true,
};

const extractData = <T,>(payload: ApiEnvelope<T> | null | undefined): T | null =>
  payload && "data" in payload ? payload.data : null;

const toAmountMinor = (value: string): number => {
  const numeric = Number(value);
  if (!Number.isFinite(numeric) || numeric <= 0) {
    throw new Error("Enter a valid positive amount.");
  }
  return Math.round(numeric * 100);
};

const toDuration = (value: string): number => {
  if (!value.trim()) {
    throw new Error("Duration is required.");
  }
  const parsed = Number.parseInt(value, 10);
  if (!Number.isFinite(parsed) || parsed < 0) {
    throw new Error("Duration must be zero or a positive number of months.");
  }
  return parsed;
};

const makeFormState = (cycle: TierAdminBillingCycle): CycleFormState => ({
  billingPeriod: cycle.billingPeriod,
  amount: (cycle.amount / 100).toFixed(2),
  durationMonths: String(cycle.durationMonths),
  description: cycle.description ?? "",
  isActive: cycle.isActive,
});

export default function TierManagementPage() {
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
    data: tiersPayload,
    loading,
    error,
    refetch,
  } = useGet<ApiEnvelope<TierAdminDetails[]>>("/admin/tiers", enabledConfig);

  const createCycle = usePost<ApiEnvelope<TierAdminBillingCycle>>("/admin/tiers/billing-cycles");

  const tiers = useMemo(() => extractData(tiersPayload) ?? [], [tiersPayload]);

  const [editState, setEditState] = useState<EditState | null>(null);
  const [createState, setCreateState] = useState<CreateState | null>(null);
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

  const handleEdit = (tierId: number, cycle: TierAdminBillingCycle) => {
    setCreateState(null);
    setDeleteState(null);
    setEditState({
      tierId,
      cycleId: cycle.id,
      form: makeFormState(cycle),
    });
  };

  const handleEditChange = (field: keyof CycleFormState, value: string | boolean) => {
    setEditState((prev) =>
      prev
        ? {
            ...prev,
            form: {
              ...prev.form,
              [field]: value as CycleFormState[typeof field],
            },
          }
        : prev,
    );
  };

  const handleCreateChange = (field: keyof CycleFormState, value: string | boolean) => {
    setCreateState((prev) =>
      prev
        ? {
            ...prev,
            form: {
              ...prev.form,
              [field]: value as CycleFormState[typeof field],
            },
          }
        : prev,
    );
  };

  const submitEdit = async () => {
    if (!editState) return;
    try {
      setIsMutating(true);
      const amount = toAmountMinor(editState.form.amount);
      const duration = toDuration(editState.form.durationMonths);

      await api.put<ApiEnvelope<TierAdminBillingCycle>>(`/admin/tiers/billing-cycles/${editState.cycleId}`, {
        billingPeriod: editState.form.billingPeriod.trim(),
        amount,
        durationMonths: duration,
        description: editState.form.description.trim() || null,
        isActive: editState.form.isActive,
      });

      toast.success("Billing cycle updated.");
      setEditState(null);
      await refetch();
    } catch (err) {
      console.error(err);
      toast.error(getApiErrorMessage(err, "Failed to update billing cycle."));
    } finally {
      setIsMutating(false);
    }
  };

  const submitCreate = async () => {
    if (!createState) return;
    try {
      setIsMutating(true);
      const amount = toAmountMinor(createState.form.amount);
      const duration = toDuration(createState.form.durationMonths);

      await createCycle.mutate({
        tierId: createState.tierId,
        billingPeriod: createState.form.billingPeriod.trim(),
        amount,
        durationMonths: duration,
        description: createState.form.description.trim() || null,
        isActive: createState.form.isActive,
      });

      toast.success("Billing cycle created.");
      setCreateState(null);
      await refetch();
    } catch (err) {
      console.error(err);
      toast.error(getApiErrorMessage(err, "Failed to create billing cycle."));
    } finally {
      setIsMutating(false);
    }
  };

  const beginDelete = (tierId: number, cycle: TierAdminBillingCycle) => {
    setEditState(null);
    setCreateState(null);
    setDeleteState({ tierId, cycleId: cycle.id, billingPeriod: cycle.billingPeriod });
  };

  const cancelDelete = () => setDeleteState(null);

  const confirmDelete = async () => {
    if (!deleteState) return;
    try {
      setIsMutating(true);
      setDeleteState((prev) => (prev ? { ...prev, error: undefined } : prev));
      await api.delete(`/admin/tiers/billing-cycles/${deleteState.cycleId}`);
      toast.success("Billing cycle deleted.");
      setDeleteState(null);
      await refetch();
    } catch (err) {
      console.error(err);
      const message = getApiErrorMessage(err, "Failed to delete billing cycle.");
      toast.error(message);
      setDeleteState((prev) => (prev ? { ...prev, error: message } : prev));
    } finally {
      setIsMutating(false);
    }
  };

  const isBusy = loading || isMutating || createCycle.loading;

  return (
    <div className="container mx-auto min-h-screen px-4 py-10">
      <header className="mb-8 space-y-2">
        <p className="text-sm text-muted-foreground uppercase tracking-wide">Management · Plans</p>
        <h1 className="text-3xl font-semibold tracking-tight">Tier management</h1>
        <p className="text-muted-foreground">
          Adjust subscription tiers, billing cycles, and availability without touching configuration files.
        </p>
      </header>

      {error && (
        <div className="mb-6 rounded-lg border border-destructive/40 bg-destructive/10 p-4 text-sm text-destructive">
          {getApiErrorMessage(error, "Unable to load tier details.")}
        </div>
      )}

      <div className="grid gap-6 md:grid-cols-2">
        {tiers.map((tier) => {
          const isCreatingForTier = createState?.tierId === tier.tierId;

          return (
            <Card key={tier.tierId} className="flex h-full flex-col">
              <CardHeader>
                <div className="flex items-center justify-between">
                  <CardTitle className="text-2xl capitalize">{tier.name}</CardTitle>
                  <Badge variant="outline">{tier.currency}</Badge>
                </div>
                <CardDescription>{tier.description ?? "No description provided."}</CardDescription>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="space-y-3">
                  {tier.billingCycles.length === 0 ? (
                    <div className="rounded-lg border border-dashed p-4 text-sm text-muted-foreground">
                      No billing cycles configured yet.
                    </div>
                  ) : (
                    tier.billingCycles.map((cycle) => {
                      const isEditing = editState?.cycleId === cycle.id;
                      const isDeleting = deleteState?.cycleId === cycle.id;
                      let body: JSX.Element;

                      if (isEditing) {
                        body = (
                          <div className="space-y-3">
                            <div className="grid gap-3 sm:grid-cols-2">
                              <div className="space-y-1">
                                <label className="text-xs font-medium text-muted-foreground">Billing period</label>
                                <Input
                                  value={editState.form.billingPeriod}
                                  onChange={(event) => handleEditChange("billingPeriod", event.target.value)}
                                  placeholder="monthly"
                                />
                              </div>
                              <div className="space-y-1">
                                <label className="text-xs font-medium text-muted-foreground">Price ({tier.currency})</label>
                                <Input
                                  value={editState.form.amount}
                                  onChange={(event) => handleEditChange("amount", event.target.value)}
                                  placeholder="69.00"
                                />
                              </div>
                            </div>
                            <div className="grid gap-3 sm:grid-cols-2">
                              <div className="space-y-1">
                                <label className="text-xs font-medium text-muted-foreground">Duration (months)</label>
                                <Input
                                  value={editState.form.durationMonths}
                                  onChange={(event) => handleEditChange("durationMonths", event.target.value)}
                                  placeholder="1"
                                />
                              </div>
                              <div className="space-y-1">
                                <label className="text-xs font-medium text-muted-foreground">Active</label>
                                <div className="flex h-10 items-center gap-2 rounded-md border px-3">
                                  <Switch
                                    checked={editState.form.isActive}
                                    onCheckedChange={(checked) => handleEditChange("isActive", checked)}
                                    disabled={isBusy}
                                  />
                                  <span className="text-xs text-muted-foreground">
                                    {editState.form.isActive ? "Available for purchase" : "Hidden"}
                                  </span>
                                </div>
                              </div>
                            </div>
                            <div className="space-y-1">
                              <label className="text-xs font-medium text-muted-foreground">Description</label>
                              <Input
                                value={editState.form.description}
                                onChange={(event) => handleEditChange("description", event.target.value)}
                                placeholder="Optional description override"
                              />
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
                            <p>Confirm deletion in the dialog to remove this plan.</p>
                            {deleteState?.error ? <p className="text-destructive">{deleteState.error}</p> : null}
                          </div>
                        );
                      } else {
                        body = (
                          <div className="space-y-3">
                            <div className="flex flex-wrap items-center justify-between gap-2">
                              <div>
                                <p className="text-sm font-semibold capitalize">{cycle.billingPeriod}</p>
                                <p className="text-xs text-muted-foreground">
                                  {formatCurrency(cycle.amount, tier.currency)} - {cycle.durationMonths || 0} month
                                  {cycle.durationMonths === 1 ? "" : "s"}
                                </p>
                              </div>
                              <Badge variant={cycle.isActive ? "default" : "outline"}>
                                {cycle.isActive ? "Active" : "Inactive"}
                              </Badge>
                            </div>
                            {cycle.description ? (
                              <p className="text-xs text-muted-foreground">{cycle.description}</p>
                            ) : null}
                            <div className="flex items-center gap-2">
                              <Button
                                size="sm"
                                variant="outline"
                                onClick={() => handleEdit(tier.tierId, cycle)}
                                disabled={isBusy}
                              >
                                <Pencil className="mr-1 h-4 w-4" />
                                Edit
                              </Button>
                              <Button
                                size="sm"
                                variant="ghost"
                                onClick={() => beginDelete(tier.tierId, cycle)}
                                disabled={isBusy}
                              >
                                <Trash2 className="mr-1 h-4 w-4" />
                                Delete
                              </Button>
                            </div>
                          </div>
                        );
                      }

              return (
                <div key={cycle.id} className="rounded-lg border p-4">
                  {body}
                </div>
              );
                    })
                  )}
                </div>

                <Separator />

                {isCreatingForTier ? (
                  <div className="space-y-3 rounded-lg border border-dashed p-4">
                    <div className="grid gap-3 sm:grid-cols-2">
                      <div className="space-y-1">
                        <label className="text-xs font-medium text-muted-foreground">Billing period</label>
                        <Input
                          value={createState.form.billingPeriod}
                          onChange={(event) => handleCreateChange("billingPeriod", event.target.value)}
                          placeholder="monthly"
                        />
                      </div>
                      <div className="space-y-1">
                        <label className="text-xs font-medium text-muted-foreground">Price ({tier.currency})</label>
                        <Input
                          value={createState.form.amount}
                          onChange={(event) => handleCreateChange("amount", event.target.value)}
                          placeholder="69.00"
                        />
                      </div>
                    </div>
                    <div className="grid gap-3 sm:grid-cols-2">
                      <div className="space-y-1">
                        <label className="text-xs font-medium text-muted-foreground">Duration (months)</label>
                        <Input
                          value={createState.form.durationMonths}
                          onChange={(event) => handleCreateChange("durationMonths", event.target.value)}
                          placeholder="1"
                        />
                      </div>
                      <div className="space-y-1">
                        <label className="text-xs font-medium text-muted-foreground">Active</label>
                        <div className="flex h-10 items-center gap-2 rounded-md border px-3">
                          <Switch
                            checked={createState.form.isActive}
                            onCheckedChange={(checked) => handleCreateChange("isActive", checked)}
                            disabled={isBusy}
                          />
                          <span className="text-xs text-muted-foreground">
                            {createState.form.isActive ? "Available for purchase" : "Hidden"}
                          </span>
                        </div>
                      </div>
                    </div>
                    <div className="space-y-1">
                      <label className="text-xs font-medium text-muted-foreground">Description</label>
                      <Input
                        value={createState.form.description}
                        onChange={(event) => handleCreateChange("description", event.target.value)}
                        placeholder="Optional description override"
                      />
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
                ) : (
                  <Button
                    type="button"
                    size="sm"
                    variant="outline"
                    onClick={() =>
                      setCreateState({
                        tierId: tier.tierId,
                        form: { ...EMPTY_FORM, isActive: true },
                      })
                    }
                    disabled={isBusy}
                  >
                    <Plus className="mr-1 h-4 w-4" />
                    Add billing cycle
                  </Button>
                )}
              </CardContent>
            </Card>
          );
        })}
      </div>

      {loading && tiers.length === 0 && (
        <div className="mt-10 flex items-center justify-center text-muted-foreground">
          <Loader2 className="mr-2 h-5 w-5 animate-spin" />
          Loading tiers…
        </div>
      )}

      <ConfirmDialog
        open={Boolean(deleteState)}
        title="Delete billing cycle?"
        description={deleteState ? `Remove the ${deleteState.billingPeriod} plan? Existing subscribers keep their access.` : undefined}
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
