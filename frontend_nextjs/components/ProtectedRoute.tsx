"use client";

import { ReactNode, useEffect, useState } from 'react';
import { useUser } from '@/contexts/UserContext';
import { useApi } from '@/hooks/useApi';
import AccessDenied from './AccessDenied';
import { applyCacheBustingParam } from '@/lib/cacheBust';

interface ProtectedRouteProps {
  children: ReactNode;
  fallback?: ReactNode;
}

export default function ProtectedRoute({ children, fallback }: ProtectedRouteProps) {
  const { user, isAuthenticated, isInitialized, setUser } = useUser();
  const { attemptSessionRestore } = useApi();
  const [isAttemptingRestore, setIsAttemptingRestore] = useState(false);
  const [restoreAttempted, setRestoreAttempted] = useState(false);

  // Attempt session restoration if user is not authenticated but might have valid cookies
  useEffect(() => {
    const attemptRestore = async () => {
      // Only attempt if:
      // 1. User context is initialized
      // 2. User is not authenticated
      // 3. We haven't already attempted restoration
      // 4. There's a refresh token cookie
      if (
        isInitialized && 
        !isAuthenticated && 
        !restoreAttempted && 
        !isAttemptingRestore &&
        document.cookie.includes('refreshToken=')
      ) {
        console.log('🔄 ProtectedRoute: Attempting session restoration...');
        setIsAttemptingRestore(true);
        setRestoreAttempted(true);

        try {
          const userData = await attemptSessionRestore();
          
          if (userData) {
            const avatarUrl =
              applyCacheBustingParam(userData.avatarUrl ?? undefined, userData.updatedAt) ??
              userData.avatarUrl ?? undefined;
            const coverUrl =
              applyCacheBustingParam(userData.coverUrl ?? undefined, userData.updatedAt) ??
              userData.coverUrl ?? undefined;

            const restoredUser = {
              id: userData.id,
              firstName: userData.firstName || '',
              lastName: userData.lastName || '',
              username: userData.username || '',
              email: userData.email || '',
              avatarUrl,
              coverUrl,
              login: true,
              expiry: userData.expiry ? new Date(userData.expiry) : undefined,
              role: userData.role,
              preferences: userData.preferences
            };
            
            setUser(restoredUser);
            console.log(' ProtectedRoute: Session restored successfully');
          } else {
            console.log('❌ ProtectedRoute: Session restoration failed');
          }
        } catch (error) {
          console.log('❌ ProtectedRoute: Session restoration error:', error);
        } finally {
          setIsAttemptingRestore(false);
        }
      }
    };

    attemptRestore();
  }, [isInitialized, isAuthenticated, restoreAttempted, isAttemptingRestore, attemptSessionRestore, setUser]);

  // Show loading while user context is initializing or while attempting restore
  if (!isInitialized || isAttemptingRestore) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-background">
        <div className="flex flex-col items-center space-y-4">
          <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary"></div>
          <p className="text-sm text-muted-foreground">
            {!isInitialized ? 'Loading...' : 'Restoring session...'}
          </p>
        </div>
      </div>
    );
  }

  // If user is authenticated, render children
  if (isAuthenticated && user) {
    return <>{children}</>;
  }

  // If user is not authenticated and we've attempted restoration, show access denied
  return fallback || <AccessDenied />;
}
