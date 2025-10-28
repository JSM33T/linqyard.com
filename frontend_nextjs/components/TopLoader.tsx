'use client';

import { useEffect, useState } from 'react';
import NextTopLoader from 'nextjs-toploader';

export default function TopLoader() {
  const [color, setColor] = useState('#f97316');

  useEffect(() => {
    // Get the computed primary color from CSS variable
    const primaryColor = getComputedStyle(document.documentElement)
      .getPropertyValue('--primary')
      .trim();
    
    if (primaryColor) {
      // Convert oklch to hex or use a library
      // For now, we'll use the oklch value directly as some browsers support it
      setColor(primaryColor);
    }
  }, []);

  return (
    <NextTopLoader
      color={color}
      initialPosition={0.08}
      crawlSpeed={200}
      height={2}
      crawl={true}
      showSpinner={false}
      easing="ease"
      speed={200}
      shadow={`0 0 10px ${color},0 0 5px ${color}`}
    />
  );
}
