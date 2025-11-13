"use client";

import type { ChangeEvent, FormEvent } from "react";
import { useEffect, useMemo, useRef, useState } from "react";
import { useUser } from "@/contexts/UserContext";
import AccessDenied from "@/components/AccessDenied";
import { apiUtils, useGet, usePost, usePut } from "@/hooks";
import type {
  AdminUserDetails,
  AdminUserDetailsResponse,
  AdminUserListResponse,
  AdminUpdateUserRequest,
  AdminUpgradeUserTierRequest,
  PagedMeta,
  TierAdminDetails,
} from "@/hooks/types";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Switch } from "@/components/ui/switch";
import { Separator } from "@/components/ui/separator";
import { toast } from "sonner";
import { Loader2, RefreshCcw, Search, ShieldCheck, Users as UsersIcon, Info } from "lucide-react";
import { cn } from "@/lib/utils";

type ApiEnvelope<T> = {
  data: T;
  meta?: any;
};

const ROLE_OPTIONS = ["admin", "mod", "user"];
const PAGE_SIZE = 20;

type AdminUserFormState = {
  email: string;
  emailVerified: boolean;
  username: string;
  firstName: string;
  lastName: string;
  displayName: string;
  bio: string;
  timezone: string;
  locale: string;
  verifiedBadge: boolean;
  isActive: boolean;
  roles: string[];
};

type TierAssignmentFormState = {
  tierId: number | null;
  activeFrom: string;
  activeUntil: string;
  notes: string;
};

const normalizeForCompare = (state: AdminUserFormState) => ({
  ...state,
  roles: [...state.roles].map((role) => role.toLowerCase()).sort(),
});

const toFormState = (details: AdminUserDetails): AdminUserFormState => {
  const roles = details.profile.roles?.length
    ? Array.from(new Set(details.profile.roles.map((role) => role.toLowerCase())))
    : ["user"];

  return {
    email: details.profile.email ?? "",
    emailVerified: details.profile.emailVerified,
    username: details.profile.username ?? "",
    firstName: details.profile.firstName ?? "",
    lastName: details.profile.lastName ?? "",
    displayName: details.profile.displayName ?? "",
    bio: details.profile.bio ?? "",
    timezone: details.profile.timezone ?? "",
    locale: details.profile.locale ?? "",
    verifiedBadge: details.profile.verifiedBadge,
    isActive: details.isActive,
    roles,
  };
};

const buildDisplayName = (user: {
  displayName?: string | null;
  firstName?: string | null;
  lastName?: string | null;
  username: string;
}) => {
  const fullName = `${user.firstName ?? ""} ${user.lastName ?? ""}`.trim();
  return user.displayName && user.displayName.trim().length > 0
    ? user.displayName
    : fullName.length > 0
    ? fullName
    : user.username;
};

const formatDateTime = (value?: string | null) => {
  if (!value) return "-";
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return value;
  return date.toLocaleString();
};

const formatDate = (value?: string | null) => {
  if (!value) return "-";
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return value;
  return date.toLocaleDateString();
};

const optionalString = (value: string) => {
  const trimmed = value.trim();
  return trimmed.length ? trimmed : null;
};

const toIsoIfValid = (value: string) => {
  if (!value) return undefined;
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return undefined;
  return date.toISOString();
};

const ensureRoleLabel = (role: string) => role.charAt(0).toUpperCase() + role.slice(1);

