import type { Metadata } from "next";
import { Geist, Geist_Mono } from "next/font/google";
import "./globals.css";
import "./override.css";
import Navbar from "@/components/navbar";
import Footer from "@/components/footer";
import BackToTop from "@/components/BackToTop";
import { UserProvider } from "@/contexts/UserContext";
import { ThemeProvider } from "@/contexts/ThemeContext";
import { NavbarVisibilityProvider } from "@/contexts/NavbarVisibilityContext";
import { FooterVisibilityProvider } from "@/contexts/FooterVisibilityContext";
import { Toaster } from "@/components/ui/sonner";
import { SessionChecker } from "@/components/SessionChecker";
import { FingerprintInitializer } from "@/components/FingerprintInitializer";

const geistSans = Geist({
  variable: "--font-geist-sans",
  subsets: ["latin"],
});

const geistMono = Geist_Mono({
  variable: "--font-geist-mono",
  subsets: ["latin"],
});

export const metadata: Metadata = {
  title: { default: "linqyard", template: `%s • linqyard` },
  description: "All your links in one place.",
   icons: {
    icon: [
      { url: "/favicon-32x32.png", sizes: "32x32", type: "image/png" },
      { url: "/favicon-16x16.png", sizes: "16x16", type: "image/png" },
    ],
    apple: "/apple-touch-icon.png",
  },
  manifest: "/site.webmanifest",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="en">
      {/* <head><script
          async
          crossOrigin="anonymous"
          src="https://tweakcn.com/live-preview.min.js"
        /></head> */}
      <body
        className={`${geistSans.variable} ${geistMono.variable} antialiased flex flex-col min-h-screen`}
      >
        <ThemeProvider defaultTheme="dark">
          <UserProvider>
            <NavbarVisibilityProvider>
              <FooterVisibilityProvider>
              <FingerprintInitializer />
              <SessionChecker />
              <Navbar />
              <main className="flex-1">{children}</main>
              <Footer />
              <BackToTop />
              <Toaster />
              </FooterVisibilityProvider>
            </NavbarVisibilityProvider>
          </UserProvider>
        </ThemeProvider>
      </body>
    </html>
  );
}
