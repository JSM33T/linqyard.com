"use client";

import { motion } from "framer-motion";
import React, { useEffect } from 'react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Badge } from "@/components/ui/badge";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
import Link from "next/link";
import { ArrowLeft, Camera, User, Mail, Calendar, Shield } from "lucide-react";
import { useUser, userHelpers } from "@/contexts/UserContext";
import { useGet, usePut } from '@/hooks/useApi';
import { ProfileDetailsResponse, UpdateProfileRequest } from '@/hooks/types';

// Inline profile editor component
function InlineProfileEditor({ user, onSave, onCancel } : { user: any, onSave: (u: UpdateProfileRequest) => Promise<void>, onCancel: () => void }) {
  const [editing, setEditing] = React.useState(false);
  const [form, setForm] = React.useState<UpdateProfileRequest>({
    firstName: user.firstName || '',
    lastName: user.lastName || '',
    displayName: user.displayName || '',
    bio: '',
    avatarUrl: user.avatarUrl || undefined,
    locale: user.locale || undefined,
    timezone: user.timezone || undefined,
  });

  React.useEffect(() => {
    setForm((f) => ({
      ...f,
      firstName: user.firstName || '',
      lastName: user.lastName || '',
      displayName: user.displayName || '',
      avatarUrl: user.avatarUrl || undefined,
    }));
  }, [user]);

  const handleChange = (key: keyof UpdateProfileRequest, value: any) => {
    setForm((f) => ({ ...f, [key]: value }));
  };

  const handleSave = async () => {
    await onSave(form);
    setEditing(false);
  };

  if (!editing) {
    return (
      <div className="flex flex-wrap gap-3 pt-4">
        <Button variant="default" onClick={() => setEditing(true)}>Edit Profile</Button>
        <Link href="/account/settings">
          <Button variant="ghost">Account Settings</Button>
        </Link>
      </div>
    );
  }

  return (
    <div className="space-y-4 pt-4">
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <div className="space-y-2">
          <label className="text-sm font-medium">First Name</label>
          <Input value={form.firstName || ''} onChange={(e: any) => handleChange('firstName', e.target.value)} />
        </div>
        <div className="space-y-2">
          <label className="text-sm font-medium">Last Name</label>
          <Input value={form.lastName || ''} onChange={(e: any) => handleChange('lastName', e.target.value)} />
        </div>
        <div className="space-y-2">
          <label className="text-sm font-medium">Display Name</label>
          <Input value={form.displayName || ''} onChange={(e: any) => handleChange('displayName', e.target.value)} />
        </div>
        <div className="space-y-2">
          <label className="text-sm font-medium">Avatar URL</label>
          <Input value={form.avatarUrl || ''} onChange={(e: any) => handleChange('avatarUrl', e.target.value)} />
        </div>
      </div>

      <div className="flex gap-3">
        <Button variant="default" onClick={handleSave}>Save</Button>
        <Button variant="outline" onClick={() => { setEditing(false); onCancel(); }}>Cancel</Button>
      </div>
    </div>
  );
}

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