export default function AdminUsersPage() {
  const { user, isAuthenticated, isInitialized } = useUser();
  const isAdmin = (user?.role ?? "").toLowerCase() === "admin";

  const [searchTerm, setSearchTerm] = useState("");
  const [appliedSearch, setAppliedSearch] = useState("");
  const [page, setPage] = useState(1);
  const [selectedUserId, setSelectedUserId] = useState<string | null>(null);
  const [formState, setFormState] = useState<AdminUserFormState | null>(null);
  const initialFormRef = useRef<AdminUserFormState | null>(null);
  const [tierForm, setTierForm] = useState<TierAssignmentFormState>({
    tierId: null,
    activeFrom: "",
    activeUntil: "",
    notes: "",
  });
  const viewedProfileIdRef = useRef<string | null>(null);

  const listEndpoint = useMemo(
    () =>
      apiUtils.withQuery("/admin/users", {
        search: appliedSearch || undefined,
        page,
        pageSize: PAGE_SIZE,
      }),
    [appliedSearch, page]
  );

  const listEnabled = isAdmin && isInitialized;
  const listConfig = useMemo(
    () => ({
      enabled: listEnabled,
    }),
    [listEnabled]
  );

  const {
    data: listData,
    loading: listLoading,
    error: listError,
    refetch: refetchUsers,
  } = useGet<AdminUserListResponse>(listEndpoint, listConfig);

  const users = useMemo(() => listData?.data ?? [], [listData?.data]);
  const meta: PagedMeta | null = listData?.meta ?? null;
  const totalPages = useMemo(() => {
    if (!meta) return 1;
    return Math.max(1, Math.ceil(meta.total / meta.pageSize));
  }, [meta]);

  useEffect(() => {
    if (!isAdmin) return;
    if (!users.length) {
      setSelectedUserId(null);
      return;
    }
    if (!selectedUserId || !users.some((item) => item.id === selectedUserId)) {
      setSelectedUserId(users[0].id);
    }
  }, [isAdmin, users, selectedUserId]);

  const detailsEndpoint = selectedUserId ? `/admin/users/${selectedUserId}` : "";
  const shouldFetchDetails = isAdmin && !!selectedUserId;
  const detailsConfig = useMemo(
    () => ({
      enabled: shouldFetchDetails,
    }),
    [shouldFetchDetails]
  );
  const {
    data: detailsEnvelope,
    loading: detailsLoading,
    error: detailsError,
    refetch: refetchDetails,
  } = useGet<AdminUserDetailsResponse>(detailsEndpoint, detailsConfig);
  const details = detailsEnvelope?.data;

  const tiersConfig = useMemo(
    () => ({
      enabled: isAdmin && isInitialized,
    }),
    [isAdmin, isInitialized]
  );

  const { data: tiersEnvelope, loading: tiersLoading } = useGet<ApiEnvelope<TierAdminDetails[]>>(
    "/admin/tiers",
    tiersConfig
  );
  const tierOptions = useMemo(() => tiersEnvelope?.data ?? [], [tiersEnvelope?.data]);

  const updateUserConfig = useMemo(
    () => ({
      requireAuth: true as const,
    }),
    []
  );

  const { mutate: updateUser, loading: isUpdating } = usePut<AdminUserDetailsResponse>(
    detailsEndpoint,
    updateUserConfig
  );
  const assignTierEndpoint = selectedUserId ? `${detailsEndpoint}/tier` : "";
  const { mutate: assignTier, loading: isAssigning } = usePost<AdminUserDetailsResponse>(
    assignTierEndpoint,
    updateUserConfig
  );

  useEffect(() => {
    if (details) {
      const nextState = toFormState(details);
      setFormState(nextState);
      initialFormRef.current = nextState;
    } else if (!detailsLoading && !shouldFetchDetails) {
      setFormState(null);
      initialFormRef.current = null;
    }
  }, [details, detailsLoading, shouldFetchDetails]);

  useEffect(() => {
    if (details?.profile?.id) {
      setTierForm({
        tierId: details.activeTier?.tierId ?? null,
        activeFrom: "",
        activeUntil: "",
        notes: "",
      });
    } else if (!detailsLoading && !shouldFetchDetails) {
      setTierForm({
        tierId: null,
        activeFrom: "",
        activeUntil: "",
        notes: "",
      });
    }
  }, [details?.profile?.id, details?.activeTier?.tierId, detailsLoading, shouldFetchDetails]);

  useEffect(() => {
    if (tierForm.tierId !== null) return;
    if (!tierOptions.length) return;
    setTierForm((prev) => ({
      ...prev,
      tierId: tierOptions[0].tierId,
    }));
  }, [tierOptions, tierForm.tierId]);

  const isDirty = useMemo(() => {
    if (!formState || !initialFormRef.current) return false;
    return (
      JSON.stringify(normalizeForCompare(formState)) !==
      JSON.stringify(normalizeForCompare(initialFormRef.current))
    );
  }, [formState]);

  const selectableRoles = useMemo(() => {
    const current = formState?.roles ?? [];
    return Array.from(new Set([...ROLE_OPTIONS, ...current]));
  }, [formState?.roles]);

  if (!isInitialized) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-background">
        <Loader2 className="h-8 w-8 animate-spin text-primary" />
      </div>
    );
  }

  if (!isAuthenticated) {
    return <AccessDenied />;
  }

  if (!isAdmin) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-background px-4">
        <Card className="max-w-lg">
          <CardHeader>
            <CardTitle>Admin Access Required</CardTitle>
            <CardDescription>
              You need administrator privileges to manage users. Please contact your workspace owner if
              you believe this is a mistake.
            </CardDescription>
          </CardHeader>
          <CardContent className="flex items-center gap-3 text-sm text-muted-foreground">
            <ShieldCheck className="h-5 w-5 text-primary" />
            <span>Only admins can access management tools.</span>
          </CardContent>
        </Card>
      </div>
    );
  }

  const start = meta ? Math.min(meta.total, (meta.page - 1) * meta.pageSize + 1) : users.length ? 1 : 0;
  const end = meta ? Math.min(meta.total, meta.page * meta.pageSize) : users.length;
  const totalCount = meta?.total ?? users.length;

  const handleApplyFilters = () => {
    setPage(1);
    setAppliedSearch(searchTerm.trim());
  };

  const handleRoleToggle = (role: string) => {
    setFormState((prev) => {
      if (!prev) return prev;
      const normalizedRole = role.toLowerCase();
      const current = new Set(prev.roles);

      if (current.has(normalizedRole)) {
        if (current.size === 1) {
          toast.error("Users must have at least one role.");
          return prev;
        }
        current.delete(normalizedRole);
      } else {
        current.add(normalizedRole);
      }

      return {
        ...prev,
        roles: Array.from(current),
      };
    });
  };

  const handleInputChange =
    (field: keyof AdminUserFormState) =>
    (event: ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => {
      const value = event.target.value;
      setFormState((prev) => (prev ? { ...prev, [field]: value } : prev));
    };

  const handleReset = () => {
    if (!initialFormRef.current) return;
    const resetState = {
      ...initialFormRef.current,
      roles: [...initialFormRef.current.roles],
    };
    setFormState(resetState);
  };

  const handleSave = async () => {
    if (!selectedUserId || !formState) return;

    const email = formState.email.trim();
    const username = formState.username.trim();

    if (!email) {
      toast.error("Email is required.");
      return;
    }

    if (!username) {
      toast.error("Username is required.");
      return;
    }

    const roles = formState.roles.map((role) => role.trim()).filter(Boolean);
    if (!roles.length) {
      toast.error("Assign at least one role.");
      return;
    }

    const payload: AdminUpdateUserRequest = {
      email,
      emailVerified: formState.emailVerified,
      username,
      firstName: optionalString(formState.firstName),
      lastName: optionalString(formState.lastName),
      displayName: optionalString(formState.displayName),
      bio: optionalString(formState.bio),
      timezone: optionalString(formState.timezone),
      locale: optionalString(formState.locale),
      verifiedBadge: formState.verifiedBadge,
      isActive: formState.isActive,
      roles,
    };

    try {
      const response = await updateUser(payload);
      if (response.status >= 200 && response.status < 300 && response.data) {
        const updatedDetails = response.data.data;
        const nextState = toFormState(updatedDetails);
        setFormState(nextState);
        initialFormRef.current = nextState;
        toast.success("User updated successfully.");
        refetchUsers();
        refetchDetails();
      }
    } catch (error: any) {
      if (error?.data?.title) {
        toast.error(error.data.title);
      } else {
        toast.error(error?.message ?? "Failed to update user.");
      }
    }
  };

  const updateTierFormField = <K extends keyof TierAssignmentFormState>(
    field: K,
    value: TierAssignmentFormState[K]
  ) => {
    setTierForm((prev) => ({
      ...prev,
      [field]: value,
    }));
  };

  const handleTierReset = () => {
    setTierForm({
      tierId: details?.activeTier?.tierId ?? (tierOptions[0]?.tierId ?? null),
      activeFrom: "",
      activeUntil: "",
      notes: "",
    });
  };

  const handleTierAssign = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!selectedUserId) return;
    if (!tierForm.tierId) {
      toast.error("Select a tier to assign.");
      return;
    }

    if (tierForm.activeFrom && tierForm.activeUntil) {
      const from = new Date(tierForm.activeFrom);
      const until = new Date(tierForm.activeUntil);
      if (!Number.isNaN(from.getTime()) && !Number.isNaN(until.getTime()) && until <= from) {
        toast.error("Active until must be later than active from.");
        return;
      }
    }

    const payload: AdminUpgradeUserTierRequest = {
      tierId: tierForm.tierId,
      activeFrom: toIsoIfValid(tierForm.activeFrom),
      activeUntil: toIsoIfValid(tierForm.activeUntil),
      notes: optionalString(tierForm.notes),
    };

    try {
      const response = await assignTier(payload);
      if (response.status >= 200 && response.status < 300 && response.data) {
        const updatedDetails = response.data.data;
        const nextState = toFormState(updatedDetails);
        setFormState(nextState);
        initialFormRef.current = nextState;
        setTierForm({
          tierId: updatedDetails.activeTier?.tierId ?? tierForm.tierId,
          activeFrom: "",
          activeUntil: "",
          notes: "",
        });
        toast.success("Tier assignment updated.");
        refetchUsers();
        refetchDetails();
      }
    } catch (error: any) {
      if (error?.data?.title) {
        toast.error(error.data.title);
      } else {
        toast.error(error?.message ?? "Failed to assign tier.");
      }
    }
  };

  return (
    <div className="min-h-screen bg-background py-10">
      <div className="container mx-auto px-4">
        <div className="mb-8 flex items-center justify-between">
          <div>
            <p className="text-sm text-muted-foreground mb-1">Admin • User Management</p>
            <h1 className="text-3xl font-semibold tracking-tight">Manage Users</h1>
          </div>
          <Badge variant="secondary" className="flex items-center gap-1">
            <UsersIcon className="h-4 w-4" />
            {totalCount} total
          </Badge>
        </div>

        <div className="grid gap-6 lg:grid-cols-[420px_1fr]">
          <Card className="h-full">
            <CardHeader>
              <CardTitle>User Directory</CardTitle>
              <CardDescription>
                Search, filter, and select a user to review their profile and update account details.
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="flex items-center gap-2">
                <div className="relative flex-1">
                  <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                  <Input
                    placeholder="Search by name, username, or email"
                    value={searchTerm}
                    onChange={(event) => setSearchTerm(event.target.value)}
                    className="pl-9"
                  />
                </div>
                <Button
                  type="button"
                  variant="default"
                  onClick={handleApplyFilters}
                  disabled={listLoading}
                >
                  {listLoading ? <Loader2 className="h-4 w-4 animate-spin" /> : "Apply"}
                </Button>
                <Button
                  type="button"
                  variant="outline"
                  onClick={() => refetchUsers()}
                  disabled={listLoading}
                >
                  {listLoading ? (
                    <Loader2 className="h-4 w-4 animate-spin" />
                  ) : (
                    <RefreshCcw className="h-4 w-4" />
                  )}
                </Button>
              </div>

              {listError ? (
                <div className="rounded-md border border-destructive/30 bg-destructive/10 px-3 py-4 text-sm text-destructive">
                  Unable to load users. Try refreshing the list.
                </div>
              ) : (
                <div className="space-y-2">
                  {listLoading && !users.length ? (
                    <div className="flex justify-center py-10">
                      <Loader2 className="h-6 w-6 animate-spin text-primary" />
                    </div>
                  ) : users.length ? (
                    <div className="space-y-2">
                      {users.map((item) => {
                        const isSelected = item.id === selectedUserId;
                        return (
                          <button
                            type="button"
                            key={item.id}
                            onClick={() => setSelectedUserId(item.id)}
                            className={cn(
                              "w-full rounded-lg border px-4 py-3 text-left transition-colors",
                              isSelected
                                ? "border-primary bg-primary/10"
                                : "border-border hover:bg-muted/60"
                            )}
                          >
                            <div className="flex flex-wrap items-center justify-between gap-2">
                              <div>
                                <p className="text-sm font-medium">
                                  {buildDisplayName({
                                    displayName: item.displayName,
                                    firstName: item.firstName,
                                    lastName: item.lastName,
                                    username: item.username,
                                  })}
                                </p>
                                <p className="text-xs text-muted-foreground">{item.email}</p>
                              </div>
                              <div className="flex items-center gap-2">
                                <Badge variant={item.isActive ? "default" : "secondary"}>
                                  {item.isActive ? "Active" : "Suspended"}
                                </Badge>
                                {item.activeTier?.name ? (
                                  <Badge variant="outline">{ensureRoleLabel(item.activeTier.name)}</Badge>
                                ) : (
                                  <Badge variant="outline">Free</Badge>
                                )}
                              </div>
                            </div>
                            <div className="mt-2 flex flex-wrap items-center gap-2 text-xs text-muted-foreground">
                              <span>Username: {item.username}</span>
                              <span className="h-1 w-1 rounded-full bg-muted-foreground/40" />
                              <span>Updated {formatDate(item.updatedAt)}</span>
                            </div>
                            <div className="mt-2 flex flex-wrap gap-2">
                              {item.roles.map((role) => (
                                <Badge key={role} variant="secondary">
                                  {ensureRoleLabel(role)}
                                </Badge>
                              ))}
                              {!item.roles.length && (
                                <Badge variant="secondary" className="opacity-60">
                                  No roles
                                </Badge>
                              )}
                            </div>
                          </button>
                        );
                      })}
                    </div>
                  ) : (
                    <div className="rounded-md border border-dashed px-4 py-10 text-center text-sm text-muted-foreground">
                      No users match the current filters.
                    </div>
                  )}
                </div>
              )}

              <Separator />

              <div className="flex flex-wrap items-center justify-between gap-3 text-xs text-muted-foreground">
                <div>
                  Showing{" "}
                  <span className="font-medium text-foreground">
                    {start && end ? `${start}-${end}` : "0"}
                  </span>{" "}
                  of <span className="font-medium text-foreground">{totalCount}</span>
                </div>
                <div className="flex items-center gap-2">
                  <Button
                    type="button"
                    variant="outline"
                    size="sm"
                    onClick={() => setPage((prev) => Math.max(1, prev - 1))}
                    disabled={page <= 1 || listLoading}
                  >
                    Previous
                  </Button>
                  <span className="text-foreground">
                    Page {page} of {totalPages}
                  </span>
                  <Button
                    type="button"
                    variant="outline"
                    size="sm"
                    onClick={() => setPage((prev) => Math.min(totalPages, prev + 1))}
                    disabled={page >= totalPages || listLoading}
                  >
                    Next
                  </Button>
                </div>
              </div>
            </CardContent>
          </Card>

          <Card className="h-full">
            <CardHeader>
              <CardTitle>User Details</CardTitle>
              <CardDescription>
                Review profile information, toggle account flags, and manage roles for the selected user.
              </CardDescription>
            </CardHeader>
            <CardContent>
              {!selectedUserId ? (
                <div className="rounded-md border border-dashed px-6 py-12 text-center text-sm text-muted-foreground">
                  Select a user on the left to view their details.
                </div>
              ) : detailsLoading && !details ? (
                <div className="flex justify-center py-10">
                  <Loader2 className="h-6 w-6 animate-spin text-primary" />
                </div>
              ) : detailsError ? (
                <div className="rounded-md border border-destructive/30 bg-destructive/10 px-3 py-4 text-sm text-destructive">
                  Unable to load user details. Try selecting another user or refreshing.
                </div>
              ) : formState && details ? (
                <>
                  <form
                    className="space-y-6"
                    onSubmit={(event) => {
                      event.preventDefault();
                      handleSave();
                    }}
                  >
                    <div className="flex flex-wrap items-center gap-2">
                      <Badge variant={formState.isActive ? "default" : "secondary"}>
                        {formState.isActive ? "Active account" : "Suspended"}
                      </Badge>
                    {formState.emailVerified ? (
                      <Badge variant="outline" className="flex items-center gap-1">
                        <ShieldCheck className="h-3 w-3" />
                        Email verified
                      </Badge>
                    ) : (
                      <Badge variant="outline">Email pending</Badge>
                    )}
                    <span className="text-xs text-muted-foreground">
                      Created {formatDateTime(details.profile.createdAt)}
                    </span>
                  </div>

                  <div className="space-y-4">
                    <div className="grid gap-3 md:grid-cols-2">
                      <div className="space-y-1.5">
                        <label className="text-sm font-medium">Email</label>
                        <Input
                          value={formState.email}
                          onChange={handleInputChange("email")}
                          type="email"
                          autoComplete="email"
                          disabled={isUpdating}
                        />
                      </div>
                      <div className="space-y-1.5">
                        <label className="text-sm font-medium">Username</label>
                        <Input
                          value={formState.username}
                          onChange={handleInputChange("username")}
                          autoComplete="username"
                          disabled={isUpdating}
                        />
                      </div>
                      <div className="space-y-1.5">
                        <label className="text-sm font-medium">First name</label>
                        <Input
                          value={formState.firstName}
                          onChange={handleInputChange("firstName")}
                          disabled={isUpdating}
                        />
                      </div>
                      <div className="space-y-1.5">
                        <label className="text-sm font-medium">Last name</label>
                        <Input
                          value={formState.lastName}
                          onChange={handleInputChange("lastName")}
                          disabled={isUpdating}
                        />
                      </div>
                      <div className="space-y-1.5 md:col-span-2">
                        <label className="text-sm font-medium">Display name</label>
                        <Input
                          value={formState.displayName}
                          onChange={handleInputChange("displayName")}
                          placeholder="Shown publicly if provided"
                          disabled={isUpdating}
                        />
                      </div>
                      <div className="space-y-1.5 md:col-span-2">
                        <label className="text-sm font-medium">Bio</label>
                        <textarea
                          value={formState.bio}
                          onChange={handleInputChange("bio")}
                          rows={3}
                          className="w-full rounded-md border border-input bg-background px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-ring focus:border-transparent"
                          disabled={isUpdating}
                          placeholder="Short profile description"
                        />
                      </div>
                      <div className="space-y-1.5">
                        <label className="text-sm font-medium">Timezone</label>
                        <Input
                          value={formState.timezone}
                          onChange={handleInputChange("timezone")}
                          placeholder="e.g., America/New_York"
                          disabled={isUpdating}
                        />
                      </div>
                      <div className="space-y-1.5">
                        <label className="text-sm font-medium">Locale</label>
                        <Input
                          value={formState.locale}
                          onChange={handleInputChange("locale")}
                          placeholder="e.g., en-US"
                          disabled={isUpdating}
                        />
                      </div>
                    </div>
                  </div>

                  <div className="grid gap-3 md:grid-cols-3">
                    <div className="flex items-center justify-between rounded-md border px-3 py-3">
                      <div>
                        <p className="text-sm font-medium">Email verified</p>
                        <p className="text-xs text-muted-foreground">Mark user email as verified.</p>
                      </div>
                      <Switch
                        checked={formState.emailVerified}
                        onCheckedChange={(checked) =>
                          setFormState((prev) => (prev ? { ...prev, emailVerified: checked } : prev))
                        }
                        disabled={isUpdating}
                      />
                    </div>
                    <div className="flex items-center justify-between rounded-md border px-3 py-3">
                      <div>
                        <p className="text-sm font-medium">Verified badge</p>
                        <p className="text-xs text-muted-foreground">Show verified badge on profile.</p>
                      </div>
                      <Switch
                        checked={formState.verifiedBadge}
                        onCheckedChange={(checked) =>
                          setFormState((prev) => (prev ? { ...prev, verifiedBadge: checked } : prev))
                        }
                        disabled={isUpdating}
                      />
                    </div>
                    <div className="flex items-center justify-between rounded-md border px-3 py-3">
                      <div>
                        <p className="text-sm font-medium">Account active</p>
                        <p className="text-xs text-muted-foreground">Disable to suspend account access.</p>
                      </div>
                      <Switch
                        checked={formState.isActive}
                        onCheckedChange={(checked) =>
                          setFormState((prev) => (prev ? { ...prev, isActive: checked } : prev))
                        }
                        disabled={isUpdating}
                      />
                    </div>
                  </div>

                  <div className="space-y-2">
                    <div className="flex items-center gap-2 text-sm font-medium">
                      <span>Roles</span>
                      <Badge variant="outline">{formState.roles.length}</Badge>
                    </div>
                    <p className="text-xs text-muted-foreground">
                      Assign one or more roles. Removing all roles is not permitted.
                    </p>
                    <div className="flex flex-wrap gap-2">
                      {selectableRoles.map((role) => {
                        const normalized = role.toLowerCase();
                        const selected = formState.roles.includes(normalized);
                        return (
                          <Button
                            key={normalized}
                            type="button"
                            size="sm"
                            variant={selected ? "default" : "outline"}
                            onClick={() => handleRoleToggle(normalized)}
                            disabled={isUpdating}
                          >
                            {ensureRoleLabel(normalized)}
                          </Button>
                        );
                      })}
                    </div>
                  </div>

                  <Separator />

                  <div className="space-y-3">
                    <div className="flex flex-wrap items-center justify-between gap-2">
                      <h2 className="text-sm font-semibold">Tier</h2>
                      {details.activeTier ? (
                        <Badge variant="secondary" className="uppercase">
                          {details.activeTier.name}
                        </Badge>
                      ) : (
                        <Badge variant="outline">Free tier</Badge>
                      )}
                    </div>
                    <div className="rounded-md border bg-muted/40 px-3 py-3 text-xs text-muted-foreground">
                      <div className="flex items-start gap-2">
                        <Info className="mt-0.5 h-4 w-4 text-primary" />
                        <div>
                          <p>
                            {details.activeTier
                              ? `Active since ${formatDate(details.activeTier.activeFrom)}`
                              : "User is on the free plan. Activate a tier from the dashboard if needed."}
                          </p>
                          {details.activeTier?.activeUntil && (
                            <p>Renewal due {formatDate(details.activeTier.activeUntil)}</p>
                          )}
                        </div>
                      </div>
                    </div>

                    <div className="space-y-2">
                      <p className="text-sm font-semibold">Tier history</p>
                      <div className="rounded-md border">
                        <div className="grid grid-cols-[2fr_2fr_2fr_1fr] gap-2 border-b bg-muted px-3 py-2 text-xs font-medium text-muted-foreground">
                          <span>Tier</span>
                          <span>Active from</span>
                          <span>Active until</span>
                          <span className="text-right">Status</span>
                        </div>
                        {details.tierHistory.length ? (
                          details.tierHistory.map((entry) => (
                            <div
                              key={entry.assignmentId}
                              className="grid grid-cols-[2fr_2fr_2fr_1fr] gap-2 border-b px-3 py-2 text-xs last:border-b-0"
                            >
                              <span className="font-medium">{entry.tierName}</span>
                              <span>{formatDate(entry.activeFrom)}</span>
                              <span>{entry.activeUntil ? formatDate(entry.activeUntil) : "Current"}</span>
                              <span className="text-right">
                                <Badge variant={entry.isActive ? "default" : "outline"} className="uppercase">
                                  {entry.isActive ? "Active" : "Expired"}
                                </Badge>
                              </span>
                            </div>
                          ))
                        ) : (
                          <div className="px-3 py-6 text-center text-xs text-muted-foreground">
                            No historical tier assignments recorded.
                          </div>
                        )}
                      </div>
                    </div>
                    </div>

                    <div className="flex items-center justify-end gap-2">
                      <Button
                        type="button"
                        variant="outline"
                        onClick={handleReset}
                        disabled={!isDirty || isUpdating}
                      >
                        Reset
                      </Button>
                      <Button type="submit" disabled={!isDirty || isUpdating}>
                        {isUpdating ? <Loader2 className="h-4 w-4 animate-spin" /> : "Save changes"}
                      </Button>
                    </div>
                  </form>

                  <div className="space-y-3 rounded-md border px-4 py-4">
                    <div className="flex items-center justify-between">
                      <div>
                        <p className="text-sm font-semibold">Manual tier assignment</p>
                        <p className="text-xs text-muted-foreground">
                          Override a user&apos;s tier without payment. Leave dates empty to activate immediately.
                        </p>
                      </div>
                      {isAssigning && <Loader2 className="h-4 w-4 animate-spin text-primary" />}
                    </div>
                    {tiersLoading ? (
                      <div className="flex items-center gap-2 text-xs text-muted-foreground">
                        <Loader2 className="h-4 w-4 animate-spin" />
                        Loading available tiers...
                      </div>
                    ) : tierOptions.length ? (
                      <form className="space-y-3" onSubmit={handleTierAssign}>
                        <div className="space-y-1">
                          <label className="text-xs font-medium uppercase text-muted-foreground">Tier</label>
                          <select
                            className="w-full rounded-md border bg-background px-3 py-2 text-sm"
                            value={tierForm.tierId ?? ""}
                            onChange={(event) => {
                              const { value } = event.target;
                              updateTierFormField("tierId", value ? Number(value) : null);
                            }}
                            disabled={isAssigning || !selectedUserId}
                          >
                            {!tierForm.tierId && <option value="">Select a tier</option>}
                            {tierOptions.map((tier) => (
                              <option key={tier.tierId} value={tier.tierId}>
                                {tier.name}
                              </option>
                            ))}
                          </select>
                        </div>
                        <div className="grid gap-3 md:grid-cols-2">
                          <div className="space-y-1">
                            <label className="text-xs font-medium uppercase text-muted-foreground">
                              Active from
                            </label>
                            <Input
                              type="datetime-local"
                              value={tierForm.activeFrom}
                              onChange={(event) => updateTierFormField("activeFrom", event.target.value)}
                              disabled={isAssigning}
                            />
                          </div>
                          <div className="space-y-1">
                            <label className="text-xs font-medium uppercase text-muted-foreground">
                              Active until
                            </label>
                            <Input
                              type="datetime-local"
                              value={tierForm.activeUntil}
                              onChange={(event) => updateTierFormField("activeUntil", event.target.value)}
                              disabled={isAssigning}
                            />
                          </div>
                        </div>
                        <div className="space-y-1">
                          <label className="text-xs font-medium uppercase text-muted-foreground">
                            Notes (optional)
                          </label>
                          <textarea
                            className="min-h-[90px] w-full rounded-md border bg-background px-3 py-2 text-sm"
                            value={tierForm.notes}
                            onChange={(event) => updateTierFormField("notes", event.target.value)}
                            maxLength={512}
                            disabled={isAssigning}
                          />
                          <p className="text-[11px] text-muted-foreground">
                            Mention the reason for the manual upgrade. Notes are stored with the assignment.
                          </p>
                        </div>
                        <div className="flex items-center justify-end gap-2">
                          <Button type="button" variant="outline" onClick={handleTierReset} disabled={isAssigning}>
                            Clear
                          </Button>
                          <Button type="submit" disabled={!tierForm.tierId || isAssigning || !selectedUserId}>
                            {isAssigning ? <Loader2 className="h-4 w-4 animate-spin" /> : "Assign tier"}
                          </Button>
                        </div>
                      </form>
                    ) : (
                      <div className="rounded-md border border-dashed px-3 py-4 text-xs text-muted-foreground">
                        No tiers available. Create tiers first from the management area.
                      </div>
                    )}
                  </div>
                </>
              ) : (
                <div className="rounded-md border border-dashed px-6 py-12 text-center text-sm text-muted-foreground">
                  Select a user to load their details.
                </div>
              )}
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  );
}
