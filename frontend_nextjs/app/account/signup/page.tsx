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
  Github,
  ArrowLeft,
  UserPlus,
  Check 
} from "lucide-react";
import { toast } from "sonner";
import { usePost } from "@/hooks/useApi";
import { SignupRequest, SignupResponse } from "@/hooks/types";
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

interface PasswordRequirement {
  text: string;
  met: boolean;
}

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
  
  // Hooks nuked: , error: signupError
  const { mutate: signup, loading: isLoading } = usePost<SignupResponse>("/auth/register");
  const router = useRouter();

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setFormData(prev => ({
      ...prev,
      [e.target.name]: e.target.value
    }));
  };

  const passwordRequirements: PasswordRequirement[] = [
    { text: "At least 8 characters", met: formData.password.length >= 8 },
  ];

  const isPasswordValid = formData.password.length >= 8;
  const doPasswordsMatch = formData.password === formData.confirmPassword && formData.confirmPassword !== "";
  
  // Check if all required fields are filled
  const areRequiredFieldsFilled = formData.firstName.trim() && 
                                  formData.lastName.trim() && 
                                  formData.username.trim() && 
                                  formData.email.trim() && 
                                  formData.password.trim() && 
                                  formData.confirmPassword.trim();
  
  // Button should be enabled when all conditions are met
  const isFormValid = areRequiredFieldsFilled && 
                     isPasswordValid && 
                     doPasswordsMatch && 
                     acceptTerms;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    // Validate all required fields
    if (!formData.firstName.trim() || !formData.lastName.trim() || !formData.username.trim() || !formData.email.trim()) {
      toast.error("Please fill in all required fields");
      return;
    }

    if (formData.password.length < 8) {
      toast.error("Password must be at least 8 characters long");
      return;
    }

    if (!doPasswordsMatch) {
      toast.error("Passwords do not match");
      return;
    }

    if (!acceptTerms) {
      toast.error("Please accept the terms and conditions");
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

  const handleSocialSignup = (provider: string) => {
    toast.info(`${provider} signup coming soon!`);
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
              <CardDescription className="text-base">
                Get started with linqyard today
              </CardDescription>
            </CardHeader>

            <CardContent className="space-y-6">
              {/* Social Signup Buttons */}
              <div className="grid grid-cols-2 gap-3">
                <GoogleOAuthButton className="w-full" />
                <Button
                  variant="outline"
                  className="w-full"
                  onClick={() => handleSocialSignup("GitHub")}
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
                    Or create account with email
                  </span>
                </div>
              </div>

              {/* Signup Form */}
              <form onSubmit={handleSubmit} className="space-y-4">
                <div className="grid grid-cols-2 gap-3">
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

                <div className="space-y-2">
                  <label htmlFor="username" className="text-sm font-medium">
                    Username
                  </label>
                  <Input
                    id="username"
                    name="username"
                    type="text"
                    placeholder="johndoe"
                    value={formData.username}
                    onChange={handleInputChange}
                    pattern="^[a-zA-Z0-9_]+$"
                    title="Username can only contain letters, numbers, and underscores"
                    required
                  />
                </div>

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
                </div>

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
                  
                  {/* Password Requirements */}
                  {formData.password && (
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

                {/* Terms and Conditions */}
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
                    <Link href="/terms" className="text-primary hover:underline">
                      Terms of Service
                    </Link>
                    {" "}and{" "}
                    <Link href="/privacy" className="text-primary hover:underline">
                      Privacy Policy
                    </Link>
                  </label>
                </div>

                <Button 
                  type="submit" 
                  className="w-full" 
                  disabled={isLoading || !isFormValid}
                >
                  {isLoading ? (
                    <div className="flex items-center">
                      <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-current mr-2"></div>
                      Creating account...
                    </div>
                  ) : (
                    <>
                      <UserPlus className="h-4 w-4 mr-2" />
                      Create Account
                    </>
                  )}
                </Button>
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