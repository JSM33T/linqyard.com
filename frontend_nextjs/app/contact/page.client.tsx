"use client";

import {
  type ChangeEvent,
  type FormEvent,
  useMemo,
  useState,
} from "react";
import Link from "next/link";
import { motion } from "framer-motion";
import {
  Calendar,
  LifeBuoy,
  Mail,
  MessageCircle,
  Send,
  type LucideIcon,
} from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Separator } from "@/components/ui/separator";

const containerVariants = {
  hidden: { opacity: 0 },
  visible: {
    opacity: 1,
    transition: { delayChildren: 0.2, staggerChildren: 0.12 },
  },
};

const itemVariants = {
  hidden: { y: 12, opacity: 0 },
  visible: { y: 0, opacity: 1 },
};

type ChannelMeta = {
  title: string;
  description: string;
  href: string;
  label: string;
  cta: string;
  icon: LucideIcon;
  external?: boolean;
};

const channelMeta: ChannelMeta[] = [
  {
    title: "Product Support",
    description: "For technical issues, bug reports, or status updates.",
    href: "mailto:mail@jsm33t.com",
    label: "mail@jsm33t.com",
    cta: "Email support",
    icon: LifeBuoy,
  },
  {
    title: "Telegram Chat",
    description: "Reach out instantly via Telegram for a quick conversation.",
    href: "https://t.me/jsm33t",
    label: "@jsm33t",
    cta: "Open Telegram",
    icon: MessageCircle,
    external: true,
  },
];

type ContactFormState = {
  name: string;
  email: string;
  company: string;
  subject: string;
  message: string;
};

const initialFormState: ContactFormState = {
  name: "",
  email: "",
  company: "",
  subject: "",
  message: "",
};

