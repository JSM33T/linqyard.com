"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { Button } from "@/components/ui/button";
import { Loader2 } from "lucide-react";

export default function AccessDenied() {
  const [show, setShow] = useState(false);

  useEffect(() => {
    // Show loader for 3 seconds, then show access denied content
    const t = setTimeout(() => setShow(true), 3000);
    return () => clearTimeout(t);
  }, []);

  return (
    <div className="min-h-screen bg-background flex items-center justify-center px-4">
      <div className="text-center">
        {!show ? (
          <div className="flex flex-col items-center">
            <Loader2 className="h-8 w-8 animate-spin text-primary mb-4" />
            <p className="text-sm text-muted-foreground">Checking access…</p>
          </div>
        ) : (
          <div>
            <h1 className="text-2xl font-bold mb-4">Access Denied</h1>
            <p className="text-muted-foreground mb-6">You need to be logged in to view this page.</p>
            <Link href="/account/login">
              <Button>Sign In</Button>
            </Link>
          </div>
        )}
      </div>
    </div>
  );
}
