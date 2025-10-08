"use client";

import { motion } from "framer-motion";
import { Badge } from "@/components/ui/badge";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Separator } from "@/components/ui/separator";


const containerVariants = {
  hidden: { opacity: 0 },
  visible: { opacity: 1, transition: { delayChildren: 0.2, staggerChildren: 0.12 } },
};

const itemVariants = {
  hidden: { y: 10, opacity: 0 },
  visible: { y: 0, opacity: 1 },
};

export default function PrivacyClient() {
  return (
    <div className="min-h-screen bg-background">
      <motion.section
        className="container mx-auto px-4 py-16"
        variants={containerVariants}
        initial="hidden"
        animate="visible"
      >
        <motion.div className="space-y-4 max-w-4xl mx-auto" variants={itemVariants}>
          <Badge variant="secondary" className="text-sm px-4 py-2">
            Privacy Policy
          </Badge>
          <h1 className="text-4xl md:text-5xl font-bold tracking-tight">Privacy Policy</h1>
          <p className="text-muted-foreground">Effective Date: [Insert Date]</p>

          <Card className="mt-6">
            <CardHeader>
              <CardTitle>Privacy Policy — linqyard.com</CardTitle>
              <CardDescription>How we collect, use, and protect your information.</CardDescription>
            </CardHeader>
            <CardContent className="prose max-w-none">
              <p>Welcome to linqyard.com (“we,” “us,” or “the Service”). This Privacy Policy describes the categories of personal information we collect, how we use it, how we protect it, and the choices available to you.</p>

              <ul className="list-disc pl-5 space-y-3">
                <li>
                  <span className="font-semibold">Information Collected:</span>{' '}
                  <span>We collect account information (email and phone for authentication), usage data (pages visited, device and browser details, IP address, cookies), and content you voluntarily provide (links, profile text, images).</span>
                </li>

                <li>
                  <span className="font-semibold">Purpose of Processing:</span>{' '}
                  <span>We use your information to provide and improve the Service, authenticate users (including OTP delivery), communicate important updates, and detect and prevent abuse.</span>
                </li>

                <li>
                  <span className="font-semibold">Cookies and Tracking:</span>{' '}
                  <span>We use cookies and similar technologies for session management, security, and analytics. You may disable cookies via your browser, but certain features may be impacted.</span>
                </li>

                <li>
                  <span className="font-semibold">Security:</span>{' '}
                  <span>We implement administrative, technical, and physical safeguards designed to protect your information. However, no system can be guaranteed completely secure.</span>
                </li>

                <li>
                  <span className="font-semibold">Data Retention:</span>{' '}
                  <span>We retain personal data only as long as necessary to provide the Service, comply with legal obligations, and resolve disputes. To request deletion, contact us at the address below.</span>
                </li>

                <li>
                  <span className="font-semibold">Third-Party Services:</span>{' '}
                  <span>We may share data with third-party providers who perform services on our behalf (for example, analytics or email delivery). These providers are contractually bound to protect your data.</span>
                </li>

                <li>
                  <span className="font-semibold">Children’s Privacy:</span>{' '}
                  <span>The Service is not intended for children under 13. We do not knowingly collect personal information from children under 13.</span>
                </li>

                <li>
                  <span className="font-semibold">Changes to This Policy:</span>{' '}
                  <span>We may update this policy from time to time. Material changes will be posted here with an updated effective date.</span>
                </li>

                <li>
                  <span className="font-semibold">Contact:</span>{' '}
                  <span>If you have questions or requests regarding your data, please contact: <a href="mailto:mail@linqyard.com" className="underline">mail@linqyard.com</a>.</span>
                </li>
              </ul>
            </CardContent>
          </Card>

          <div className="mt-6 text-sm text-muted-foreground">
            <Separator />
            <p className="mt-3">Questions? <a href="mailto:mail@linqyard.com" className="underline">mail@linqyard.com</a></p>
          </div>
        </motion.div>
      </motion.section>
    </div>
  );
}
