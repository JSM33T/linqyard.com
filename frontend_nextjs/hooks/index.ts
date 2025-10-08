// Export all types
export * from './types';

// Export API service
export { apiService, ApiService } from './apiService';

// Export hooks
export { useGet, usePost, useApi } from './useApi';
export { useSessionCheck } from './useSessionCheck';

// Utility functions for common API patterns
export const apiUtils = {
  // Helper to create query strings
  createQueryString: (params: Record<string, any>): string => {
    const searchParams = new URLSearchParams();
    Object.entries(params).forEach(([key, value]) => {
      if (value !== undefined && value !== null) {
        searchParams.append(key, String(value));
      }
    });
    return searchParams.toString();
  },

  // Helper to append query params to endpoint
  withQuery: (endpoint: string, params: Record<string, any>): string => {
    const queryString = apiUtils.createQueryString(params);
    const separator = endpoint.includes('?') ? '&' : '?';
    return queryString ? `${endpoint}${separator}${queryString}` : endpoint;
  },

  // Helper to check if error is authentication related
  isAuthError: (error: any): boolean => {
    return error?.status === 401 || error?.status === 403;
  },

  // Helper to check if error is network related
  isNetworkError: (error: any): boolean => {
    return error?.status === 0 || error?.message?.includes('timeout') || error?.message?.includes('fetch');
  },
};