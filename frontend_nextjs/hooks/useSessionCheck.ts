"use client";

import { useEffect, useRef } from 'react';
import { useUser } from '@/contexts/UserContext';
import { apiService } from './apiService';

/**
 * Hook that proactively checks and restores user session on page load
 * This ensures session restoration happens even on pages that don't make API calls
 * It makes an authenticated API request to trigger the token refresh mechanism
 */
export function useSessionCheck() {
  const { user, isInitialized } = useUser();
  const hasCheckedSession = useRef(false);

  useEffect(() => {
    // Only run once per session and only after UserContext is initialized
    if (!isInitialized || hasCheckedSession.current) {
      return;
    }

    const checkSession = async () => {
      try {
        // If we already have a valid user session, no need to check
        if (user?.login) {
          console.log(' User session already active, skipping proactive session check');
          hasCheckedSession.current = true;
          return;
        }

        // Check if we have any authentication indicators
        const hasToken = apiService.hasToken();
        const hasRefreshToken = apiService.hasRefreshToken();
        const hasRefreshTokenCookie = document.cookie.includes('refreshToken=');
        
        // If we have any form of authentication, make a request to trigger restoration
        if (hasToken || hasRefreshToken || hasRefreshTokenCookie) {
          console.log(' Proactive session check: Found auth indicators, making authenticated request');
          
          try {
            // Make an authenticated request - this will trigger token refresh if needed
            // The apiService interceptor will handle session restoration automatically
            await apiService.get('/auth/me');
            console.log(' Proactive session check completed successfully');
          } catch (error) {
            console.log(' Proactive session check failed - user may not be authenticated');
            console.error(' Session check error:', error);
            // This is expected if the user is not authenticated
            // The apiService will handle cleanup if tokens are invalid
          }
        } else {
          console.log(' No authentication indicators found, skipping proactive session check');
        }
      } catch (error) {
        console.error('Proactive session check error:', error);
      } finally {
        hasCheckedSession.current = true;
      }
    };

    // Run session check after a short delay to avoid blocking initial render
    // This delay allows UserContext initialization to complete first
    const timeoutId = setTimeout(checkSession, 250);

    return () => {
      clearTimeout(timeoutId);
    };
  }, [isInitialized, user?.login]);

  // Reset session check flag when user logs out or context changes
  useEffect(() => {
    if (!user?.login && hasCheckedSession.current) {
      hasCheckedSession.current = false;
    }
  }, [user?.login]);
}