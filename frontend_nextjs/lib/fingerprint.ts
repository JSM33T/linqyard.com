/**
 * Generate a browser fingerprint for tracking unique visitors
 * This is a simple implementation - for production, consider using @fingerprintjs/fingerprintjs
 */

export function generateFingerprint(): string {
  // Check if we already have a fingerprint in localStorage
  if (typeof window === 'undefined') {
    return '';
  }

  try {
    const stored = localStorage.getItem('fp');
    if (stored) {
      return stored;
    }
  } catch (e) {
    console.warn('localStorage access failed:', e);
  }

  // Generate a new fingerprint
  const components: string[] = [];

  // Screen resolution
  components.push(`${window.screen.width}x${window.screen.height}`);
  components.push(`${window.screen.colorDepth}`);

  // Timezone
  components.push(`${new Date().getTimezoneOffset()}`);

  // Language
  components.push(navigator.language);

  // Platform
  components.push(navigator.platform);

  // User agent
  components.push(navigator.userAgent);

  // Hardware concurrency (CPU cores)
  if ('hardwareConcurrency' in navigator) {
    components.push(`${navigator.hardwareConcurrency}`);
  }

  // Device memory
  if ('deviceMemory' in navigator) {
    components.push(`${(navigator as any).deviceMemory}`);
  }

  // Canvas fingerprint (simplified)
  try {
    const canvas = document.createElement('canvas');
    const ctx = canvas.getContext('2d');
    if (ctx) {
      ctx.textBaseline = 'top';
      ctx.font = '14px Arial';
      ctx.fillStyle = '#f60';
      ctx.fillRect(0, 0, 100, 50);
      ctx.fillStyle = '#069';
      ctx.fillText('Linqyard', 2, 15);
      components.push(canvas.toDataURL());
    }
  } catch (e) {
    console.warn('Canvas fingerprinting failed:', e);
    // Canvas fingerprinting blocked
  }

  // WebGL fingerprint (simplified)
  try {
    const canvas = document.createElement('canvas');
    const gl = canvas.getContext('webgl') || canvas.getContext('experimental-webgl');
    if (gl && gl instanceof WebGLRenderingContext) {
      const debugInfo = gl.getExtension('WEBGL_debug_renderer_info');
      if (debugInfo) {
        components.push(gl.getParameter(debugInfo.UNMASKED_VENDOR_WEBGL));
        components.push(gl.getParameter(debugInfo.UNMASKED_RENDERER_WEBGL));
      }
    }
  } catch (e) {
    console.warn('WebGL fingerprinting failed:', e);
    // WebGL fingerprinting blocked
  }

  // Create hash from components
  const fingerprint = simpleHash(components.join('|||'));

  // Store in localStorage
  try {
    localStorage.setItem('fp', fingerprint);
  } catch (e) {
    console.warn('localStorage write failed:', e);
  }

  return fingerprint;
}

/**
 * Simple hash function (DJB2)
 */
function simpleHash(str: string): string {
  let hash = 5381;
  for (let i = 0; i < str.length; i++) {
    hash = ((hash << 5) + hash) + str.charCodeAt(i);
  }
  return Math.abs(hash).toString(36);
}

/**
 * Get or generate fingerprint
 */
export function getFingerprint(): string {
  if (typeof window === 'undefined') {
    return '';
  }

  try {
    const stored = localStorage.getItem('fp');
    if (stored) {
      return stored;
    }
  } catch (e) {
    console.warn('localStorage access failed:', e);
    // Fall through to generate new one
  }

  // Generate new fingerprint if not exists
  return generateFingerprint();
}

/**
 * Initialize fingerprint on app load
 * Call this early in the app lifecycle to ensure fingerprint exists
 */
export function initializeFingerprint(): void {
  if (typeof window === 'undefined') {
    return;
  }

  try {
    const stored = localStorage.getItem('fp');
    if (!stored) {
      generateFingerprint();
      console.debug('Fingerprint initialized');
    }
  } catch (e) {
    console.warn('Failed to initialize fingerprint:', e);
  }
}

/**
 * Generate a session ID for grouping views
 * Session expires after 30 minutes of inactivity
 */
export function getSessionId(): string {
  if (typeof window === 'undefined') {
    return '';
  }

  try {
    const stored = sessionStorage.getItem('sessionId');
    const storedTime = sessionStorage.getItem('sessionTime');
    
    if (stored && storedTime) {
      const elapsed = Date.now() - parseInt(storedTime, 10);
      // Session valid for 30 minutes
      if (elapsed < 30 * 60 * 1000) {
        // Update last activity time
        sessionStorage.setItem('sessionTime', Date.now().toString());
        return stored;
      }
    }
  } catch (e) {
    console.warn('sessionStorage access failed:', e);
  }

  // Generate new session ID
  const sessionId = `${Date.now()}-${Math.random().toString(36).substring(2, 15)}`;
  
  try {
    sessionStorage.setItem('sessionId', sessionId);
    sessionStorage.setItem('sessionTime', Date.now().toString());
  } catch (e) {
    console.warn('sessionStorage write failed:', e);
  }

  return sessionId;
}

/**
 * Parse UTM parameters from URL
 */
export function getUtmParameters(): {
  source?: string;
  medium?: string;
  campaign?: string;
  term?: string;
  content?: string;
} | null {
  if (typeof window === 'undefined') {
    return null;
  }

  const params = new URLSearchParams(window.location.search);
  const utm: any = {};
  
  if (params.has('utm_source')) utm.source = params.get('utm_source');
  if (params.has('utm_medium')) utm.medium = params.get('utm_medium');
  if (params.has('utm_campaign')) utm.campaign = params.get('utm_campaign');
  if (params.has('utm_term')) utm.term = params.get('utm_term');
  if (params.has('utm_content')) utm.content = params.get('utm_content');

  return Object.keys(utm).length > 0 ? utm : null;
}

/**
 * Get traffic source from referrer or URL parameters
 */
export function getTrafficSource(): string | null {
  if (typeof window === 'undefined') {
    return null;
  }

  // Check for explicit source parameter (src or source)
  const params = new URLSearchParams(window.location.search);
  if (params.has('src')) {
    return params.get('src');
  }
  if (params.has('source')) {
    return params.get('source');
  }

  // Check UTM source
  if (params.has('utm_source')) {
    return params.get('utm_source');
  }

  // Parse from referrer
  const referrer = document.referrer.toLowerCase();
  if (!referrer) {
    return 'direct';
  }

  if (referrer.includes('whatsapp') || referrer.includes('wa.me')) return 'whatsapp';
  if (referrer.includes('twitter') || referrer.includes('t.co')) return 'twitter';
  if (referrer.includes('facebook') || referrer.includes('fb.com')) return 'facebook';
  if (referrer.includes('linkedin')) return 'linkedin';
  if (referrer.includes('instagram')) return 'instagram';
  if (referrer.includes('google')) return 'google';
  if (referrer.includes('bing')) return 'bing';
  if (referrer.includes('reddit')) return 'reddit';
  if (referrer.includes('tiktok')) return 'tiktok';
  if (referrer.includes('youtube')) return 'youtube';
  if (referrer.includes('telegram')) return 'telegram';

  return 'other';
}
