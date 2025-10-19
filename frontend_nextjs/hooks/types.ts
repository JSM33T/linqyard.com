// API Types and Interfaces
export interface ApiConfig {
  baseURL?: string;
  timeout?: number;
  headers?: Record<string, string>;
}

export interface ApiResponse<T = any> {
  data: T;
  status: number;
  statusText: string;
  headers: Record<string, string>;
}

export interface ApiError {
  message: string;
  status?: number;
  statusText?: string;
  data?: any;
}

export interface RequestConfig {
  headers?: Record<string, string>;
  timeout?: number;
  requireAuth?: boolean;
}

export interface PagedMeta {
  page: number;
  pageSize: number;
  total: number;
}

export interface UseApiState<T = any> {
  data: T | null;
  loading: boolean;
  error: ApiError | null;
}

export interface UseApiReturn<T = any> extends UseApiState<T> {
  refetch: () => Promise<void>;
  reset: () => void;
}

export interface UsePostReturn<T = any> {
  mutate: (data?: any) => Promise<ApiResponse<T>>;
  loading: boolean;
  error: ApiError | null;
  data: T | null;
  reset: () => void;
}

// Tier & billing related types
export interface TierBillingCycle {
  billingPeriod: string;
  amount: number;
  durationMonths: number;
  description?: string | null;
}

export interface UserTierInfo {
  tierId: number;
  name: string;
  activeFrom: string;
  activeUntil?: string | null;
}

export interface TierDetails {
  tierId: number;
  name: string;
  description?: string | null;
  currency: string;
  plans: TierBillingCycle[];
}

export interface TierAdminBillingCycle {
  id: number;
  billingPeriod: string;
  amount: number;
  durationMonths: number;
  description?: string | null;
  isActive: boolean;
}

export interface TierAdminDetails {
  tierId: number;
  name: string;
  currency: string;
  description?: string | null;
  billingCycles: TierAdminBillingCycle[];
}

export interface CouponAdmin {
  id: string;
  code: string;
  discountPercentage: number;
  description?: string | null;
  tierId?: number | null;
  tierName?: string | null;
  maxRedemptions?: number | null;
  redemptionCount: number;
  validFrom?: string | null;
  validUntil?: string | null;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface TierOrderData {
  tierName: string;
  billingPeriod: string;
  orderId: string;
  receipt: string;
  amount: number;
  currency: string;
  razorpayKeyId: string;
  subtotalAmount: number;
  discountAmount: number;
  couponCode?: string | null;
}

export interface TierUpgradeConfirmationData {
  tier: UserTierInfo;
  message: string;
  billingPeriod: string;
}

export interface TierCouponPreviewData {
  couponCode: string;
  discountPercentage: number;
  discountAmount: number;
  subtotalAmount: number;
  finalAmount: number;
  currency: string;
  description?: string | null;
  validUntil?: string | null;
}

// Authentication API Types
export interface LoginRequest {
  emailOrUsername: string;
  password: string;
}

export interface LoginResponse {
  data: {
    accessToken: string;
    refreshToken: string;
    expiresAt: string;
    user: {
      id: string;
      email: string;
      emailVerified: boolean;
      username: string;
      firstName: string;
      lastName: string;
      avatarUrl: string | null;
      coverUrl: string | null;
      createdAt: string;
      roles: string[];
      tierId?: number | null;
      tierName?: string | null;
    };
  };
  meta: any | null;
}

export interface LoginErrorResponse {
  type: string;
  title: string;
  status: number;
  instance: string;
  CorrelationId: string;
}

export interface SignupRequest {
  email: string;
  password: string;
  username: string;
  firstName: string;
  lastName: string;
}

export interface SignupResponse {
  data: {
    id: string;
    email: string;
    emailVerified: boolean;
    username: string;
    firstName: string;
    lastName: string;
    avatarUrl: string | null;
    coverUrl: string | null;
    createdAt: string;
    roles: string[];
    tierId?: number | null;
    tierName?: string | null;
  };
  meta: any | null;
}

export interface SignupErrorResponse {
  type: string;
  title: string;
  status: number;
  instance: string;
  CorrelationId: string;
}

// Email Verification Types
export interface VerifyEmailRequest {
  email: string;
  token: string;
}

export interface VerifyEmailResponse {
  data: {
    message: string;
    emailVerified: boolean;
  };
  meta: any | null;
}

// Forgot Password Types
export interface ForgotPasswordRequest {
  email: string;
}

export interface ForgotPasswordResponse {
  data: {
    message: string;
    verificationSent: boolean;
  };
  meta: any | null;
}

// Reset Password Types
export interface ResetPasswordRequest {
  email: string;
  token: string;
  newPassword: string;
}

export interface ResetPasswordResponse {
  data: {
    message: string;
    passwordReset: boolean;
  };
  meta: any | null;
}

// Resend Verification Types
export interface ResendVerificationRequest {
  email: string;
}

export interface ResendVerificationResponse {
  data: {
    message: string;
    verificationSent: boolean;
  };
  meta: any | null;
}

// Google OAuth Types
export interface GoogleOAuthResponse {
  data: {
    AuthUrl: string; // Backend returns AuthUrl with capital A
  };
  meta: any | null;
}

export interface GoogleInitResponse {
  data: {
    authUrl: string; // Backend returns nested data.authUrl (lowercase)
  };
  meta: any | null;
}

export interface GoogleCallbackRequest {
  code: string;
  state: string;
}

export interface GoogleCallbackResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  user: {
      id: string;
      email: string;
      emailVerified: boolean;
      username: string;
      firstName: string;
      lastName: string;
      avatarUrl: string | null;
      coverUrl: string | null;
      createdAt: string;
      roles: string[];
      tierId?: number | null;
      tierName?: string | null;
    };
}

