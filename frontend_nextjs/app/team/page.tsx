"use client";

import * as React from "react";
import { motion } from "framer-motion";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from "@/components/ui/tooltip";
import { Separator } from "@/components/ui/separator";
import { Github, Linkedin, Mail, Globe, ShieldCheck, Users } from "lucide-react";

// --- Types ---
export type TeamMember = {
  id: string;
  name: string;
  role: string;
  bio?: string;
  avatarUrl?: string;
  email?: string;
  links?: Partial<{ github: string; linkedin: string; website: string }>;
  tags?: string[];
  isCore?: boolean;
};

// --- Sample data ---
const DEFAULT_MEMBERS: TeamMember[] = [
  {
    id: "1",
    name: "Jasmeet Singh",
    role: "Founder",
    bio: "Product-first founder.",
    avatarUrl: "https://i.pravatar.cc/160?img=12",
    email: "aarav@example.com",
    links: { linkedin: "https://linkedin.com", github: "https://github.com", website: "https://example.com" },
    tags: ["C#", "DevOps", "AI"],
    isCore: true,
  },
   {
    id: "1",
    name: "Sai Sunder",
    role: "Co-Founder",
    bio: "Business and UX guy",
    avatarUrl: "https://i.pravatar.cc/160?img=12",
    email: "aarav@example.com",
    links: { linkedin: "https://linkedin.com", github: "https://github.com", website: "https://example.com" },
    tags: ["C#", "DevOps", "AI"],
    isCore: true,
  },
  {
    id: "2",
    name: "Pepe Yz",
    role: "Contributor",
    bio: "Ensures quality and reliability through rigorous testing.",
    avatarUrl: "https://i.pravatar.cc/160?img=32",
    links: { github: "https://github.com" },
    isCore: false,
  },
  {
    id: "3",
    name: "Dodo Yz",
    role: "Contributor",
    bio: "Design systems and accessibility.",
    avatarUrl: "https://i.pravatar.cc/160?img=5",
    links: { github: "https://github.com" },
  },
   {
    id: "3",
    name: "Jaived Shrivas",
    role: "Contributor",
    bio: "Design systems and accessibility.",
    avatarUrl: "https://i.pravatar.cc/160?img=5",
    links: { github: "https://github.com" },
  },
];

// --- Utilities ---
function initials(name: string) {
  return name
    .split(" ")
    .map((n) => n[0])
    .join("")
    .slice(0, 2)
    .toUpperCase();
}

// --- Core Member Card ---
function CoreMemberCard({ member }: { member: TeamMember }) {
  return (
    <motion.div initial={{ opacity: 0, y: 8 }} animate={{ opacity: 1, y: 0 }} transition={{ duration: 0.2 }}>
      <Card className="h-full">
        <CardHeader className="flex-row items-start gap-3">
          <Avatar className="h-12 w-12 shrink-0">
            {member.avatarUrl ? (
              <AvatarImage src={member.avatarUrl} alt={member.name} />
            ) : (
              <AvatarFallback>{initials(member.name)}</AvatarFallback>
            )}
          </Avatar>
          <div className="flex-1">
            <CardTitle className="text-base font-semibold">{member.name}</CardTitle>
            <div className="text-sm text-muted-foreground">{member.role}</div>
            <div className="mt-2 flex flex-wrap gap-1">
              {member.tags?.slice(0, 3).map((t) => (
                <Badge key={t} variant="secondary" className="capitalize">
                  {t}
                </Badge>
              ))}
              {member.isCore && (
                <TooltipProvider>
                  <Tooltip>
                    <TooltipTrigger asChild>
                      <Badge variant="outline" className="gap-1">
                        <ShieldCheck className="h-3.5 w-3.5" /> Core
                      </Badge>
                    </TooltipTrigger>
                    <TooltipContent>Core team member</TooltipContent>
                  </Tooltip>
                </TooltipProvider>
              )}
            </div>
          </div>
        </CardHeader>
        <CardContent className="space-y-3">
          {member.bio && <p className="text-sm text-muted-foreground line-clamp-3">{member.bio}</p>}

          <div className="flex items-center gap-2">
            {member.email && (
              <Button asChild size="icon" variant="ghost" className="h-9 w-9">
                <a href={`mailto:${member.email}`}>
                  <Mail className="h-4 w-4" />
                </a>
              </Button>
            )}
            {member.links?.github && (
              <Button asChild size="icon" variant="ghost" className="h-9 w-9">
                <a href={member.links.github} target="_blank" rel="noreferrer noopener">
                  <Github className="h-4 w-4" />
                </a>
              </Button>
            )}
            {member.links?.linkedin && (
              <Button asChild size="icon" variant="ghost" className="h-9 w-9">
                <a href={member.links.linkedin} target="_blank" rel="noreferrer noopener">
                  <Linkedin className="h-4 w-4" />
                </a>
              </Button>
            )}
            {member.links?.website && (
              <Button asChild size="icon" variant="ghost" className="h-9 w-9">
                <a href={member.links.website} target="_blank" rel="noreferrer noopener">
                  <Globe className="h-4 w-4" />
                </a>
              </Button>
            )}
          </div>
        </CardContent>
      </Card>
    </motion.div>
  );
}