export default function ProfilePage() {
  const { user, isAuthenticated, updateUser, setUser } = useUser();

  // Fetch profile details from backend
  const { data: profileData, loading: profileLoading, error: profileError, refetch } = useGet<any>('/profile');

  // PUT hook for updating profile
  const { mutate: updateProfile, loading: updating } = usePut<any>('/profile');

  // Sync fetched profile into user context when available
  useEffect(() => {
    if (profileData && (profileData as any).data) {
      const p = (profileData as any).data as ProfileDetailsResponse;
      updateUser({
        firstName: p.firstName || '',
        lastName: p.lastName || '',
        username: p.username,
        email: p.email,
        avatarUrl: p.avatarUrl || undefined,
      });
    }
  }, [profileData, updateUser]);

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
              href="/"
              className="inline-flex items-center text-sm text-muted-foreground hover:text-foreground transition-colors"
            >
              <ArrowLeft className="h-4 w-4 mr-2" />
              Back to Home
            </Link>
          </motion.div>

          {/* Header */}
          <motion.div variants={itemVariants} className="space-y-2">
            <h1 className="text-3xl font-bold">Profile</h1>
            <p className="text-muted-foreground">
              Manage your account settings and personal information
            </p>
          </motion.div>

          {/* Profile Information Card */}
          <motion.div variants={itemVariants}>
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center">
                  <User className="h-5 w-5 mr-2" />
                  Personal Information
                </CardTitle>
                <CardDescription>
                  Your basic account information
                </CardDescription>
              </CardHeader>
              <CardContent className="space-y-6">
                {/* Avatar Section */}
                <div className="flex items-center space-x-4">
                  <div className="relative">
                    <Avatar className="h-20 w-20">
                      <AvatarImage src={user.avatarUrl || "/placeholder-avatar.jpg"} alt="Profile" />
                      <AvatarFallback className="text-lg">
                        {userHelpers.getInitials(user)}
                      </AvatarFallback>
                    </Avatar>
                    <Button
                      size="sm"
                      variant="secondary"
                      className="absolute -bottom-2 -right-2 rounded-full h-8 w-8 p-0"
                    >
                      <Camera className="h-4 w-4" />
                    </Button>
                  </div>
                  <div>
                    <h3 className="text-lg font-semibold">{userHelpers.getFullName(user)}</h3>
                    <p className="text-sm text-muted-foreground">@{user.username}</p>
                    <Badge variant="secondary" className="mt-1">
                      {user.role || 'User'}
                    </Badge>
                  </div>
                </div>

                {/* Profile Details */}
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  <div className="space-y-2">
                    <label className="text-sm font-medium">First Name</label>
                    <Input value={user.firstName} readOnly />
                  </div>
                  <div className="space-y-2">
                    <label className="text-sm font-medium">Last Name</label>
                    <Input value={user.lastName} readOnly />
                  </div>
                  <div className="space-y-2">
                    <label className="text-sm font-medium">Username</label>
                    <Input value={user.username} readOnly />
                  </div>
                  <div className="space-y-2">
                    <label className="text-sm font-medium">Email</label>
                    <div className="flex items-center space-x-2">
                      <Input value={user.email} readOnly className="flex-1" />
                      <Badge variant="outline" className="text-xs">
                        <Mail className="h-3 w-3 mr-1" />
                        Verified
                      </Badge>
                    </div>
                  </div>
                </div>

                {/* Account Info */}
                <div className="pt-4 border-t">
                  <div className="grid grid-cols-1 md:grid-cols-2 gap-4 text-sm">
                    <div className="flex items-center text-muted-foreground">
                      <Calendar className="h-4 w-4 mr-2" />
                      Member since {new Date().getFullYear()}
                    </div>
                    <div className="flex items-center text-muted-foreground">
                      <Shield className="h-4 w-4 mr-2" />
                      Account verified
                    </div>
                  </div>
                </div>

                {/* Action Buttons / Inline Edit Form */}
                <InlineProfileEditor
                  user={user}
                  onCancel={() => { /* no-op handled inside */ }}
                  onSave={async (updates: UpdateProfileRequest) => {
                    try {
                      const res = await updateProfile(updates);
                      // Debug log so devs can inspect the exact API envelope
                      console.debug('updateProfile response', res);

                      if (res.status === 200 && res.data) {
                        // Support multiple backend envelope shapes:
                        // 1) { data: { /* profile */ } }
                        // 2) { data: { profile: { /* profile */ } } }
                        // 3) { profile: { /* profile */ } }
                        const body = res.data as any;
                        let updated: ProfileDetailsResponse | undefined;

                        if (body.data) {
                          if (body.data.profile) {
                            updated = body.data.profile as ProfileDetailsResponse;
                          } else {
                            updated = body.data as ProfileDetailsResponse;
                          }
                        } else if (body.profile) {
                          updated = body.profile as ProfileDetailsResponse;
                        } else {
                          // Fallback: maybe the API returned the profile directly
                          updated = body as ProfileDetailsResponse;
                        }

                        if (updated) {
                          // Update context (and localStorage) so UI re-renders
                          if (user) {
                            updateUser({
                              firstName: updated.firstName || '',
                              lastName: updated.lastName || '',
                              username: updated.username,
                              email: updated.email,
                              avatarUrl: updated.avatarUrl || undefined,
                            });
                          } else {
                            // If for some reason user is null, set a minimal user object
                            setUser({
                              id: updated.id,
                              firstName: updated.firstName || '',
                              lastName: updated.lastName || '',
                              username: updated.username,
                              email: updated.email,
                              avatarUrl: updated.avatarUrl || undefined,
                              login: true,
                            } as any);
                          }

                          // Wait for the refetch so any other consumers get updated data
                          try {
                            await refetch();
                          } catch (e) {
                            // Don't block on refetch failures, but log them
                            console.warn('Refetch after profile update failed', e);
                          }
                        }
                      }
                    } catch (err) {
                      console.error('Failed to update profile', err);
                      alert('Failed to update profile');
                    }
                  }}
                />
              </CardContent>
            </Card>
          </motion.div>

          {/* Quick Stats Card */}
          <motion.div variants={itemVariants}>
            <Card>
              <CardHeader>
                <CardTitle>Account Activity</CardTitle>
                <CardDescription>
                  Your account statistics and activity overview
                </CardDescription>
              </CardHeader>
              <CardContent>
                <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                  <div className="text-center p-4 rounded-lg border">
                    <div className="text-2xl font-bold text-primary">0</div>
                    <div className="text-sm text-muted-foreground">Posts</div>
                  </div>
                  <div className="text-center p-4 rounded-lg border">
                    <div className="text-2xl font-bold text-primary">0</div>
                    <div className="text-sm text-muted-foreground">Friends</div>
                  </div>
                  <div className="text-center p-4 rounded-lg border">
                    <div className="text-2xl font-bold text-primary">1</div>
                    <div className="text-sm text-muted-foreground">Days Active</div>
                  </div>
                </div>
              </CardContent>
            </Card>
          </motion.div>
        </motion.div>
      </div>
    </div>
  );
}