// Profile API Types
export interface ProfileData {
  id: string;
  email: string;
  emailVerified: boolean;
  username: string;
  firstName: string;
  lastName: string;
  displayName?: string;
  bio?: string;
  avatarUrl: string | null;
  coverUrl: string | null;
  timezone?: string;
  locale?: string;
  verifiedBadge: boolean;
  createdAt: string;
  updatedAt: string;
  roles: string[];
}

export interface GetProfileResponse {
  data: ProfileData;
  meta: any | null;
}

export interface UpdateProfileRequest {
  username?: string;
  firstName?: string;
  lastName?: string;
  displayName?: string;
  bio?: string;
  avatarUrl?: string;
  coverUrl?: string;
  timezone?: string;
  locale?: string;
}

export interface UpdateProfileResponse {
  data: {
    message: string;
    updatedAt: string;
    profile: ProfileData;
  };
  meta: any | null;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}

export interface ChangePasswordResponse {
  data: {
    message: string;
    changedAt: string;
  };
  meta: any | null;
}

export interface SetPasswordRequest {
  email: string;
  currentPassword?: string | null;
  newPassword: string;
}

export interface SetPasswordResponse {
  data: {
    message: string;
    updatedAt: string;
  };
  meta: any | null;
}

// Session Management Types
export interface SessionData {
  id: string;
  authMethod: string;
  ipAddress: string;
  userAgent: string;
  createdAt: string;
  lastSeenAt: string;
  isCurrentSession: boolean;
}

export interface GetSessionsResponse {
  data: {
    sessions: SessionData[];
  };
  meta: any | null;
}

export interface LogoutSessionResponse {
  data: {
    message: string;
    deletedAt: string;
  };
  meta: any | null;
}

export interface LogoutAllSessionsResponse {
  data: {
    message: string;
    deletedAt: string;
  };
  meta: any | null;
}

// Admin user management types
export interface AdminUserListItem {
  id: string;
  email: string;
  emailVerified: boolean;
  username: string;
  firstName?: string | null;
  lastName?: string | null;
  displayName?: string | null;
  verifiedBadge: boolean;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
  activeTier?: UserTierInfo | null;
  roles: string[];
}

export interface AdminUserListResponse {
  data: AdminUserListItem[];
  meta: PagedMeta | null;
}

export interface AdminUserTierAssignment {
  assignmentId: string;
  tierId: number;
  tierName: string;
  activeFrom: string;
  activeUntil?: string | null;
  isActive: boolean;
  notes?: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface AdminUserDetails {
  profile: ProfileData;
  activeTier?: UserTierInfo | null;
  tierHistory: AdminUserTierAssignment[];
  isActive: boolean;
}

export interface AdminUserDetailsResponse {
  data: AdminUserDetails;
  meta: any | null;
}

export interface AdminUpdateUserRequest {
  email?: string | null;
  emailVerified?: boolean | null;
  username?: string | null;
  firstName?: string | null;
  lastName?: string | null;
  displayName?: string | null;
  bio?: string | null;
  avatarUrl?: string | null;
  coverUrl?: string | null;
  timezone?: string | null;
  locale?: string | null;
  verifiedBadge?: boolean | null;
  isActive?: boolean | null;
  roles?: string[] | null;
}

// Refresh Token Types
export interface RefreshTokenRequest {
  refreshToken: string;
}

export interface RefreshTokenResponse {
  data: {
    accessToken: string;
    refreshToken: string;
    expiresAt: string;
  };
  meta: any | null;
}

// Links & Groups Types
export interface LinkItem {
  id: string;
  name: string;
  url: string;
  description?: string | null;
  isActive: boolean;
  sequence: number;
  groupId: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface LinkGroup {
  id: string;
  name: string;
  description?: string | null;
  sequence: number;
  isActive: boolean;
  links: LinkItem[];
}

export interface GetGroupedLinksResponse {
  data: {
    groups: LinkGroup[];
    ungrouped: LinkGroup; // server returns an "ungrouped" bucket with same shape
  };
  meta: any | null;
}

export interface CreateOrEditLinkRequest {
  id?: string; // optional when creating
  name: string;
  url: string;
  description?: string | null;
  groupId?: string | null;
  sequence?: number;
  isActive?: boolean;
}

export interface UpdateGroupRequest {
  name?: string | null;
  description?: string | null;
  sequence?: number | null;
  isActive?: boolean | null;
}

export interface GroupResequenceItemRequest {
  id: string;
  sequence: number;
}



