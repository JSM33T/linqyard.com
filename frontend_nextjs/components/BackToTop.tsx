"use client";

import { useEffect, useState } from "react";
import { ArrowUp } from "lucide-react";
import { Button } from "@/components/ui/button";

export default function BackToTop() {
  const [isVisible, setIsVisible] = useState(false);

  useEffect(() => {
    const toggleVisibility = () => {
      // Show button when page is scrolled down 300px
      if (window.scrollY > 300) {
        setIsVisible(true);
      } else {
        setIsVisible(false);
      }
    };

    window.addEventListener("scroll", toggleVisibility, { passive: true });

    return () => window.removeEventListener("scroll", toggleVisibility);
  }, []);

  const scrollToTop = () => {
    window.scrollTo({
      top: 0,
      behavior: "smooth",
    });
  };

  return (
    <div
      className={[
        "fixed bottom-6 right-6 z-50 transition-all duration-300",
        isVisible
          ? "opacity-100 translate-y-0"
          : "opacity-0 translate-y-10 pointer-events-none",
      ].join(" ")}
    >
      <Button
        onClick={scrollToTop}
        size="icon"
        className={[
          "h-12 w-12 rounded-full shadow-lg",
          "bg-background/70 backdrop-blur-md",
          // subtle secondary outline + fallback border
          "border border-border/50 dark:border-border/40",
          "border-secondary/30 dark:border-secondary/40",
          // visible focus outline in secondary color
          "focus-visible:ring-2 focus-visible:ring-secondary/60 focus-visible:ring-offset-2 focus-visible:ring-offset-background",
          "hover:bg-background/90 hover:scale-110",
          "transition-all duration-300",
          "text-foreground hover:text-foreground",
        ].join(" ")}
        aria-label="Back to top"
      >
        <ArrowUp className="h-5 w-5" />
      </Button>
    </div>
  );
}