function ContributorCard({ member }: { member: TeamMember }) {
  return (
    <motion.div
      initial={{ opacity: 0, y: 8 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.2 }}
    >
      <Card className="h-full border-muted/40 bg-muted/10 hover:bg-muted/20 transition-colors">
        <CardHeader className="flex items-center justify-between gap-3">
          {/* Left section: avatar + text */}
          <div className="flex items-center gap-3">
            <Avatar className="h-10 w-10 shrink-0">
              {member.avatarUrl ? (
                <AvatarImage src={member.avatarUrl} alt={member.name} />
              ) : (
                <AvatarFallback>{initials(member.name)}</AvatarFallback>
              )}
            </Avatar>
            <div className="flex flex-col">
              <CardTitle className="text-sm font-medium leading-tight">{member.name}</CardTitle>
              <div className="text-xs text-muted-foreground">{member.role}</div>
            </div>
          </div>

          {/* Right side: GitHub icon */}
          {member.links?.github && (
            <Button
              asChild
              size="icon"
              variant="ghost"
              className="h-8 w-8 shrink-0"
              aria-label="GitHub"
            >
              <a href={member.links.github} target="_blank" rel="noreferrer noopener">
                <Github className="h-4 w-4" />
              </a>
            </Button>
          )}
        </CardHeader>
      </Card>
    </motion.div>
  );
}

// --- Toolbar ---
function Toolbar({ total, coreCount }: { total: number; coreCount: number }) {
  return (
    <div className="flex items-center gap-2">
      <Users className="h-5 w-5" />
      <span className="text-sm text-muted-foreground">
        {coreCount} core • {total} total
      </span>
    </div>
  );
}

// --- Main Component ---
export default function TeamMembersPanel({ members = DEFAULT_MEMBERS }: { members?: TeamMember[] }) {
  const coreMembers = React.useMemo(() => members.filter((m) => m.isCore), [members]);
  const contributors = React.useMemo(() => members.filter((m) => !m.isCore), [members]);

  const coreCount = coreMembers.length;
  const total = members.length;

  return (
    <div className="space-y-6 container mx-auto px-4 py-8">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-xl font-semibold tracking-tight">Our Team</h2>
          <p className="text-sm text-muted-foreground">Meet the people building and supporting the product.</p>
        </div>
        <Toolbar total={total} coreCount={coreCount} />
      </div>

      <Separator />

      {/* Core Section */}
      <section className="space-y-3">
        <h3 className="text-base font-semibold">Core Team</h3>
        {coreMembers.length > 0 ? (
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
            <TooltipProvider delayDuration={150}>
              {coreMembers.map((m) => (
                <CoreMemberCard key={m.id} member={m} />
              ))}
            </TooltipProvider>
          </div>
        ) : (
          <p className="text-sm text-muted-foreground">No core members yet.</p>
        )}
      </section>

      <Separator />

      {/* Contributors Section */}
      <section className="space-y-3">
        <h3 className="text-base font-semibold">Contributors</h3>
        {contributors.length > 0 ? (
          <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
            {contributors.map((m) => (
              <ContributorCard key={m.id} member={m} />
            ))}
          </div>
        ) : (
          <p className="text-sm text-muted-foreground">No contributors yet.</p>
        )}
      </section>
    </div>
  );
}
