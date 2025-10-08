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

export default function TermsClient() {
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
            Terms of Service
          </Badge>
          <h1 className="text-4xl md:text-5xl font-bold tracking-tight">Terms of Service</h1>
          <p className="text-muted-foreground">Effective Date: [Insert Date]</p>

          <Card className="mt-6">
            <CardHeader>
              <CardTitle>Terms & Conditions — linqyard.com</CardTitle>
              <CardDescription>Rules and responsibilities for using the Service.</CardDescription>
            </CardHeader>
            <CardContent className="prose max-w-none">
              <p>Effective Date: [Insert Date]</p>
              <p>Welcome to linqyard.com (“Linqyard”, “we”, “us”, or “the Service”). These Terms of Service govern your access to and use of the Service. By using the Service you accept these Terms.</p>

              <ul className="list-disc pl-5 space-y-3">
                <li>
                  <span className="font-semibold">Permitted Use:</span>{' '}
                  <span>You may use the Service to create and publish a personal link page. You must provide accurate information and may not use the Service for unlawful activities. You are responsible for all content you submit.</span>
                </li>

                <li>
                  <span className="font-semibold">Account Security:</span>{' '}
                  <span>Keep your account credentials secure. Notify us immediately of any unauthorized use. We may suspend or terminate accounts that violate these Terms or pose security risks.</span>
                </li>

                <li>
                  <span className="font-semibold">User Content & License:</span>{' '}
                  <span>You retain ownership of content you post. By submitting content, you grant us a non-exclusive, worldwide license to host and display it as part of the Service. Do not post content that infringes others’ rights or violates the law.</span>
                </li>

                <li>
                  <span className="font-semibold">Paid Features:</span>{' '}
                  <span>We may introduce paid features. Any fees, billing, and refund terms will be disclosed when such features are offered.</span>
                </li>

                <li>
                  <span className="font-semibold">Intellectual Property:</span>{' '}
                  <span>All trademarks, logos, and software provided by us are our property. You may not reproduce or distribute them without permission.</span>
                </li>

                <li>
                  <span className="font-semibold">Limitation of Liability:</span>{' '}
                  <span>The Service is provided “as is” and “as available.” To the extent permitted by law, we are not liable for indirect, incidental, or consequential damages.</span>
                </li>

                <li>
                  <span className="font-semibold">Termination:</span>{' '}
                  <span>We may terminate or restrict access to the Service for violations of these Terms or for security reasons.</span>
                </li>

                <li>
                  <span className="font-semibold">Changes to These Terms:</span>{' '}
                  <span>We may update these Terms. Significant changes will be posted with a new effective date.</span>
                </li>

                <li>
                  <span className="font-semibold">Contact:</span>{' '}
                  <span>For questions about these Terms, contact: <a href="mailto:support@linqyard.com" className="underline">support@linqyard.com</a>.</span>
                </li>
              </ul>
            </CardContent>
          </Card>

          <div className="mt-6 text-sm text-muted-foreground">
            <Separator />
            <p className="mt-3">Questions? <a href="mailto:support@linqyard.com" className="underline">support@linqyard.com</a></p>
          </div>
        </motion.div>
      </motion.section>
    </div>
  );
}
