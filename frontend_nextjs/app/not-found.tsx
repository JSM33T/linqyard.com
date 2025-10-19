"use client";

import Link from "next/link";
import Image from "next/image";
import { Button } from "@/components/ui/button";
import { Card, CardHeader, CardTitle, CardContent, CardDescription } from "@/components/ui/card";
import { Separator } from "@/components/ui/separator";
import { Badge } from "@/components/ui/badge";
import { ArrowLeft, Compass } from "lucide-react";

export default function NotFound() {
  return (
    <div className="relative min-h-[80vh] flex items-center justify-center px-4">
      {/* soft watermark top-left using your /logo.svg */}
      <div
        aria-hidden
        className="pointer-events-none absolute top-0 left-0
                   w-[50vw] max-w-[640px] h-[45vh]
                   opacity-[0.12] dark:opacity-[0.14]"
        style={{
          zIndex: -9,
          backgroundImage: "url('/logo.svg')",
          backgroundRepeat: "no-repeat",
          backgroundSize: "contain",
          backgroundPosition: "top left",
          maskImage: "radial-gradient(80% 80% at 0% 0%, black 65%, transparent 100%)",
          WebkitMaskImage: "radial-gradient(80% 80% at 0% 0%, black 65%, transparent 100%)",
          mixBlendMode: "soft-light",
        }}
      />
      <div className="flex flex-col items-center gap-8 w-full max-w-4xl">
        <Card className="w-full max-w-lg shadow-md">
        <CardHeader>
          <div className="flex items-center justify-center mb-4">
            <Image
              src="/logo.svg"
              alt="Linqyard logo"
              width={112}
              height={112}
              className="h-28 w-28 object-contain drop-shadow-[0_10px_20px_rgba(0,0,0,0.06)]"
            />
          </div>
          <Badge variant="secondary" className="w-fit">404</Badge>
          <CardTitle className="mt-2 flex items-center gap-2 text-2xl">
            <Compass className="h-5 w-5 text-primary" />
            Page not found
          </CardTitle>
          <CardDescription>
            The page you’re looking for doesn’t exist or was moved.
          </CardDescription>
        </CardHeader>
        <Separator />
        <CardContent className="pt-6 flex flex-col sm:flex-row gap-3">
          <Button asChild className="px-6">
            <Link href="/" className="flex items-center">
              <ArrowLeft className="mr-2 h-4 w-4" />
              Go home
            </Link>
          </Button>
          <Button asChild variant="outline" className="px-6">
            <Link href="/about">About</Link>
          </Button>
        </CardContent>
      </Card>
      </div>
    </div>
  );
}
