import { ApiConfig, ApiResponse, ApiError, RequestConfig } from './types';

// Default configuration
// If NEXT_PUBLIC_API_URL is not set and we're on localhost, default to the backend dev port 5283
const deriveDefaultBaseUrl = (): string => {
  const env = process.env.NEXT_PUBLIC_API_URL;
  if (env && env.length > 0) return env;
  try {
    if (typeof window !== 'undefined') {
      const host = window.location.hostname;
      if (host === 'localhost' || host === '127.0.0.1') {
        return 'http://localhost:5283';
      }
    }
  } catch {
    // ignore
  }
  return '/api';
};

const DEFAULT_CONFIG: ApiConfig = {
  baseURL: deriveDefaultBaseUrl(),
  timeout: 50000,
  headers: {
    'Content-Type': 'application/json',
  },
};

class ApiService {
  private config: ApiConfig;
  private onAuthError?: () => void;
  // Track an in-flight refresh to avoid parallel refresh calls
  private refreshPromise: Promise<string | null> | null = null;

  constructor(config: Partial<ApiConfig> = {}) {
    this.config = { ...DEFAULT_CONFIG, ...config };
  }

  // Set auth error callback (to clear user context)
  setAuthErrorCallback(callback: () => void) {
    this.onAuthError = callback;
  }

  // Get token from localStorage
  private getToken(): string | null {
    if (typeof window === 'undefined') return null;
    return localStorage.getItem('userToken');
  }

  // Build headers with auth token
  private buildHeaders(customHeaders?: Record<string, string>): Record<string, string> {
    const headers = { ...this.config.headers, ...customHeaders };
    
    const token = this.getToken();
    if (token) {
      headers.Authorization = `Bearer ${token}`;
    }

    return headers;
  }

  // Create AbortController for timeout
  private createAbortController(timeout?: number): AbortController {
    const controller = new AbortController();
    const timeoutMs = timeout || this.config.timeout || 50000;
    
    setTimeout(() => controller.abort(), timeoutMs);
    return controller;
  }

  // Attempt to refresh access token using httpOnly refresh cookie.
  // Returns the new access token string or null on failure.
  private async refreshAccessToken(): Promise<string | null> {
    // If a refresh is already in progress, return the same promise
    if (this.refreshPromise) return this.refreshPromise;

    this.refreshPromise = (async () => {
      try {
        // Choose refresh endpoint URL:
        // - If baseURL is an absolute URL (contains protocol or ':'), use it (API deployed behind explicit URL)
        // - Otherwise fall back to origin-relative '/auth/refresh' to avoid accidental '/api' prefixes
        const base = this.config.baseURL || '';
        let url: string;
        if (base && (base.startsWith('http://') || base.startsWith('https://') || base.includes(':'))) {
          url = this.buildUrl('/auth/refresh');
        } else {
          url = '/auth/refresh';
        }
        console.debug('[ApiService] refreshAccessToken - using URL', url, 'baseURL=', base);
  const controller = this.createAbortController();

        // Send POST with credentials so httpOnly cookie is included
        console.debug('[ApiService] refreshAccessToken - sending refresh request', url);
        const resp = await fetch(url, {
          method: 'POST',
          headers: { ...this.config.headers },
          credentials: 'include',
          signal: controller.signal,
        });

        console.debug('[ApiService] refreshAccessToken - refresh response status', resp.status);

        if (!resp.ok) {
          // Refresh failed - notify auth error handlers
          const text = await resp.text().catch(() => '<no-body>');
          console.warn('[ApiService] refreshAccessToken failed', resp.status, text);
          this.onAuthError?.();
          return null;
        }

        const body = await resp.json();
        console.debug('[ApiService] refreshAccessToken - response body', body);

        // Support a few envelope shapes: body.data.AccessToken | body.data.accessToken | body.accessToken
        const token = (body && (body.data?.accessToken || body.data?.AccessToken || body.accessToken || body.AccessToken)) || null;

        if (token) {
          console.debug('[ApiService] refreshAccessToken - storing new token');
          this.setToken(token);
          return token as string;
        }

        // No token in response -> treat as auth error
        console.warn('[ApiService] refreshAccessToken - no token in response');
        this.onAuthError?.();
        return null;
      } catch (err) {
        console.error('[ApiService] refreshAccessToken - exception', err);
        // Treat any error as failed refresh
        this.onAuthError?.();
        return null;
      } finally {
        // Clear the in-flight promise so subsequent refreshes can run
        this.refreshPromise = null;
      }
    })();

    return this.refreshPromise;
  }

  // Handle API errors
  private async handleError(response: Response): Promise<never> {
    let errorData;
    try {
      errorData = await response.json();
    } catch {
      errorData = { message: response.statusText };
    }

    const apiError: ApiError = {
      message: errorData.message || `HTTP ${response.status}`,
      status: response.status,
      statusText: response.statusText,
      data: errorData,
    };

    // Handle authentication errors
    if (response.status === 401 || response.status === 403) {
      // Clear token from localStorage
      if (typeof window !== 'undefined') {
        localStorage.removeItem('userToken');
      }
      // Call auth error callback to clear user context
      this.onAuthError?.();
    }

    throw apiError;
  }

