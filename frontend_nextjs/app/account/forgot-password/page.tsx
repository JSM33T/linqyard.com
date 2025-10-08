"use client";

import { useState } from "react";
import { motion } from "framer-motion";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import Link from "next/link";
import { 
  Mail, 
  ArrowLeft,
  Send,
  Lock,
  KeyRound
} from "lucide-react";
import { toast } from "sonner";
import { usePost } from "@/hooks/useApi";
import { ForgotPasswordRequest, ForgotPasswordResponse, VerifyEmailResponse } from "@/hooks/types";
import { useRouter } from "next/navigation";

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

export default function ForgotPasswordPage() {
  const [step, setStep] = useState<'email' | 'verify'>('email');
  const [email, setEmail] = useState("");
  const [verificationCode, setVerificationCode] = useState("");
  
  // Hooks
  const { mutate: forgotPassword, loading: isSendingEmail } = usePost<ForgotPasswordResponse>("/auth/forgot-password");
  const { loading: isVerifying } = usePost<VerifyEmailResponse>("/auth/verify-email");
  const router = useRouter();

  const handleSendResetEmail = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!email.trim()) {
      toast.error("Please enter your email address");
      return;
    }

    try {
      const forgotPasswordData: ForgotPasswordRequest = {
        email: email.trim()
      };
      
      const response = await forgotPassword(forgotPasswordData);
      
      if (response.status === 200) {
        toast.success("Password reset code sent! Check your email.");
        setStep('verify');
      }
      
    } catch (error: any) {
      console.error("Forgot password failed:", error);
      
      if (error?.status && error?.data?.title) {
        toast.error(error.data.title);
      } else if (error?.message) {
        toast.error(error.message);
      } else {
        toast.error("Failed to send reset email. Please try again.");
      }
    }
  };

  const handleVerifyCode = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!verificationCode.trim()) {
      toast.error("Please enter the verification code");
      return;
    }

    // For password reset, we don't need to verify the code separately
    // We pass it directly to the reset password page
    toast.success("Redirecting to reset password...");
    router.push(`/account/reset-password?email=${encodeURIComponent(email)}&token=${encodeURIComponent(verificationCode.trim())}`);
  };

  const handleBackToEmail = () => {
    setStep('email');
    setVerificationCode("");
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
              
              <CardTitle className="text-2xl font-bold">
                {step === 'email' ? 'Forgot your password?' : 'Enter verification code'}
              </CardTitle>
              <CardDescription className="text-base">
                {step === 'email' 
                  ? "No worries! Enter your email address and we'll send you a reset code."
                  : `We've sent a verification code to ${email}`
                }
              </CardDescription>
            </CardHeader>

            <CardContent className="space-y-6">
              {step === 'email' ? (
                <>
                  {/* Instructions */}
                  <div className="bg-muted/50 rounded-lg p-4 text-sm text-muted-foreground">
                    <div className="flex items-start space-x-3">
                      <Mail className="h-5 w-5 mt-0.5 text-primary" />
                      <div>
                        <p className="font-medium text-foreground mb-1">How it works</p>
                        <p>Enter your email address and we&apos;ll send you a verification code to reset your password.</p>
                      </div>
                    </div>
                  </div>

                  {/* Email Form */}
                  <form onSubmit={handleSendResetEmail} className="space-y-4">
                    <div className="space-y-2">
                      <label htmlFor="email" className="text-sm font-medium leading-none">
                        Email Address
                      </label>
                      <div className="relative">
                        <Mail className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-muted-foreground" />
                        <Input
                          id="email"
                          name="email"
                          type="email"
                          placeholder="Enter your email address"
                          value={email}
                          onChange={(e) => setEmail(e.target.value)}
                          className="pl-10"
                          required
                        />
                      </div>
                    </div>

                    <Button 
                      type="submit" 
                      className="w-full" 
                      disabled={isSendingEmail || !email.trim()}
                    >
                      {isSendingEmail ? (
                        <div className="flex items-center">
                          <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-current mr-2"></div>
                          Sending...
                        </div>
                      ) : (
                        <>
                          <Send className="h-4 w-4 mr-2" />
                          Send Reset Code
                        </>
                      )}
                    </Button>
                  </form>
                </>
              ) : (
                <>
                  {/* Instructions */}
                  <div className="bg-muted/50 rounded-lg p-4 text-sm text-muted-foreground">
                    <div className="flex items-start space-x-3">
                      <KeyRound className="h-5 w-5 mt-0.5 text-primary" />
                      <div>
                        <p className="font-medium text-foreground mb-1">Check your email</p>
                        <p>Enter the 8-character verification code from your email to continue with password reset.</p>
                      </div>
                    </div>
                  </div>

                  {/* Verification Form */}
                  <form onSubmit={handleVerifyCode} className="space-y-4">
                    <div className="space-y-2">
                      <label htmlFor="verificationCode" className="text-sm font-medium leading-none">
                        Verification Code
                      </label>
                      <div className="relative">
                        <KeyRound className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-muted-foreground" />
                        <Input
                          id="verificationCode"
                          name="verificationCode"
                          type="text"
                          placeholder="Enter 8-character code"
                          value={verificationCode}
                          onChange={(e) => setVerificationCode(e.target.value.toUpperCase())}
                          className="pl-10 text-center tracking-widest"
                          maxLength={8}
                          required
                          autoComplete="off"
                        />
                      </div>
                    </div>

                    <Button 
                      type="submit" 
                      className="w-full" 
                      disabled={isVerifying || !verificationCode.trim()}
                    >
                      {isVerifying ? (
                        <div className="flex items-center">
                          <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-current mr-2"></div>
                          Verifying...
                        </div>
                      ) : (
                        <>
                          <KeyRound className="h-4 w-4 mr-2" />
                          Verify Code
                        </>
                      )}
                    </Button>
                  </form>

                  {/* Back to Email */}
                  <Button
                    variant="outline"
                    onClick={handleBackToEmail}
                    className="w-full"
                  >
                    <ArrowLeft className="h-4 w-4 mr-2" />
                    Back to Email
                  </Button>
                </>
              )}

              {/* Help Text */}
              <div className="text-xs text-center text-muted-foreground space-y-1">
                <p>Check your spam folder if you don&apos;t see the email.</p>
                <p>The verification code expires in 15 minutes.</p>
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