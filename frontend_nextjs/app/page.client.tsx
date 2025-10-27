"use client";

import { motion, useMotionValue, useSpring, useTransform } from "framer-motion";
import Link from "next/link";
import Image from "next/image";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Separator } from "@/components/ui/separator";
import { ScrollArea } from "@/components/ui/scroll-area";
import {
  ArrowRight,
  CheckCircle,
  Sparkles,
  Link2,
  Edit,
  ExternalLink,
} from "lucide-react";
import { useUser, userHelpers } from "@/contexts/UserContext";

// ---- Motion presets ----
const container = {
  hidden: { opacity: 0 },
  visible: {
    opacity: 1,
    transition: { delayChildren: 0.2, staggerChildren: 0.12 },
  },
};
const item = { hidden: { y: 16, opacity: 0 }, visible: { y: 0, opacity: 1 } };

// ---- Mobile Mockup (re‑used as section image) ----
function DeviceMockup() {
  const mouseX = useMotionValue(0);
  const mouseY = useMotionValue(0);

  const springConfig = { damping: 25, stiffness: 150 };
  const smoothX = useSpring(mouseX, springConfig);
  const smoothY = useSpring(mouseY, springConfig);

  const rotateX = useTransform(smoothY, [-300, 300], [15, -15]);
  const rotateY = useTransform(smoothX, [-300, 300], [-15, 15]);

  const handleMouseMove = (e: React.MouseEvent<HTMLDivElement>) => {
    const rect = e.currentTarget.getBoundingClientRect();
    const centerX = rect.left + rect.width / 2;
    const centerY = rect.top + rect.height / 2;
    
    mouseX.set(e.clientX - centerX);
    mouseY.set(e.clientY - centerY);
  };

  const handleMouseLeave = () => {
    mouseX.set(0);
    mouseY.set(0);
  };

  return (
    <div 
      onMouseMove={handleMouseMove}
      onMouseLeave={handleMouseLeave}
      className="relative"
      style={{ perspective: "1200px" }}
    >
      <motion.div
        className="relative mx-auto w-full max-w-[320px] md:max-w-[360px]"
        initial={{ y: 10, opacity: 0 }}
        whileInView={{ y: 0, opacity: 1 }}
        viewport={{ once: true, amount: 0.3 }}
        transition={{ duration: 0.8, ease: "easeOut" }}
        style={{
          rotateX,
          rotateY,
          transformStyle: "preserve-3d",
        }}
      >
      {/* phone frame */}
      <div className="relative rounded-[3rem] border bg-gradient-to-b from-background/40 to-background/70 shadow-2xl backdrop-blur supports-[backdrop-filter]:bg-background/60">
        {/* top bar / notch */}
        <div className="absolute left-1/2 top-2 z-20 h-6 w-36 -translate-x-1/2 rounded-full bg-muted" />
        <div className="p-4 pt-8">
          <div className="rounded-[2.2rem] border overflow-hidden">
            {/* screen */}
            <div className="relative bg-gradient-to-b from-muted/50 to-background">
              <div className="p-4">
                {/* profile header */}
                <div className="flex items-center gap-3">
                  <div className="h-12 w-12 shrink-0 rounded-full bg-primary/10 grid place-items-center">
                    <Link2 className="h-5 w-5 text-primary" />
                  </div>
                  <div>
                    <p className="text-sm text-muted-foreground">linqyard</p>
                    <p className="text-base font-semibold">
                      <span className="text-primary">@linqyard</span>
                    </p>
                  </div>
                </div>

                <Separator className="my-4" />

                {/* links list */}
                <ScrollArea className="h-[360px] pr-2">
                  <div className="space-y-3">
                    {[
                      { label: "Portfolio", icon: <ExternalLink className="h-4 w-4" /> },
                      { label: "YouTube", icon: <ExternalLink className="h-4 w-4" /> },
                      { label: "Instagram", icon: <ExternalLink className="h-4 w-4" /> },
                      { label: "Newsletter", icon: <ExternalLink className="h-4 w-4" /> },
                      { label: "Contact", icon: <ExternalLink className="h-4 w-4" /> },
                      { label: "Latest project", icon: <ExternalLink className="h-4 w-4" /> },
                      { label: "Speaking", icon: <ExternalLink className="h-4 w-4" /> },
                    ].map((l, i) => (
                      <motion.button
                        key={i}
                        className="group w-full rounded-xl border bg-background px-4 py-3 text-left shadow-sm transition-all hover:shadow-md focus-visible:outline-none"
                        whileHover={{ scale: 1.05, y: -8 }}
                        transition={{ type: "spring", stiffness: 400, damping: 17 }}
                      >
                        <div className="flex items-center justify-between">
                          <span className="inline-flex items-center gap-2 font-medium">
                            <Link2 className="h-4 w-4 text-muted-foreground" />
                            {l.label}
                          </span>
                          <span className="opacity-60 transition-opacity group-hover:opacity-100">
                            {l.icon}
                          </span>
                        </div>
                      </motion.button>
                    ))}
                  </div>
                </ScrollArea>

                <Separator className="my-4" />

                {/* foot chips */}
                <div className="flex flex-wrap gap-2 text-xs">
                  <Badge variant="secondary">Demo</Badge>
                  <Badge variant="secondary">Less chaos</Badge>
                  <Badge variant="secondary">More Clicks</Badge>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* subtle glow */}
      <div
        className="pointer-events-none absolute -inset-10 -z-10 blur-2xl opacity-50"
        style={{
          background:
            "radial-gradient(400px 200px at 50% 100%, hsl(var(--primary)/0.25), transparent)",
        }}
      />
    </motion.div>
    </div>
  );
}

