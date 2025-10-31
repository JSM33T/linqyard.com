"use client";

import React, { useEffect, useMemo, useState } from "react";
import Link from "next/link";
import Image from "next/image";
import { motion, AnimatePresence } from "framer-motion";

import { Button } from "@/components/ui/button";
import { Accordion, AccordionItem, AccordionTrigger, AccordionContent } from "@/components/ui/accordion";
import { Sun, Moon } from "lucide-react";
import { toast } from "sonner";

// no user guard in public profile view
import { useNavbarVisibility } from "@/contexts/NavbarVisibilityContext";
import { useFooterVisibility } from "@/contexts/FooterVisibilityContext";
import { useUser } from "@/contexts/UserContext";
import { useTheme } from "@/contexts/ThemeContext";
import { useGet } from "@/hooks/useApi";
import { GetGroupedLinksResponse, LinkItem } from "@/hooks/types";
import { apiService } from '@/hooks/apiService';
import { getFingerprint, getSessionId, getUtmParameters, getTrafficSource, initializeFingerprint } from '@/lib/fingerprint';

/* ---------- FX presets ---------- */
const containerVariants = {
  hidden: { opacity: 0, y: 20 },
  visible: { opacity: 1, y: 0, transition: { duration: 0.6 } },
};
const cardVariants = {
  hidden: { opacity: 0, scale: 0.98 },
  visible: { opacity: 1, scale: 1, transition: { duration: 0.25 } },
};

/* ---------- Simple (non-sortable) row ---------- */
function LinkRow({ item }: { item: LinkItem }) {
  const handleClick = (e: React.MouseEvent) => {
    // Fire-and-forget analytics: do not prevent default or open window manually.
    // Ensure fingerprint is initialized and get it
    initializeFingerprint();
    const fp = getFingerprint();

    const payload: Record<string, any> = { linkId: item.id, fp: fp || null };

    try {
      apiService.post(`/link/${item.id}/click`, payload).catch((err) => console.warn('Analytics post failed', err));
    } catch (err) {
      console.warn("Initial analytics send failed", err);
    }
  };

  return (
    <a
      href={item.url}
      target="_blank"
      rel="noopener noreferrer"
      onClick={handleClick}
      className="group/link flex items-center gap-3 rounded-lg border bg-background/60 hover:bg-accent/50 transition-all p-3 block"
    >
      <div className="flex-1 min-w-0">
          <div className="flex items-center gap-2">
            <span className="font-medium text-sm truncate hover:text-primary transition-colors">{item.name}</span>
          </div>
        {item.description && (
          <p className="text-xs text-muted-foreground mt-1 truncate">{item.description}</p>
        )}
      </div>
    </a>
  );
}

/* ---------- Group section (read-only) ---------- */
function GroupSection({
  id,
  name,
  description,
  items,
}: {
  id: string | null; // null for ungrouped
  name: string;
  description?: string | null;
  items: LinkItem[];
}) {
  const containerId = id ?? "__ungrouped__";

  return (
    <AccordionItem value={containerId} className="rounded-xl border bg-card">
      <AccordionTrigger className="px-4">
        <div className="flex items-start justify-between w-full gap-3">
          <div className="flex-1 text-left">
              <div className="flex items-center gap-2">
              <div className={`h-2 w-2 rounded-full ${id ? "bg-gradient-to-r from-primary to-blue-500" : "bg-muted-foreground"}`} />
              <span className="font-semibold no-underline hover:no-underline">{name}</span>
            </div>
            {description ? <p className="text-xs text-muted-foreground mt-1">{description}</p> : null}
          </div>
        </div>
      </AccordionTrigger>
      <AccordionContent className="px-4 pb-4">
        <div className="space-y-2">
          <AnimatePresence initial={false}>
            {items.map((link) => (
              <motion.div key={link.id} variants={cardVariants} initial="hidden" animate="visible" exit="hidden">
                <LinkRow item={link} />
              </motion.div>
            ))}
          </AnimatePresence>

          {items.length === 0 && (
            <div className="flex items-center justify-center h-24 border-2 border-dashed rounded-lg">
              <div className="text-center">
                <p className="text-sm text-muted-foreground">No links in this group</p>
              </div>
            </div>
          )}
        </div>
      </AccordionContent>
    </AccordionItem>
  );
}

