import { Metadata } from 'next';
import LinksPageClient from './LinksPageClient';

type UserPublic = {
  id: string;
  username: string;
  firstName?: string | null;
  lastName?: string | null;
  avatarUrl?: string | null;
  coverUrl?: string | null;
};

type GetUserPublicResponse = {
  data: UserPublic;
  meta: any | null;
};

async function getUserPublicData(username: string): Promise<UserPublic | null> {
  const apiBase = process.env.NEXT_PUBLIC_API_URL ?? '';
  
  try {
    const res = await fetch(`${apiBase}/user/${encodeURIComponent(username)}/public`, { 
      cache: 'no-store',
      headers: {
        'Content-Type': 'application/json',
      }
    });
    
    if (res.ok) {
      const json: GetUserPublicResponse = await res.json();
      return json?.data ?? null;
    }
  } catch (error) {
    console.warn('Failed to fetch user data for SSR:', error);
  }
  
  return null;
}

export async function generateMetadata({ params }: { params: Promise<{ slug?: string }> }): Promise<Metadata> {
  const { slug: username } = await params;
  
  if (!username) {
    return {
      title: 'Links — Linqyard',
      description: 'View links and profile on Linqyard.',
    };
  }

  const userData = await getUserPublicData(username);
  const displayName = userData 
    ? ([userData.firstName, userData.lastName].filter(Boolean).join(' ') || userData.username)
    : username;

  const title = `${displayName} — Linqyard`;
  const description = `${displayName}'s Linqyard page: links, socials, and profile.`;
  const keywords = `Linqyard, links, profile, ${username}`;
  
  // Prefer avatarUrl from API if available; use it verbatim (assumed to be a full URL).
  const image = userData?.avatarUrl ?? `https://linqyard.com/og/${encodeURIComponent(username)}.png`;

  return {
    title,
    description,
    keywords,
    openGraph: {
      title,
      description,
      images: [{ url: image }],
      type: 'profile',
    },
    twitter: {
      card: 'summary_large_image',
      title,
      description,
      images: [image],
    },
  };
}

export default async function LinksPage({ params }: { params: Promise<{ slug?: string }> }) {
  const { slug: username } = await params;
  let initialUserData: UserPublic | null = null;

  if (username) {
    initialUserData = await getUserPublicData(username);
  }

  return <LinksPageClient username={username} initialUserData={initialUserData} />;
}