// ---- Reusable alternating section ----
function AltSection({
  eyebrow,
  title,
  text,
  bullets,
  cta,
  image,
  flip = false,
}: {
  eyebrow?: string;
  title: string;
  text: string;
  bullets?: string[];
  cta?: { href: string; label: string };
  image: React.ReactNode;
  flip?: boolean;
}) {
  return (
    <section className="container mx-auto px-4 py-14 md:py-20">
      <div
        className={`mx-auto grid max-w-5xl items-center gap-10 md:gap-12 lg:max-w-6xl lg:grid-cols-2 lg:gap-16 ${flip ? "lg:[&>div:nth-child(1)]:order-2" : ""
          }`}
      >
        {/* text */}
        <motion.div
          initial={{ opacity: 0, y: 24 }}
          whileInView={{ opacity: 1, y: 0 }}
          viewport={{ once: true, amount: 0.35 }}
          transition={{ duration: 0.5 }}
          className="space-y-5"
        >
          {eyebrow && (
            <Badge variant="secondary" className="px-3 py-1.5 text-sm">
              {eyebrow}
            </Badge>
          )}
          <h3 className="text-3xl md:text-5xl font-bold tracking-tight">{title}</h3>
          <p className="text-lg text-muted-foreground max-w-prose">{text}</p>
          {bullets && bullets.length > 0 && (
            <ul className="mt-2 space-y-2">
              {bullets.map((b, i) => (
                <li key={i} className="flex items-start gap-2 text-base">
                  <CheckCircle className="mt-0.5 h-4 w-4 text-green-600 dark:text-green-400" />
                  <span className="text-muted-foreground">{b}</span>
                </li>
              ))}
            </ul>
          )}
          {cta && (
            <div className="pt-2">
              <Button asChild size="lg" className="text-base px-7">
                <Link href={cta.href} className="inline-flex items-center">
                  {cta.label} <ArrowRight className="ml-2 h-5 w-5" />
                </Link>
              </Button>
            </div>
          )}
        </motion.div>

        {/* image */}
        <div className="relative pt-6 md:mt-10">
          {image}
        </div>
      </div>
    </section>
  );
}

