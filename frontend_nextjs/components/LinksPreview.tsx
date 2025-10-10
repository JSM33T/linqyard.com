"use client";

import React, { useMemo, useState } from "react";
import { motion, AnimatePresence } from "framer-motion";
import { Accordion, AccordionItem, AccordionTrigger, AccordionContent } from "@/components/ui/accordion";
import { Globe, ExternalLink } from "lucide-react";
import { LinkItem } from "@/hooks/types";

const cardVariants = {
  hidden: { opacity: 0, scale: 0.98 },
  visible: { opacity: 1, scale: 1, transition: { duration: 0.25 } },
};

function LinkRow({ item }: { item: LinkItem }) {
  return (
    <div className="group/link flex items-center gap-3 rounded-lg border bg-background/60 hover:bg-accent/50 transition-all p-3">
      <div className="flex-1 min-w-0">
        <div className="flex items-center gap-2">
          <a href={item.url} target="_blank" rel="noreferrer" className="font-medium text-sm truncate hover:text-primary transition-colors">
            {item.name}
          </a>
          <ExternalLink className="h-3 w-3 text-muted-foreground flex-shrink-0" />
        </div>
        {item.description && <p className="text-xs text-muted-foreground mt-1 truncate">{item.description}</p>}
      </div>
    </div>
  );
}

function GroupSection({ id, name, description, items }: { id: string | null; name: string; description?: string | null; items: LinkItem[] }) {
  const containerId = id ?? "__ungrouped__";

  return (
    <AccordionItem value={containerId} className="rounded-xl border bg-card">
      <AccordionTrigger className="px-4">
        <div className="flex items-start justify-between w-full gap-3">
          <div className="flex-1 text-left">
            <div className="flex items-center gap-2">
              <div className={`h-2 w-2 rounded-full ${id ? "bg-gradient-to-r from-primary to-blue-500" : "bg-muted-foreground"}`} />
              <span className="font-semibold no-underline hover:no-underline">{name}</span>
            </div>
            {description ? <p className="text-xs text-muted-foreground mt-1">{description}</p> : null}
          </div>
        </div>
      </AccordionTrigger>
      <AccordionContent className="px-4 pb-4">
        <div className="space-y-2">
          <AnimatePresence initial={false}>
            {items.map((link) => (
              <motion.div key={link.id} variants={cardVariants} initial="hidden" animate="visible" exit="hidden">
                <LinkRow item={link} />
              </motion.div>
            ))}
          </AnimatePresence>

          {items.length === 0 && (
            <div className="flex items-center justify-center h-24 border-2 border-dashed rounded-lg">
              <div className="text-center">
                <p className="text-sm text-muted-foreground">No links in this group</p>
              </div>
            </div>
          )}
        </div>
      </AccordionContent>
    </AccordionItem>
  );
}

export default function LinksPreview({ groups, ungrouped, header, user }: { groups: { id: string; name: string; description?: string | null; links: LinkItem[] }[]; ungrouped: LinkItem[]; header?: React.ReactNode; user?: { id: string; username: string; firstName?: string | null; lastName?: string | null; avatarUrl?: string | null; coverUrl?: string | null } }) {
  // groups array comes pre-ordered from the parent (account page manages order).
  // Keep the incoming order and only ensure links inside groups are sorted by their sequence.
  const sortedGroups = useMemo(() => {
    return groups
      .map((g) => ({ ...g, links: [...g.links].sort((a, b) => a.sequence - b.sequence) }))
      .filter((g) => (g.links ?? []).length > 0);
  }, [groups]);

  const sortedUngrouped = useMemo(() => {
    return [...ungrouped].sort((a, b) => a.sequence - b.sequence);
  }, [ungrouped]);

  return (
    <div className="w-full flex justify-center">
  {/* iPhone mock (reduced height) */}
  <div className="relative rounded-[36px] border-[1.5px] border-accent bg-accent/10 shadow-2xl overflow-hidden" style={{ width: 380, height: 700 }}>
  {/* Top sensor housing / speaker (follow accent) */}
  <div className="absolute top-3 left-1/2 -translate-x-1/2 rounded-full bg-accent/25" style={{ width: 120, height: 8 }} />

  {/* Inner screen (use accent so the mock follows the project's accent color) */}
  <div className="absolute inset-5 bg-accent rounded-[20px] overflow-hidden flex flex-col" style={{ boxShadow: 'inset 0 0 0 1px rgba(0,0,0,0.03)' }}>
          {/* In-phone navbar: simplified to only 'Preview' and follow accent */}
          <div className="flex items-center justify-center px-4 py-2 border-b bg-accent text-accent-foreground">
            <div className="flex items-center gap-2 text-sm font-semibold">
              <Globe className="h-4 w-4" />
              <span>Preview</span>
            </div>
          </div>

          {/* Scrollable content area */}
          <div className="p-3 overflow-y-auto flex-1 bg-background links-preview-scroll">
            {/* Optional header injected from parent - renders inside the mock above the links */}
            {header ? (
              <div className="mb-3">{header}</div>
            ) : user ? (
              // Render simple header (cover + avatar + name/username) similar to LinksPageClient
              <div className="mb-3">
                <div className="bg-card rounded-lg border overflow-hidden">
                  <div className="relative w-full">
                    <div className="overflow-hidden aspect-[820/312] bg-gradient-to-r from-primary/8 via-transparent to-primary/8">
                      {user.coverUrl ? (
                        // eslint-disable-next-line jsx-a11y/img-redundant-alt
                        <img src={user.coverUrl} alt="Cover image" className="w-full h-full object-cover" />
                      ) : (
                        <div className="h-full w-full bg-gradient-to-r from-primary/10 via-background to-primary/10" />
                      )}
                    </div>

                    <div className="absolute inset-x-0 bottom-0 flex justify-center translate-y-1/2 z-20">
                      <div className="h-20 w-20 sm:h-24 sm:w-24 rounded-full ring-4 ring-card bg-white overflow-hidden shadow-lg relative">
                        <img src={user.avatarUrl ?? "/images/avatar-placeholder.png"} alt={user.username} className="w-full h-full object-cover" />
                      </div>
                    </div>
                  </div>

                  <div className="mt-10 sm:mt-12 px-4 pb-4 flex flex-col items-center text-center">
                    <h2 className="text-lg sm:text-xl font-semibold truncate">{[user.firstName, user.lastName].filter(Boolean).join(' ') || user.username}</h2>
                    <p className="mt-1 text-sm text-muted-foreground">@{user.username}</p>
                  </div>
                </div>
              </div>
            ) : null}

            {/* Ungrouped links - render directly without titled AccordionItem */}
            {sortedUngrouped.length > 0 && (
              <div className="space-y-2 mb-3">
                <AnimatePresence initial={false}>
                  {sortedUngrouped.map((link) => (
                    <motion.div key={link.id} variants={cardVariants} initial="hidden" animate="visible" exit="hidden">
                      <LinkRow item={link} />
                    </motion.div>
                  ))}
                </AnimatePresence>
              </div>
            )}

            {/* Render groups only when there are groups */}
            {sortedGroups.length > 0 && (
              <Accordion type="multiple" defaultValue={[...sortedGroups.map((g) => g.id)]} className="space-y-3">
                {sortedGroups.map((g) => (
                  <GroupSection key={g.id} id={g.id} name={g.name} description={g.description} items={g.links} />
                ))}
              </Accordion>
            )}
          </div>

          {/* Bottom home indicator */}
          <div className="flex items-center justify-center p-3">
            <div className="w-20 h-1.5 rounded-full bg-primary/20" />
          </div>
        </div>
      </div>
    </div>
  );
}
