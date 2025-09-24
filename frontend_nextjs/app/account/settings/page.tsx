"use client";

import { motion } from "framer-motion";
import React from 'react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Badge } from "@/components/ui/badge";
import { Switch } from "@/components/ui/switch";
import Link from "next/link";
import { 
  ArrowLeft, 
  Settings as SettingsIcon, 
  Bell, 
  Shield, 
  Globe, 
  Smartphone,
  Eye,
  Lock,
  Trash2,
  Download
} from "lucide-react";
import { useUser } from "@/contexts/UserContext";
import { useTheme } from "@/contexts/ThemeContext";
import { toast } from "sonner";
import { useGet, usePut, useDelete, useApi } from '@/hooks/useApi';
import { ChangePasswordRequest, DeleteAccountRequest } from '@/hooks/types';

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
  const { user, isAuthenticated, logout } = useUser();
  const { theme, toggleTheme } = useTheme();

  if (!isAuthenticated || !user) {
    return (
      <div className="min-h-screen bg-background flex items-center justify-center px-4">
        <div className="text-center">
          <h1 className="text-2xl font-bold mb-4">Access Denied</h1>
          <p className="text-muted-foreground mb-6">You need to be logged in to view this page.</p>
          <Link href="/account/login">
            <Button>Sign In</Button>
          </Link>
        </div>
      </div>
    );
  }

  const handleSaveSettings = () => {
    toast.success("Settings saved successfully!");
  };

  const handleExportData = () => {
    toast.info("Data export will be available soon!");
  };

  // Hooks for profile actions
  const api = useApi();
  const { mutate: changePasswordMutate } = usePut<any>('/profile/password');
  const { data: sessionsData, refetch: refetchSessions } = useGet<any>('/profile/sessions', { enabled: false });
  const { mutate: deleteAccountMutate } = useDelete<any>('/profile');

  const handleViewSessions = async () => {
    try {
  const res = await api.get<any>('/profile/sessions');
      const sessions = res?.data?.data?.sessions || res?.data?.sessions || [];
      if (sessions.length === 0) {
        alert('No active sessions found');
        return;
      }
      const list = sessions.map((s: any) => `${s.userAgent || s.authMethod} - ${s.ipAddress} - Last seen: ${s.lastSeenAt}`).join('\n');
      alert(list);
    } catch (err) {
      console.error('Failed to load sessions', err);
      toast.error('Failed to load sessions');
    }
  };

  const handleChangePassword = async () => {
    const current = window.prompt('Enter your current password');
    if (!current) return;
    const nw = window.prompt('Enter your new password (min length enforced server-side)');
    if (!nw) return;
    try {
      const payload: ChangePasswordRequest = { currentPassword: current, newPassword: nw };
      const res = await changePasswordMutate(payload);
      if (res.status === 200) {
        toast.success('Password changed. You may need to log in again on other devices.');
      }
    } catch (err) {
      console.error('Change password failed', err);
      toast.error('Failed to change password');
    }
  };

  const handleDeleteAccount = async () => {
    const confirmation = window.prompt('Type "DELETE MY ACCOUNT" to confirm');
    if (confirmation !== 'DELETE MY ACCOUNT') {
      toast.error('Incorrect confirmation text');
      return;
    }
    const password = window.prompt('Enter your password to confirm');
    if (!password) return;
    try {
      const payload: DeleteAccountRequest = { confirmationText: confirmation, password };
      const res = await deleteAccountMutate(payload);
      if (res.status === 200) {
        toast.success('Account deleted');
        // Clear user state and tokens
        window.localStorage.removeItem('userToken');
        window.localStorage.removeItem('frencircle_user');
        // Redirect to home
        window.location.href = '/';
      }
    } catch (err) {
      console.error('Delete account failed', err);
      toast.error('Failed to delete account');
    }
  };

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
            <h1 className="text-3xl font-bold">Settings</h1>
            <p className="text-muted-foreground">
              Manage your account preferences and privacy settings
            </p>
          </motion.div>

          {/* Keep only Change Password and Delete Account sections */}
          <motion.div variants={itemVariants}>
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center">
                  <Shield className="h-5 w-5 mr-2" />
                  Security
                </CardTitle>
                <CardDescription>
                  Manage your password and account deletion
                </CardDescription>
              </CardHeader>
              <CardContent className="space-y-6">
                <div className="flex items-center justify-between">
                  <div>
                    <label className="text-sm font-medium">Change Password</label>
                    <p className="text-xs text-muted-foreground">Update your account password</p>
                  </div>
                  <div className="w-full md:w-auto mt-2 md:mt-0">
                    <ChangePasswordCard onChangePassword={async (current, nw) => {
                      try {
                        const payload: ChangePasswordRequest = { currentPassword: current, newPassword: nw };
                        const res = await changePasswordMutate(payload);
                        if (res.status === 200) {
                          toast.success('Password changed. You may need to log in again on other devices.');
                        }
                      } catch (err) {
                        console.error('Change password failed', err);
                        toast.error('Failed to change password');
                      }
                    }} />
                  </div>
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

          {/* Data & Account */}
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
  );
}

// ChangePasswordCard component
function ChangePasswordCard({ onChangePassword }: { onChangePassword: (current: string, nw: string) => Promise<void> }) {
  const [current, setCurrent] = React.useState('');
  const [nw, setNew] = React.useState('');
  const [confirm, setConfirm] = React.useState('');
  const [loading, setLoading] = React.useState(false);

  const submit = async () => {
    if (!current || !nw || !confirm) {
      toast.error('Please fill all fields');
      return;
    }
    if (nw !== confirm) {
      toast.error('New passwords do not match');
      return;
    }
    if (nw.length < 8) {
      toast.error('Password must be at least 8 characters');
      return;
    }
    try {
      setLoading(true);
      await onChangePassword(current, nw);
      setCurrent(''); setNew(''); setConfirm('');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="flex items-center space-x-2">
      <Input type="password" placeholder="Current" value={current} onChange={(e: any) => setCurrent(e.target.value)} />
      <Input type="password" placeholder="New password" value={nw} onChange={(e: any) => setNew(e.target.value)} />
      <Input type="password" placeholder="Confirm" value={confirm} onChange={(e: any) => setConfirm(e.target.value)} />
      <Button size="sm" onClick={submit} disabled={loading}>{loading ? 'Saving...' : 'Change'}</Button>
    </div>
  );
}