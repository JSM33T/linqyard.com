"use client";

import { useState } from "react";
import { motion } from "framer-motion";
import {
  Accordion,
  AccordionContent,
  AccordionItem,
  AccordionTrigger,
} from "@/components/ui/accordion";
import { Input } from "@/components/ui/input"; // assuming you have a standard Input component

export default function FaqPage() {
  const [query, setQuery] = useState("");

  const faqs = [
    {
      q: "How do I get started?",
      a: "Create a free LinqYard account with your email, verify it, and set your username. Once logged in, you can add links, group them, sort them, and share your public page instantly.",
    },
    {
      q: "Is LinqYard free?",
      a: "Yes! LinqYard offers a Free plan that includes up to 12 links, 2 groups, and basic analytics such as total and top link clicks. You can upgrade to Plus anytime for advanced analytics and design options.",
    },
    {
      q: "What’s included in the Plus plan?",
      a: "Plus users get unlimited links and groups, advanced analytics (device metrics, location insights, and click-time patterns), along with custom backgrounds, gradients, and theme colors.",
    },
    {
      q: "Can I customize my page?",
      a: "Yes. You can personalize your LinqYard page with different themes and layouts. Free users will have access to two basic themes (coming soon), while Plus users can add custom backgrounds and colors for a branded look.",
    },
    {
      q: "How secure is my data?",
      a: "LinqYard uses secure email verification, encrypted credentials, and modern authentication practices. Two-factor authentication (2FA) is coming soon for an extra layer of protection.",
    },
    {
      q: "Do I get my own link or subdomain?",
      a: "Absolutely! Every user gets a unique subdomain such as username.linqyard.com to share anywhere online. QR codes for profiles are also coming soon.",
    },
    {
      q: "What analytics are available?",
      a: "Free users can view total click counts and top-performing links. Plus users unlock advanced analytics, including device type, location, and engagement by day and time, all visualized inside the dashboard.",
    },
    {
      q: "Is a Pro plan available?",
      a: "Not yet — LinqYard currently offers Free and Plus plans. A future Pro tier may include additional dashboards and professional features based on community feedback.",
    },
    {
      q: "Where can I get help or report an issue?",
      a: "You can reach us anytime at support@linqyard.com, or check the help section in your dashboard for quick guides and FAQs.",
    },
  ];

  const filteredFaqs = faqs.filter((item) =>
    item.q.toLowerCase().includes(query.toLowerCase()) ||
    item.a.toLowerCase().includes(query.toLowerCase())
  );

  return (
    <div className="min-h-screen bg-background">
      <motion.section
        className="container mx-auto px-4 py-14"
        initial={{ opacity: 0, y: 20 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.6 }}
      >
        <div className="text-center mb-8">
          <h1 className="text-3xl md:text-5xl font-bold">Frequently Asked Questions</h1>
          <p className="mt-3 text-muted-foreground max-w-2xl mx-auto">
            Everything you need to know about using LinqYard and managing your digital yard.
          </p>
        </div>

        {/* Search Bar */}
        <div className="max-w-md mx-auto mb-8">
          <Input
            type="text"
            placeholder="Search your question..."
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            className="w-full"
          />
        </div>

        {/* FAQ Accordion */}
        <Accordion type="single" collapsible className="max-w-3xl mx-auto space-y-2">
          {filteredFaqs.length > 0 ? (
            filteredFaqs.map((item, idx) => (
              <AccordionItem key={idx} value={`item-${idx}`}>
                <AccordionTrigger>{item.q}</AccordionTrigger>
                <AccordionContent>{item.a}</AccordionContent>
              </AccordionItem>
            ))
          ) : (
            <p className="text-center text-muted-foreground mt-6">
              No questions found matching your search.
            </p>
          )}
        </Accordion>
      </motion.section>
    </div>
  );
}