export default function ContactClient() {
  const [formState, setFormState] =
    useState<ContactFormState>(initialFormState);

  const mailtoHref = useMemo(() => {
    const subject =
      formState.subject.trim() ||
      `Message from ${formState.name || "Linqyard visitor"}`;
    const lines = [
      `Name: ${formState.name || "-"}`,
      `Email: ${formState.email || "-"}`,
      formState.company ? `Company: ${formState.company}` : null,
      "",
      formState.message || "(add your message here)",
    ]
      .filter(Boolean)
      .join("\n");

    return `mailto:support@linqyard.com?subject=${encodeURIComponent(
      subject,
    )}&body=${encodeURIComponent(lines)}`;
  }, [formState]);

  const handleChange = (
    event: ChangeEvent<HTMLInputElement | HTMLTextAreaElement>,
  ) => {
    const { name, value } = event.target;
    setFormState((prev) => ({ ...prev, [name]: value }));
  };

  const handleSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    window.location.href = mailtoHref;
  };

  return (
    <div className="min-h-screen bg-background">
      <motion.section
        className="container mx-auto px-4 py-16"
        variants={containerVariants}
        initial="hidden"
        animate="visible"
      >
        <motion.div
          className="max-w-3xl mx-auto text-center space-y-4"
          variants={itemVariants}
        >
          <Badge variant="secondary" className="px-4 py-1.5 text-sm">
            Contact
          </Badge>
          <h1 className="text-4xl md:text-5xl font-bold tracking-tight">
            We&apos;re here to help
          </h1>
          <p className="text-muted-foreground max-w-2xl mx-auto">
            Whether you have a product question, need help troubleshooting, or
            want to collaborate, the Linqyard team is a message away.
          </p>
        </motion.div>

        <motion.div
          className="mt-14 grid gap-6 md:grid-cols-3"
          variants={itemVariants}
        >
          {channelMeta.map(
            ({ title, description, href, label, cta, icon: Icon, external }) => (
              <Card key={title} className="flex flex-col justify-between">
                <CardHeader className="space-y-3 pb-4">
                  <div className="inline-flex size-10 items-center justify-center rounded-full bg-primary/10 text-primary">
                    <Icon className="size-5" />
                  </div>
                  <CardTitle className="text-xl">{title}</CardTitle>
                  <CardDescription>{description}</CardDescription>
                </CardHeader>
                <CardContent className="pt-0">
                  <div className="flex flex-col gap-3">
                    <a
                      href={href}
                      className="text-sm font-medium hover:underline"
                      target={external ? "_blank" : undefined}
                      rel={external ? "noreferrer" : undefined}
                    >
                      {label}
                    </a>
                    <Button asChild variant="secondary">
                      <Link
                        href={href}
                        target={external ? "_blank" : undefined}
                        rel={external ? "noreferrer" : undefined}
                      >
                        {cta}
                      </Link>
                    </Button>
                  </div>
                </CardContent>
              </Card>
            ),
          )}
        </motion.div>

        <motion.div
          className="mt-12 grid gap-6 lg:grid-cols-[2fr,1fr]"
          variants={itemVariants}
        >
          <Card>
            <CardHeader className="space-y-2">
              <CardTitle>Send us a message</CardTitle>
              <CardDescription>
                Share a few details and we&apos;ll respond within one business day.
              </CardDescription>
            </CardHeader>
            <CardContent>
              <form className="space-y-5" onSubmit={handleSubmit}>
                <div className="grid gap-4 md:grid-cols-2">
                  <div className="space-y-2">
                    <Label htmlFor="name">Full name</Label>
                    <Input
                      id="name"
                      name="name"
                      placeholder="Ada Lovelace"
                      autoComplete="name"
                      value={formState.name}
                      onChange={handleChange}
                    />
                  </div>
                  <div className="space-y-2">
                    <Label htmlFor="email">Email</Label>
                    <Input
                      id="email"
                      name="email"
                      type="email"
                      placeholder="you@company.com"
                      autoComplete="email"
                      required
                      value={formState.email}
                      onChange={handleChange}
                    />
                  </div>
                  <div className="space-y-2 md:col-span-2">
                    <Label htmlFor="company">Company or team</Label>
                    <Input
                      id="company"
                      name="company"
                      placeholder="Linqyard"
                      autoComplete="organization"
                      value={formState.company}
                      onChange={handleChange}
                    />
                  </div>
                  <div className="space-y-2 md:col-span-2">
                    <Label htmlFor="subject">Subject</Label>
                    <Input
                      id="subject"
                      name="subject"
                      placeholder="How can we help?"
                      value={formState.subject}
                      onChange={handleChange}
                    />
                  </div>
                </div>
                <div className="space-y-2">
                  <Label htmlFor="message">Message</Label>
                  <textarea
                    id="message"
                    name="message"
                    rows={4}
                    placeholder="Share a bit more detail so we can prepare the right answer."
                    className="w-full resize-none rounded-md border border-input bg-background px-3 py-2 text-sm shadow-xs focus:outline-none focus:ring-2 focus:ring-ring focus:border-transparent"
                    value={formState.message}
                    onChange={handleChange}
                    required
                  />
                </div>
                <div className="flex flex-wrap items-center gap-3">
                  <Button type="submit" className="gap-2">
                    <Send className="size-4" />
                    Compose email
                  </Button>
                  <p className="text-xs text-muted-foreground">
                    Submitting opens your email client with the details filled
                    in for quick sending.
                  </p>
                </div>
              </form>
            </CardContent>
          </Card>

          <Card className="h-full">
            <CardHeader className="space-y-3">
              <div className="inline-flex size-10 items-center justify-center rounded-full bg-muted text-muted-foreground">
                <Calendar className="size-5" />
              </div>
              <CardTitle>Active hours</CardTitle>
              <CardDescription>
                We follow a privacy-first support model with real humans behind
                every reply.
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-6 text-sm">
              <div>
                <p className="font-semibold">Availability</p>
                <p className="text-muted-foreground">
                  Monday - Friday, 09:00 - 18:00 UTC
                </p>
              </div>
              <Separator />
              <div>
                <p className="font-semibold">Average response</p>
                <p className="text-muted-foreground">
                  Under 10 hours.
                </p>
              </div>
              {/* <Separator /> */}
              {/* <div>
                <p className="font-semibold">Need real-time help?</p>
                <p className="text-muted-foreground">
                  Join our community chat to share feedback and see product
                  updates as they ship.
                </p>
              </div>
              <Button className="disabled" asChild variant="outline">
                <Link
                  href="https://linqyard.com/community"
                  target="_blank"
                  rel="noreferrer"
                >
                  Open community chat
                </Link>
              </Button> */}
            </CardContent>
          </Card>
        </motion.div>
      </motion.section>
    </div>
  );
}
