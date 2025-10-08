"use client";

import { useState, useEffect } from "react";
import { motion } from "framer-motion";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import Link from "next/link";
import { 
  Mail, 
  ArrowLeft,
  CheckCircle,
  RefreshCw,
  KeyRound
} from "lucide-react";
import { toast } from "sonner";
import { usePost } from "@/hooks/useApi";
import { VerifyEmailRequest, VerifyEmailResponse, ResendVerificationRequest, ResendVerificationResponse } from "@/hooks/types";
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

export default function VerifyEmailPage() {
  const [verificationCode, setVerificationCode] = useState("");
  const [email, setEmail] = useState("");
  const [canResend, setCanResend] = useState(false);
  const [resendCountdown, setResendCountdown] = useState(60);
  
  // Hooks
  const { mutate: verifyEmail, loading: isVerifying } = usePost<VerifyEmailResponse>("/auth/verify-email");
  const { mutate: resendVerification, loading: isResending } = usePost<ResendVerificationResponse>("/auth/resend-verification");
  const router = useRouter();
  const searchParams = useSearchParams();

  // Get email from URL params (passed from signup)
  useEffect(() => {
    const emailParam = searchParams.get('email');
    if (emailParam) {
      setEmail(decodeURIComponent(emailParam));
    } else {
      // If no email param, redirect to signup
      router.push('/account/signup');
    }
  }, [searchParams, router]);

  // Countdown for resend button
  useEffect(() => {
    if (resendCountdown > 0) {
      const timer = setTimeout(() => setResendCountdown(resendCountdown - 1), 1000);
      return () => clearTimeout(timer);
    } else {
      setCanResend(true);
    }
  }, [resendCountdown]);

  const handleVerify = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!verificationCode.trim()) {
      toast.error("Please enter the verification code");
      return;
    }

    if (!email) {
      toast.error("Email address is missing");
      return;
    }

    try {
      const verificationData: VerifyEmailRequest = {
        email: email,
        token: verificationCode.trim()
      };
      
      const response = await verifyEmail(verificationData);
      
      if (response.status === 200) {
        toast.success("Email verified successfully! You can now sign in.");
        router.push('/account/login');
      }
      
    } catch (error: any) {
      console.error("Email verification failed:", error);
      
      if (error?.status && error?.data?.title) {
        toast.error(error.data.title);
      } else if (error?.message) {
        toast.error(error.message);
      } else {
        toast.error("Email verification failed. Please try again.");
      }
    }
  };

  const handleResendVerification = async () => {
    if (!email) {
      toast.error("Email address is missing");
      return;
    }

    try {
      const resendData: ResendVerificationRequest = {
        email: email
      };
      
      const response = await resendVerification(resendData);
      
      if (response.status === 200) {
        toast.success("Verification code sent! Check your email.");
        setCanResend(false);
        setResendCountdown(60);
      }
      
    } catch (error: any) {
      console.error("Resend verification failed:", error);
      
      if (error?.status && error?.data?.title) {
        toast.error(error.data.title);
      } else if (error?.message) {
        toast.error(error.message);
      } else {
        toast.error("Failed to resend verification code. Please try again.");
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
        {/* Back to Signup */}
        <motion.div variants={itemVariants} className="mb-8">
          <Link 
            href="/account/signup"
            className="inline-flex items-center text-sm text-muted-foreground hover:text-foreground transition-colors"
          >
            <ArrowLeft className="h-4 w-4 mr-2" />
            Back to Signup
          </Link>
        </motion.div>

        <motion.div variants={itemVariants}>
          <Card className="shadow-lg">
            <CardHeader className="text-center space-y-4">
              <div className="mx-auto">
                <Badge variant="secondary" className="px-3 py-1">
                  <Mail className="h-4 w-4 mr-2" />
                  Verify Email
                </Badge>
              </div>
              
              <CardTitle className="text-2xl font-bold">Check your email</CardTitle>
              <CardDescription className="text-base">
                We&apos;ve sent a verification code to<br />
                <span className="font-medium text-foreground">{email}</span>
              </CardDescription>
            </CardHeader>

            <CardContent className="space-y-6">
              {/* Instructions */}
              <div className="bg-muted/50 rounded-lg p-4 text-sm text-muted-foreground">
                <div className="flex items-start space-x-3">
                  <Mail className="h-5 w-5 mt-0.5 text-primary" />
                  <div>
                    <p className="font-medium text-foreground mb-1">Check your inbox</p>
                    <p>Enter the 8-character verification code from your email to complete your account setup.</p>
                  </div>
                </div>
              </div>

              {/* Verification Form */}
              <form onSubmit={handleVerify} className="space-y-4">
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
                      <CheckCircle className="h-4 w-4 mr-2" />
                      Verify Email
                    </>
                  )}
                </Button>
              </form>

              {/* Resend Verification */}
              <div className="text-center">
                <p className="text-sm text-muted-foreground mb-3">
                  Didn&apos;t receive the code?
                </p>
                
                <Button
                  variant="outline"
                  onClick={handleResendVerification}
                  disabled={!canResend || isResending}
                  className="w-full"
                >
                  {isResending ? (
                    <div className="flex items-center">
                      <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-current mr-2"></div>
                      Sending...
                    </div>
                  ) : canResend ? (
                    <>
                      <RefreshCw className="h-4 w-4 mr-2" />
                      Resend Code
                    </>
                  ) : (
                    <>
                      <RefreshCw className="h-4 w-4 mr-2" />
                      Resend in {resendCountdown}s
                    </>
                  )}
                </Button>
              </div>

              {/* Help Text */}
              <div className="text-xs text-center text-muted-foreground space-y-1">
                <p>Check your spam folder if you don&apos;t see the email.</p>
                <p>The verification code expires in 15 minutes.</p>
              </div>

              {/* Sign in link */}
              <div className="text-center text-sm text-muted-foreground">
                Already verified?{" "}
                <Link href="/account/login" className="text-primary hover:underline font-medium">
                  Sign in here
                </Link>
              </div>
            </CardContent>
          </Card>
        </motion.div>
      </motion.div>
    </div>
  );
}