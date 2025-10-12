"use client";

import { motion } from "framer-motion";
import {
  Accordion,
  AccordionContent,
  AccordionItem,
  AccordionTrigger,
} from "@/components/ui/accordion";

export default function FaqPage() {
  return (
    <div className="min-h-screen bg-background">
      {/* FAQ Section */}
      <motion.section
        className="container mx-auto px-4 py-14"
        initial={{ opacity: 0, y: 20 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.6 }}
      >
        <div className="text-center mb-8">
          <h1 className="text-3xl md:text-5xl font-bold">Frequently Asked Questions</h1>
          <p className="mt-3 text-muted-foreground max-w-2xl mx-auto">
            Quick answers to help you get the most out of Linqyard.
          </p>
        </div>

        <Accordion type="single" collapsible className="max-w-3xl mx-auto space-y-2">
          <AccordionItem value="item-1">
            <AccordionTrigger>How do I get started?</AccordionTrigger>
            <AccordionContent>
              Sign up for a free account, add your links, customize your page design, 
              and share your linqyard URL wherever you need it.
            </AccordionContent>
          </AccordionItem>
          <AccordionItem value="item-2">
            <AccordionTrigger>Is there a free plan?</AccordionTrigger>
            <AccordionContent>
              Yes! Our free tier includes everything you need to get started: unlimited links, 
              basic analytics, theme customization, and mobile optimization. No trial period or time limits.
            </AccordionContent>
          </AccordionItem>
          <AccordionItem value="item-3">
            <AccordionTrigger>How secure is my data?</AccordionTrigger>
            <AccordionContent>
              We use industry-standard security practices including encrypted data storage, 
              secure authentication, and regular security audits. Your privacy is our priority.
            </AccordionContent>
          </AccordionItem>
          <AccordionItem value="item-4">
            <AccordionTrigger>Can I customize how my page looks?</AccordionTrigger>
            <AccordionContent>
              Absolutely! Choose from multiple themes, customize colors, add your branding, 
              and arrange your links exactly how you want them.
            </AccordionContent>
          </AccordionItem>
          <AccordionItem value="item-5">
            <AccordionTrigger>Where can I get help?</AccordionTrigger>
            <AccordionContent>
              Email <a className="underline hover:text-primary transition-colors" href="mailto:support@linqyard.com">support@linqyard.com</a> for direct support, or check our docs for guides and tutorials.
            </AccordionContent>
          </AccordionItem>
          <AccordionItem value="item-6">
            <AccordionTrigger>Can I use my own domain?</AccordionTrigger>
            <AccordionContent>
              Custom domains are available on our premium plans. You can connect your own domain 
              to give your link page a professional, branded appearance.
            </AccordionContent>
          </AccordionItem>
          <AccordionItem value="item-7">
            <AccordionTrigger>What analytics do I get?</AccordionTrigger>
            <AccordionContent>
              Track total clicks, individual link performance, geographic data, and referral sources. 
              Premium plans include advanced analytics with detailed insights and export options.
            </AccordionContent>
          </AccordionItem>
          <AccordionItem value="item-8">
            <AccordionTrigger>Can I schedule links?</AccordionTrigger>
            <AccordionContent>
              Link scheduling is on our roadmap! Soon you&apos;ll be able to automatically show or 
              hide links based on date and time, perfect for limited-time promotions.
            </AccordionContent>
          </AccordionItem>
        </Accordion>
      </motion.section>
    </div>
  );
}
