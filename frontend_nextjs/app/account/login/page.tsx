"use client";

import { useState } from "react";
import { motion } from "framer-motion";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import Link from "next/link";
import { 
  Eye, 
  EyeOff, 
  Mail, 
  Lock, 
  Github,
  ArrowLeft,
  LogIn 
} from "lucide-react";
import { toast } from "sonner";
import { usePost, useApi } from "@/hooks/useApi";
import { LoginRequest, LoginResponse } from "@/hooks/types";
import { useUser } from "@/contexts/UserContext";
import { useRouter } from "next/navigation";
import GoogleOAuthButton from "@/components/GoogleOAuthButton";

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

export default function LoginPage() {
  const [showPassword, setShowPassword] = useState(false);
  const [formData, setFormData] = useState({
    emailOrUsername: "",
    password: ""
  });
  
  // Hooks //nuked login error state
  const { mutate: login, loading: isLoading } = usePost<LoginResponse>("/auth/login");
  const { setUser } = useUser();
  const { setTokens } = useApi();
  const router = useRouter();

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setFormData(prev => ({
      ...prev,
      [e.target.name]: e.target.value
    }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    try {
      const loginData: LoginRequest = {
        emailOrUsername: formData.emailOrUsername,
        password: formData.password
      };
      
      const response = await login(loginData);
      
      // Handle successful login (status 200)
      if (response.status === 200 && response.data) {
        const { data } = response.data;
        
        // Set both access and refresh tokens in localStorage
        setTokens(data.accessToken, data.refreshToken);
        
        // Update user context with user data
        setUser({
          id: data.user.id,
          firstName: data.user.firstName,
          lastName: data.user.lastName,
          username: data.user.username,
          email: data.user.email,
          avatarUrl: data.user.avatarUrl || undefined,
          login: true,
          expiry: new Date(data.expiresAt),
          role: data.user.roles[0] || 'user' // Take first role or default to 'user'
        });
        
        toast.success("Login successful!");
        
        // Redirect to home page
        router.push('/');
      }
      
    } catch (error: any) {
      // Handle API errors with proper toast messages
      console.error("Login failed:", error);
      
      // Check if it's an API error with status and title
      if (error?.status && error?.data?.title) {
        toast.error(error.data.title);
      } else if (error?.message) {
        toast.error(error.message);
      } else {
        toast.error("Login failed. Please try again.");
      }
    }
  };

  const handleSocialLogin = (provider: string) => {
    toast.info(`${provider} login coming soon!`);
  };

  return (
    <div className="min-h-screen bg-background flex items-center justify-center px-4 py-8">
      <motion.div
        className="w-full max-w-md"
        variants={containerVariants}
        initial="hidden"
        animate="visible"
      >
        {/* Back to Home */}
        <motion.div variants={itemVariants} className="mb-8">
          <Link 
            href="/"
            className="inline-flex items-center text-sm text-muted-foreground hover:text-foreground transition-colors"
          >
            <ArrowLeft className="h-4 w-4 mr-2" />
            Back to linqyard
          </Link>
        </motion.div>

        <motion.div variants={itemVariants}>
          <Card className="shadow-lg">
            <CardHeader className="text-center space-y-4">
              <div className="mx-auto">
                <Badge variant="secondary" className="px-3 py-1">
                  <LogIn className="h-4 w-4 mr-2" />
                  Welcome Back
                </Badge>
              </div>
              
              <CardTitle className="text-2xl font-bold">Sign in to your account</CardTitle>
              <CardDescription className="text-base">
                Enter your credentials to access linqyard
              </CardDescription>
            </CardHeader>

            <CardContent className="space-y-6">
              {/* Social Login Buttons */}
              <div className="grid grid-cols-2 gap-3">
                <GoogleOAuthButton className="w-full" />
                <Button
                  variant="outline"
                  className="w-full"
                  onClick={() => handleSocialLogin("GitHub")}
                >
                  <Github className="h-4 w-4 mr-2" />
                  GitHub
                </Button>
              </div>

              {/* Divider */}
              <div className="relative">
                <div className="absolute inset-0 flex items-center">
                  <span className="w-full border-t" />
                </div>
                <div className="relative flex justify-center text-xs uppercase">
                  <span className="bg-background px-2 text-muted-foreground">
                    Or continue with email
                  </span>
                </div>
              </div>

              {/* Login Form */}
              <form onSubmit={handleSubmit} className="space-y-4">
                <div className="space-y-2">
                  <label htmlFor="emailOrUsername" className="text-sm font-medium leading-none peer-disabled:cursor-not-allowed peer-disabled:opacity-70">
                    Email or Username
                  </label>
                  <div className="relative">
                    <Mail className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-muted-foreground" />
                    <Input
                      id="emailOrUsername"
                      name="emailOrUsername"
                      type="text"
                      placeholder="Enter your email or username"
                      value={formData.emailOrUsername}
                      onChange={handleInputChange}
                      className="pl-10"
                      required
                    />
                  </div>
                </div>

                <div className="space-y-2">
                  <label htmlFor="password" className="text-sm font-medium leading-none peer-disabled:cursor-not-allowed peer-disabled:opacity-70">
                    Password
                  </label>
                  <div className="relative">
                    <Lock className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-muted-foreground" />
                    <Input
                      id="password"
                      name="password"
                      type={showPassword ? "text" : "password"}
                      placeholder="Enter your password"
                      value={formData.password}
                      onChange={handleInputChange}
                      className="pl-10 pr-10"
                      required
                    />
                    <Button
                      type="button"
                      variant="ghost"
                      size="sm"
                      className="absolute right-0 top-0 h-full px-3 py-2 hover:bg-transparent"
                      onClick={() => setShowPassword(!showPassword)}
                    >
                      {showPassword ? (
                        <EyeOff className="h-4 w-4 text-muted-foreground" />
                      ) : (
                        <Eye className="h-4 w-4 text-muted-foreground" />
                      )}
                    </Button>
                  </div>
                </div>

                {/* Forgot Password */}
                <div className="flex justify-end">
                  <Link 
                    href="/account/forgot-password" 
                    className="text-sm text-primary hover:underline"
                  >
                    Forgot your password?
                  </Link>
                </div>

                <Button 
                  type="submit" 
                  className="w-full" 
                  disabled={isLoading}
                >
                  {isLoading ? (
                    <div className="flex items-center">
                      <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-current mr-2"></div>
                      Signing in...
                    </div>
                  ) : (
                    <>
                      <LogIn className="h-4 w-4 mr-2" />
                      Sign in
                    </>
                  )}
                </Button>
              </form>

              {/* Sign up link */}
              <div className="text-center text-sm text-muted-foreground">
                Don&apos;t have an account?{" "}
                <Link href="/account/signup" className="text-primary hover:underline font-medium">
                  Create one here
                </Link>
              </div>

              {/* Terms */}
              <div className="text-xs text-center text-muted-foreground">
                By signing in, you agree to our{" "}
                <Link href="/terms" className="hover:underline">Terms of Service</Link>
                {" "}and{" "}
                <Link href="/privacy" className="hover:underline">Privacy Policy</Link>
              </div>
            </CardContent>
          </Card>
        </motion.div>
      </motion.div>
    </div>
  );
}