/* ---------- User Header ---------- */
function UserHeader({ 
  username, 
  initialUserData 
}: { 
  username: string;
  initialUserData?: {
    id: string;
    username: string;
    firstName?: string | null;
    lastName?: string | null;
    avatarUrl?: string | null;
    coverUrl?: string | null;
    bio?: string | null;
  } | null;
}) {
  type UserPublic = { id: string; username: string; firstName?: string | null; lastName?: string | null; avatarUrl?: string | null; coverUrl?: string | null; bio?: string | null };
  type GetUserPublicResponse = { data: UserPublic; meta: any | null };

  const [coverImageError, setCoverImageError] = useState(false);
  const [avatarImageError, setAvatarImageError] = useState(false);

  const { data: userData, loading: loadingUser, error: userError } = useGet<GetUserPublicResponse>(
    username ? `/user/${encodeURIComponent(username)}/public` : ''
  );

  // Use server-side data as fallback if client-side data is loading
  const user = userData?.data || initialUserData;

  if (loadingUser && !initialUserData) return null;
  if (userError && !initialUserData) {
    console.warn("User API error:", userError);
    return <p className="text-red-500 text-sm">Failed to load user info.</p>;
  }
  if (!user) return null;

  const { firstName, lastName, avatarUrl, coverUrl, bio } = user;
  const displayName = [firstName, lastName].filter(Boolean).join(" ") || user.username;

  return (
    <div className="bg-card rounded-lg border overflow-hidden">
      <div className="relative w-full">
        <div className="overflow-hidden aspect-[820/312] bg-gradient-to-r from-primary/8 via-transparent to-primary/8">
          {coverUrl && !coverImageError ? (
            <Image 
              src={coverUrl} 
              alt="Cover image" 
              fill
              className="object-cover"
              onError={() => setCoverImageError(true)}
              unoptimized={coverUrl.includes('cloudinary.com')}
            />
          ) : (
            <div className="h-full w-full bg-gradient-to-r from-primary/10 via-background to-primary/10" />
          )}
        </div>

        <div className="absolute inset-x-0 bottom-0 flex justify-center translate-y-1/2 z-20">
          <div className="h-20 w-20 sm:h-24 sm:w-24 rounded-full ring-4 ring-card bg-white overflow-hidden shadow-lg relative">
            <Image 
              src={avatarUrl && !avatarImageError ? avatarUrl : "/images/avatar-placeholder.png"} 
              alt={displayName} 
              fill
              className="object-cover"
              onError={() => setAvatarImageError(true)}
              unoptimized={avatarUrl?.includes('cloudinary.com')}
            />
          </div>
        </div>
      </div>

      <div className="mt-10 sm:mt-12 px-4 pb-4 flex flex-col items-center text-center">
        <h2 className="text-lg sm:text-xl font-semibold truncate">{displayName}</h2>
        <p className="mt-1 text-sm text-muted-foreground">@{user.username}</p>
        {bio ? (
          <p className="mt-2 text-sm text-muted-foreground max-w-prose">{bio}</p>
        ) : null}
      </div>
    </div>
  );
}

interface LinksPageClientProps {
  username?: string;
  initialUserData?: {
    id: string;
    username: string;
    firstName?: string | null;
    lastName?: string | null;
    avatarUrl?: string | null;
    coverUrl?: string | null;
  } | null;
}

