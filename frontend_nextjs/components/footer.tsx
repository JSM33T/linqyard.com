"use client";

import Link from "next/link";
import Image from "next/image";
import { Separator } from "@/components/ui/separator";
import { Book, Info, Mail } from "lucide-react";

export default function Footer() {
  const currentYear = new Date().getFullYear();

  return (
    <footer className="mt-auto border-t bg-background">
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        <div className="flex flex-col items-center py-10 space-y-6 text-center">
          {/* Logo + Branding stacked */}
          <Link
            href="/"
            className="flex flex-col items-center gap-2 text-foreground hover:opacity-85 transition-opacity"
          >
            <Image
              src="/logo.svg"
              alt="Linqyard Logo"
              width={56}
              height={56}
              className="text-foreground"
              priority
            />
            <span className="text-xl font-bold tracking-tight">Linqyard</span>
          </Link>

          {/* Links row */}
          <nav className="flex flex-wrap items-center justify-center gap-4 text-sm text-muted-foreground">
            <Link
              href="/about"
              className="flex items-center gap-1 hover:text-foreground transition-colors"
            >
              <Info className="h-4 w-4" />
              About
            </Link>

            <Separator orientation="vertical" className="h-4" />

            <Link
              href="/docs"
              className="flex items-center gap-1 hover:text-foreground transition-colors"
            >
              <Book className="h-4 w-4" />
              Docs
            </Link>

            <Separator orientation="vertical" className="h-4" />

            <Link
              href="/contact"
              className="flex items-center gap-1 hover:text-foreground transition-colors"
            >
              <Mail className="h-4 w-4" />
              Contact
            </Link>
          </nav>

          {/* Copyright */}
          <div className="text-sm text-muted-foreground">
            © {currentYear} Linqyard. All rights reserved.
          </div>
        </div>
      </div>
    </footer>
  );
}
