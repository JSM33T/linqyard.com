"use client";

import { motion } from "framer-motion";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Switch } from "@/components/ui/switch";
import Link from "next/link";
import ProtectedRoute from "@/components/ProtectedRoute";
import { 
  ArrowLeft, 
  Settings as SettingsIcon, 
  Bell, 
  Shield, 
  Globe, 
  Smartphone,
  Eye,
  Lock,
} from "lucide-react";
// import { useUser } from "@/contexts/UserContext";
import { useTheme } from "@/contexts/ThemeContext";
import { toast } from "sonner";

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

export default function SettingsPage() {
 // const { user, logout } = useUser();
  const { theme, toggleTheme } = useTheme();

  const handleSaveSettings = () => {
    toast.success("Settings saved successfully!");
  };

  // No Data & Account handlers here — moved to Security page

  return (
    <ProtectedRoute>
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
            <h1 className="text-3xl font-bold">Settings</h1>
            <p className="text-muted-foreground">
              Manage your account preferences and privacy settings
            </p>
          </motion.div>

          {/* General Settings */}
          <motion.div variants={itemVariants}>
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center">
                  <SettingsIcon className="h-5 w-5 mr-2" />
                  General Settings
                </CardTitle>
                <CardDescription>
                  Basic preferences for your account
                </CardDescription>
              </CardHeader>
              <CardContent className="space-y-6">
                {/* Theme Toggle */}
                <div className="flex items-center justify-between">
                  <div>
                    <label className="text-sm font-medium">Theme</label>
                    <p className="text-xs text-muted-foreground">Choose between light and dark mode</p>
                  </div>
                  <div className="flex items-center space-x-2">
                    <span className="text-sm">{theme === 'light' ? 'Light' : 'Dark'}</span>
                    <Switch 
                      checked={theme === 'dark'} 
                      onCheckedChange={toggleTheme}
                    />
                  </div>
                </div>

                {/* Language */}
                <div className="flex items-center justify-between">
                  <div>
                    <label className="text-sm font-medium">Language</label>
                    <p className="text-xs text-muted-foreground">Select your preferred language</p>
                  </div>
                  <Badge variant="outline">English</Badge>
                </div>

                {/* Time Zone */}
                <div className="flex items-center justify-between">
                  <div>
                    <label className="text-sm font-medium">Time Zone</label>
                    <p className="text-xs text-muted-foreground">Your current time zone</p>
                  </div>
                  <Badge variant="outline">UTC</Badge>
                </div>
              </CardContent>
            </Card>
          </motion.div>

          {/* Notifications */}
          <motion.div variants={itemVariants}>
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center">
                  <Bell className="h-5 w-5 mr-2" />
                  Notifications
                </CardTitle>
                <CardDescription>
                  Configure how you receive notifications
                  <span className="ml-2 text-xs text-muted-foreground">(Coming soon)</span>
                </CardDescription>
              </CardHeader>
              <CardContent className="space-y-6 opacity-50 pointer-events-none" role="group" aria-disabled="true">
                <div className="flex items-center justify-between">
                  <div>
                    <label className="text-sm font-medium">Email Notifications</label>
                    <p className="text-xs text-muted-foreground">Receive updates via email</p>
                  </div>
                  <Switch defaultChecked />
                </div>

                <div className="flex items-center justify-between">
                  <div>
                    <label className="text-sm font-medium">Push Notifications</label>
                    <p className="text-xs text-muted-foreground">Get notified on your device</p>
                  </div>
                  <Switch defaultChecked />
                </div>

                <div className="flex items-center justify-between">
                  <div>
                    <label className="text-sm font-medium">Marketing Emails</label>
                    <p className="text-xs text-muted-foreground">Receive promotional content</p>
                  </div>
                  <Switch />
                </div>
              </CardContent>
            </Card>
          </motion.div>

          {/* Privacy & Security */}
          <motion.div variants={itemVariants}>
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center">
                  <Shield className="h-5 w-5 mr-2" />
                  Privacy & Security
                </CardTitle>
                <CardDescription>
                  Control your privacy and security settings
                  <span className="ml-2 text-xs text-muted-foreground">(Coming soon)</span>
                </CardDescription>
              </CardHeader>
              <CardContent className="space-y-6" role="group" aria-disabled="false">
                <div className="flex items-center justify-between opacity-50 pointer-events-none">
                  <div>
                    <label className="text-sm font-medium">Profile Visibility</label>
                    <p className="text-xs text-muted-foreground">Who can see your profile</p>
                  </div>
                  <div className="flex items-center space-x-2">
                    <Globe className="h-4 w-4" />
                    <span className="text-sm">Public</span>
                  </div>
                </div>

                <div className="flex items-center justify-between opacity-50 pointer-events-none">
                  <div>
                    <label className="text-sm font-medium">Two-Factor Authentication</label>
                    <p className="text-xs text-muted-foreground">Add an extra layer of security</p>
                  </div>
                  <Button variant="outline" size="sm">
                    <Smartphone className="h-4 w-4 mr-2" />
                    Setup
                  </Button>
                </div>

                <div className="flex items-center justify-between">
                  <div>
                    <label className="text-sm font-medium">Login Activity</label>
                    <p className="text-xs text-muted-foreground">View recent login sessions</p>
                  </div>
                  <Link href="/account/security">
                    <Button variant="ghost" size="sm">
                      <Eye className="h-4 w-4 mr-2" />
                      View
                    </Button>
                  </Link>
                </div>

                <div className="flex items-center justify-between opacity-50 pointer-events-none">
                  <div>
                    <label className="text-sm font-medium">Change Password</label>
                    <p className="text-xs text-muted-foreground">Update your account password</p>
                  </div>
                  <Button variant="outline" size="sm">
                    <Lock className="h-4 w-4 mr-2" />
                    Change
                  </Button>
                </div>
              </CardContent>
            </Card>
          </motion.div>

          {/* Data & Account moved to Security page (no duplication here) */}

          {/* Save Button */}
          <motion.div variants={itemVariants}>
            <div className="flex justify-end space-x-4">
              <Button variant="outline" onClick={() => window.history.back()}>
                Cancel
              </Button>
              <Button onClick={handleSaveSettings}>
                Save Changes
              </Button>
            </div>
          </motion.div>
        </motion.div>
        </div>
      </div>
    </ProtectedRoute>
  );
}