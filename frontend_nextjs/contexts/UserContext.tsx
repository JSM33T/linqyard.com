"use client";

import React, { createContext, useContext, useState, useCallback, ReactNode, useEffect } from "react";

// User interface definition
export interface User {
  id?: string;
  firstName: string;
  lastName: string;
  username: string;
  email: string;
  avatarUrl?: string;
  coverUrl?: string;
  bio?: string;
  login: boolean;
  expiry?: Date;
  // Additional optional fields for future use
  role?: string;
  preferences?: Record<string, any>;
}

// Context value interface
interface UserContextType {
  user: User | null;
  setUser: (user: User | null) => void;
  updateUser: (updates: Partial<User>) => void;
  logout: () => Promise<void>;
  isAuthenticated: boolean;
  isTokenExpired: () => boolean;
  isInitialized: boolean;
  restoreUserData: (userData: User) => void;
}

// Create the context
const UserContext = createContext<UserContextType | undefined>(undefined);

// Storage keys
const USER_STORAGE_KEY = 'linqyard_user';

// Default user state
const defaultUser: User | null = null;

// Helper functions for localStorage operations
const userStorage = {
  save: (user: User | null) => {
    if (typeof window !== 'undefined') {
      if (user) {
        localStorage.setItem(USER_STORAGE_KEY, JSON.stringify({
          ...user,
          expiry: user.expiry?.toISOString() // Convert Date to string for storage
        }));
      } else {
        localStorage.removeItem(USER_STORAGE_KEY);
      }
    }
  },
  
  load: (): User | null => {
    if (typeof window !== 'undefined') {
      try {
        const stored = localStorage.getItem(USER_STORAGE_KEY);
        if (stored) {
          const parsed = JSON.parse(stored);
          // Convert expiry back to Date object
          if (parsed.expiry) {
            parsed.expiry = new Date(parsed.expiry);
          }
          return parsed;
        }
      } catch (error) {
        console.error('Error loading user from localStorage:', error);
        localStorage.removeItem(USER_STORAGE_KEY);
      }
    }
    return null;
  },
  
  clear: () => {
    if (typeof window !== 'undefined') {
      localStorage.removeItem(USER_STORAGE_KEY);
    }
  }
};

// UserProvider component
interface UserProviderProps {
  children: ReactNode;
}

export function UserProvider({ children }: UserProviderProps) {
  const [user, setUser] = useState<User | null>(defaultUser);
  const [isInitialized, setIsInitialized] = useState(false);

  // Initialize user context on mount - restore from localStorage
  useEffect(() => {
    const initializeUser = async () => {
      const storedUser = userStorage.load();
      const storedToken = localStorage.getItem('userToken');
      
      if (storedUser) {
        // We have user data - check if it's still valid
        if (storedUser.expiry && new Date() > storedUser.expiry) {
          // User session has expired completely, clear everything
          userStorage.clear();
          localStorage.removeItem('userToken');
          setUser(null);
        } else {
          // User session is still valid, restore user
          const userWithLogin = { ...storedUser, login: true };
          setUser(userWithLogin);
          userStorage.save(userWithLogin);
          console.log('User data restored to localStorage on page load');
        }
        // Set initialized immediately when we have stored user data
        setIsInitialized(true);
      } else if (storedToken) {
        // Token exists but no user data - invalid state, clear token
        localStorage.removeItem('userToken');
        setUser(null);
        setIsInitialized(true);
      } else {
        // No user data and no token - check if we have any cookies that indicate a session
        // This handles cases where localStorage was cleared but HTTP-only cookies remain
        const hasRefreshTokenCookie = document.cookie.includes('refreshToken=');
        if (hasRefreshTokenCookie) {
          console.log('ðŸª Refresh token cookie found but no local user data - attempting session restore');
          
          // Attempt to restore the session proactively
          try {
            // Import apiService dynamically to avoid circular dependency
            const { apiService } = await import('@/hooks/apiService');
            const userData = await apiService.attemptSessionRestore();
            
            if (userData) {
              const restoredUser = {
                id: userData.id,
                firstName: userData.firstName || '',
                lastName: userData.lastName || '',
                username: userData.username || '',
                email: userData.email || '',
                avatarUrl: userData.avatarUrl,
                coverUrl: userData.coverUrl,
                login: true,
                expiry: undefined, // Will be set from token expiry
                role: userData.roles?.[0] || 'user',
                preferences: userData.preferences
              };
              
              setUser(restoredUser);
              userStorage.save(restoredUser);
              console.log(' Session restored successfully from cookies');
            } else {
              console.log('Session restoration failed');
              setUser(null);
            }
          } catch (error) {
            console.log('Error during session restoration:', error);
            setUser(null);
          }
        } else {
          // No refresh token cookie, definitely not authenticated
          setUser(null);
        }
        
        // CRITICAL: Only set initialized AFTER session restoration completes
        setIsInitialized(true);
      }
    };

    initializeUser();
  }, []);

  // Listen for automatic session restoration events
  useEffect(() => {
    const handleSessionRestored = (event: CustomEvent) => {
      const restoredUser = event.detail;
      if (restoredUser && !user) {
        console.log('User session restored via event:', restoredUser.username || restoredUser.email);
        setUser(restoredUser);
      }
    };

    window.addEventListener('userSessionRestored', handleSessionRestored as EventListener);
    
    return () => {
      window.removeEventListener('userSessionRestored', handleSessionRestored as EventListener);
    };
  }, [user]);

  // Custom setUser that also persists to localStorage
  const setUserWithPersistence = useCallback((newUser: User | null) => {
    setUser(newUser);
    userStorage.save(newUser);
    if (newUser) {
      console.log(' User data set and persisted to localStorage:', newUser.username || newUser.email);
    }
  }, []);

  // Update user with partial data
  const updateUser = useCallback((updates: Partial<User>) => {
    setUser(currentUser => {
      if (!currentUser) return null;
      const updatedUser = { ...currentUser, ...updates };
      userStorage.save(updatedUser); // Persist the update
      return updatedUser;
    });
  }, []);

  // Logout function
  const logout = useCallback(async (): Promise<void> => {
    // Optional: Call backend logout endpoint with refresh token
    const refreshToken = typeof window !== 'undefined' ? localStorage.getItem('refreshToken') : null;
    if (refreshToken) {
      // Fire-and-forget: kick off the request but don't await it so UI can continue
      fetch(`${process.env.NEXT_PUBLIC_API_URL || 'https://api.linqyard.com'}/auth/logout`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ refreshToken })
      }).catch(() => {
        // Ignore errors - we're logging out anyway
      });
    }

    // Clear local state/storage immediately
    setUser(null);
    userStorage.clear();
    if (typeof window !== 'undefined') {
      localStorage.removeItem('userToken');
      localStorage.removeItem('refreshToken');
      sessionStorage.removeItem('userSession');
    }

    // Return immediately; callers may await this resolved promise
    return;
  }, []);

  // Check if user is authenticated
  const isAuthenticated = user?.login === true;

  // Check if token is expired
  const isTokenExpired = useCallback(() => {
    if (!user?.expiry) return false;
    return new Date() > user.expiry;
  }, [user?.expiry]);

  // Restore user data to localStorage (useful when localStorage was cleared but session is valid)
  const restoreUserData = useCallback((userData: User) => {
    const userWithLogin = { ...userData, login: true };
    setUser(userWithLogin);
    userStorage.save(userWithLogin);
    console.log(' User data manually restored to localStorage');
  }, []);

  const contextValue: UserContextType = {
    user,
    setUser: setUserWithPersistence,
    updateUser,
    logout,
    isAuthenticated,
    isTokenExpired,
    isInitialized,
    restoreUserData,
  };

  // Show loading state while initializing to prevent content flash
  if (!isInitialized) {
    return (
      <UserContext.Provider value={contextValue}>
        <div className="min-h-screen flex items-center justify-center bg-background">
          <div className="flex flex-col items-center space-y-4">
            <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary"></div>
            <p className="text-sm text-muted-foreground">Loading...</p>
          </div>
        </div>
      </UserContext.Provider>
    );
  }

  return (
    <UserContext.Provider value={contextValue}>
      {children}
    </UserContext.Provider>
  );
}

