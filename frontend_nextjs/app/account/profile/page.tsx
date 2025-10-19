"use client";

import { useState, useEffect, useRef } from "react";
import { motion } from "framer-motion";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Badge } from "@/components/ui/badge";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
import Link from "next/link";
import Image from "next/image";
import AccessDenied from "@/components/AccessDenied";
import { ArrowLeft, Camera, User, Mail, Calendar, Shield, Edit, Save, X, Clock, Globe } from "lucide-react";
import { toast } from "sonner";
import { useUser } from "@/contexts/UserContext";
import { useGet, usePost } from "@/hooks/useApi";
import { GetProfileResponse, UpdateProfileRequest, UpdateProfileResponse } from "@/hooks/types";
import AvatarCropDialog from "@/components/profile/AvatarCropDialog";

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
  const { user, isAuthenticated, setUser } = useUser();
  const [isEditing, setIsEditing] = useState(false);
  const [formData, setFormData] = useState({
    username: "",
    firstName: "",
    lastName: "",
    displayName: "",
    bio: "",
    timezone: "",
    locale: ""
  });
  const [avatarPreview, setAvatarPreview] = useState<string | null>(null);
  const [coverPreview, setCoverPreview] = useState<string | null>(null);
  const [isAvatarCropOpen, setIsAvatarCropOpen] = useState(false);
  const [avatarCropSrc, setAvatarCropSrc] = useState<string | null>(null);
  const [pendingAvatarName, setPendingAvatarName] = useState<string | null>(null);
  const [isCoverCropOpen, setIsCoverCropOpen] = useState(false);
  const [coverCropSrc, setCoverCropSrc] = useState<string | null>(null);
  const [pendingCoverName, setPendingCoverName] = useState<string | null>(null);
  const avatarInputRef = useRef<HTMLInputElement>(null);
  const coverInputRef = useRef<HTMLInputElement>(null);

  // API hooks
  const { data: profileData, loading: profileLoading, error: profileError, refetch: refetchProfile } = useGet<GetProfileResponse>("/profile");
  const { mutate: updateProfile, loading: isUpdating } = usePost<UpdateProfileResponse>("/profile");
  const { mutate: uploadAvatar, loading: isUploadingAvatar } = usePost<UpdateProfileResponse>("/media/avatar");
  const { mutate: uploadCover, loading: isUploadingCover } = usePost<UpdateProfileResponse>("/media/cover");

  // Initialize form data when profile data loads
  useEffect(() => {
    if (profileData?.data) {
      const profile = profileData.data;
      setFormData({
        username: profile.username || "",
        firstName: profile.firstName || "",
        lastName: profile.lastName || "",
        displayName: profile.displayName || "",
        bio: profile.bio || "",
        timezone: profile.timezone || "",
        locale: profile.locale || ""
      });
      setAvatarPreview(null);
      setCoverPreview(null);
    }
  }, [profileData]);

  useEffect(() => {
    return () => {
      if (avatarPreview) {
        URL.revokeObjectURL(avatarPreview);
      }
    };
  }, [avatarPreview]);

  useEffect(() => {
    return () => {
      if (avatarCropSrc) {
        URL.revokeObjectURL(avatarCropSrc);
      }
    };
  }, [avatarCropSrc]);

  useEffect(() => {
    return () => {
      if (coverPreview) {
        URL.revokeObjectURL(coverPreview);
      }
    };
  }, [coverPreview]);

  useEffect(() => {
    return () => {
      if (coverCropSrc) {
        URL.revokeObjectURL(coverCropSrc);
      }
    };
  }, [coverCropSrc]);

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => {
    setFormData(prev => ({
      ...prev,
      [e.target.name]: e.target.value
    }));
  };

  const handleAvatarButtonClick = () => {
    avatarInputRef.current?.click();
  };

  const handleAvatarSelected = (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (!file) {
      return;
    }

    if (!file.type.startsWith("image/")) {
      toast.error("Please choose an image file.");
      event.target.value = "";
      return;
    }

    const maxSizeMb = 5;
    if (file.size > maxSizeMb * 1024 * 1024) {
      toast.error(`Please upload an image smaller than ${maxSizeMb}MB.`);
      event.target.value = "";
      return;
    }

    const objectUrl = URL.createObjectURL(file);
    setAvatarCropSrc(objectUrl);
    setPendingAvatarName(file.name);
    setIsAvatarCropOpen(true);
    event.target.value = "";
  };

  const handleAvatarCropDialogChange = (open: boolean) => {
    setIsAvatarCropOpen(open);
    if (!open) {
      setAvatarCropSrc(null);
      setPendingAvatarName(null);
    }
  };

  const handleAvatarCropped = async (blob: Blob) => {
    const baseName = pendingAvatarName ? pendingAvatarName.replace(/\.[^/.]+$/, "") : "avatar";
    const fileName = `${baseName}.jpg`;
    const file = new File([blob], fileName, { type: blob.type || "image/jpeg" });
    const previewUrl = URL.createObjectURL(file);
    setAvatarPreview(previewUrl);

    const formDataPayload = new FormData();
    formDataPayload.append("file", file);

    try {
      const response = await uploadAvatar(formDataPayload);

      if (response.status === 200 && response.data) {
        toast.success("Avatar updated successfully!");

        const updatedProfile = response.data.data.profile;
        if (user) {
          setUser({
            ...user,
            firstName: updatedProfile.firstName,
            lastName: updatedProfile.lastName,
            avatarUrl: updatedProfile.avatarUrl ?? undefined,
            coverUrl: updatedProfile.coverUrl ?? undefined,
          });
        }

        await refetchProfile();
        setAvatarPreview(null);
      }
    } catch (error: any) {
      console.error("Avatar upload failed:", error);

      if (error?.status && error?.data?.title) {
        toast.error(error.data.title);
      } else if (error?.message) {
        toast.error(error.message);
      } else {
        toast.error("Failed to upload avatar. Please try again.");
      }

      setAvatarPreview(null);
    }
  };

  const handleCoverButtonClick = () => {
    coverInputRef.current?.click();
  };

  const handleCoverSelected = (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (!file) {
      return;
    }

    if (!file.type.startsWith("image/")) {
      toast.error("Please choose an image file.");
      event.target.value = "";
      return;
    }

    const maxSizeMb = 5;
    if (file.size > maxSizeMb * 1024 * 1024) {
      toast.error(`Please upload an image smaller than ${maxSizeMb}MB.`);
      event.target.value = "";
      return;
    }

    const objectUrl = URL.createObjectURL(file);
    setCoverCropSrc(objectUrl);
    setPendingCoverName(file.name);
    setIsCoverCropOpen(true);
    event.target.value = "";
  };

  const handleCoverCropDialogChange = (open: boolean) => {
    setIsCoverCropOpen(open);
    if (!open) {
      setCoverCropSrc(null);
      setPendingCoverName(null);
    }
  };

  const handleCoverCropped = async (blob: Blob) => {
    const baseName = pendingCoverName ? pendingCoverName.replace(/\.[^/.]+$/, "") : "cover";
    const fileName = `${baseName}.jpg`;
    const file = new File([blob], fileName, { type: blob.type || "image/jpeg" });
    const previewUrl = URL.createObjectURL(file);
    setCoverPreview(previewUrl);

    const formDataPayload = new FormData();
    formDataPayload.append("file", file);

    try {
      const response = await uploadCover(formDataPayload);

      if (response.status === 200 && response.data) {
        toast.success("Cover image updated successfully!");

        const updatedProfile = response.data.data.profile;
        if (user) {
          setUser({
            ...user,
            firstName: updatedProfile.firstName,
            lastName: updatedProfile.lastName,
            avatarUrl: updatedProfile.avatarUrl ?? undefined,
            coverUrl: updatedProfile.coverUrl ?? undefined,
          });
        }

        await refetchProfile();
        setCoverPreview(null);
      }
    } catch (error: any) {
      console.error("Cover upload failed:", error);

      if (error?.status && error?.data?.title) {
        toast.error(error.data.title);
      } else if (error?.message) {
        toast.error(error.message);
      } else {
        toast.error("Failed to upload cover image. Please try again.");
      }

      setCoverPreview(null);
    }
  };

  const handleEditToggle = () => {
    if (isEditing) {
      // Reset form data when canceling edit
      if (profileData?.data) {
        const profile = profileData.data;
        setFormData({
          username: profile.username || "",
          firstName: profile.firstName || "",
          lastName: profile.lastName || "",
          displayName: profile.displayName || "",
          bio: profile.bio || "",
          timezone: profile.timezone || "",
          locale: profile.locale || ""
        });
      }
    }
    setIsEditing(!isEditing);
  };

  const handleSaveProfile = async () => {
    try {
      // Client-side username validation
      if (formData.username.trim()) {
        const username = formData.username.trim();
        if (username.length < 3) {
          toast.error("Username must be at least 3 characters long");
          return;
        }
        if (username.length > 30) {
          toast.error("Username cannot exceed 30 characters");
          return;
        }
        if (!/^[a-zA-Z0-9_.-]+$/.test(username)) {
          toast.error("Username can only contain letters, numbers, underscores, dots, and hyphens");
          return;
        }
      }

      // Only send fields that have values
      const updateData: UpdateProfileRequest = {};
      
      if (formData.username.trim()) updateData.username = formData.username.trim();
      if (formData.firstName.trim()) updateData.firstName = formData.firstName.trim();
      if (formData.lastName.trim()) updateData.lastName = formData.lastName.trim();
      if (formData.displayName.trim()) updateData.displayName = formData.displayName.trim();
      if (formData.bio.trim()) updateData.bio = formData.bio.trim();
      if (formData.timezone.trim()) updateData.timezone = formData.timezone.trim();
      if (formData.locale.trim()) updateData.locale = formData.locale.trim();

      const response = await updateProfile(updateData);

      if (response.status === 200 && response.data) {
        toast.success("Profile updated successfully!");
        
        // Update the user context with new data
        const updatedProfile = response.data.data.profile;
        if (user) {
          setUser({
            ...user,
            username: updatedProfile.username ?? user.username,
            firstName: updatedProfile.firstName,
            lastName: updatedProfile.lastName,
            avatarUrl: updatedProfile.avatarUrl ?? user.avatarUrl,
            coverUrl: updatedProfile.coverUrl ?? user.coverUrl,
            // Note: email cannot be changed via profile update
          });
        }

        // Refetch profile data to get the latest state
        await refetchProfile();
        setIsEditing(false);
      }
    } catch (error: any) {
      console.error("Profile update failed:", error);
      
      if (error?.status && error?.data?.title) {
        toast.error(error.data.title);
      } else if (error?.message) {
        toast.error(error.message);
      } else {
        toast.error("Failed to update profile. Please try again.");
      }
    }
  };

  if (!isAuthenticated || !user) {
    return <AccessDenied />;
  }

  // Use profile data from API if available, otherwise fall back to user context
  const displayProfile = profileData?.data || {
    id: user.id,
    email: user.email,
    emailVerified: true,
    username: user.username,
    firstName: user.firstName,
    lastName: user.lastName,
    displayName: user.firstName + " " + user.lastName,
    bio: "",
    avatarUrl: user.avatarUrl,
    coverUrl: user.coverUrl,
    timezone: "",
    locale: "",
    verifiedBadge: false,
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
    roles: user.role ? [user.role] : ["user"]
  };

  const coverImage = coverPreview || displayProfile.coverUrl;

  // Only show loading when we don't have any data yet
  if (profileLoading && !profileData && !profileError) {
    return (
      <div className="min-h-screen bg-background flex items-center justify-center px-4">
        <div className="text-center">
          <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary mx-auto mb-4"></div>
          <p className="text-muted-foreground">Loading profile...</p>
        </div>
      </div>
    );
  }

  // If there's an API error, show a warning but continue with user context data
  if (profileError) {
    console.warn("Profile API error:", profileError);
    toast.error("Could not load latest profile data. Using cached information.", {
      description: "Some features may be limited. Please refresh the page to try again."
    });
  }

  return (
    <div className="min-h-screen bg-background">
      <AvatarCropDialog
        isOpen={isAvatarCropOpen}
        src={avatarCropSrc}
        onOpenChange={handleAvatarCropDialogChange}
        onCropped={handleAvatarCropped}
        aspect={1}
        cropShape="round"
        dialogTitle="Adjust your avatar"
        initialZoom={1.2}
      />
      <AvatarCropDialog
        isOpen={isCoverCropOpen}
        src={coverCropSrc}
        onOpenChange={handleCoverCropDialogChange}
        onCropped={handleCoverCropped}
        aspect={820 / 312}
        cropShape="rect"
        dialogTitle="Adjust your cover image"
        initialZoom={1}
        minZoom={1}
        maxZoom={5}
        outputWidth={820}
        outputHeight={312}
      />
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
            {profileError && (
              <div className="bg-yellow-50 dark:bg-yellow-900/20 border border-yellow-200 dark:border-yellow-800 rounded-lg p-4">
                <div className="flex items-center">
                  <div className="text-yellow-600 dark:text-yellow-400 text-sm">
                    Warning: Using cached profile data. Some information may not be up to date.
                  </div>
                </div>
              </div>
            )}
          </motion.div>

          {/* Profile Information Card */}
          <motion.div variants={itemVariants}>
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center justify-between">
                  <div className="flex items-center">
                    <User className="h-5 w-5 mr-2" />
                    Personal Information
                  </div>
                  <div className="flex items-center gap-2">
                    {isEditing ? (
                      <>
                        <Button
                          size="sm"
                          variant="outline"
                          onClick={handleEditToggle}
                          disabled={isUpdating}
                        >
                          <X className="h-4 w-4 mr-2" />
                          Cancel
                        </Button>
                        <Button
                          size="sm"
                          onClick={handleSaveProfile}
                          disabled={isUpdating}
                        >
                          {isUpdating ? (
                            <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-current mr-2"></div>
                          ) : (
                            <Save className="h-4 w-4 mr-2" />
                          )}
                          {isUpdating ? "Saving..." : "Save"}
                        </Button>
                      </>
                    ) : (
                      <Button
                        size="sm"
                        variant="outline"
                        onClick={handleEditToggle}
                      >
                        <Edit className="h-4 w-4 mr-2" />
                        Edit
                      </Button>
                    )}
                  </div>
                </CardTitle>
                <CardDescription>
                  {isEditing ? "Update your personal information" : "Your basic account information"}
                </CardDescription>
              </CardHeader>
              <CardContent className="space-y-6">
                {/* Cover Section */}
                <div className="relative w-full overflow-hidden rounded-xl bg-muted aspect-[820/312]">
                  {coverImage ? (
                    <div className="absolute inset-0">
                      <Image
                        src={coverImage}
                        alt="Cover"
                        fill
                        className="object-cover"
                        sizes="(max-width: 768px) 100vw, 820px"
                      />
                    </div>
                  ) : (
                    <div className="h-full w-full bg-gradient-to-br from-muted to-muted-foreground/30" />
                  )}
                  {isUploadingCover && (
                    <div className="absolute inset-0 flex items-center justify-center bg-black/40 backdrop-blur-sm">
                      <div className="h-8 w-8 animate-spin rounded-full border-2 border-white border-t-transparent"></div>
                    </div>
                  )}
                  <input
                    ref={coverInputRef}
                    type="file"
                    accept="image/*"
                    className="hidden"
                    onChange={handleCoverSelected}
                  />
                  <Button
                    size="sm"
                    variant="secondary"
                    className="absolute bottom-3 right-3 rounded-full h-9 px-4"
                    onClick={handleCoverButtonClick}
                    disabled={isUploadingCover}
                  >
                    {isUploadingCover ? (
                      <div className="h-4 w-4 animate-spin rounded-full border-2 border-current border-t-transparent" />
                    ) : (
                      <div className="flex items-center">
                        <Camera className="h-4 w-4 mr-2" />
                        Change Cover
                      </div>
                    )}
                  </Button>
                </div>

                {/* Avatar Section */}
                <div className="flex items-center space-x-4">
                  <div className="relative">
                    <Avatar className="h-20 w-20">
                      <AvatarImage src={avatarPreview || displayProfile.avatarUrl || "/placeholder-avatar.jpg"} alt="Profile" />
                      <AvatarFallback className="text-lg">
                        {displayProfile.firstName?.[0]}{displayProfile.lastName?.[0]}
                      </AvatarFallback>
                    </Avatar>
                    {isUploadingAvatar && (
                      <div className="absolute inset-0 flex items-center justify-center rounded-full bg-black/50">
                        <div className="h-6 w-6 animate-spin rounded-full border-2 border-white border-t-transparent"></div>
                      </div>
                    )}
                    <input
                      ref={avatarInputRef}
                      type="file"
                      accept="image/*"
                      className="hidden"
                      onChange={handleAvatarSelected}
                    />
                    <Button
                      size="sm"
                      variant="secondary"
                      className="absolute -bottom-2 -right-2 rounded-full h-8 w-8 p-0"
                      onClick={handleAvatarButtonClick}
                      disabled={isUploadingAvatar}
                    >
                      {isUploadingAvatar ? (
                        <div className="h-4 w-4 animate-spin rounded-full border-2 border-current border-t-transparent" />
                      ) : (
                        <Camera className="h-4 w-4" />
                      )}
                    </Button>
                  </div>
                  <div>
                    <h3 className="text-lg font-semibold">
                      {displayProfile.displayName || `${displayProfile.firstName} ${displayProfile.lastName}`}
                    </h3>
                    <p className="text-sm text-muted-foreground">@{displayProfile.username}</p>
                    <Badge variant="secondary" className="mt-1">
                      {displayProfile.roles[0] || 'User'}
                    </Badge>
                  </div>
                </div>

                {/* Profile Details */}
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  <div className="space-y-2">
                    <label className="text-sm font-medium">First Name</label>
                    <Input 
                      name="firstName"
                      value={isEditing ? formData.firstName : displayProfile.firstName} 
                      onChange={handleInputChange}
                      readOnly={!isEditing}
                      className={isEditing ? "" : "bg-muted/50"}
                    />
                  </div>
                  <div className="space-y-2">
                    <label className="text-sm font-medium">Last Name</label>
                    <Input 
                      name="lastName"
                      value={isEditing ? formData.lastName : displayProfile.lastName} 
                      onChange={handleInputChange}
                      readOnly={!isEditing}
                      className={isEditing ? "" : "bg-muted/50"}
                    />
                  </div>
                  <div className="space-y-2">
                    <label className="text-sm font-medium">Display Name</label>
                    <Input 
                      name="displayName"
                      value={isEditing ? formData.displayName : (displayProfile.displayName || "")} 
                      onChange={handleInputChange}
                      readOnly={!isEditing}
                      className={isEditing ? "" : "bg-muted/50"}
                      placeholder="How you'd like to be called"
                    />
                  </div>
                  <div className="space-y-2">
                    <label className="text-sm font-medium">Username</label>
                    <Input 
                      name="username"
                      value={isEditing ? formData.username : displayProfile.username} 
                      onChange={handleInputChange}
                      readOnly={!isEditing}
                      className={isEditing ? "" : "bg-muted/50"}
                      placeholder="Choose a unique username"
                    />
                    {isEditing && (
                      <p className="text-xs text-muted-foreground">3-30 characters, letters, numbers, dots, hyphens, and underscores only</p>
                    )}
                  </div>
                  <div className="space-y-2 md:col-span-2">
                    <label className="text-sm font-medium">Email</label>
                    <div className="flex items-center space-x-2">
                      <Input value={displayProfile.email} readOnly className="flex-1 bg-muted/50" />
                      <Badge variant="outline" className="text-xs">
                        <Mail className="h-3 w-3 mr-1" />
                        {displayProfile.emailVerified ? "Verified" : "Unverified"}
                      </Badge>
                    </div>
                    <p className="text-xs text-muted-foreground">Email cannot be changed here</p>
                  </div>
                </div>

                {/* Additional Profile Fields */}
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  <div className="space-y-2">
                    <label className="text-sm font-medium">Timezone</label>
                    <div className="relative">
                      <Clock className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-muted-foreground" />
                      <Input 
                        name="timezone"
                        value={isEditing ? formData.timezone : (displayProfile.timezone || "")} 
                        onChange={handleInputChange}
                        readOnly={!isEditing}
                        className={`pl-10 ${isEditing ? "" : "bg-muted/50"}`}
                        placeholder="e.g., America/New_York"
                      />
                    </div>
                  </div>
                  <div className="space-y-2">
                    <label className="text-sm font-medium">Locale</label>
                    <div className="relative">
                      <Globe className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-muted-foreground" />
                      <Input 
                        name="locale"
                        value={isEditing ? formData.locale : (displayProfile.locale || "")} 
                        onChange={handleInputChange}
                        readOnly={!isEditing}
                        className={`pl-10 ${isEditing ? "" : "bg-muted/50"}`}
                        placeholder="e.g., en-US"
                      />
                    </div>
                  </div>
                  <div className="space-y-2 md:col-span-2">
                    <label className="text-sm font-medium">Bio</label>
                    <textarea
                      name="bio"
                      value={isEditing ? formData.bio : (displayProfile.bio || "")}
                      onChange={handleInputChange}
                      readOnly={!isEditing}
                      rows={3}
                      className={`w-full px-3 py-2 text-sm bg-background border border-input rounded-md resize-none focus:outline-none focus:ring-2 focus:ring-ring focus:border-transparent ${
                        isEditing ? "" : "bg-muted/50 cursor-default"
                      }`}
                      placeholder="Tell us about yourself..."
                    />
                  </div>
                </div>

                {/* Account Info */}
                <div className="pt-4 border-t">
                  <div className="grid grid-cols-1 md:grid-cols-2 gap-4 text-sm">
                    <div className="flex items-center text-muted-foreground">
                      <Calendar className="h-4 w-4 mr-2" />
                      Member since {new Date(displayProfile.createdAt).toLocaleDateString()}
                    </div>
                    <div className="flex items-center text-muted-foreground">
                      <Shield className="h-4 w-4 mr-2" />
                      {displayProfile.verifiedBadge ? "Verified Account" : "Standard Account"}
                    </div>
                  </div>
                </div>

                {/* Action Buttons - Only show when not editing */}
                {!isEditing && (
                  <div className="flex flex-wrap gap-3 pt-4">
                    <Link href="/account/insights">
                      <Button variant="default">
                        View Insights
                      </Button>
                    </Link>
                    <Button variant="outline">
                      Change Password
                    </Button>
                    <Link href="/account/settings">
                      <Button variant="ghost">
                        Account Settings
                      </Button>
                    </Link>
                  </div>
                )}
              </CardContent>
            </Card>
          </motion.div>


        </motion.div>
      </div>
    </div>
  );
}