  // Build full URL
  private buildUrl(endpoint: string): string {
    const baseURL = this.config.baseURL || '';
    const cleanEndpoint = endpoint.startsWith('/') ? endpoint : `/${endpoint}`;
    return `${baseURL}${cleanEndpoint}`;
  }

  // Generic request method
  private async request<T = any>(
    method: 'GET' | 'POST',
    endpoint: string,
    data?: any,
    config: RequestConfig = {}
  ): Promise<ApiResponse<T>> {
    const url = this.buildUrl(endpoint);
    const headers = this.buildHeaders(config.headers);
    const controller = this.createAbortController(config.timeout);

    const requestInit: RequestInit = {
      method,
      headers,
      signal: controller.signal,
    };

    if (method === 'POST' && data) {
      requestInit.body = JSON.stringify(data);
    }

    try {
      const response = await fetch(url, requestInit);

      if (!response.ok) {
        await this.handleError(response);
      }

      const responseData = await response.json();
      
      return {
        data: responseData,
        status: response.status,
        statusText: response.statusText,
        headers: Object.fromEntries(response.headers.entries()),
      };
    } catch (error) {
      if (error instanceof Error && error.name === 'AbortError') {
        throw {
          message: 'Request timeout',
          status: 408,
          statusText: 'Request Timeout',
        } as ApiError;
      }
      throw error;
    }
  }

  // GET method
  async get<T = any>(endpoint: string, config?: RequestConfig): Promise<ApiResponse<T>> {
    return this.requestAny<T>('GET', endpoint, undefined, config);
  }

  // POST method
  async post<T = any>(endpoint: string, data?: any, config?: RequestConfig): Promise<ApiResponse<T>> {
    return this.requestAny<T>('POST', endpoint, data, config);
  }

  // PUT method
  async put<T = any>(endpoint: string, data?: any, config?: RequestConfig): Promise<ApiResponse<T>> {
    // Reuse request method but allow PUT
    return this.requestAny<T>('PUT', endpoint, data, config);
  }

  // DELETE method
  async delete<T = any>(endpoint: string, config?: RequestConfig): Promise<ApiResponse<T>> {
    return this.requestAny<T>('DELETE', endpoint, undefined, config);
  }

  // Generic requestAny to support additional methods
  private async requestAny<T = any>(
    method: 'GET' | 'POST' | 'PUT' | 'DELETE',
    endpoint: string,
    data?: any,
    config: RequestConfig = {}
  ): Promise<ApiResponse<T>> {
    const url = this.buildUrl(endpoint);

    // Helper to perform a fetch with current headers
    const doFetch = async (headersOverride?: Record<string, string>) => {
      const headers = headersOverride ?? this.buildHeaders(config.headers);
      const controller = this.createAbortController(config.timeout);

      const requestInit: RequestInit = {
        method,
        headers,
        signal: controller.signal,
        // Ensure cookies are sent/received (httpOnly refresh cookie) when applicable
        credentials: 'include',
      };

      if ((method === 'POST' || method === 'PUT') && data) {
        requestInit.body = JSON.stringify(data);
      }

      console.debug('[ApiService] fetch', method, url, { headers, timeout: config.timeout });
      return fetch(url, requestInit);
    };

    try {
  let response = await doFetch();

      // If unauthorized, attempt to refresh token once and retry
      if (response.status === 401) {
        console.debug('[ApiService] received 401, attempting token refresh');
        const newToken = await this.refreshAccessToken();
        if (newToken) {
          // rebuild headers with new token and retry
          const headersWithNewToken = this.buildHeaders(config.headers);
          response = await doFetch(headersWithNewToken);
        } else {
          // Refresh failed - handle as auth error
          await this.handleError(response);
        }
      }

      if (!response.ok) {
        await this.handleError(response);
      }

      const responseData = await response.json();

      return {
        data: responseData,
        status: response.status,
        statusText: response.statusText,
        headers: Object.fromEntries(response.headers.entries()),
      };
    } catch (error) {
      if (error instanceof Error && error.name === 'AbortError') {
        throw {
          message: 'Request timeout',
          status: 408,
          statusText: 'Request Timeout',
        } as ApiError;
      }
      throw error;
    }
  }

  // Set token in localStorage
  setToken(token: string): void {
    if (typeof window !== 'undefined') {
      localStorage.setItem('userToken', token);
    }
  }

  // Remove token from localStorage
  clearToken(): void {
    if (typeof window !== 'undefined') {
      localStorage.removeItem('userToken');
    }
  }

  // Check if token exists
  hasToken(): boolean {
    return !!this.getToken();
  }
}

// Export singleton instance
export const apiService = new ApiService();

// Expose a debug helper in browser devtools to trigger refresh manually
if (typeof window !== 'undefined') {
  // @ts-ignore - attach for debugging
  window.__apiService = apiService;
  // @ts-ignore
  window.__apiService.debugForceRefresh = async () => {
    // @ts-ignore
    return apiService['refreshAccessToken']();
  };
}

// Export class for custom instances
export { ApiService };