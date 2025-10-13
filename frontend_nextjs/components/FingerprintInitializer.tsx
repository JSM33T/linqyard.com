"use client";

import { useEffect } from 'react';
import { initializeFingerprint } from '@/lib/fingerprint';

/**
 * Component to initialize browser fingerprint on app load
 * This ensures every visitor gets a unique fingerprint stored in localStorage
 */
export function FingerprintInitializer() {
  useEffect(() => {
    // Initialize fingerprint as soon as the app loads
    initializeFingerprint();
  }, []);

  return null; // This component doesn't render anything
}
