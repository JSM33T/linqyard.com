"use client";

import { useSessionCheck } from '@/hooks/useSessionCheck';

/**
 * Component that handles session restoration on page load
 * This component should be included in the root layout to ensure
 * session restoration happens on every page visit
 */
export function SessionChecker() {
  useSessionCheck();
  
  // This component doesn't render anything visible
  return null;
}