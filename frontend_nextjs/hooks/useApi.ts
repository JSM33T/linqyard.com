"use client";

import { useState, useEffect, useCallback, useRef } from 'react';
import { apiService } from './apiService';
import { useUser } from '@/contexts/UserContext';
import {
  UseApiReturn,
  UsePostReturn,
  ApiError,
  RequestConfig,
  ApiResponse
} from './types';

// Initialize auth error callback
let isAuthCallbackInitialized = false;

function initializeAuthCallback(logout: () => void) {
  if (!isAuthCallbackInitialized) {
    apiService.setAuthErrorCallback(logout);
    isAuthCallbackInitialized = true;
  }
}

// Hook for GET requests
export function useGet<T = any>(
  endpoint: string,
  config?: RequestConfig & { enabled?: boolean }
): UseApiReturn<T> {
  const { logout } = useUser();
  const [data, setData] = useState<T | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<ApiError | null>(null);
  const mountedRef = useRef(true);

  // Initialize auth callback
  useEffect(() => {
    initializeAuthCallback(logout);
  }, [logout]);

  const fetchData = useCallback(async () => {
    if (!mountedRef.current) return;
    
    setLoading(true);
    setError(null);

    try {
      const response = await apiService.get<T>(endpoint, config);
      if (mountedRef.current) {
        setData(response.data);
      }
    } catch (err) {
      if (mountedRef.current) {
        setError(err as ApiError);
      }
    } finally {
      if (mountedRef.current) {
        setLoading(false);
      }
    }
  }, [endpoint, config]);

  const reset = useCallback(() => {
    setData(null);
    setError(null);
    setLoading(false);
  }, []);

  // Auto-fetch on mount and when dependencies change
  useEffect(() => {
    mountedRef.current = true;
    const shouldFetch = config?.enabled !== false;
    if (shouldFetch && endpoint) {
      fetchData();
    }
  }, [fetchData, config?.enabled, endpoint]);

  // Cleanup on unmount
  useEffect(() => {
    return () => {
      mountedRef.current = false;
    };
  }, []);

  return {
    data,
    loading,
    error,
    refetch: fetchData,
    reset,
  };
}

// Hook for POST requests
export function usePost<T = any>(
  endpoint: string,
  config?: RequestConfig
): UsePostReturn<T> {
  const { logout } = useUser();
  const [data, setData] = useState<T | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<ApiError | null>(null);

  // Initialize auth callback
  useEffect(() => {
    initializeAuthCallback(logout);
  }, [logout]);

  const mutate = useCallback(async (postData?: any): Promise<ApiResponse<T>> => {
    setLoading(true);
    setError(null);

    try {
      const response = await apiService.post<T>(endpoint, postData, config);
      setData(response.data);
      return response;
    } catch (err) {
      const apiError = err as ApiError;
      setError(apiError);
      throw apiError;
    } finally {
      setLoading(false);
    }
  }, [endpoint, config]);

  const reset = useCallback(() => {
    setData(null);
    setError(null);
    setLoading(false);
  }, []);

  return {
    mutate,
    loading,
    error,
    data,
    reset,
  };
}

// Hook for PUT requests
export function usePut<T = any>(
  endpoint: string,
  config?: RequestConfig
): UsePostReturn<T> {
  const { logout } = useUser();
  const [data, setData] = useState<T | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<ApiError | null>(null);

  useEffect(() => {
    initializeAuthCallback(logout);
  }, [logout]);

  const mutate = useCallback(async (putData?: any): Promise<ApiResponse<T>> => {
    setLoading(true);
    setError(null);

    try {
      const response = await apiService.put<T>(endpoint, putData, config);
      setData(response.data);
      return response;
    } catch (err) {
      const apiError = err as ApiError;
      setError(apiError);
      throw apiError;
    } finally {
      setLoading(false);
    }
  }, [endpoint, config]);

  const reset = useCallback(() => {
    setData(null);
    setError(null);
    setLoading(false);
  }, []);

  return {
    mutate,
    loading,
    error,
    data,
    reset,
  };
}

// General API hook for manual calls
export function useApi() {
  const { logout } = useUser();

  // Initialize auth callback
  useEffect(() => {
    initializeAuthCallback(logout);
  }, [logout]);
  const get = useCallback(<T = any>(endpoint: string, config?: RequestConfig) => {
    return apiService.get<T>(endpoint, config);
  }, []);

  const post = useCallback(<T = any>(endpoint: string, data?: any, config?: RequestConfig) => {
    return apiService.post<T>(endpoint, data, config);
  }, []);

  const put = useCallback(<T = any>(endpoint: string, data?: any, config?: RequestConfig) => {
    return apiService.put<T>(endpoint, data, config);
  }, []);

  // Wrap apiService methods with stable callbacks so their identity doesn't change across renders
  const setToken = useCallback((token: string) => apiService.setToken(token), []);
  const setRefreshToken = useCallback((token: string) => apiService.setRefreshToken(token), []);
  const setTokens = useCallback((accessToken: string, refreshToken: string) => apiService.setTokens(accessToken, refreshToken), []);
  const clearToken = useCallback(() => apiService.clearToken(), []);
  const clearRefreshToken = useCallback(() => apiService.clearRefreshToken(), []);
  const clearAllTokens = useCallback(() => apiService.clearAllTokens(), []);
  const hasToken = useCallback(() => apiService.hasToken(), []);
  const hasRefreshToken = useCallback(() => apiService.hasRefreshToken(), []);
  const getTokensDebug = useCallback(() => apiService.getTokensDebug(), []);
  const attemptSessionRestore = useCallback(() => apiService.attemptSessionRestore(), []);

  return {
    get,
    post,
    put,
    setToken,
    setRefreshToken,
    setTokens,
    clearToken,
    clearRefreshToken,
    clearAllTokens,
    hasToken,
    hasRefreshToken,
    getTokensDebug,
    attemptSessionRestore,
  };
}
