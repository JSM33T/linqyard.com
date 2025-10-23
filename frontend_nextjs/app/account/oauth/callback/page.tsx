"use client";

import { useEffect, useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { motion } from "framer-motion";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Loader2, CheckCircle, XCircle, Chrome, Home } from "lucide-react";
import { toast } from "sonner";
import { useApi } from "@/hooks/useApi";
import { GoogleCallbackResponse, UserTierInfo } from "@/hooks/types";
import { useUser } from "@/contexts/UserContext";
import Link from "next/link";

const containerVariants = {
  hidden: { opacity: 0, y: 20 },
  visible: {
    opacity: 1,
    y: 0,
    transition: {
      duration: 0.6,
      staggerChildren: 0.1
    }
  }
};

const itemVariants = {
  hidden: { opacity: 0, y: 20 },
  visible: { opacity: 1, y: 0 }
};

type CallbackState = "loading" | "success" | "error";

export default function OAuthCallbackPage() {
  const [state, setState] = useState<CallbackState>("loading");
  const [error, setError] = useState<string>("");
  const router = useRouter();
  const searchParams = useSearchParams();
  const { get, setToken } = useApi();
  const { setUser } = useUser();

  useEffect(() => {
    const handleCallback = async () => {
      try {
        // Check if this is a redirect from our backend with auth results
        const success = searchParams.get("success");
        const token = searchParams.get("token");
        const expires = searchParams.get("expires");
        const error = searchParams.get("error");
        
        // Legacy: Check for direct OAuth parameters (if Google redirects directly)
        const code = searchParams.get("code");
        const state = searchParams.get("state");
        const errorParam = searchParams.get("error");

        // Handle backend redirect with auth results (new approach)
        if (success === "true" && token) {
          // Authentication successful - backend already processed everything
          const expiresAt = expires ? new Date(expires) : new Date(Date.now() + 15 * 60 * 1000);
          
          // Set access token in localStorage
          setToken(token);
          
          // Get user info from the /auth/me endpoint
          try {
            const userResponse = await get<{
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
                activeTier?: UserTierInfo | null;
              };
              meta: any | null;
            }>("/auth/me");

            if (userResponse.data?.data) {
              const userData = userResponse.data.data;
              const activeTier = userData.activeTier ?? null;
              
              // Update user context with user data including tier information
              setUser({
                id: userData.id,
                firstName: userData.firstName,
                lastName: userData.lastName,
                username: userData.username,
                email: userData.email,
                avatarUrl: userData.avatarUrl || undefined,
                coverUrl: userData.coverUrl || undefined,
                login: true,
                expiry: expiresAt,
                tierId: userData.tierId ?? activeTier?.tierId ?? undefined,
                tierName: userData.tierName ?? activeTier?.name ?? undefined,
                activeTier: activeTier
                  ? {
                      tierId: activeTier.tierId,
                      name: activeTier.name,
                      activeFrom: activeTier.activeFrom,
                      activeUntil: activeTier.activeUntil,
                    }
                  : null,
                role: userData.roles[0] || 'user'
              });
            }
            
            setState("success");
            toast.success("Successfully logged in with Google!");

            // Redirect after a short delay
            setTimeout(() => {
              const redirectUrl = localStorage.getItem("oauthRedirect") || "/";
              localStorage.removeItem("oauthRedirect");
              router.push(redirectUrl);
            }, 2000);
            
          } catch (error) {
            console.error("Error getting user info:", error);
            // Set basic user state even if we can't get full user info
            setUser({
              id: 'temp',
              firstName: 'User',
              lastName: '',
              username: 'user',
              email: 'user@example.com',
              login: true,
              expiry: expiresAt
            });
            
            setState("success");
            toast.success("Successfully logged in with Google!");

            setTimeout(() => {
              const redirectUrl = localStorage.getItem("oauthRedirect") || "/";
              localStorage.removeItem("oauthRedirect");
              router.push(redirectUrl);
            }, 2000);
          }
        }
        // Handle backend redirect with error (new approach)
        else if (success === "false" || error) {
          throw new Error(error || "Authentication failed");
        }
        // Handle direct OAuth callback (legacy approach - if Google redirects directly to frontend)
        else if (code && state) {
          // Check for OAuth errors
          if (errorParam) {
            throw new Error(`OAuth error: ${errorParam}`);
          }

          // Call backend callback endpoint
          const response = await get<GoogleCallbackResponse>(`/auth/google/callback?code=${encodeURIComponent(code)}&state=${encodeURIComponent(state)}`);

          if (response.data?.accessToken && response.data?.user) {
            const data = response.data;
            
            // Set access token in localStorage (refresh token is now in HTTP-only cookie)
            setToken(data.accessToken);
            
            // Update user context with user data including tier information
            setUser({
              id: data.user.id,
              firstName: data.user.firstName,
              lastName: data.user.lastName,
              username: data.user.username,
              email: data.user.email,
              avatarUrl: data.user.avatarUrl || undefined,
              login: true,
              expiry: new Date(data.expiresAt),
              tierId: data.user.tierId ?? data.user.activeTier?.tierId ?? undefined,
              tierName: data.user.tierName ?? data.user.activeTier?.name ?? undefined,
              activeTier: data.user.activeTier
                ? {
                    tierId: data.user.activeTier.tierId,
                    name: data.user.activeTier.name,
                    activeFrom: data.user.activeTier.activeFrom,
                    activeUntil: data.user.activeTier.activeUntil,
                  }
                : null,
              role: data.user.roles[0] || 'user'
            });

            setState("success");
            toast.success("Successfully logged in with Google!");

            // Redirect after a short delay
            setTimeout(() => {
              const redirectUrl = localStorage.getItem("oauthRedirect") || "/";
              localStorage.removeItem("oauthRedirect");
              router.push(redirectUrl);
            }, 2000);

          } else {
            throw new Error("Invalid response from server");
          }
        }
        else {
          throw new Error("Missing authentication parameters");
        }

      } catch (error: any) {
        console.error("OAuth callback error:", error);
        setState("error");
        
        let errorMessage = "Authentication failed";
        if (error?.status && error?.data?.title) {
          errorMessage = error.data.title;
        } else if (error?.message) {
          errorMessage = error.message;
        }
        
        setError(errorMessage);
        toast.error(errorMessage);
      }
    };

    handleCallback();
  }, [searchParams, get, setToken, setUser, router]);

  const renderContent = () => {
    switch (state) {
      case "loading":
        return (
          <motion.div variants={itemVariants} className="text-center space-y-4">
            <div className="mx-auto">
              <Badge variant="secondary" className="px-3 py-1">
                <Chrome className="h-4 w-4 mr-2" />
                Google OAuth
              </Badge>
            </div>
            <div className="flex items-center justify-center space-x-2">
              <Loader2 className="h-5 w-5 animate-spin text-primary" />
              <span>Completing authentication...</span>
            </div>
            <p className="text-sm text-muted-foreground">
              Please wait while we process your Google login
            </p>
          </motion.div>
        );

      case "success":
        return (
          <motion.div variants={itemVariants} className="text-center space-y-4">
            <div className="mx-auto">
              <Badge variant="default" className="px-3 py-1 bg-green-600">
                <CheckCircle className="h-4 w-4 mr-2" />
                Success
              </Badge>
            </div>
            <div className="text-green-600">
              <CheckCircle className="h-12 w-12 mx-auto mb-4" />
              <h3 className="text-lg font-semibold">Authentication Successful!</h3>
            </div>
            <p className="text-sm text-muted-foreground">
              You have been successfully logged in with Google. Redirecting...
            </p>
          </motion.div>
        );

      case "error":
        return (
          <motion.div variants={itemVariants} className="text-center space-y-4">
            <div className="mx-auto">
              <Badge variant="destructive" className="px-3 py-1">
                <XCircle className="h-4 w-4 mr-2" />
                Error
              </Badge>
            </div>
            <div className="text-destructive">
              <XCircle className="h-12 w-12 mx-auto mb-4" />
              <h3 className="text-lg font-semibold">Authentication Failed</h3>
            </div>
            <p className="text-sm text-muted-foreground mb-4">
              {error || "An error occurred during authentication"}
            </p>
            <div className="flex justify-center space-x-3">
              <Button
                variant="outline"
                onClick={() => router.push("/account/login")}
              >
                Try Again
              </Button>
              <Button asChild>
                <Link href="/">
                  <Home className="h-4 w-4 mr-2" />
                  Go Home
                </Link>
              </Button>
            </div>
          </motion.div>
        );

      default:
        return null;
    }
  };

  return (
    <div className="min-h-screen bg-background flex items-center justify-center px-4 py-8">
      <motion.div
        className="w-full max-w-md"
        variants={containerVariants}
        initial="hidden"
        animate="visible"
      >
        <motion.div variants={itemVariants}>
          <Card className="shadow-lg">
            <CardHeader className="text-center">
              <CardTitle className="text-2xl font-bold">OAuth Authentication</CardTitle>
            </CardHeader>
            <CardContent className="space-y-6">
              {renderContent()}
            </CardContent>
          </Card>
        </motion.div>
      </motion.div>
    </div>
  );
}
