"use client";

import React, { useMemo } from "react";
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

export default function LinksPreview({ groups, ungrouped, header }: { groups: { id: string; name: string; description?: string | null; links: LinkItem[] }[]; ungrouped: LinkItem[]; header?: React.ReactNode }) {
  // groups array comes pre-ordered from the parent (account page manages order).
  // Keep the incoming order and only ensure links inside groups are sorted by their sequence.
  const sortedGroups = useMemo(() => {
    return groups.map((g) => ({ ...g, links: [...g.links].sort((a, b) => a.sequence - b.sequence) }));
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
            {header ? <div className="mb-3">{header}</div> : null}
            <Accordion type="multiple" defaultValue={[...sortedGroups.map((g) => g.id), "__ungrouped__"]} className="space-y-3">
              <GroupSection id={null} name="Ungrouped" description="Links without a group" items={sortedUngrouped} />
              {sortedGroups.map((g) => (
                <GroupSection key={g.id} id={g.id} name={g.name} description={g.description} items={g.links} />
              ))}
            </Accordion>
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