/* ---------- Main Client Component ---------- */
export default function LinksPageClient({ username, initialUserData }: LinksPageClientProps) {
  const { data: groupedData, loading: loadingLinks, error: linksError } =
    useGet<GetGroupedLinksResponse>(username ? `/link/user/${encodeURIComponent(username)}` : "/link");

  // Hide navbar while this page is mounted using in-memory context (no persistence).
  const { visible, setVisible } = useNavbarVisibility();
  // Hide footer while this page is mounted (default is visible)
  const { visible: footerVisible, setVisible: setFooterVisible } = useFooterVisibility();

  // Determine ownership: if the logged-in user's username matches the slug (case-insensitive)
  const { user, isAuthenticated } = useUser();
  const isOwner = isAuthenticated && user?.username && username && user.username.toLowerCase() === username.toLowerCase();
  const { theme, toggleTheme } = useTheme();

  // Initialize fingerprint on component mount
  useEffect(() => {
    initializeFingerprint();
  }, []);

  // Track profile view telemetry
  useEffect(() => {
    if (!username || isOwner) {
      // Don't track owner's own views
      console.log('Profile tracking skipped:', { username, isOwner });
      return;
    }

    const trackProfileView = async () => {
      try {
        console.log('Starting profile view tracking for:', username);
        
        // Ensure fingerprint is initialized and retrieve it
        initializeFingerprint();
        const fp = getFingerprint();
        
        console.log('Fingerprint:', fp);
        
        // Don't track if fingerprint generation failed
        if (!fp) {
          console.warn('Fingerprint not available, skipping tracking');
          return;
        }

        const sessionId = getSessionId();
        const source = getTrafficSource();
        const utm = getUtmParameters();
        const referrer = typeof document !== 'undefined' ? document.referrer : null;

        console.log('Tracking data:', { fp, sessionId, source, utm, referrer });

        // Build base payload
        const payload: any = {
          fp,
          sessionId,
          source,
          referrer,
          utm
        };

        await apiService.post(`/profile/${encodeURIComponent(username)}/view`, payload);
        console.log('Profile view tracked successfully');
      } catch (error) {
        console.error('Profile view tracking error:', error);
      }
    };

    // Track view after a short delay to ensure fingerprint is generated
    console.log('Scheduling profile view tracking...');
    const timer = setTimeout(trackProfileView, 500);
    return () => clearTimeout(timer);
  }, [username, isOwner]);

  useEffect(() => {
    const prev = visible;
    setVisible(false);
    return () => setVisible(prev);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  useEffect(() => {
    const prevF = footerVisible;
    setFooterVisible(false);
    return () => setFooterVisible(prevF);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  useEffect(() => {
    if (linksError) {
      console.warn("Links API error:", linksError);
      toast.error("Could not load links.");
    }
  }, [linksError]);

  const sortedGroups = useMemo(() => {
    const groups = groupedData?.data?.groups ?? [];
    return [...groups]
      .map((g) => ({
        ...g,
        links: [...g.links].sort((a, b) => a.sequence - b.sequence),
      }))
      .filter((g) => (g.links ?? []).length > 0)
      .sort((a, b) => a.sequence - b.sequence);
  }, [groupedData]);

  const ungroupedLinks = useMemo(() => {
    const un = groupedData?.data?.ungrouped?.links ?? [];
    return [...un].sort((a, b) => a.sequence - b.sequence);
  }, [groupedData]);

  // public view; if no username param and no data, show nothing or loading
  if (!username && !groupedData && !loadingLinks) return null;

  return (
    <div className="min-h-screen bg-links-pattern">
      <div className="container mx-auto px-4 py-8 max-w-3xl">
        <motion.div variants={containerVariants} initial="hidden" animate="visible" className="space-y-6">
          {username ? <UserHeader username={username} initialUserData={initialUserData} /> : null}

          <div className="flex items-center gap-3">
            <div className="h-8 w-8 rounded-full bg-primary/10 flex items-center justify-center">
                <Image src="/logo.svg" alt="Linqyard" width={20} height={20} className="h-4 w-4 object-contain" />
              </div>
            <div>
              <h1 className="text-3xl font-bold">Links</h1>
              <p className="text-muted-foreground text-sm">View links by group.</p>
            </div>
            <div className="ml-auto flex items-center gap-2">
              {/* Theme toggle (visible on public preview even when navbar hidden) */}
              <Button variant="outline" size="sm" onClick={toggleTheme} className="rounded-full">
                {theme === "dark" ? <Sun className="h-4 w-4" /> : <Moon className="h-4 w-4" />}
              </Button>
              {/* Edit action for owner */}
              {isOwner && (
                <div>
                  <Link href="https://linqyard.com/account/links">
                    <Button variant="default" size="sm" className="rounded-full">
                      Edit
                    </Button>
                  </Link>
                </div>
              )}
            </div>
          </div>

          {loadingLinks ? (
            <motion.div variants={cardVariants} className="py-16 text-center">
              <div className="h-8 w-8 border-2 border-primary border-t-transparent rounded-full animate-spin mx-auto mb-4" />
              <p className="text-muted-foreground">Loading your links...</p>
            </motion.div>
          ) : !groupedData ? (
            <motion.div variants={cardVariants} className="py-16 text-center">
              <div className="h-16 w-16 rounded-full bg-muted/20 flex items-center justify-center mx-auto mb-6">
                <Image src="/logo.svg" alt="Linqyard" width={40} height={40} className="h-8 w-8 text-muted-foreground" />
              </div>
              <h3 className="text-lg font-semibold mb-2">No links found</h3>
              <p className="text-muted-foreground">You don&apos;t have any links yet.</p>
            </motion.div>
          ) : (
            <>
              {/* Ungrouped links: render without a title/description (show links directly) */}
              {ungroupedLinks.length > 0 && (
                <motion.div variants={cardVariants} initial="hidden" animate="visible">
                  <div className="space-y-2">
                    <AnimatePresence initial={false}>
                      {ungroupedLinks.map((link) => (
                        <motion.div key={link.id} variants={cardVariants} initial="hidden" animate="visible" exit="hidden">
                          <LinkRow item={link} />
                        </motion.div>
                      ))}
                    </AnimatePresence>
                  </div>
                </motion.div>
              )}

              {/* Render groups (only if there are any) */}
              {sortedGroups.length > 0 && (
                <Accordion
                  type="multiple"
                  defaultValue={[...sortedGroups.map((g) => g.id)]}
                  className="space-y-3"
                >
                  {sortedGroups.map((g) => (
                    <GroupSection
                      key={g.id}
                      id={g.id}
                      name={g.name}
                      description={g.description}
                      items={g.links}
                    />
                  ))}
                </Accordion>
              )}
            </>
          )}

          {/* Branding Footer */}
          <motion.div variants={cardVariants} className="mt-8 pt-6 border-t">
            <div className="flex flex-col items-center justify-center gap-3 text-center">
              <Link href="/" className="flex items-center gap-2 group">
                <div className="h-8 w-8 rounded-full bg-primary/10 flex items-center justify-center group-hover:bg-primary/20 transition-colors">
                  <Image src="/logo.svg" alt="Linqyard" width={28} height={28} className="object-contain" />
                </div>
                <span className="font-semibold text-lg group-hover:text-primary transition-colors">Linqyard</span>
              </Link>
              <p className="text-xs text-muted-foreground max-w-md">
                powred by Linqyard,  <a href="https://linqyard.com/account/signup" target="_blank" className="text-xs text-primary hover:underline">
                Create your own
              </a>
              </p>
             
            </div>
          </motion.div>
        </motion.div>
      </div>
    </div>
  );
}
