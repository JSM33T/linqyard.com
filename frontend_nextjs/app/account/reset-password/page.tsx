"use client";

import { useState, useEffect } from "react";
import { motion } from "framer-motion";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import Link from "next/link";
import { 
  Lock, 
  ArrowLeft,
  Eye,
  EyeOff,
  CheckCircle,
  Check
} from "lucide-react";
import { toast } from "sonner";
import { usePost } from "@/hooks/useApi";
import { ResetPasswordRequest, ResetPasswordResponse } from "@/hooks/types";
import { useRouter, useSearchParams } from "next/navigation";

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

interface PasswordRequirement {
  text: string;
  met: boolean;
}

export default function ResetPasswordPage() {
  const [showPassword, setShowPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);
  const [formData, setFormData] = useState({
    newPassword: "",
    confirmPassword: ""
  });
  const [email, setEmail] = useState("");
  const [token, setToken] = useState("");
  
  // Hooks
  const { mutate: resetPassword, loading: isResetting } = usePost<ResetPasswordResponse>("/auth/reset-password");
  const router = useRouter();
  const searchParams = useSearchParams();

  // Get email and token from URL params
  useEffect(() => {
    const emailParam = searchParams.get('email');
    const tokenParam = searchParams.get('token');
    
    if (emailParam && tokenParam) {
      setEmail(decodeURIComponent(emailParam));
      setToken(decodeURIComponent(tokenParam));
    } else {
      // If no email or token param, redirect to forgot password
      router.push('/account/forgot-password');
    }
  }, [searchParams, router]);

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setFormData(prev => ({
      ...prev,
      [e.target.name]: e.target.value
    }));
  };

  const passwordRequirements: PasswordRequirement[] = [
    { text: "At least 8 characters", met: formData.newPassword.length >= 8 },
  ];

  const isPasswordValid = formData.newPassword.length >= 8;
  const doPasswordsMatch = formData.newPassword === formData.confirmPassword && formData.confirmPassword !== "";
  const isFormValid = isPasswordValid && doPasswordsMatch;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!formData.newPassword.trim()) {
      toast.error("Please enter a new password");
      return;
    }

    if (!isPasswordValid) {
      toast.error("Password does not meet requirements");
      return;
    }

    if (!doPasswordsMatch) {
      toast.error("Passwords do not match");
      return;
    }

    if (!email || !token) {
      toast.error("Missing verification information");
      return;
    }

    try {
      const resetData: ResetPasswordRequest = {
        email: email,
        token: token,
        newPassword: formData.newPassword
      };
      
      const response = await resetPassword(resetData);
      
      if (response.status === 200) {
        toast.success("Password reset successfully! You can now sign in with your new password.");
        router.push('/account/login');
      }
      
    } catch (error: any) {
      console.error("Password reset failed:", error);
      
      if (error?.status && error?.data?.title) {
        toast.error(error.data.title);
      } else if (error?.message) {
        toast.error(error.message);
      } else {
        toast.error("Password reset failed. Please try again.");
      }
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
        {/* Back to Login */}
        <motion.div variants={itemVariants} className="mb-8">
          <Link 
            href="/account/login"
            className="inline-flex items-center text-sm text-muted-foreground hover:text-foreground transition-colors"
          >
            <ArrowLeft className="h-4 w-4 mr-2" />
            Back to Sign In
          </Link>
        </motion.div>

        <motion.div variants={itemVariants}>
          <Card className="shadow-lg">
            <CardHeader className="text-center space-y-4">
              <div className="mx-auto">
                <Badge variant="secondary" className="px-3 py-1">
                  <Lock className="h-4 w-4 mr-2" />
                  Reset Password
                </Badge>
              </div>
              
              <CardTitle className="text-2xl font-bold">Create new password</CardTitle>
              <CardDescription className="text-base">
                Enter a strong password for your account<br />
                <span className="font-medium text-foreground">{email}</span>
              </CardDescription>
            </CardHeader>

            <CardContent className="space-y-6">
              {/* Instructions */}
              <div className="bg-muted/50 rounded-lg p-4 text-sm text-muted-foreground">
                <div className="flex items-start space-x-3">
                  <Lock className="h-5 w-5 mt-0.5 text-primary" />
                  <div>
                    <p className="font-medium text-foreground mb-1">Choose a strong password</p>
                    <p>Your new password should meet all the requirements below for maximum security.</p>
                  </div>
                </div>
              </div>

              {/* Reset Password Form */}
              <form onSubmit={handleSubmit} className="space-y-4">
                <div className="space-y-2">
                  <label htmlFor="newPassword" className="text-sm font-medium">
                    New Password
                  </label>
                  <div className="relative">
                    <Lock className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-muted-foreground" />
                    <Input
                      id="newPassword"
                      name="newPassword"
                      type={showPassword ? "text" : "password"}
                      placeholder="Enter your new password"
                      value={formData.newPassword}
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
                  
                  {/* Password Requirements */}
                  {formData.newPassword && (
                    <div className="space-y-1">
                      {passwordRequirements.map((requirement, index) => (
                        <div key={index} className="flex items-center text-xs">
                          <Check 
                            className={`h-3 w-3 mr-2 ${
                              requirement.met ? 'text-green-600' : 'text-muted-foreground'
                            }`} 
                          />
                          <span className={requirement.met ? 'text-green-600' : 'text-muted-foreground'}>
                            {requirement.text}
                          </span>
                        </div>
                      ))}
                    </div>
                  )}
                </div>

                <div className="space-y-2">
                  <label htmlFor="confirmPassword" className="text-sm font-medium">
                    Confirm New Password
                  </label>
                  <div className="relative">
                    <Lock className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-muted-foreground" />
                    <Input
                      id="confirmPassword"
                      name="confirmPassword"
                      type={showConfirmPassword ? "text" : "password"}
                      placeholder="Confirm your new password"
                      value={formData.confirmPassword}
                      onChange={handleInputChange}
                      className="pl-10 pr-10"
                      required
                    />
                    <Button
                      type="button"
                      variant="ghost"
                      size="sm"
                      className="absolute right-0 top-0 h-full px-3 py-2 hover:bg-transparent"
                      onClick={() => setShowConfirmPassword(!showConfirmPassword)}
                    >
                      {showConfirmPassword ? (
                        <EyeOff className="h-4 w-4 text-muted-foreground" />
                      ) : (
                        <Eye className="h-4 w-4 text-muted-foreground" />
                      )}
                    </Button>
                  </div>
                  
                  {/* Password Match Indicator */}
                  {formData.confirmPassword && (
                    <div className="flex items-center text-xs">
                      <Check 
                        className={`h-3 w-3 mr-2 ${
                          doPasswordsMatch ? 'text-green-600' : 'text-destructive'
                        }`} 
                      />
                      <span className={doPasswordsMatch ? 'text-green-600' : 'text-destructive'}>
                        {doPasswordsMatch ? 'Passwords match' : 'Passwords do not match'}
                      </span>
                    </div>
                  )}
                </div>

                <Button 
                  type="submit" 
                  className="w-full" 
                  disabled={isResetting || !isFormValid}
                >
                  {isResetting ? (
                    <div className="flex items-center">
                      <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-current mr-2"></div>
                      Resetting Password...
                    </div>
                  ) : (
                    <>
                      <CheckCircle className="h-4 w-4 mr-2" />
                      Reset Password
                    </>
                  )}
                </Button>
              </form>

              {/* Help Text */}
              <div className="text-xs text-center text-muted-foreground space-y-1">
                <p>Your password will be encrypted and stored securely.</p>
                <p>Make sure to use a password you haven&apos;t used before.</p>
              </div>

              {/* Sign up link */}
              <div className="text-center text-sm text-muted-foreground">
                Don&apos;t have an account?{" "}
                <Link href="/account/signup" className="text-primary hover:underline font-medium">
                  Create one here
                </Link>
              </div>
            </CardContent>
          </Card>
        </motion.div>
      </motion.div>
    </div>
  );
}