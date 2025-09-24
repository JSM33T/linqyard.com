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

// Authentication API Types
export interface LoginRequest {
  emailOrUsername: string;
  password: string;
}

export interface LoginResponse {
  data: {
    accessToken: string;
    refreshToken: string | null;
    expiresAt: string;
    user: {
      id: string;
      email: string;
      emailVerified: boolean;
      username: string;
      firstName: string;
      lastName: string;
      avatarUrl: string | null;
      createdAt: string;
      roles: string[];
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
    createdAt: string;
    roles: string[];
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
    authUrl: string; // Backend returns nested data.authUrl
  };
}

export interface GoogleCallbackRequest {
  code: string;
  state: string;
}

export interface GoogleCallbackResponse {
  accessToken: string;
  refreshToken: string | null;
  expiresAt: string;
  user: {
    id: string;
    email: string;
    emailVerified: boolean;
    username: string;
    firstName: string;
    lastName: string;
    avatarUrl: string | null;
    createdAt: string;
    roles: string[];
  };
}

// Profile API Types
export interface ProfileDetailsResponse {
  id: string;
  email: string;
  emailVerified: boolean;
  username: string;
  firstName?: string | null;
  lastName?: string | null;
  displayName?: string | null;
  bio?: string | null;
  avatarUrl?: string | null;
  timezone?: string | null;
  locale?: string | null;
  verifiedBadge?: boolean;
  createdAt: string;
  updatedAt: string;
  roles: string[];
}

export interface UpdateProfileRequest {
  firstName?: string | null;
  lastName?: string | null;
  displayName?: string | null;
  bio?: string | null;
  avatarUrl?: string | null;
  timezone?: string | null;
  locale?: string | null;
}

export interface ProfileUpdateResponse {
  message: string;
  updatedAt: string;
  profile: ProfileDetailsResponse;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}

export interface PasswordChangeResponse {
  message: string;
  changedAt: string;
}

export interface SessionInfo {
  id: string;
  authMethod: string;
  ipAddress: string;
  userAgent?: string | null;
  createdAt: string;
  lastSeenAt: string;
  isCurrent: boolean;
}

export interface SessionsResponse {
  data: {
    sessions: SessionInfo[];
  };
}

export interface SessionDeleteResponse {
  message: string;
  deletedAt: string;
}

export interface DeleteAccountRequest {
  confirmationText: string;
  password: string;
}

export interface AccountDeleteResponse {
  message: string;
  deletedAt: string;
}