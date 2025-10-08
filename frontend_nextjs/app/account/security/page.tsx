"use client";

import { useState } from "react";
import { motion } from "framer-motion";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Badge } from "@/components/ui/badge";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter, 
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import Link from "next/link";
import AccessDenied from "@/components/AccessDenied";
import { 
  ArrowLeft, 
  Shield, 
  Lock, 
  Eye, 
  EyeOff, 
  Check,
  Smartphone,
  Monitor,
  Globe,
  LogOut,
  AlertTriangle,
  Trash2,
  Download,
  RefreshCw
} from "lucide-react";
import { toast } from "sonner";
import { useUser } from "@/contexts/UserContext";
import { useGet, usePost } from "@/hooks/useApi";
import { apiService } from "@/hooks/apiService";
import { 
  ChangePasswordRequest, 
  ChangePasswordResponse,
  GetSessionsResponse,
  LogoutSessionResponse,
  LogoutAllSessionsResponse,
  SessionData
} from "@/hooks/types";

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

export default function SecurityPage() {
  const { user, isAuthenticated } = useUser();
  const [showCurrentPassword, setShowCurrentPassword] = useState(false);
  const [showNewPassword, setShowNewPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);
  const [showLogoutDialog, setShowLogoutDialog] = useState(false);
  const [showSessionLogoutDialog, setShowSessionLogoutDialog] = useState(false);
  const [showLogoutAllDialog, setShowLogoutAllDialog] = useState(false);
  const [sessionToLogout, setSessionToLogout] = useState<SessionData | null>(null);
  const [, setPasswordChangeSuccess] = useState(false);
  const [passwordData, setPasswordData] = useState({
    currentPassword: "",
    newPassword: "",
    confirmPassword: ""
  });
  const [showSessionsModal, setShowSessionsModal] = useState(false);

  // API hooks
  const { data: sessionsData, loading: sessionsLoading, refetch: refetchSessions } = useGet<GetSessionsResponse>("/profile/sessions");
  const { mutate: changePassword, loading: isChangingPassword } = usePost<ChangePasswordResponse>("/profile/password");
  const { mutate: setPassword, loading: isSettingPassword } = usePost<any>("/auth/set-password");
  const { loading: isLoggingOutSession } = usePost<LogoutSessionResponse>("");
  const { mutate: logoutAllSessions, loading: isLoggingOutAll } = usePost<LogoutAllSessionsResponse>("/profile/sessions/logout-all");

  const handlePasswordInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setPasswordData(prev => ({
      ...prev,
      [e.target.name]: e.target.value
    }));
  };

  const passwordRequirements: PasswordRequirement[] = [
    { text: "At least 8 characters", met: passwordData.newPassword.length >= 8 },
  ];

  const isNewPasswordValid = passwordData.newPassword.length >= 8;
  const doPasswordsMatch = passwordData.newPassword === passwordData.confirmPassword && passwordData.confirmPassword !== "";

  // Determine the authMethod for the current session and map it to known providers.
  // Backend returns a single authMethod string, e.g. "EmailPassword", "Google", "Apple", "GitHub".
  // We'll map these explicitly and default to EmailPassword (conservative) if unknown.
  const currentSessionAuthMethod = (sessionsData?.data?.sessions?.find(s => s.isCurrentSession)?.authMethod ?? "").toString();

  const mapAuthProvider = (authMethodRaw: string) => {
    const v = (authMethodRaw || '').toLowerCase();
    if (!v) return 'emailpassword'; // default
    // Exact checks first
    if (v === 'emailpassword' || v === 'email_password' || v === 'email/password') return 'emailpassword';
    if (v === 'google' || v.includes('google')) return 'google';
    if (v === 'apple' || v.includes('apple')) return 'apple';
    if (v === 'github' || v.includes('github')) return 'github';
    // Fallback heuristics
    if (v.includes('email') && v.includes('password')) return 'emailpassword';
    if (v.includes('email')) return 'emailpassword';
    if (v.includes('oauth') || v.includes('auth0')) return 'oauth';
    return 'emailpassword';
  };

  const mappedProvider = mapAuthProvider(currentSessionAuthMethod);
  const isEmailPassword = mappedProvider === 'emailpassword';
  const isGoogle = mappedProvider === 'google';
  const isApple = mappedProvider === 'apple';
  const isGitHub = mappedProvider === 'github';

  // If the auth method is EmailPassword we require the current password for changing it.
  // Otherwise (Google, OAuth providers) we show a Set Password flow which does not require the current password.
  const isPasswordFormValid = isEmailPassword
    ? passwordData.currentPassword.trim() && isNewPasswordValid && doPasswordsMatch
    : isNewPasswordValid && doPasswordsMatch;

  const handleChangePassword = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!passwordData.currentPassword.trim()) {
      toast.error("Please enter your current password");
      return;
    }

    if (!isNewPasswordValid) {
      toast.error("New password does not meet requirements");
      return;
    }

    if (!doPasswordsMatch) {
      toast.error("New passwords do not match");
      return;
    }

    try {
      const changePasswordRequest: ChangePasswordRequest = {
        currentPassword: passwordData.currentPassword,
        newPassword: passwordData.newPassword
      };
      
      const response = await changePassword(changePasswordRequest);
      
      if (response.status === 200) {
        toast.success("Password changed successfully!");
        setPasswordData({
          currentPassword: "",
          newPassword: "",
          confirmPassword: ""
        });
        setPasswordChangeSuccess(true);
        // Refetch sessions to get updated data
        refetchSessions();
        // Show dialog to ask about logging out other devices
        setShowLogoutDialog(true);
      }
    } catch (error: any) {
      console.error("Password change failed:", error);
      
      if (error?.status && error?.data?.title) {
        toast.error(error.data.title);
      } else if (error?.message) {
        toast.error(error.message);
      } else {
        toast.error("Failed to change password. Please try again.");
      }
    }
  };

  // New flow: Set password for Google-only accounts (no current password required)
  const handleSetPassword = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!isNewPasswordValid) {
      toast.error("New password does not meet requirements");
      return;
    }

    if (!doPasswordsMatch) {
      toast.error("New passwords do not match");
      return;
    }

    if (!user || !user.email) {
      toast.error("User email not available");
      return;
    }

    try {
      const setReq = {
        email: user.email,
        newPassword: passwordData.newPassword
      } as any;

      const response = await setPassword(setReq);

      if (response.status === 200) {
        toast.success("Password set successfully!");
        setPasswordData({ currentPassword: "", newPassword: "", confirmPassword: "" });
        refetchSessions();
      }
    } catch (error: any) {
      console.error("Set password failed:", error);
      if (error?.status && error?.data?.title) {
        toast.error(error.data.title);
      } else if (error?.message) {
        toast.error(error.message);
      } else {
        toast.error("Failed to set password. Please try again.");
      }
    }
  };

  const handleLogoutOtherDevicesChoice = async (shouldLogout: boolean) => {
    setShowLogoutDialog(false);
    
    if (shouldLogout) {
      try {
        const response = await logoutAllSessions();
        
        if (response.status === 200) {
          toast.success("Logged out from all other devices successfully!");
          refetchSessions();
        }
      } catch (error: any) {
        console.error("Logout all sessions failed:", error);
        toast.error("Failed to logout other devices. You can do this manually from the sessions list below.");
      }
    } else {
      toast.info("Password changed successfully. Your other devices will remain logged in.");
    }
  };

  const handleLogoutSessionClick = (session: SessionData) => {
    setSessionToLogout(session);
    setShowSessionLogoutDialog(true);
  };

  const handleLogoutSessionConfirm = async (confirmed: boolean) => {
    setShowSessionLogoutDialog(false);
    
    if (!confirmed || !sessionToLogout) {
      setSessionToLogout(null);
      return;
    }

    try {
      // Use apiService.post with dynamic endpoint
      const response = await apiService.post<LogoutSessionResponse>(`/profile/sessions/${sessionToLogout.id}/logout`, {});
      
      if (response.status === 200) {
        toast.success(response.data.data?.message || "Session logged out successfully");
        refetchSessions();
      }
    } catch (error: any) {
      console.error("Logout session failed:", error);
      
      if (error?.status && error?.data?.title) {
        toast.error(error.data.title);
      } else if (error?.message) {
        toast.error(error.message);
      } else {
        toast.error("Failed to logout session. Please try again.");
      }
    } finally {
      setSessionToLogout(null);
    }
  };

  const handleLogoutAllOtherSessionsClick = () => {
    setShowLogoutAllDialog(true);
  };

  const handleLogoutAllOtherSessionsConfirm = async (confirmed: boolean) => {
    setShowLogoutAllDialog(false);
    
    if (!confirmed) {
      return;
    }

    try {
      const response = await logoutAllSessions();
      
      if (response.status === 200) {
        toast.success(response.data.data.message);
        refetchSessions();
      }
    } catch (error: any) {
      console.error("Logout all sessions failed:", error);
      
      if (error?.status && error?.data?.title) {
        toast.error(error.data.title);
      } else if (error?.message) {
        toast.error(error.message);
      } else {
        toast.error("Failed to logout other sessions. Please try again.");
      }
    }
  };

  const handleExportData = () => {
    toast.info("Data export will be available soon!");
  };

  const handleDeleteAccount = () => {
    toast.error("Account deletion will be available soon!");
  };

  const getDeviceIcon = (userAgent: string) => {
    if (userAgent.includes('Mobile') || userAgent.includes('iPhone') || userAgent.includes('Android')) {
      return <Smartphone className="h-5 w-5" />;
    }
    return <Monitor className="h-5 w-5" />;
  };

  const formatLastSeen = (lastSeenAt: string) => {
    const date = new Date(lastSeenAt);
    const now = new Date();
    const diffInMinutes = Math.floor((now.getTime() - date.getTime()) / (1000 * 60));
    
    if (diffInMinutes < 1) return "Just now";
    if (diffInMinutes < 60) return `${diffInMinutes} minutes ago`;
    if (diffInMinutes < 1440) return `${Math.floor(diffInMinutes / 60)} hours ago`;
    return date.toLocaleDateString();
  };

  if (!isAuthenticated || !user) {
    return <AccessDenied />;
  }

  return (
    <div className="min-h-screen bg-background">
      <div className="container mx-auto px-4 py-8 max-w-4xl">
        <motion.div
          variants={containerVariants}
          initial="hidden"
          animate="visible"
          className="space-y-8"
        >
          {/* Back Navigation */}
          <motion.div variants={itemVariants}>
            <Link 
              href="/account/profile"
              className="inline-flex items-center text-sm text-muted-foreground hover:text-foreground transition-colors"
            >
              <ArrowLeft className="h-4 w-4 mr-2" />
              Back to Profile
            </Link>
          </motion.div>

          {/* Header */}
          <motion.div variants={itemVariants} className="space-y-2">
            <div className="flex items-center space-x-3">
              <Shield className="h-8 w-8 text-primary" />
              <h1 className="text-3xl font-bold">Security Settings</h1>
            </div>
            <p className="text-muted-foreground">
              Manage your account security and active sessions
            </p>
          </motion.div>

          {/* Password Change / Set Password Card */}
          <motion.div variants={itemVariants}>
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center">
                  <Lock className="h-5 w-5 mr-2" />
                  Change Password
                </CardTitle>
                <CardDescription>
                  Update your password to keep your account secure
                </CardDescription>
              </CardHeader>
              <CardContent>
                { /* Determine whether to show Set Password (non-email) or Change Password (EmailPassword) */ }
                {!isEmailPassword ? (
                  <form onSubmit={handleSetPassword} className="space-y-4">
                    <div className="space-y-2">
                      <p className="text-xs text-muted-foreground">
                        {isGoogle && 'Your account was created via Google sign-in. You can set a password to enable email/password sign in.'}
                        {isApple && 'Your account was created via Apple sign-in. You can set a password to enable email/password sign in.'}
                        {isGitHub && 'Your account was created via GitHub sign-in. You can set a password to enable email/password sign in.'}
                        {!isGoogle && !isApple && !isGitHub && 'You can set a password to enable email/password sign in for your account.'}
                      </p>
                      <label htmlFor="newPassword" className="text-sm font-medium">
                        New Password
                      </label>
                      <div className="relative">
                        <Lock className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-muted-foreground" />
                        <Input
                          id="newPassword"
                          name="newPassword"
                          type={showNewPassword ? "text" : "password"}
                          placeholder="Enter your new password"
                          value={passwordData.newPassword}
                          onChange={handlePasswordInputChange}
                          className="pl-10 pr-10"
                          required
                        />
                        <Button
                          type="button"
                          variant="ghost"
                          size="sm"
                          className="absolute right-0 top-0 h-full px-3 py-2 hover:bg-transparent"
                          onClick={() => setShowNewPassword(!showNewPassword)}
                        >
                          {showNewPassword ? (
                            <EyeOff className="h-4 w-4 text-muted-foreground" />
                          ) : (
                            <Eye className="h-4 w-4 text-muted-foreground" />
                          )}
                        </Button>
                      </div>

                      {/* Password Requirements */}
                      {passwordData.newPassword && (
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
                          value={passwordData.confirmPassword}
                          onChange={handlePasswordInputChange}
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
                      {passwordData.confirmPassword && (
                        <div className="flex items-center text-xs">
                          <Check 
                            className={`h-3 w-3 mr-2 ${
                              doPasswordsMatch ? 'text-green-600' : 'text-red-600'
                            }`} 
                          />
                          <span className={doPasswordsMatch ? 'text-green-600' : 'text-red-600'}>
                            {doPasswordsMatch ? 'Passwords match' : 'Passwords do not match'}
                          </span>
                        </div>
                      )}
                    </div>

                    <Button 
                      type="submit" 
                      disabled={!isNewPasswordValid || !doPasswordsMatch || isSettingPassword}
                      className="w-full"
                    >
                      {isSettingPassword ? (
                        <div className="flex items-center">
                          <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-current mr-2"></div>
                          Setting Password...
                        </div>
                      ) : (
                        <>
                          <Lock className="h-4 w-4 mr-2" />
                          Set Password
                        </>
                      )}
                    </Button>
                  </form>
                ) : (
                  <form onSubmit={handleChangePassword} className="space-y-4">
                  <div className="space-y-2">
                    <label htmlFor="currentPassword" className="text-sm font-medium">
                      Current Password
                    </label>
                    <div className="relative">
                      <Lock className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-muted-foreground" />
                      <Input
                        id="currentPassword"
                        name="currentPassword"
                        type={showCurrentPassword ? "text" : "password"}
                        placeholder="Enter your current password"
                        value={passwordData.currentPassword}
                        onChange={handlePasswordInputChange}
                        className="pl-10 pr-10"
                        required
                      />
                      <Button
                        type="button"
                        variant="ghost"
                        size="sm"
                        className="absolute right-0 top-0 h-full px-3 py-2 hover:bg-transparent"
                        onClick={() => setShowCurrentPassword(!showCurrentPassword)}
                      >
                        {showCurrentPassword ? (
                          <EyeOff className="h-4 w-4 text-muted-foreground" />
                        ) : (
                          <Eye className="h-4 w-4 text-muted-foreground" />
                        )}
                      </Button>
                    </div>
                  </div>

                  <div className="space-y-2">
                    <label htmlFor="newPassword" className="text-sm font-medium">
                      New Password
                    </label>
                    <div className="relative">
                      <Lock className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-muted-foreground" />
                      <Input
                        id="newPassword"
                        name="newPassword"
                        type={showNewPassword ? "text" : "password"}
                        placeholder="Enter your new password"
                        value={passwordData.newPassword}
                        onChange={handlePasswordInputChange}
                        className="pl-10 pr-10"
                        required
                      />
                      <Button
                        type="button"
                        variant="ghost"
                        size="sm"
                        className="absolute right-0 top-0 h-full px-3 py-2 hover:bg-transparent"
                        onClick={() => setShowNewPassword(!showNewPassword)}
                      >
                        {showNewPassword ? (
                          <EyeOff className="h-4 w-4 text-muted-foreground" />
                        ) : (
                          <Eye className="h-4 w-4 text-muted-foreground" />
                        )}
                      </Button>
                    </div>
                    
                    {/* Password Requirements */}
                    {passwordData.newPassword && (
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
                        value={passwordData.confirmPassword}
                        onChange={handlePasswordInputChange}
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
                    {passwordData.confirmPassword && (
                      <div className="flex items-center text-xs">
                        <Check 
                          className={`h-3 w-3 mr-2 ${
                            doPasswordsMatch ? 'text-green-600' : 'text-red-600'
                          }`} 
                        />
                        <span className={doPasswordsMatch ? 'text-green-600' : 'text-red-600'}>
                          {doPasswordsMatch ? 'Passwords match' : 'Passwords do not match'}
                        </span>
                      </div>
                    )}
                  </div>

                  <Button 
                    type="submit" 
                    disabled={!isPasswordFormValid || isChangingPassword}
                    className="w-full"
                  >
                    {isChangingPassword ? (
                      <div className="flex items-center">
                        <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-current mr-2"></div>
                        Changing Password...
                      </div>
                    ) : (
                      <>
                        <Lock className="h-4 w-4 mr-2" />
                        Change Password
                      </>
                    )}
                  </Button>
                </form>
                )}
              </CardContent>
            </Card>
          </motion.div>

          {/* Session Management: simplified - open modal to view/manage sessions */}
          <motion.div variants={itemVariants}>
            <Card>
               <CardHeader>
                <CardTitle className="flex items-center">
                  <Lock className="h-5 w-5 mr-2" />
                  Active Sessions
                </CardTitle>
                <CardDescription>
                  Manage your active sessions
                </CardDescription>
              </CardHeader>
              <CardContent className="space-y-6">
                <div className="flex items-center justify-between">
                  <div>
                    <label className="text-sm font-medium">View Sessions</label>
                    <p className="text-xs text-muted-foreground">Open modal to view and manage active sessions</p>
                  </div>
                  <div className="flex items-center space-x-2">
                    <Button variant="outline" size="sm" onClick={() => setShowSessionsModal(true)}>
                     View Sessions
                    </Button>
                  </div>
                </div>
              </CardContent>
            </Card>
          </motion.div>

          {/* Security Tips */}
          <motion.div variants={itemVariants}>
            <Card>
              <CardHeader>
                <CardTitle>Data & Account</CardTitle>
                <CardDescription>
                  Manage your data and account settings
                </CardDescription>
              </CardHeader>
              <CardContent className="space-y-6">
                <div className="flex items-center justify-between">
                  <div>
                    <label className="text-sm font-medium">Export Data</label>
                    <p className="text-xs text-muted-foreground">Download your account data</p>
                  </div>
                  <Button variant="outline" size="sm" onClick={handleExportData}>
                    <Download className="h-4 w-4 mr-2" />
                    Export
                  </Button>
                </div>

                <div className="flex items-center justify-between">
                  <div>
                    <label className="text-sm font-medium text-red-600">Delete Account</label>
                    <p className="text-xs text-muted-foreground">Permanently delete your account</p>
                  </div>
                  <Button 
                    variant="destructive" 
                    size="sm" 
                    onClick={handleDeleteAccount}
                  >
                    <Trash2 className="h-4 w-4 mr-2" />
                    Delete
                  </Button>
                </div>
              </CardContent>
            </Card>
          </motion.div>
          <motion.div variants={itemVariants}>
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center">
                  <AlertTriangle className="h-5 w-5 mr-2 text-yellow-600" />
                  Security Tips
                </CardTitle>
              </CardHeader>
              <CardContent>
                <div className="space-y-3 text-sm text-muted-foreground">
                  <div className="flex items-start space-x-2">
                    <div className="w-2 h-2 bg-primary rounded-full mt-2 flex-shrink-0"></div>
                    <p>Use a strong, unique password that you don&apos;t use anywhere else.</p>
                  </div>
                  <div className="flex items-start space-x-2">
                    <div className="w-2 h-2 bg-primary rounded-full mt-2 flex-shrink-0"></div>
                    <p>Regularly review your active sessions and logout from devices you no longer use.</p>
                  </div>
                  <div className="flex items-start space-x-2">
                    <div className="w-2 h-2 bg-primary rounded-full mt-2 flex-shrink-0"></div>
                    <p>If you notice any suspicious activity, change your password immediately.</p>
                  </div>
                  <div className="flex items-start space-x-2">
                    <div className="w-2 h-2 bg-primary rounded-full mt-2 flex-shrink-0"></div>
                    <p>Always logout from shared or public computers.</p>
                  </div>
                </div>
              </CardContent>
            </Card>
          </motion.div>
        </motion.div>

        {/* Logout Other Devices Dialog */}
        <Dialog open={showLogoutDialog} onOpenChange={setShowLogoutDialog}>
          <DialogContent className="sm:max-w-md">
            <DialogHeader>
              <DialogTitle className="flex items-center">
                <Shield className="h-5 w-5 mr-2 text-green-600" />
                Password Changed Successfully
              </DialogTitle>
              <DialogDescription className="space-y-3">
                <p>Your password has been updated successfully!</p>
                <div className="bg-yellow-50 dark:bg-yellow-900/20 border border-yellow-200 dark:border-yellow-800 rounded-lg p-3">
                  <div className="flex items-start space-x-2">
                    <AlertTriangle className="h-4 w-4 text-yellow-600 mt-0.5 flex-shrink-0" />
                    <div className="text-sm">
                      <p className="font-medium text-yellow-800 dark:text-yellow-200">Security Recommendation</p>
                      <p className="text-yellow-700 dark:text-yellow-300 mt-1">
                        For security, we recommend logging out from all other devices. This ensures your new password is required everywhere.
                      </p>
                    </div>
                  </div>
                </div>
                <p className="text-sm">
                  Would you like to logout from all other devices now? You can also do this manually later using the session management below.
                </p>
              </DialogDescription>
            </DialogHeader>
            <DialogFooter className="flex-col sm:flex-row gap-2">
              <Button
                variant="outline"
                onClick={() => handleLogoutOtherDevicesChoice(false)}
                className="w-full sm:w-auto"
              >
                Keep Other Sessions
              </Button>
              <Button
                variant="default"
                onClick={() => handleLogoutOtherDevicesChoice(true)}
                disabled={isLoggingOutAll}
                className="w-full sm:w-auto"
              >
                {isLoggingOutAll ? (
                  <div className="flex items-center">
                    <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-current mr-2"></div>
                    Logging Out...
                  </div>
                ) : (
                  <>
                    <LogOut className="h-4 w-4 mr-2" />
                    Logout Other Devices
                  </>
                )}
              </Button>
            </DialogFooter>
          </DialogContent>
        </Dialog>

        {/* Session Logout Confirmation Dialog */}
        <Dialog open={showSessionLogoutDialog} onOpenChange={setShowSessionLogoutDialog}>
          <DialogContent className="sm:max-w-md">
            <DialogHeader>
              <DialogTitle className="flex items-center">
                <LogOut className="h-5 w-5 mr-2 text-red-600" />
                Logout Session
              </DialogTitle>
              <DialogDescription className="space-y-3">
                <p>Are you sure you want to logout this session?</p>
                {sessionToLogout && (
                  <div className="bg-muted/50 rounded-lg p-3 space-y-2">
                    <div className="flex items-center space-x-2">
                      <div className="text-muted-foreground">
                        {getDeviceIcon(sessionToLogout.userAgent)}
                      </div>
                      <div>
                        <p className="font-medium text-sm">
                          {sessionToLogout.userAgent.split(' ')[0]} Browser
                        </p>
                        <p className="text-xs text-muted-foreground">
                          IP: {sessionToLogout.ipAddress}
                        </p>
                      </div>
                    </div>
                    <div className="text-xs text-muted-foreground">
                      <p>Authentication: {sessionToLogout.authMethod}</p>
                      <p>Last seen: {formatLastSeen(sessionToLogout.lastSeenAt)}</p>
                      <p>Created: {new Date(sessionToLogout.createdAt).toLocaleDateString()}</p>
                    </div>
                  </div>
                )}
                <div className="bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg p-3">
                  <div className="flex items-start space-x-2">
                    <AlertTriangle className="h-4 w-4 text-red-600 mt-0.5 flex-shrink-0" />
                    <div className="text-sm">
                      <p className="font-medium text-red-800 dark:text-red-200">Security Note</p>
                      <p className="text-red-700 dark:text-red-300 mt-1">
                        This action will immediately logout this device. If this is a device you don&apos;t recognize, logging it out is recommended for security.
                      </p>
                    </div>
                  </div>
                </div>
              </DialogDescription>
            </DialogHeader>
            <DialogFooter className="flex-col sm:flex-row gap-2">
              <Button
                variant="outline"
                onClick={() => handleLogoutSessionConfirm(false)}
                className="w-full sm:w-auto"
              >
                Cancel
              </Button>
              <Button
                variant="destructive"
                onClick={() => handleLogoutSessionConfirm(true)}
                disabled={isLoggingOutSession}
                className="w-full sm:w-auto"
              >
                {isLoggingOutSession ? (
                  <div className="flex items-center">
                    <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-current mr-2"></div>
                    Logging Out...
                  </div>
                ) : (
                  <>
                    <LogOut className="h-4 w-4 mr-2" />
                    Logout Session
                  </>
                )}
              </Button>
            </DialogFooter>
          </DialogContent>
        </Dialog>

        {/* Sessions Modal - shows full sessions list and actions */}
        <Dialog open={showSessionsModal} onOpenChange={setShowSessionsModal}>
          <DialogContent className="max-w-2xl">
            <DialogHeader>
              <DialogTitle className="flex items-center justify-between">
                <div className="flex items-center space-x-2">
                  <Globe className="h-5 w-5" />
                  <span>Active Sessions</span>
                </div>
              </DialogTitle>
              <DialogDescription>
                Manage your active sessions across different devices. From here you can logout individual sessions or all other sessions.
              </DialogDescription>
            </DialogHeader>
            <div className="py-4 space-y-3">
              {sessionsLoading ? (
                <div className="flex items-center justify-center py-8">
                  <div className="animate-spin rounded-full h-6 w-6 border-b-2 border-primary"></div>
                </div>
              ) : sessionsData?.data.sessions && sessionsData.data.sessions.length > 0 ? (
                <div className="space-y-3 max-h-96 overflow-y-auto">
                  {sessionsData.data.sessions.map((session: SessionData) => (
                    <div key={session.id} className={`p-3 rounded-lg border ${session.isCurrentSession ? 'bg-primary/5 border-primary/20' : 'bg-muted/30'}`}>
                      <div className="flex items-center justify-between">
                        <div className="flex items-center space-x-3">
                          <div className="text-muted-foreground">{getDeviceIcon(session.userAgent)}</div>
                          <div>
                            <p className="font-medium text-sm">{session.userAgent.split(' ')[0]} Browser</p>
                            <p className="text-xs text-muted-foreground">IP: {session.ipAddress}</p>
                          </div>
                        </div>
                        <div className="flex items-center space-x-2">
                          <div className="text-xs text-muted-foreground">{formatLastSeen(session.lastSeenAt)}</div>
                          {!session.isCurrentSession ? (
                            <Button size="sm" variant="outline" onClick={() => handleLogoutSessionClick(session)} disabled={isLoggingOutSession}>
                              Logout
                            </Button>
                          ) : (
                            <Badge variant="default">Current</Badge>
                          )}
                        </div>
                      </div>
                    </div>
                  ))}
                </div>
              ) : (
                <div className="text-center py-8 text-muted-foreground">No active sessions found</div>
              )}
            </div>
            <DialogFooter className="flex-col sm:flex-row sm:justify-between gap-2">
              <div className="flex items-center space-x-2 w-full sm:w-auto">
                <Button variant="outline" size="sm" onClick={() => refetchSessions()} disabled={sessionsLoading}>
                  <RefreshCw className={`h-4 w-4 ${sessionsLoading ? 'animate-spin' : ''}`} />
                </Button>
                {sessionsData?.data.sessions && sessionsData.data.sessions.length > 1 && (
                  <Button variant="destructive" size="sm" onClick={handleLogoutAllOtherSessionsClick} disabled={isLoggingOutAll}>
                    Logout All Others
                  </Button>
                )}
              </div>
              <div className="w-full sm:w-auto">
                <Button variant="outline" onClick={() => setShowSessionsModal(false)}>Close</Button>
              </div>
            </DialogFooter>
          </DialogContent>
        </Dialog>

        {/* Logout All Other Sessions Confirmation Dialog */}
        <Dialog open={showLogoutAllDialog} onOpenChange={setShowLogoutAllDialog}>
          <DialogContent className="sm:max-w-md">
            <DialogHeader>
              <DialogTitle className="flex items-center">
                <LogOut className="h-5 w-5 mr-2 text-red-600" />
                Logout All Other Sessions
              </DialogTitle>
              <DialogDescription className="space-y-3">
                <p>Are you sure you want to logout from all other devices?</p>
                
                {sessionsData?.data.sessions && (
                  <div className="bg-muted/50 rounded-lg p-3">
                    <p className="text-sm font-medium mb-2">
                      This will logout {sessionsData.data.sessions.filter(s => !s.isCurrentSession).length} other session(s):
                    </p>
                    <div className="space-y-2 max-h-32 overflow-y-auto">
                      {sessionsData.data.sessions
                        .filter(session => !session.isCurrentSession)
                        .map((session) => (
                          <div key={session.id} className="flex items-center space-x-2 text-xs">
                            <div className="text-muted-foreground">
                              {getDeviceIcon(session.userAgent)}
                            </div>
                            <span className="flex-1">
                              {session.userAgent.split(' ')[0]} • {session.ipAddress}
                            </span>
                            <span className="text-muted-foreground">
                              {formatLastSeen(session.lastSeenAt)}
                            </span>
                          </div>
                        ))}
                    </div>
                  </div>
                )}

                <div className="bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg p-3">
                  <div className="flex items-start space-x-2">
                    <AlertTriangle className="h-4 w-4 text-red-600 mt-0.5 flex-shrink-0" />
                    <div className="text-sm">
                      <p className="font-medium text-red-800 dark:text-red-200">Security Action</p>
                      <p className="text-red-700 dark:text-red-300 mt-1">
                        All other devices will be immediately logged out and will need to sign in again. Your current session will remain active.
                      </p>
                    </div>
                  </div>
                </div>
              </DialogDescription>
            </DialogHeader>
            <DialogFooter className="flex-col sm:flex-row gap-2">
              <Button
                variant="outline"
                onClick={() => handleLogoutAllOtherSessionsConfirm(false)}
                className="w-full sm:w-auto"
              >
                Cancel
              </Button>
              <Button
                variant="destructive"
                onClick={() => handleLogoutAllOtherSessionsConfirm(true)}
                disabled={isLoggingOutAll}
                className="w-full sm:w-auto"
              >
                {isLoggingOutAll ? (
                  <div className="flex items-center">
                    <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-current mr-2"></div>
                    Logging Out...
                  </div>
                ) : (
                  <>
                    <LogOut className="h-4 w-4 mr-2" />
                    Logout All Others
                  </>
                )}
              </Button>
            </DialogFooter>
          </DialogContent>
        </Dialog>
      </div>
    </div>
  );
}