export default function HomeClient() {
  const { user, isAuthenticated } = useUser();
  const displayName = userHelpers.getDisplayName(user);
  return (
    <div className="min-h-screen bg-background relative">
      {/* background paints */}
      <div
        aria-hidden
        className="pointer-events-none absolute inset-0 -z-10"
        style={{
          background:
            "linear-gradient(to bottom right, hsl(var(--primary)/0.05), transparent 70%)",
        }}
      />

      <div
        aria-hidden
        className="pointer-events-none absolute top-0 left-0 w-[50vw] max-w-[640px] h-[45vh] bg-no-repeat bg-left-top bg-contain opacity-10 dark:opacity-60 mix-blend-multiply dark:mix-blend-soft-light [mask-image:radial-gradient(80%_80%_at_0%_0%,#000_65%,transparent_100%)]"
        style={{
          zIndex: 9,
          backgroundImage: "url('/logo.svg')",
          WebkitMaskImage:
            "radial-gradient(80% 80% at 0% 0%, black 65%, transparent 100%)",
          maskImage:
            "radial-gradient(80% 80% at 0% 0%, black 65%, transparent 100%)",
        }}
      />

      {/* Hero */}
      <motion.section
        className="container mx-auto px-4 pt-20 pb-10 md:py-24"
        variants={container}
        initial="hidden"
        animate="visible"
      >
  <div className="mx-auto grid max-w-5xl items-center gap-10 lg:max-w-6xl lg:grid-cols-2 lg:gap-16">
          <motion.div className="space-y-6" variants={item}>
            {isAuthenticated ? (
              <>
                <Badge variant="secondary" className="text-sm px-3 py-1.5 inline-flex items-center">
                  <Sparkles className="h-4 w-4 mr-2" /> Welcome aboard
                </Badge>
             
                <motion.h1 variants={item} className="text-4xl md:text-6xl lg:text-7xl font-bold tracking-tight">
                  Welcome onboard, <span className="text-primary">{displayName}</span>!
                </motion.h1>
                <motion.p variants={item} className="text-lg md:text-xl text-muted-foreground max-w-2xl">
                  Jump back into your page. Create new links or tweak your profile.
                </motion.p>
                <motion.div variants={item} className="flex flex-col sm:flex-row gap-3">
                  <Button asChild size="lg" className="text-base px-7">
                    <Link href="/account/links" className="inline-flex items-center"><Link2 className="h-5 w-5 mr-2" />Create Links</Link>
                  </Button>
                  <Button asChild variant="outline" size="lg" className="text-base px-7">
                    <Link href="/account/profile" className="inline-flex items-center"><Edit className="h-5 w-5 mr-2" />Edit Profile</Link>
                  </Button>
                </motion.div>
              </>
            ) : (
              <>
                <Badge variant="secondary" className="text-sm px-3 py-1.5 inline-flex items-center">
                  <Link2 className="h-4 w-4 mr-2" /> Less Chaos • More Clicks
                </Badge>
                <motion.h1 variants={item} className="text-4xl md:text-6xl lg:text-7xl font-bold tracking-tight">
                  Stop juggling links. Share one.
                </motion.h1>
                <motion.p variants={item} className="text-lg md:text-xl text-muted-foreground max-w-2xl">
                  Share a single page for all your socials and actions. Update in minutes,
                  keep things simple and privacy‑aware.
                </motion.p>
                <motion.div variants={item} className="flex flex-col sm:flex-row gap-3">
                  <Button size="lg" className="text-base px-7">
                    <Link className="inline-flex items-center" href={"/account/signup"}>Join Now<ArrowRight className="ml-2 h-5 w-5" /></Link>
                  </Button>
                  <Button asChild variant="outline" size="lg" className="text-base px-7">
                    <Link href="/about" className="inline-flex items-center">Learn more</Link>
                  </Button>
                </motion.div>
                <motion.ul
                  variants={item}
                  className="pt-2 space-y-2 text-sm text-muted-foreground"
                >
                  {[
                    "No credit card for demo",
                    "Email support",
                    "Sensible defaults",
                  ].map((feature) => (
                    <li key={feature} className="flex items-center gap-2">
                      <CheckCircle className="h-4 w-4 text-green-600 dark:text-green-400" />
                      <span>{feature}</span>
                    </li>
                  ))}
                </motion.ul>
              </>
            )}
          </motion.div>

          {/* phone mockup */}
          <DeviceMockup />
        </div>
      </motion.section>

      {/* Logos / social proof */}
      <section className="container mx-auto px-4 pb-6 md:pb-10">
        <div className="flex flex-wrap items-center justify-center gap-3 md:gap-6 text-xs md:text-sm text-muted-foreground">
          <span className="opacity-70">Used in demos by</span>
          <span className="rounded-full border px-3 py-1">Design teams</span>
          <span className="rounded-full border px-3 py-1">Creators</span>
          <span className="rounded-full border px-3 py-1">Student groups</span>
          <span className="rounded-full border px-3 py-1">Small projects</span>
        </div>
      </section>

      <AltSection
        eyebrow="Share Anywhere"
        title="One link that just works everywhere"
        text="Drop it in your bio, business card, email signature, or QR. Your page is responsive and optimized for modern devices."
        bullets={[
          "Fast loading on mobile and desktop",
          "SEO‑friendly profile details",
          "Custom domain or yourname.linqyard.com",
        ]}
        image={
          <div className="relative mx-auto w-full max-w-[480px] aspect-square overflow-hidden rounded-3xl border shadow-xl bg-background">
            <Image src="/hero/click.png" alt="Devices" fill className="object-contain p-6 md:p-8" />
          </div>
        }
        flip
      />

      <AltSection
        eyebrow="Lightweight Insights"
        title="Understand what people click and alll "
        text="Optional, privacy‑respecting counters help you see what's working—no creepy tracking."
        bullets={[
          "Per‑link click counts",
          "Top links at a glance",
          "Export when you need to go deeper",
        ]}
        image={
          <div className="relative mx-auto w-full max-w-[480px] aspect-square overflow-hidden rounded-3xl border shadow-xl bg-background">
            <Image src="/hero/analytics.png" alt="Analytics" fill className="object-contain p-6 md:p-8" />
          </div>
        }
      />

      {/* CTA */}
      <section className="container mx-auto px-4 py-16 md:py-24">
        <div className="rounded-3xl border p-8 md:p-12 text-center bg-gradient-to-b from-muted/40 to-background">
          <h3 className="text-3xl md:text-5xl font-bold">Less chaos. More clicks.</h3>
          <p className="mt-3 text-muted-foreground max-w-2xl mx-auto">
            Start with the essentials today and upgrade as you grow. No credit card required for the demo.
          </p>
          <div className="mt-6 flex flex-col sm:flex-row gap-3 justify-center">
            <Button asChild size="lg" className="text-base px-7">
              <Link href="/account/signup" className="inline-flex items-center">Get started <ArrowRight className="ml-2 h-5 w-5" /></Link>
            </Button>
            <Button asChild variant="outline" size="lg" className="text-base px-7">
              <Link href="/about">Learn more</Link>
            </Button>
          </div>
        </div>
      </section>
    </div>
  );
}
