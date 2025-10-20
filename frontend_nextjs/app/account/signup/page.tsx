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
  User,
  ArrowLeft,
  UserPlus,
  Check 
} from "lucide-react";
import { toast } from "sonner";
import { usePost } from "@/hooks/useApi";
import { apiService } from "@/hooks/apiService";
import { AvailabilityCheckResponse, SignupRequest, SignupResponse } from "@/hooks/types";
import { useRouter } from "next/navigation";
import GoogleOAuthButton from "@/components/GoogleOAuthButton";
import GitHubOAuthButton from "@/components/GitHubOAuthButton";

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

type AvailabilityState = {
  loading: boolean;
  isValid: boolean | null;
  available: boolean | null;
  reason: string | null;
  conflictType: string | null;
  value: string;
};

const createInitialAvailabilityState = (): AvailabilityState => ({
  loading: false,
  isValid: null,
  available: null,
  reason: null,
  conflictType: null,
  value: ""
});

export default function SignupPage() {
  const [showPassword, setShowPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);
  const [formData, setFormData] = useState({
    firstName: "",
    lastName: "",
    username: "",
    email: "",
    password: "",
    confirmPassword: ""
  });
  const [acceptTerms, setAcceptTerms] = useState(false);
  const [currentStep, setCurrentStep] = useState(0);
  const [emailAvailability, setEmailAvailability] = useState<AvailabilityState>(createInitialAvailabilityState);
  const [usernameAvailability, setUsernameAvailability] = useState<AvailabilityState>(createInitialAvailabilityState);
  const rootDomain = process.env.NEXT_PUBLIC_ROOT_DOMAIN || "linqyard.com";
  const steps = [
    { title: "Your name", description: "Tell us how you'd like to appear." },
    { title: "Secure your account", description: "Create a password with at least 8 characters." },
    { title: "Contact details", description: "We'll send a verification email to this address." },
    { title: "Claim your domain", description: `Reserve your subdomain on ${rootDomain}.` }
  ];
  
  // Hooks nuked: , error: signupError
  const { mutate: signup, loading: isLoading } = usePost<SignupResponse>("/auth/register");
  const router = useRouter();

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value } = e.target;
    setFormData(prev => ({
      ...prev,
      [name]: value
    }));

    if (name === "email") {
      setEmailAvailability(createInitialAvailabilityState());
    }

    if (name === "username") {
      setUsernameAvailability(createInitialAvailabilityState());
    }
  };

  const passwordRequirements: PasswordRequirement[] = [
    { text: "At least 8 characters", met: formData.password.length >= 8 },
  ];

  const isPasswordValid = formData.password.length >= 8;
  const doPasswordsMatch = formData.password === formData.confirmPassword && formData.confirmPassword !== "";
  const trimmedEmail = formData.email.trim();
  const trimmedUsername = formData.username.trim();
  const isNameStepValid = Boolean(formData.firstName.trim() && formData.lastName.trim());
  const isPasswordStepValid = Boolean(formData.password && formData.confirmPassword && isPasswordValid && doPasswordsMatch);
  const emailRejected = emailAvailability.isValid === false || emailAvailability.available === false;
  const usernameRejected = usernameAvailability.isValid === false || usernameAvailability.available === false;
  const isEmailStepValid = Boolean(trimmedEmail) && !emailRejected;
  const isDomainStepValid = Boolean(trimmedUsername) && acceptTerms && !usernameRejected;
  const stepValidity = [isNameStepValid, isPasswordStepValid, isEmailStepValid, isDomainStepValid];
  const currentStepIsValid = stepValidity[currentStep];
  const isLastStep = currentStep === steps.length - 1;
  const progress = ((currentStep + 1) / steps.length) * 100;
  const isCurrentStepChecking =
    (currentStep === 2 && emailAvailability.loading) ||
    (currentStep === 3 && usernameAvailability.loading);
  const subdomainPreview = trimmedUsername || "yourname";
  const previewUrl = `https://${subdomainPreview}.${rootDomain}`;

  const handleBack = () => {
    setCurrentStep(prev => (prev > 0 ? prev - 1 : prev));
  };

  const validateStep = (step: number) => {
    switch (step) {
      case 0:
        if (!isNameStepValid) {
          toast.error("Please enter your first and last name.");
          return false;
        }
        return true;
      case 1:
        if (!formData.password.trim() || !formData.confirmPassword.trim()) {
          toast.error("Please provide and confirm your password.");
          return false;
        }
        if (!isPasswordValid) {
          toast.error("Password must be at least 8 characters long.");
          return false;
        }
        if (!doPasswordsMatch) {
          toast.error("Passwords do not match.");
          return false;
        }
        return true;
      case 2:
        if (!trimmedEmail) {
          toast.error("Please enter your email address.");
          return false;
        }
        if (emailAvailability.isValid === false) {
          toast.error(emailAvailability.reason ?? "Please provide a valid email address.");
          return false;
        }
        if (emailAvailability.available === false) {
          toast.error(emailAvailability.reason ?? "An account with this email already exists.");
          return false;
        }
        return true;
      case 3:
        if (!trimmedUsername) {
          toast.error("Choose a username to continue.");
          return false;
        }
        if (usernameAvailability.isValid === false) {
          toast.error(usernameAvailability.reason ?? "Please choose a valid username.");
          return false;
        }
        if (usernameAvailability.available === false) {
          toast.error(usernameAvailability.reason ?? "This username is already taken.");
          return false;
        }
        if (!acceptTerms) {
          toast.error("Please accept the terms and conditions.");
          return false;
        }
        return true;
      default:
        return true;
    }
  };

  const checkEmailAvailability = async (): Promise<boolean> => {
    const email = formData.email.trim();

    if (!email) {
      return false;
    }

    if (
      emailAvailability.value === email &&
      emailAvailability.available === true &&
      emailAvailability.isValid === true
    ) {
      return true;
    }

    setEmailAvailability(prev => ({ ...prev, loading: true }));

    try {
      const response = await apiService.get<AvailabilityCheckResponse>(`auth/availability/email?email=${encodeURIComponent(email)}`);
      const payload = response.data?.data;

      if (!payload) {
        throw new Error("Missing availability data.");
      }

      const isValid = payload.isValid;
      const isAvailable = isValid && payload.available;

      setEmailAvailability({
        loading: false,
        isValid,
        available: isAvailable,
        reason: payload.reason ?? null,
        conflictType: payload.conflictType ?? null,
        value: payload.value
      });

      if (!isValid) {
        toast.error(payload.reason ?? "Please provide a valid email address.");
        return false;
      }

      if (!isAvailable) {
        toast.error(payload.reason ?? "An account with this email already exists.");
        return false;
      }

      return true;
    } catch (error) {
      console.error("Email availability check failed", error);
      setEmailAvailability(prev => ({ ...prev, loading: false }));
      toast.error("Could not check email availability. Please try again.");
      return false;
    }
  };

  const checkUsernameAvailability = async (): Promise<boolean> => {
    const username = formData.username.trim();

    if (!username) {
      return false;
    }

    if (
      usernameAvailability.value === username &&
      usernameAvailability.available === true &&
      usernameAvailability.isValid === true
    ) {
      return true;
    }

    setUsernameAvailability(prev => ({ ...prev, loading: true }));

    try {
      const response = await apiService.get<AvailabilityCheckResponse>(`auth/availability/username?username=${encodeURIComponent(username)}`);
      const payload = response.data?.data;

      if (!payload) {
        throw new Error("Missing availability data.");
      }

      const isValid = payload.isValid;
      const isAvailable = isValid && payload.available;

      setUsernameAvailability({
        loading: false,
        isValid,
        available: isAvailable,
        reason: payload.reason ?? null,
        conflictType: payload.conflictType ?? null,
        value: payload.value
      });

      if (!isValid) {
        toast.error(payload.reason ?? "Please choose a valid username.");
        return false;
      }

      if (!isAvailable) {
        toast.error(payload.reason ?? "This username is already taken.");
        return false;
      }

      return true;
    } catch (error) {
      console.error("Username availability check failed", error);
      setUsernameAvailability(prev => ({ ...prev, loading: false }));
      toast.error("Could not check username availability. Please try again.");
      return false;
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!validateStep(currentStep)) {
      return;
    }

    if (currentStep === 2) {
      const emailOk = await checkEmailAvailability();
      if (!emailOk) {
        return;
      }
    }

    if (currentStep === 3) {
      const usernameOk = await checkUsernameAvailability();
      if (!usernameOk) {
        return;
      }
    }

    if (!isLastStep) {
      setCurrentStep(prev => Math.min(prev + 1, steps.length - 1));
      return;
    }

    if (!isNameStepValid) {
      setCurrentStep(0);
      return;
    }

    if (!isPasswordStepValid) {
      setCurrentStep(1);
      return;
    }

    if (!isEmailStepValid) {
      setCurrentStep(2);
      return;
    }

    if (!formData.username.trim() || !acceptTerms) {
      return;
    }

    try {
      const signupData: SignupRequest = {
        email: formData.email,
        password: formData.password,
        username: formData.username,
        firstName: formData.firstName,
        lastName: formData.lastName
      };
      
      const response = await signup(signupData);
      
      // Handle successful signup (status 200 or 201)
      if (response.status === 200 || response.status === 201) {
        //const { data } = response.data;
        
        toast.success(`Account created successfully! Please check your email to verify your account.`);
        console.log("Signup response:", response.data);
        
        // Redirect to email verification page with email parameter
        router.push(`/account/verify-email?email=${encodeURIComponent(formData.email)}`);
      }
      
    } catch (error: any) {
      // Handle API errors with proper toast messages
      console.error("Signup failed:", error);
      
      // Check if it's an API error with status and title
      if (error?.status && error?.data?.title) {
        toast.error(error.data.title);
      } else if (error?.message) {
        toast.error(error.message);
      } else {
        toast.error("Account creation failed. Please try again.");
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
                  <UserPlus className="h-4 w-4 mr-2" />
                  Join linqyard
                </Badge>
              </div>
              
              <CardTitle className="text-2xl font-bold">Create your account</CardTitle>
              <CardDescription className="text-base font-semibold">
                {steps[currentStep].title}
              </CardDescription>
              <p className="text-sm text-muted-foreground">
                {steps[currentStep].description}
              </p>
            </CardHeader>

            <CardContent className="space-y-6">
              {/* Social Signup Buttons */}
              <div className="grid grid-cols-2 gap-3">
                <GoogleOAuthButton className="w-full" />
                <GitHubOAuthButton className="w-full" />
              </div>

              {/* Divider */}
              <div className="relative">
                <div className="absolute inset-0 flex items-center">
                  <span className="w-full border-t" />
                </div>
                <div className="relative flex justify-center text-xs uppercase">
                  <span className="bg-background px-2 text-muted-foreground">
                    Or create account with email
                  </span>
                </div>
              </div>

              {/* Signup Form */}
              <form onSubmit={handleSubmit} className="space-y-6">
                <div className="space-y-2">
                  <div className="flex items-center justify-between text-xs uppercase tracking-wide text-muted-foreground">
                    <span>Step {currentStep + 1} of {steps.length}</span>
                    <span className="font-medium text-foreground">{steps[currentStep].title}</span>
                  </div>
                  <div className="h-2 overflow-hidden rounded-full bg-muted">
                    <div
                      className="h-full rounded-full bg-primary transition-all"
                      style={{ width: `${progress}%` }}
                    />
                  </div>
                </div>

                <div className="space-y-6">
                  {currentStep === 0 && (
                    <div className="grid gap-3 sm:grid-cols-2">
                      <div className="space-y-2">
                        <label htmlFor="firstName" className="text-sm font-medium">
                          First Name
                        </label>
                        <div className="relative">
                          <User className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-muted-foreground" />
                          <Input
                            id="firstName"
                            name="firstName"
                            type="text"
                            placeholder="John"
                            value={formData.firstName}
                            onChange={handleInputChange}
                            className="pl-10"
                            required
                          />
                        </div>
                      </div>
                      <div className="space-y-2">
                        <label htmlFor="lastName" className="text-sm font-medium">
                          Last Name
                        </label>
                        <Input
                          id="lastName"
                          name="lastName"
                          type="text"
                          placeholder="Doe"
                          value={formData.lastName}
                          onChange={handleInputChange}
                          required
                        />
                      </div>
                    </div>
                  )}

                  {currentStep === 1 && (
                    <div className="space-y-4">
                      <div className="space-y-2">
                        <label htmlFor="password" className="text-sm font-medium">
                          Password
                        </label>
                        <div className="relative">
                          <Lock className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-muted-foreground" />
                          <Input
                            id="password"
                            name="password"
                            type={showPassword ? "text" : "password"}
                            placeholder="Create a strong password"
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
                        {formData.password && (
                          <div className="space-y-1">
                            {passwordRequirements.map((requirement, index) => (
                              <div key={index} className="flex items-center text-xs">
                                <Check
                                  className={`h-3 w-3 mr-2 ${requirement.met ? "text-green-600" : "text-muted-foreground"}`}
                                />
                                <span className={requirement.met ? "text-green-600" : "text-muted-foreground"}>
                                  {requirement.text}
                                </span>
                              </div>
                            ))}
                          </div>
                        )}
                      </div>

                      <div className="space-y-2">
                        <label htmlFor="confirmPassword" className="text-sm font-medium">
                          Confirm Password
                        </label>
                        <div className="relative">
                          <Lock className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-muted-foreground" />
                          <Input
                            id="confirmPassword"
                            name="confirmPassword"
                            type={showConfirmPassword ? "text" : "password"}
                            placeholder="Confirm your password"
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
                        {formData.confirmPassword && (
                          <div className="flex items-center text-xs">
                            <Check
                              className={`h-3 w-3 mr-2 ${doPasswordsMatch ? "text-green-600" : "text-destructive"}`}
                            />
                            <span className={doPasswordsMatch ? "text-green-600" : "text-destructive"}>
                              {doPasswordsMatch ? "Passwords match" : "Passwords do not match"}
                            </span>
                          </div>
                        )}
                      </div>
                    </div>
                  )}

                  {currentStep === 2 && (
                    <div className="space-y-2">
                      <label htmlFor="email" className="text-sm font-medium">
                        Email address
                      </label>
                  <div className="relative">
                    <Mail className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-muted-foreground" />
                    <Input
                      id="email"
                      name="email"
                          type="email"
                          placeholder="john@example.com"
                          value={formData.email}
                      onChange={handleInputChange}
                      className="pl-10"
                      required
                    />
                  </div>
                  {emailAvailability.reason && (
                    <p
                      className={`text-xs ${
                        emailAvailability.available
                          ? emailAvailability.conflictType === "PendingVerification"
                            ? "text-amber-600"
                            : "text-muted-foreground"
                          : "text-destructive"
                      }`}
                    >
                      {emailAvailability.reason}
                    </p>
                  )}
                </div>
              )}

              {currentStep === 3 && (
                <div className="space-y-4">
                      <div className="space-y-2">
                        <label htmlFor="username" className="text-sm font-medium">
                          Choose your username
                        </label>
                        <div className="relative">
                          <Input
                            id="username"
                            name="username"
                            type="text"
                            placeholder="johndoe"
                            value={formData.username}
                            onChange={handleInputChange}
                            pattern="^[a-zA-Z0-9_]+$"
                            title="Username can only contain letters, numbers, and underscores"
                            className="pr-36"
                            required
                          />
                          <span className="pointer-events-none absolute right-3 top-1/2 -translate-y-1/2 text-sm text-muted-foreground">
                            .{rootDomain}
                          </span>
                        </div>
                        <p className="text-xs text-muted-foreground">
                          Your profile will live at{" "}
                          <span className="font-medium text-foreground">{previewUrl}</span>
                        </p>
                        {usernameAvailability.reason && (
                          <p
                            className={`text-xs ${
                              usernameAvailability.available
                                ? usernameAvailability.conflictType === "PendingVerification"
                                  ? "text-amber-600"
                                  : "text-muted-foreground"
                                : "text-destructive"
                            }`}
                          >
                            {usernameAvailability.reason}
                          </p>
                        )}
                      </div>

                      <div className="flex items-center space-x-2">
                        <input
                          type="checkbox"
                          id="acceptTerms"
                          checked={acceptTerms}
                          onChange={(e) => setAcceptTerms(e.target.checked)}
                          className="rounded border border-input bg-background"
                        />
                        <label htmlFor="acceptTerms" className="text-sm text-muted-foreground">
                          I agree to the{" "}
                          <Link href="/terms" target="_blank" className="text-primary hover:underline">
                            Terms of Service
                          </Link>
                          {" "}and{" "}
                          <Link href="/privacy" target="_blank" className="text-primary hover:underline">
                            Privacy Policy
                          </Link>
                        </label>
                      </div>
                    </div>
                  )}
                </div>

                <div className="flex items-center justify-between">
                  {currentStep > 0 ? (
                    <Button
                      type="button"
                      variant="ghost"
                      onClick={handleBack}
                      disabled={isLoading || isCurrentStepChecking}
                    >
                      <ArrowLeft className="h-4 w-4 mr-2" />
                      Back
                    </Button>
                  ) : (
                    <div />
                  )}
                  <Button
                    type="submit"
                    className="ml-auto"
                    disabled={isLoading || isCurrentStepChecking || !currentStepIsValid}
                  >
                    {isLastStep ? (
                      isLoading ? (
                        <div className="flex items-center">
                          <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-current mr-2"></div>
                          Creating account...
                        </div>
                      ) : isCurrentStepChecking ? (
                        <div className="flex items-center">
                          <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-current mr-2"></div>
                          Checking availability...
                        </div>
                      ) : (
                        <>
                          <UserPlus className="h-4 w-4 mr-2" />
                          Create Account
                        </>
                      )
                    ) : (
                      isCurrentStepChecking ? (
                        <div className="flex items-center">
                          <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-current mr-2"></div>
                          Checking...
                        </div>
                      ) : (
                        "Continue"
                      )
                    )}
                  </Button>
                </div>
              </form>

              {/* Sign in link */}
              <div className="text-center text-sm text-muted-foreground">
                Already have an account?{" "}
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