// Custom hook to use the UserContext
export function useUser() {
  const context = useContext(UserContext);
  if (context === undefined) {
    throw new Error('useUser must be used within a UserProvider');
  }
  return context;
}

// Helper functions for working with user data
export const userHelpers = {
  getFullName: (user: User | null): string => {
    if (!user) return 'Guest';
    return `${user.firstName} ${user.lastName}`.trim();
  },
  
  getDisplayName: (user: User | null): string => {
    if (!user) return 'Guest';
    return user.username || userHelpers.getFullName(user);
  },

  // Return only the first name for UI sections that require it
  getFirstName: (user: User | null): string => {
    if (!user) return 'Guest';
    if (user.firstName && user.firstName.trim().length > 0) return user.firstName;
    // Fallback to username or full name initial
    if (user.username) return user.username;
    return userHelpers.getFullName(user).split(' ')[0] || 'Guest';
  },

  getLastName: (user: User | null): string => {
    if (!user) return '';
    return user.lastName || '';
  },

  // Safe avatar URL accessor with fallback
  getAvatarUrl: (user: User | null): string => {
    if (!user) return '/placeholder-avatar.jpg';
    return user.avatarUrl || '/placeholder-avatar.jpg';
  },
  
  getInitials: (user: User | null): string => {
    if (!user) return 'G';
    const firstName = user.firstName?.charAt(0)?.toUpperCase() || '';
    const lastName = user.lastName?.charAt(0)?.toUpperCase() || '';
    return firstName + lastName || user.username?.charAt(0)?.toUpperCase() || 'U';
  },
  
  isSessionValid: (user: User | null): boolean => {
    if (!user?.login) return false;
    if (!user.expiry) return true; // No expiry means valid
    return new Date() < user.expiry;
  },

  // Check if user session is valid (token might be refreshed automatically)
  hasValidAuthState: (): boolean => {
    if (typeof window === 'undefined') return false;
    const storedUser = userStorage.load();
    // Focus on user session validity rather than requiring both user and token
    // Token can be refreshed automatically if refresh token is valid
    return !!(storedUser && userHelpers.isSessionValid(storedUser));
  },

  // Ensure user data is always in localStorage if we have a valid session
  ensureUserDataInStorage: (userData?: User): void => {
    if (typeof window === 'undefined') return;
    
    const storedUser = userStorage.load();
    if (!storedUser && userData && userHelpers.isSessionValid(userData)) {
      // User data is missing from localStorage but we have valid user data
      const userWithLogin = { ...userData, login: true };
      userStorage.save(userWithLogin);
      console.log(' User data automatically restored to localStorage');
    }
  }
};

