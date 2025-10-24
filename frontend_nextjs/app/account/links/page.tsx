"use client";

import React, { useEffect, useMemo, useRef, useState } from "react";
import Link from "next/link";
import NextImage from "next/image";
import { motion, AnimatePresence } from "framer-motion";
import LinksPreview from "@/components/LinksPreview";
import {
  DndContext,
  DragOverlay,
  PointerSensor,
  KeyboardSensor,
  TouchSensor,
  useSensor,
  useSensors,
  DragStartEvent,
  DragEndEvent,
  pointerWithin,
  MeasuringStrategy,
  defaultDropAnimation,
  useDroppable,
} from "@dnd-kit/core";
import {
  SortableContext,
  useSortable,
  arrayMove,
  verticalListSortingStrategy,
} from "@dnd-kit/sortable";
import { restrictToVerticalAxis } from "@dnd-kit/modifiers";
import { CSS } from "@dnd-kit/utilities";

import AccessDenied from "@/components/AccessDenied";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Accordion, AccordionItem, AccordionTrigger, AccordionContent } from "@/components/ui/accordion";
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from "@/components/ui/dropdown-menu";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";

import { ArrowLeft, Globe, Plus, GripVertical, ExternalLink, Edit3, Trash2, FolderPlus, ChevronsUpDown, Share2, Copy, Check } from "lucide-react";
import { toast } from "sonner";
import QRCode from "qrcode";

import { useUser } from "@/contexts/UserContext";
import { useApi, useGet } from "@/hooks/useApi";
import { GetGroupedLinksResponse, LinkItem, CreateOrEditLinkRequest, UpdateGroupRequest, GroupResequenceItemRequest } from "@/hooks/types";
import { userHelpers } from "@/contexts/UserContext";

/* ---------- FX presets ---------- */
const containerVariants = {
  hidden: { opacity: 0, y: 20 },
  visible: { opacity: 1, y: 0, transition: { duration: 0.6 } },
};
const cardVariants = {
  hidden: { opacity: 0, scale: 0.98 },
  visible: { opacity: 1, scale: 1, transition: { duration: 0.25 } },
};

const PRIMARY_COLOR_FALLBACK = "#5c558b";

type UrlProtocol = "https://" | "http://";

const MAX_AUTO_DESCRIPTION_LENGTH = 220;

/* ---------- Sortable row (single Link chip) ---------- */
function SortableLinkRow({ item, onEdit, onDelete }: { item: LinkItem; onEdit: (l: LinkItem) => void; onDelete: (l: LinkItem) => void }) {
  const { attributes, listeners, setNodeRef, transform, transition, isDragging } = useSortable({ id: item.id });
  const style = {
    transform: CSS.Transform.toString(transform),
    transition,
    zIndex: isDragging ? 50 : ("auto" as const),
    opacity: isDragging ? 0.5 : 1,
    boxShadow: isDragging ? "0 8px 24px rgba(0,0,0,0.20)" : undefined,
  } as React.CSSProperties;

  return (
    <div
      ref={setNodeRef}
      style={style}
      className={"group/link flex items-center gap-3 rounded-lg border bg-background/60 hover:bg-accent/50 transition-all p-3"}
    >
      <button
        {...attributes}
        {...listeners}
        className="cursor-grab active:cursor-grabbing p-1 -ml-1 rounded hover:bg-accent/50 transition-colors touch-none"
        onClick={(e) => e.stopPropagation()}
      >
        <GripVertical className="h-4 w-4 text-muted-foreground opacity-50 group-hover/link:opacity-100 transition-opacity" />
      </button>
      <div className="flex-1 min-w-0">
        <div className="flex items-center gap-2">
          <a
            href={item.url}
            target="_blank"
            rel="noreferrer"
            className="font-medium text-sm truncate hover:text-primary transition-colors"
            onClick={(e) => e.stopPropagation()}
          >
            {item.name}
          </a>
          <ExternalLink className="h-3 w-3 text-muted-foreground flex-shrink-0" />
        </div>
        {item.description && (
          <p className="text-xs text-muted-foreground mt-1 truncate">{item.description}</p>
        )}
      </div>
      <div className="flex items-center gap-1">
        <Button
          size="icon"
          variant="ghost"
          className="h-7 w-7 opacity-0 group-hover/link:opacity-100 transition-opacity"
          onClick={(e) => {
            e.stopPropagation();
            onEdit(item);
          }}
        >
          <Edit3 className="h-4 w-4" />
        </Button>
        <Button
          size="icon"
          variant="ghost"
          className="h-7 w-7 opacity-0 group-hover/link:opacity-100 transition-opacity text-destructive hover:text-destructive"
          onClick={(e) => {
            e.stopPropagation();
            onDelete(item);
          }}
        >
          <Trash2 className="h-4 w-4" />
        </Button>
      </div>
    </div>
  );
}

/* ---------- Sortable Group Header ---------- */
function SortableGroupHeader({ id, name, description, onCreateLink, onDeleteGroup, onEditGroup, canCreateLink }: {
  id: string;
  name: string;
  description?: string | null;
  onCreateLink: (groupId: string) => void;
  onDeleteGroup?: (groupId: string) => void;
  onEditGroup?: (groupId: string) => void;
  canCreateLink?: boolean;
}) {
  const { attributes, listeners, setNodeRef, transform, transition, isDragging } = useSortable({ 
    id: `group-${id}`,
  });
  
  const style = {
    transform: CSS.Transform.toString(transform),
    transition,
    zIndex: isDragging ? 50 : ("auto" as const),
  } as React.CSSProperties;

  return (
    <div
      ref={setNodeRef}
      style={style}
      className={`flex items-start justify-between w-full gap-3 ${isDragging ? 'opacity-40' : ''}`}
    >
      <div className="flex-1 text-left">
        <div className="flex items-center gap-2">
          <button
            {...attributes}
            {...listeners}
            className="cursor-grab active:cursor-grabbing p-1 -ml-1 rounded hover:bg-accent/50 transition-colors touch-none"
            onClick={(e) => e.stopPropagation()}
          >
            <GripVertical className="h-4 w-4 text-muted-foreground opacity-70 hover:opacity-100 transition-opacity" />
          </button>
          <div className="h-2 w-2 rounded-full bg-gradient-to-r from-primary to-blue-500" />
          <span className="font-semibold no-underline hover:no-underline decoration-none hover:decoration-none" style={{ textDecoration: 'none' }}>{name}</span>
        </div>
        {description ? <p className="text-xs text-muted-foreground mt-1 ml-9">{description}</p> : null}
      </div>
      <div className="flex items-center gap-1">
        <Button 
          size="sm" 
          variant="ghost" 
          onClick={(e) => { e.stopPropagation(); if (canCreateLink !== false) onCreateLink(id); }} 
          disabled={canCreateLink === false}
          className={canCreateLink === false ? "opacity-50 cursor-not-allowed" : ""}
        >
          <Plus className="h-4 w-4" />
        </Button>
        {onEditGroup ? (
          <Button size="sm" variant="ghost" onClick={(e) => { e.stopPropagation(); onEditGroup(id); }}>
            <Edit3 className="h-4 w-4" />
          </Button>
        ) : null}
        {onDeleteGroup ? (
          <Button size="sm" variant="ghost" onClick={(e) => { e.stopPropagation(); onDeleteGroup(id); }}>
            <Trash2 className="h-4 w-4" />
          </Button>
        ) : null}
      </div>
    </div>
  );
}

/* ---------- Group section (droppable + sortable container) ---------- */
function GroupSection({
  id,
  name,
  description,
  items,
  onCreateLink,
  onDeleteGroup,
  onEditGroup,
  onEdit,
  onDelete,
  onEmptyDropHint,
  canCreateLink,
}: {
  id: string | null; // null for ungrouped
  name: string;
  description?: string | null;
  items: LinkItem[];
  onCreateLink: (groupId: string | null) => void;
  onDeleteGroup?: (groupId: string) => void;
  onEditGroup?: (groupId: string) => void;
  onEdit: (l: LinkItem) => void;
  onDelete: (l: LinkItem) => void;
  onEmptyDropHint?: string;
  canCreateLink?: boolean;
}) {
  const containerId = id ?? "__ungrouped__";
  const { setNodeRef, isOver } = useDroppable({ id: containerId });

  return (
    <AccordionItem value={containerId} className="rounded-xl border bg-card">
  <AccordionTrigger className="px-4 no-underline hover:no-underline">
        {id ? (
          <SortableGroupHeader 
            id={id}
            name={name}
            description={description}
            onCreateLink={onCreateLink}
            onDeleteGroup={onDeleteGroup}
            onEditGroup={onEditGroup}
            canCreateLink={canCreateLink}
          />
        ) : (
          <div className="flex items-start justify-between w-full gap-3">
            <div className="flex-1 text-left">
              <div className="flex items-center gap-2">
                <div className="h-2 w-2 rounded-full bg-muted-foreground" />
                <span className="font-semibold no-underline hover:no-underline decoration-none hover:decoration-none" style={{ textDecoration: 'none' }}>{name}</span>
              </div>
              {description ? <p className="text-xs text-muted-foreground mt-1">{description}</p> : null}
            </div>
            <div className="flex items-center gap-1">
              <Button 
                size="sm" 
                variant="ghost" 
                onClick={(e) => { e.stopPropagation(); if (canCreateLink !== false) onCreateLink(id); }}
                disabled={canCreateLink === false}
                className={canCreateLink === false ? "opacity-50 cursor-not-allowed" : ""}
              >
                <Plus className="h-4 w-4" />
              </Button>
            </div>
          </div>
        )}
      </AccordionTrigger>
      <AccordionContent className="px-4 pb-4">
        <SortableContext items={items.map((i) => i.id)} strategy={verticalListSortingStrategy}>
          <div
            ref={setNodeRef}
            className={`space-y-2 min-h-[84px] rounded-lg transition ring-offset-2 ${isOver ? "ring-2 ring-primary/40" : ""}`}
          >
            <AnimatePresence initial={false}>
              {items.map((link) => (
                <motion.div key={link.id} variants={cardVariants} initial="hidden" animate="visible" exit="hidden">
                  <SortableLinkRow item={link} onEdit={onEdit} onDelete={onDelete} />
                </motion.div>
              ))}
            </AnimatePresence>

            {items.length === 0 && (
              <div className="flex items-center justify-center h-24 border-2 border-dashed rounded-lg">
                <div className="text-center">
                  <Plus className="h-6 w-6 text-muted-foreground mx-auto mb-2" />
                  <p className="text-sm text-muted-foreground">{onEmptyDropHint ?? "Drop links here"}</p>
                </div>
              </div>
            )}
          </div>
        </SortableContext>
      </AccordionContent>
    </AccordionItem>
  );
}

/* ---------- Main Page ---------- */
export default function LinksPage() {
  const { user, isAuthenticated } = useUser();
  const { post } = useApi();
  const { data: groupedData, loading: loadingLinks, error: linksError, refetch: refetchLinks } =
    useGet<GetGroupedLinksResponse>("/link");
  
  // Fetch full user profile data to get bio and other fields for preview
  type UserPublic = { id: string; username: string; firstName?: string | null; lastName?: string | null; avatarUrl?: string | null; coverUrl?: string | null; bio?: string | null };
  type GetUserPublicResponse = { data: UserPublic; meta: any | null };
  const { data: userProfileData } = useGet<GetUserPublicResponse>(
    user?.username ? `/user/${encodeURIComponent(user.username)}/public` : ''
  );

  // create/edit state
  const [isCreating, setIsCreating] = useState(false);
  const [editingLinkId, setEditingLinkId] = useState<string | null>(null);
  const [form, setForm] = useState<CreateOrEditLinkRequest>({
    name: "",
    url: "",
    description: "",
    groupId: null,
    sequence: 0,
    isActive: true,
  });
  const [urlProtocol, setUrlProtocol] = useState<UrlProtocol>("https://");
  const [urlRemainder, setUrlRemainder] = useState("");
  const [isCustomUrl, setIsCustomUrl] = useState(false);

  // group create modal
  const [isCreatingGroup, setIsCreatingGroup] = useState(false);
  const [groupForm, setGroupForm] = useState<{ name: string; description?: string | null; sequence?: number }>({
    name: "",
    description: "",
    sequence: 0,
  });

  // group edit modal
  const [editingGroupId, setEditingGroupId] = useState<string | null>(null);
  const [groupEditForm, setGroupEditForm] = useState<UpdateGroupRequest>({
    name: "",
    description: "",
    sequence: 0,
    isActive: true,
  });

  // mobile preview modal
  const [isPreviewModalOpen, setIsPreviewModalOpen] = useState(false);

  // share modal
  const [isShareModalOpen, setIsShareModalOpen] = useState(false);
  const [copiedPlatform, setCopiedPlatform] = useState<string | null>(null);
  const [qrCodeDataUrl, setQrCodeDataUrl] = useState<string | null>(null);
  const [isGeneratingQrCode, setIsGeneratingQrCode] = useState(false);
  const [isDownloadingQrCard, setIsDownloadingQrCard] = useState(false);
  const [qrCodeError, setQrCodeError] = useState<string | null>(null);

  const getPrimaryColorHex = () => {
    return PRIMARY_COLOR_FALLBACK;
  };

  // delete confirmation modal
  const [deleteConfirmOpen, setDeleteConfirmOpen] = useState(false);
  const [itemToDelete, setItemToDelete] = useState<{ type: 'link' | 'group'; id: string; name: string } | null>(null);

  // accordion state for collapse/expand all
  const [openAccordionItems, setOpenAccordionItems] = useState<string[]>([]);

  // Local optimistic structure
  const [localGroups, setLocalGroups] = useState<
    { id: string; name: string; description?: string | null; links: LinkItem[] }[]
  >([]);
  const [localUngrouped, setLocalUngrouped] = useState<LinkItem[]>([]);

  // drag state
  const [, setActiveId] = useState<string | null>(null);
  const activeItemRef = useRef<LinkItem | null>(null);
  const metadataAttemptedUrlsRef = useRef<Set<string>>(new Set());

  useEffect(() => {
    if (linksError) {
      console.warn("Links API error:", linksError);
      toast.error("Could not load links.");
    }
  }, [linksError]);

  // hydrate local state from API
  useEffect(() => {
    if (!groupedData?.data) return;
    const groups = [...groupedData.data.groups]
      .sort((a, b) => a.sequence - b.sequence) // Sort groups by sequence
      .map((g) => ({
        id: g.id,
        name: g.name,
        description: g.description,
        links: [...g.links].sort((a, b) => a.sequence - b.sequence),
      }));
    setLocalGroups(groups);
    setLocalUngrouped([...groupedData.data.ungrouped.links].sort((a, b) => a.sequence - b.sequence));
    
    // Initialize accordion with all items open by default
    setOpenAccordionItems([...groups.map((g) => g.id), "__ungrouped__"]);
  }, [groupedData]);

  useEffect(() => {
    if (!isCreating || editingLinkId) return;

    const urlValue = form.url?.trim();
    if (!urlValue) return;

    let parsedUrl: URL;
    try {
      parsedUrl = new URL(urlValue);
    } catch {
      return;
    }

    if (!["http:", "https:"].includes(parsedUrl.protocol)) return;
    if (!parsedUrl.hostname || !parsedUrl.hostname.includes(".")) return;
    if (form.description?.trim()) return;

    const normalizedUrl = parsedUrl.toString();
    if (metadataAttemptedUrlsRef.current.has(normalizedUrl)) return;

    metadataAttemptedUrlsRef.current.add(normalizedUrl);

    const controller = new AbortController();
    let cancelled = false;

    const fetchMetadata = async () => {
      try {
        const response = await fetch("/api/link-metadata", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ url: normalizedUrl }),
          signal: controller.signal,
        });

        if (!response.ok) return;

        const data = (await response.json()) as { description?: string | null };
        const fetchedDescription = typeof data?.description === "string" ? data.description.trim() : "";
        if (!fetchedDescription || cancelled) return;

        const limitedDescription =
          fetchedDescription.length > MAX_AUTO_DESCRIPTION_LENGTH
            ? `${fetchedDescription.slice(0, MAX_AUTO_DESCRIPTION_LENGTH).trim()}`
            : fetchedDescription;

        setForm((prev) => {
          if (prev.description?.trim()) {
            return prev;
          }
          return { ...prev, description: limitedDescription };
        });
      } catch (error) {
        if ((error as Error).name !== "AbortError") {
          console.warn("Failed to fetch link metadata description", error);
        }
      }
    };

    fetchMetadata();

    return () => {
      cancelled = true;
      controller.abort();
    };
  }, [editingLinkId, form.description, form.url, isCreating]);

  // Calculate tier limits
  const totalLinks = useMemo(() => {
    const groupedLinks = localGroups.reduce((sum, group) => sum + group.links.length, 0);
    return groupedLinks + localUngrouped.length;
  }, [localGroups, localUngrouped]);

  const totalGroups = useMemo(() => localGroups.length, [localGroups]);

  const isFreeTier = userHelpers.isFreeTier(user);
  const canCreateLink = !isFreeTier || totalLinks < 12;
  const canCreateGroup = !isFreeTier || totalGroups < 2;

  const findItem = (id: string): { from: "group" | "ungrouped"; groupId: string | null; index: number } | null => {
    const unIdx = localUngrouped.findIndex((l) => l.id === id);
    if (unIdx > -1) return { from: "ungrouped", groupId: null, index: unIdx };
    for (const g of localGroups) {
      const idx = g.links.findIndex((l) => l.id === id);
      if (idx > -1) return { from: "group", groupId: g.id, index: idx };
    }
    return null;
  };

  /* ---------- DnD sensors ---------- */
  const sensors = useSensors(
    useSensor(PointerSensor, { activationConstraint: { distance: 4 } }),
    useSensor(TouchSensor, { activationConstraint: { delay: 75, tolerance: 8 } }),
    useSensor(KeyboardSensor)
  );

  const allContainerIds = useMemo(() => {
    return ["__ungrouped__", ...localGroups.map((g) => g.id)];
  }, [localGroups]);

  const getContainerFromLinkId = (id: string) => {
    const meta = findItem(id);
    if (!meta) return null;
    return meta.groupId ?? "__ungrouped__";
  };

  const onDragStart = (event: DragStartEvent) => {
    const id = String(event.active.id);
    setActiveId(id);
    
    // Check if it's a group being dragged
    if (id.startsWith('group-')) {
      // Group drag - no need to set activeItemRef for groups
      return;
    }
    
    // Link drag
    const meta = findItem(id);
    if (!meta) return;
    activeItemRef.current =
      meta.from === "ungrouped" ? localUngrouped[meta.index] : localGroups.find((g) => g.id === meta.groupId)!.links[meta.index];
  };

  // const buildResequencePayload = () => {
  //   const payload: { id: string; groupId: string | null; sequence: number }[] = [];
  //   localGroups.forEach((g) => {
  //     g.links.forEach((l, i) => payload.push({ id: l.id, groupId: g.id, sequence: i }));
  //   });
  //   localUngrouped.forEach((l, i) => payload.push({ id: l.id, groupId: null, sequence: i }));
  //   return payload;
  // };

  const onDragEnd = async (event: DragEndEvent) => {
    const { active, over } = event;
    setActiveId(null);
    if (!over) return;

    const activeIdStr = String(active.id);
    const overIdStr = String(over.id);

    // Handle group reordering
    if (activeIdStr.startsWith('group-') && overIdStr.startsWith('group-')) {
      const activeGroupId = activeIdStr.replace('group-', '');
      const overGroupId = overIdStr.replace('group-', '');
      
      const oldIndex = localGroups.findIndex(g => g.id === activeGroupId);
      const newIndex = localGroups.findIndex(g => g.id === overGroupId);
      
      if (oldIndex === -1 || newIndex === -1 || oldIndex === newIndex) return;
      
      // Calculate final group order
      const finalGroupOrder = arrayMove(localGroups, oldIndex, newIndex);
      
      // Build group resequence payload
      const groupResequencePayload: GroupResequenceItemRequest[] = finalGroupOrder.map((group, index) => ({
        id: group.id,
        sequence: index
      }));

      try {
        await post("/group/resequence", groupResequencePayload);
        await refetchLinks(); // Refresh data from server
        toast.success("Groups reordered");
      } catch (e: any) {
        toast.error(e?.data?.title || "Failed to reorder groups");
      }
      return;
    }

    // Handle link reordering (existing logic)
    // over.id can be a container id or another item id
    const activeContainer = getContainerFromLinkId(activeIdStr);
    const overContainer = allContainerIds.includes(overIdStr) ? overIdStr : getContainerFromLinkId(overIdStr);

    if (!activeContainer || !overContainer) return;

    // BUILD PAYLOAD FROM INTENDED FINAL STATE - DON'T USE OPTIMISTIC UPDATES
    const resequencePayload: { id: string; groupId: string | null; sequence: number }[] = [];

    // Cross-container move
    if (activeContainer !== overContainer) {
      let moved: LinkItem | null = null;

      // Find the moved item
      if (activeContainer === "__ungrouped__") {
        const idx = localUngrouped.findIndex((l) => l.id === activeIdStr);
        if (idx > -1) moved = localUngrouped[idx];
      } else {
        const group = localGroups.find((g) => g.id === activeContainer);
        if (group) {
          const idx = group.links.findIndex((l) => l.id === activeIdStr);
          if (idx > -1) moved = group.links[idx];
        }
      }

      if (!moved) return;

      // Build payload for cross-container move
      // 1. Add moved item to target container at position 0
      const targetGroupId = overContainer === "__ungrouped__" ? null : overContainer;
      resequencePayload.push({ id: moved.id, groupId: targetGroupId, sequence: 0 });

      // 2. Add existing items in target container, shifted by 1
      if (overContainer === "__ungrouped__") {
        localUngrouped.forEach((link, i) => {
          resequencePayload.push({ id: link.id, groupId: null, sequence: i + 1 });
        });
      } else {
        const targetGroup = localGroups.find((g) => g.id === overContainer);
        if (targetGroup) {
          targetGroup.links.forEach((link, i) => {
            resequencePayload.push({ id: link.id, groupId: overContainer, sequence: i + 1 });
          });
        }
      }

      // 3. Resequence source container (excluding moved item)
      if (activeContainer === "__ungrouped__") {
        localUngrouped
          .filter((l) => l.id !== activeIdStr)
          .forEach((link, i) => {
            resequencePayload.push({ id: link.id, groupId: null, sequence: i });
          });
      } else {
        const sourceGroup = localGroups.find((g) => g.id === activeContainer);
        if (sourceGroup) {
          sourceGroup.links
            .filter((l) => l.id !== activeIdStr)
            .forEach((link, i) => {
              resequencePayload.push({ id: link.id, groupId: activeContainer, sequence: i });
            });
        }
      }

      try {
        await post("/link/resequence", resequencePayload);
        await refetchLinks(); // Refresh data from server
        toast.success("Link moved");
      } catch (e: any) {
        toast.error(e?.data?.title || "Failed to move link");
      }
      return;
    }

    // Same container reorder - calculate final order
    let finalOrder: LinkItem[] = [];
    if (activeContainer === "__ungrouped__") {
      const oldIndex = localUngrouped.findIndex((i) => i.id === activeIdStr);
      const newIndex = localUngrouped.findIndex((i) => i.id === overIdStr);
      if (oldIndex === -1 || newIndex === -1 || oldIndex === newIndex) return;
      finalOrder = arrayMove(localUngrouped, oldIndex, newIndex);
      
      // Build payload from final order
      finalOrder.forEach((link, i) => {
        resequencePayload.push({ id: link.id, groupId: null, sequence: i });
      });
    } else {
      const group = localGroups.find((g) => g.id === activeContainer);
      if (!group) return;
      
      const oldIndex = group.links.findIndex((i) => i.id === activeIdStr);
      const newIndex = group.links.findIndex((i) => i.id === overIdStr);
      if (oldIndex === -1 || newIndex === -1 || oldIndex === newIndex) return;
      finalOrder = arrayMove(group.links, oldIndex, newIndex);
      
      // Build payload from final order
      finalOrder.forEach((link, i) => {
        resequencePayload.push({ id: link.id, groupId: activeContainer, sequence: i });
      });
    }

    // Persist resequence
    try {
      await post("/link/resequence", resequencePayload);
      await refetchLinks(); // Refresh data from server
    } catch (e: any) {
      toast.error(e?.data?.title || "Failed to save order");
    }
  };

  // Share functionality
  const getShareUrl = (platform: string) => {
    if (!user?.username) return '';
    return `https://${user.username}.linqyard.com?src=${platform}`;
  };

  useEffect(() => {
    if (!isShareModalOpen) {
      setQrCodeDataUrl(null);
      setQrCodeError(null);
      return;
    }

    if (!user?.username) {
      setQrCodeError("Missing username for QR code");
      setQrCodeDataUrl(null);
      return;
    }

    let cancelled = false;

    const generateQrCode = async () => {
      setIsGeneratingQrCode(true);
      setQrCodeError(null);
      setQrCodeDataUrl(null);

      try {
        const shareUrlForQr = `https://${user.username}.linqyard.com?src=qr`;
        const primaryColorHex = getPrimaryColorHex();
        const dataUrl = await QRCode.toDataURL(shareUrlForQr, {
          margin: 1,
          width: 600,
          color: {
            dark: primaryColorHex,
            light: "#ffffff",
          },
        });

        if (!cancelled) {
          setQrCodeDataUrl(dataUrl);
        }
      } catch (error) {
        console.error("Failed to generate QR code", error);
        if (!cancelled) {
          setQrCodeError("We couldn't generate your QR code.");
          toast.error("Failed to generate QR code");
        }
      } finally {
        if (!cancelled) {
          setIsGeneratingQrCode(false);
        }
      }
    };

    generateQrCode();

    return () => {
      cancelled = true;
    };
  }, [isShareModalOpen, user?.username]);

  const copyToClipboard = async (platform: string) => {
    const url = getShareUrl(platform);
    try {
      await navigator.clipboard.writeText(url);
      setCopiedPlatform(platform);
      toast.success(`Link copied to clipboard!`);
      setTimeout(() => setCopiedPlatform(null), 2000);
    } catch (err) {
      toast.error("Failed to copy link");
    }
  };

  const shareOnPlatform = (platform: 'whatsapp' | 'telegram' | 'twitter') => {
    const url = getShareUrl(platform);
    const text = `Check out my Linqyard profile!`;
    
    let shareUrl = '';
    switch (platform) {
      case 'whatsapp':
        shareUrl = `https://wa.me/?text=${encodeURIComponent(`${text} ${url}`)}`;
        break;
      case 'telegram':
        shareUrl = `https://t.me/share/url?url=${encodeURIComponent(url)}&text=${encodeURIComponent(text)}`;
        break;
      case 'twitter':
        shareUrl = `https://twitter.com/intent/tweet?text=${encodeURIComponent(text)}&url=${encodeURIComponent(url)}`;
        break;
    }
    
    if (shareUrl) {
      window.open(shareUrl, '_blank', 'noopener,noreferrer');
    }
  };

  const resetUrlState = () => {
    setUrlProtocol("https://");
    setUrlRemainder("");
    setIsCustomUrl(false);
  };

  const applyUrlState = (protocol: UrlProtocol, remainder: string) => {
    setUrlProtocol(protocol);
    setUrlRemainder(remainder);
    setIsCustomUrl(false);
    setForm((prev) => ({
      ...prev,
      url: remainder ? `${protocol}${remainder}` : "",
    }));
  };

  const applyCustomUrl = (value: string) => {
    setIsCustomUrl(true);
    setUrlRemainder("");
    setForm((prev) => ({
      ...prev,
      url: value,
    }));
  };

  const syncUrlStateFromFull = (full: string) => {
    if (!full) {
      resetUrlState();
      return;
    }

    const trimmed = full.trim();
    const lower = trimmed.toLowerCase();

    if (lower.startsWith("https://")) {
      setUrlProtocol("https://");
      setUrlRemainder(trimmed.slice("https://".length));
      setIsCustomUrl(false);
      return;
    }

    if (lower.startsWith("http://")) {
      setUrlProtocol("http://");
      setUrlRemainder(trimmed.slice("http://".length));
      setIsCustomUrl(false);
      return;
    }

    if (/^[a-z][a-z\d+\-.]*:/i.test(trimmed)) {
      applyCustomUrl(trimmed);
      return;
    }

    applyUrlState("https://", trimmed);
  };

  const handleUrlRemainderChange = (value: string) => {
    const raw = value.trim();

    if (!raw) {
      applyUrlState(urlProtocol, "");
      return;
    }

    const lower = raw.toLowerCase();

    if (lower.startsWith("https://")) {
      applyUrlState("https://", raw.slice("https://".length));
      return;
    }

    if (lower.startsWith("http://")) {
      applyUrlState("http://", raw.slice("http://".length));
      return;
    }

    if (/^[a-z][a-z\d+\-.]*:/i.test(raw)) {
      applyCustomUrl(raw);
      return;
    }

    applyUrlState(urlProtocol, raw);
  };

  const handleUrlPaste = (event: React.ClipboardEvent<HTMLInputElement>) => {
    const pasted = event.clipboardData.getData("text");
    if (!pasted) {
      return;
    }

    const trimmed = pasted.trim();
    event.preventDefault();

    if (!trimmed) {
      applyUrlState(urlProtocol, "");
      return;
    }

    const lower = trimmed.toLowerCase();

    if (lower.startsWith("https://")) {
      applyUrlState("https://", trimmed.slice("https://".length));
      return;
    }

    if (lower.startsWith("http://")) {
      applyUrlState("http://", trimmed.slice("http://".length));
      return;
    }

    if (/^[a-z][a-z\d+\-.]*:/i.test(trimmed)) {
      applyCustomUrl(trimmed);
      return;
    }

    applyUrlState(urlProtocol, trimmed);
  };

  const handleCustomUrlChange = (value: string) => {
    const raw = value.trim();

    if (!raw) {
      resetUrlState();
      setForm((prev) => ({
        ...prev,
        url: "",
      }));
      return;
    }

    const lower = raw.toLowerCase();

    if (lower.startsWith("https://")) {
      applyUrlState("https://", raw.slice("https://".length));
      return;
    }

    if (lower.startsWith("http://")) {
      applyUrlState("http://", raw.slice("http://".length));
      return;
    }

    if (/^[a-z][a-z\d+\-.]*:/i.test(raw)) {
      applyCustomUrl(raw);
      return;
    }

    applyUrlState("https://", raw);
  };

  const handleCustomUrlPaste = (event: React.ClipboardEvent<HTMLInputElement>) => {
    const pasted = event.clipboardData.getData("text");
    if (!pasted) {
      return;
    }

    const trimmed = pasted.trim();
    event.preventDefault();

    if (!trimmed) {
      resetUrlState();
      setForm((prev) => ({
        ...prev,
        url: "",
      }));
      return;
    }

    const lower = trimmed.toLowerCase();

    if (lower.startsWith("https://")) {
      applyUrlState("https://", trimmed.slice("https://".length));
      return;
    }

    if (lower.startsWith("http://")) {
      applyUrlState("http://", trimmed.slice("http://".length));
      return;
    }

    if (/^[a-z][a-z\d+\-.]*:/i.test(trimmed)) {
      applyCustomUrl(trimmed);
      return;
    }

    applyUrlState("https://", trimmed);
  };

  const loadImage = (src: string) =>
    new Promise<HTMLImageElement>((resolve, reject) => {
      const img = new window.Image();
      img.crossOrigin = "anonymous";
      img.onload = () => resolve(img);
      img.onerror = () => reject(new Error("Failed to load image"));
      img.src = src;
    });

  const drawRoundedRect = (
    context: CanvasRenderingContext2D,
    x: number,
    y: number,
    width: number,
    height: number,
    radius: number
  ) => {
    const r = Math.min(radius, width / 2, height / 2);
    context.beginPath();
    context.moveTo(x + r, y);
    context.lineTo(x + width - r, y);
    context.quadraticCurveTo(x + width, y, x + width, y + r);
    context.lineTo(x + width, y + height - r);
    context.quadraticCurveTo(x + width, y + height, x + width - r, y + height);
    context.lineTo(x + r, y + height);
    context.quadraticCurveTo(x, y + height, x, y + height - r);
    context.lineTo(x, y + r);
    context.quadraticCurveTo(x, y, x + r, y);
    context.closePath();
    context.fill();
  };

  const downloadQrCard = async () => {
    if (!qrCodeDataUrl || !user?.username) {
      toast.error("QR code is not ready yet");
      return;
    }

    setIsDownloadingQrCard(true);
    try {
      if (typeof document === "undefined" || typeof window === "undefined") {
        throw new Error("Download is only available in the browser");
      }
      const primaryColorHex = getPrimaryColorHex();
      const canvas = document.createElement("canvas");
      canvas.width = 900;
      canvas.height = 1400;
      const ctx = canvas.getContext("2d");
      if (!ctx) throw new Error("Canvas context unavailable");

      // Background
      ctx.fillStyle = "#f8fafc";
      ctx.fillRect(0, 0, canvas.width, canvas.height);

      // Title
      ctx.fillStyle = "#0f172a";
      ctx.font = "600 56px 'Inter', 'Segoe UI', sans-serif";
      ctx.textAlign = "center";
      ctx.fillText("Linqyard Profile", canvas.width / 2, 110);

      // QR code container
      const qrImage = await loadImage(qrCodeDataUrl);
      let logoImage: HTMLImageElement | null = null;
      try {
        const logoUrl = new URL("/logo.svg", window.location.origin).toString();
        logoImage = await loadImage(logoUrl);
      } catch (logoError) {
        console.warn("Failed to load watermark logo", logoError);
      }
      const qrSize = 640;
      const qrX = (canvas.width - qrSize) / 2;
      const qrY = 200;

      ctx.shadowColor = "rgba(15, 23, 42, 0.16)";
      ctx.shadowBlur = 48;
      ctx.shadowOffsetY = 24;
      ctx.fillStyle = "#ffffff";
      drawRoundedRect(ctx, qrX - 36, qrY - 36, qrSize + 72, qrSize + 72, 40);
      ctx.shadowColor = "transparent";

      ctx.drawImage(qrImage, qrX, qrY, qrSize, qrSize);

      // Username
      ctx.fillStyle = "#111827";
      ctx.font = "700 52px 'Inter', 'Segoe UI', sans-serif";
      ctx.fillText(`@${user.username}`, canvas.width / 2, qrY + qrSize + 130);

      // Subtitle
      ctx.fillStyle = "#475569";
      ctx.font = "400 30px 'Inter', 'Segoe UI', sans-serif";
      ctx.fillText("Scan to visit my Linqyard links", canvas.width / 2, qrY + qrSize + 190);

      if (logoImage) {
        const naturalWidth = logoImage.naturalWidth || logoImage.width || 1;
        const naturalHeight = logoImage.naturalHeight || logoImage.height || 1;
        const logoWidth = 280;
        const logoHeight = (logoWidth / naturalWidth) * naturalHeight;
        const logoX = (canvas.width - logoWidth) / 2;
        const baseY = qrY + qrSize + 300;
        const maxY = canvas.height - logoHeight - 120;
        const logoY = Math.min(maxY, baseY);

        ctx.save();
        ctx.globalAlpha = 0.22;
        ctx.drawImage(logoImage, logoX, logoY, logoWidth, logoHeight);
        ctx.restore();
      }

      // Direct URL
      const directLink = `https://${user.username}.linqyard.com`;
      ctx.fillStyle = primaryColorHex;
      ctx.font = "600 30px 'Inter', 'Segoe UI', sans-serif";
      ctx.fillText(directLink, canvas.width / 2, qrY + qrSize + 240);

      ctx.fillStyle = "#475569";
      ctx.font = "500 24px 'Inter', 'Segoe UI', sans-serif";
      ctx.fillText("Powered by linqyard.com", canvas.width / 2, qrY + qrSize + 300);

      const link = document.createElement("a");
      link.href = canvas.toDataURL("image/png");
      link.download = `${user.username}-linqyard-qr.png`;
      link.click();
      toast.success("QR card downloaded");
    } catch (error) {
      console.error("Failed to download QR card", error);
      toast.error("Failed to download QR card");
    } finally {
      setIsDownloadingQrCard(false);
    }
  };

  if (!isAuthenticated || !user) return <AccessDenied />;

  const dragOverlayItem = activeItemRef.current;

  return (
    <div className="min-h-screen bg-background">
      <div className="container mx-auto px-4 py-8 max-w-6xl">
        <motion.div variants={containerVariants} initial="hidden" animate="visible" className="space-y-6">
          <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
            <div className="lg:col-span-2">
              <div>
                <Link
                  href="/account/profile"
                  className="inline-flex items-center text-sm text-muted-foreground hover:text-foreground transition-colors"
                >
                  <ArrowLeft className="h-4 w-4 mr-2" />
                  Back to Profile
                </Link>
              </div>

              <div className="flex flex-col sm:flex-row sm:items-center gap-4 pb-6">
                <div className="h-8 w-8 rounded-full bg-primary/10 flex items-center justify-center">
                  <Globe className="h-4 w-4 text-primary" />
                </div>
                <div>
                  <h1 className="text-3xl font-bold">Links</h1>
                  <p className="text-muted-foreground text-sm mt-1 mb-3 sm:mb-0">Drag to reorder. Drop into any group.</p>
                </div>
                <div className="sm:ml-auto flex items-center gap-2">
                  {/* Button Group - Desktop & Mobile */}
                  <div className="flex items-center border rounded-lg overflow-hidden">
                    <Button 
                      size="sm" 
                      variant="ghost"
                      className="rounded-none border-r h-9"
                      onClick={() => {
                        const allIds = [...localGroups.map((g) => g.id), "__ungrouped__"];
                        setOpenAccordionItems(openAccordionItems.length === allIds.length ? [] : allIds);
                      }}
                    >
                      <ChevronsUpDown className="h-4 w-4 sm:mr-2" />
                      <span className="hidden sm:inline">
                        {openAccordionItems.length === 0 ? "Expand All" : "Collapse All"}
                      </span>
                    </Button>
                    
                    <Button 
                      size="sm" 
                      variant="ghost"
                      className="rounded-none border-r h-9 sm:hidden"
                      onClick={() => setIsPreviewModalOpen(true)}
                    >
                      <Globe className="h-4 w-4" />
                    </Button>
                    
                    <Button 
                      size="sm" 
                      variant="ghost"
                      className="rounded-none h-9"
                      onClick={() => setIsShareModalOpen(true)}
                    >
                      <Share2 className="h-4 w-4 sm:mr-2" />
                      <span className="hidden sm:inline">Share</span>
                    </Button>
                  </div>
                  
                  <DropdownMenu>
                    <DropdownMenuTrigger asChild>
                      <Button size="sm" className="h-9">
                        <Plus className="h-4 w-4 mr-2" />
                        New
                      </Button>
                    </DropdownMenuTrigger>
                    <DropdownMenuContent align="end">
                      <DropdownMenuItem 
                        onClick={() => canCreateLink && startCreate(null)}
                        disabled={!canCreateLink}
                        className={!canCreateLink ? "opacity-50 cursor-not-allowed" : ""}
                      >
                        <Plus className="h-4 w-4 mr-2" />
                        New Link
                        {!canCreateLink && (
                          <span className="ml-2 text-xs text-muted-foreground">
                            (Limit: 12)
                          </span>
                        )}
                      </DropdownMenuItem>
                      <DropdownMenuItem 
                        onClick={() => {
                          if (!canCreateGroup) {
                            toast.error("Free tier limit reached: Maximum 2 groups allowed", {
                              description: "Upgrade to Plus or Pro for unlimited groups"
                            });
                            return;
                          }
                          setIsCreatingGroup(true);
                        }}
                        disabled={!canCreateGroup}
                        className={!canCreateGroup ? "opacity-50 cursor-not-allowed" : ""}
                      >
                        <FolderPlus className="h-4 w-4 mr-2" />
                        New Group
                        {!canCreateGroup && (
                          <span className="ml-2 text-xs text-muted-foreground">
                            (Limit: 2)
                          </span>
                        )}
                      </DropdownMenuItem>
                    </DropdownMenuContent>
                  </DropdownMenu>
                </div>
              </div>

              

              {/* Editor content (same as before) */}
              <div>
                {loadingLinks ? (
                  <motion.div variants={cardVariants} className="py-16 text-center">
                    <div className="h-8 w-8 border-2 border-primary border-t-transparent rounded-full animate-spin mx-auto mb-4" />
                    <p className="text-muted-foreground">Loading your links...</p>
                  </motion.div>
                ) : !groupedData ? (
                  <motion.div variants={cardVariants} className="py-16 text-center">
                    <div className="h-16 w-16 rounded-full bg-muted/20 flex items-center justify-center mx-auto mb-6">
                      <Globe className="h-8 w-8 text-muted-foreground" />
                    </div>
                    <h3 className="text-lg font-semibold mb-2">No links yet</h3>
                    <p className="text-muted-foreground mb-6">Start by creating your first link or group</p>
                    <div className="flex items-center justify-center gap-3">
                      <Button 
                        onClick={() => {
                          if (!canCreateGroup) {
                            toast.error("Free tier limit reached: Maximum 2 groups allowed", {
                              description: "Upgrade to Plus or Pro for unlimited groups"
                            });
                            return;
                          }
                          setIsCreatingGroup(true);
                        }} 
                        variant="outline"
                        disabled={!canCreateGroup}
                      >
                        <FolderPlus className="h-4 w-4 mr-2" />
                        Create Group
                      </Button>
                      <Button onClick={() => startCreate(null)}>
                        <Plus className="h-4 w-4 mr-2" />
                        Create Link
                      </Button>
                    </div>
                  </motion.div>
                ) : (
                  <>
                    <DndContext
                      sensors={sensors}
                      collisionDetection={pointerWithin}
                      onDragStart={onDragStart}
                      onDragEnd={onDragEnd}
                      modifiers={[restrictToVerticalAxis]}
                      measuring={{ droppable: { strategy: MeasuringStrategy.Always } }}
                    >
                      <SortableContext items={localGroups.map((g) => `group-${g.id}`)} strategy={verticalListSortingStrategy}>
                        <Accordion 
                          type="multiple" 
                          value={openAccordionItems}
                          onValueChange={setOpenAccordionItems}
                          className="space-y-3"
                        >
                          <GroupSection
                            id={null}
                            name="Ungrouped"
                            description="Links without a group"
                            items={localUngrouped}
                            onCreateLink={startCreate}
                            onEdit={startEdit}
                            onDelete={handleDeleteLink}
                            onEmptyDropHint="Drop links here"
                            canCreateLink={canCreateLink}
                          />

                          {localGroups.map((g) => (
                            <GroupSection
                              key={g.id}
                              id={g.id}
                              name={g.name}
                              description={g.description}
                              items={g.links}
                              onCreateLink={startCreate}
                              onDeleteGroup={deleteGroup}
                              onEditGroup={startEditGroup}
                              onEdit={startEdit}
                              onDelete={handleDeleteLink}
                              canCreateLink={canCreateLink}
                            />
                          ))}
                        </Accordion>
                      </SortableContext>

                      <DragOverlay dropAnimation={defaultDropAnimation}>
                        {dragOverlayItem ? (
                          <div className="w-full opacity-90 shadow-2xl ring-2 ring-primary/30">
                            <div className="flex items-center gap-3 rounded-lg border-2 border-primary/50 bg-card p-3">
                              <GripVertical className="h-4 w-4 text-primary" />
                              <div className="flex-1 min-w-0">
                                <div className="flex items-center gap-2">
                                  <span className="font-medium text-sm truncate">{dragOverlayItem.name}</span>
                                  <ExternalLink className="h-3 w-3 text-muted-foreground flex-shrink-0" />
                                </div>
                                {dragOverlayItem.description && (
                                  <p className="text-xs text-muted-foreground mt-1 truncate">{dragOverlayItem.description}</p>
                                )}
                              </div>
                            </div>
                          </div>
                        ) : null}
                      </DragOverlay>
                    </DndContext>

                    {/* Create Group Modal */}
                    <AnimatePresence>
                      {isCreatingGroup && (
                        <motion.div
                          initial={{ opacity: 0, scale: 0.95, y: 20 }}
                          animate={{ opacity: 1, scale: 1, y: 0 }}
                          exit={{ opacity: 0, scale: 0.95, y: 20 }}
                          className="fixed inset-0 bg-background/80 backdrop-blur-sm flex items-center justify-center z-50"
                          onClick={() => setIsCreatingGroup(false)}
                        >
                          <Card className="w-full max-w-md mx-4" onClick={(e) => e.stopPropagation()}>
                            <CardHeader>
                              <CardTitle>Create New Group</CardTitle>
                              <CardDescription>Organize your links into groups</CardDescription>
                            </CardHeader>
                            <CardContent className="space-y-4">
                              <div>
                                <label className="text-sm font-medium mb-2 block">Group Name</label>
                                <Input
                                  placeholder="e.g. Work Links"
                                  value={groupForm.name}
                                  onChange={(e) => setGroupForm((f) => ({ ...f, name: e.target.value }))}
                                  autoFocus
                                />
                              </div>
                              <div>
                                <label className="text-sm font-medium mb-2 block">Description (Optional)</label>
                                <Input
                                  placeholder="What kind of links are these?"
                                  value={groupForm.description ?? ""}
                                  onChange={(e) => setGroupForm((f) => ({ ...f, description: e.target.value }))}
                                />
                              </div>
                              <div className="flex gap-2 pt-4">
                                <Button onClick={createGroup} className="flex-1">Create Group</Button>
                                <Button variant="ghost" onClick={() => setIsCreatingGroup(false)}>Cancel</Button>
                              </div>
                            </CardContent>
                          </Card>
                        </motion.div>
                      )}
                    </AnimatePresence>

                    {/* Edit Group Modal */}
                    <AnimatePresence>
                      {editingGroupId && (
                        <motion.div
                          initial={{ opacity: 0, scale: 0.95, y: 20 }}
                          animate={{ opacity: 1, scale: 1, y: 0 }}
                          exit={{ opacity: 0, scale: 0.95, y: 20 }}
                          className="fixed inset-0 bg-background/80 backdrop-blur-sm flex items-center justify-center z-50"
                          onClick={cancelGroupEdit}
                        >
                          <Card className="w-full max-w-md mx-4" onClick={(e) => e.stopPropagation()}>
                            <CardHeader>
                              <CardTitle>Edit Group</CardTitle>
                              <CardDescription>Update group details</CardDescription>
                            </CardHeader>
                            <CardContent className="space-y-4">
                              <div>
                                <label className="text-sm font-medium mb-2 block">Group Name</label>
                                <Input
                                  placeholder="e.g. Work Links"
                                  value={groupEditForm.name ?? ""}
                                  onChange={(e) => setGroupEditForm((f) => ({ ...f, name: e.target.value }))}
                                  autoFocus
                                />
                              </div>
                              <div>
                                <label className="text-sm font-medium mb-2 block">Description (Optional)</label>
                                <Input
                                  placeholder="What kind of links are these?"
                                  value={groupEditForm.description ?? ""}
                                  onChange={(e) => setGroupEditForm((f) => ({ ...f, description: e.target.value }))}
                                />
                              </div>
                              <div className="flex gap-2 pt-4">
                                <Button onClick={saveGroupEdit} className="flex-1">Save Changes</Button>
                                <Button variant="ghost" onClick={cancelGroupEdit}>Cancel</Button>
                              </div>
                            </CardContent>
                          </Card>
                        </motion.div>
                      )}
                    </AnimatePresence>

                    {/* Create/Edit Link Modal */}
                    <AnimatePresence>
                      {(isCreating || editingLinkId) && (
                        <motion.div
                          initial={{ opacity: 0, scale: 0.95, y: 20 }}
                          animate={{ opacity: 1, scale: 1, y: 0 }}
                          exit={{ opacity: 0, scale: 0.95, y: 20 }}
                          className="fixed inset-0 bg-background/80 backdrop-blur-sm flex items-center justify-center z-50"
                          onClick={cancel}
                        >
                          <Card className="w-full max-w-lg mx-4" onClick={(e) => e.stopPropagation()}>
                            <CardHeader>
                              <CardTitle>{editingLinkId ? "Edit Link" : "Create New Link"}</CardTitle>
                              <CardDescription>Add a new link to your collection</CardDescription>
                            </CardHeader>
                            <CardContent className="space-y-4">
                              <div className="grid grid-cols-2 gap-4">
                                <div>
                                  <label className="text-sm font-medium mb-2 block">Link Name</label>
                                  <Input
                                    placeholder="e.g. Google"
                                    value={form.name}
                                    onChange={(e) => setForm((f) => ({ ...f, name: e.target.value }))}
                                    autoFocus
                                  />
                                </div>
                                <div>
                                  <label className="text-sm font-medium mb-2 block">URL</label>
                                  {isCustomUrl ? (
                                    <Input
                                      placeholder="https://..."
                                      value={form.url}
                                      onChange={(e) => handleCustomUrlChange(e.target.value)}
                                      onPaste={handleCustomUrlPaste}
                                    />
                                  ) : (
                                    <div className="flex h-9 w-full items-center rounded-md border border-input bg-background shadow-xs transition-[color,box-shadow] focus-within:border-ring focus-within:ring-ring/50 focus-within:ring-[3px]">
                                      <span className="flex h-full items-center border-r border-input bg-muted/50 px-3 text-sm text-muted-foreground select-none">
                                        {urlProtocol}
                                      </span>
                                      <Input
                                        placeholder="yourlink.com"
                                        value={urlRemainder}
                                        onChange={(e) => handleUrlRemainderChange(e.target.value)}
                                        onPaste={handleUrlPaste}
                                        className="flex-1 h-full rounded-l-none border-0 bg-transparent px-3 shadow-none focus-visible:border-0 focus-visible:ring-0"
                                      />
                                    </div>
                                  )}
                                </div>
                              </div>
                              <div>
                                <label className="text-sm font-medium mb-2 block">Description (Optional)</label>
                                <Input
                                  placeholder="What is this link for?"
                                  value={form.description ?? ""}
                                  onChange={(e) => setForm((f) => ({ ...f, description: e.target.value }))}
                                />
                              </div>
                              <div>
                                <label className="text-sm font-medium mb-2 block">Group</label>
                                <select
                                  value={form.groupId ?? "ungrouped"}
                                  onChange={(e) => setForm((f) => ({ ...f, groupId: e.target.value === "ungrouped" ? null : (e.target.value as string) }))}
                                  className="w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
                                >
                                  <option value="ungrouped">Ungrouped</option>
                                  {(groupedData?.data?.groups ?? []).map((g) => (
                                    <option key={g.id} value={g.id}>{g.name}</option>
                                  ))}
                                </select>
                              </div>
                              <div className="flex gap-2 pt-4">
                                <Button onClick={saveLink} className="flex-1">
                                  {editingLinkId ? "Save Changes" : "Create Link"}
                                </Button>
                                <Button variant="ghost" onClick={cancel}>Cancel</Button>
                              </div>
                            </CardContent>
                          </Card>
                        </motion.div>
                      )}
                    </AnimatePresence>
                  </>
                )}
              </div>
            </div>

            {/* Mobile Preview Modal */}
            <AnimatePresence>
              {isPreviewModalOpen && (
                <motion.div
                  initial={{ opacity: 0, scale: 0.98 }}
                  animate={{ opacity: 1, scale: 1 }}
                  exit={{ opacity: 0, scale: 0.98 }}
                  className="fixed inset-0 bg-background/80 backdrop-blur-sm flex items-center justify-center z-50 sm:hidden"
                  onClick={() => setIsPreviewModalOpen(false)}
                >
                  <div className="w-full max-w-md mx-4" onClick={(e) => e.stopPropagation()}>
                    <div className="rounded-xl overflow-hidden shadow-lg bg-card">
                      <div className="p-4">
                        <div className="flex items-center justify-between mb-3">
                          <div className="text-lg font-semibold">Preview</div>
                          <Button size="sm" variant="ghost" onClick={() => setIsPreviewModalOpen(false)}>Close</Button>
                        </div>
                        {/* Pass full profile data to LinksPreview to render profile header with bio */}
                        <LinksPreview groups={localGroups} ungrouped={localUngrouped} user={userProfileData?.data} />
                      </div>
                    </div>
                  </div>
                </motion.div>
              )}
            </AnimatePresence>

            <div className="hidden lg:block lg:col-span-1">
              <div className="sticky top-20">
              {/* Desktop preview: pass full profile data to render profile header with bio inside the mock */}
              <LinksPreview groups={localGroups} ungrouped={localUngrouped} user={userProfileData?.data} />
              </div>
            </div>
          </div>
        </motion.div>

        {/* Delete Confirmation Dialog */}
        <Dialog open={deleteConfirmOpen} onOpenChange={setDeleteConfirmOpen}>
          <DialogContent>
            <DialogHeader>
              <DialogTitle>
                {itemToDelete?.type === 'group' ? 'Delete Group?' : 'Delete Link?'}
              </DialogTitle>
              <DialogDescription>
                {itemToDelete?.type === 'group' ? (
                  <>
                    Are you sure you want to delete <strong>{itemToDelete.name}</strong>?
                    <br />
                    Links in this group will be moved to Ungrouped.
                  </>
                ) : (
                  <>
                    Are you sure you want to delete <strong>{itemToDelete?.name}</strong>?
                    <br />
                    This action cannot be undone.
                  </>
                )}
              </DialogDescription>
            </DialogHeader>
            <DialogFooter>
              <Button variant="outline" onClick={() => setDeleteConfirmOpen(false)}>
                Cancel
              </Button>
              <Button variant="destructive" onClick={confirmDelete}>
                Delete
              </Button>
            </DialogFooter>
          </DialogContent>
        </Dialog>

        {/* Share Profile Modal */}
        <Dialog open={isShareModalOpen} onOpenChange={setIsShareModalOpen}>
          <DialogContent className="sm:max-w-md max-h-[90vh]">
            <DialogHeader>
              <DialogTitle>Share Your Profile</DialogTitle>
              <DialogDescription>
                Share your Linqyard profile across different platforms
              </DialogDescription>
            </DialogHeader>
            <div className="space-y-5 max-h-[65vh] overflow-y-auto pr-1">
              {/* QR share */}
              <div className="rounded-xl border bg-card/80 p-4">
                <div className="space-y-4">
                  <div className="text-center space-y-1">
                    <p className="text-sm font-semibold">Share with QR code</p>
                    <p className="text-xs text-muted-foreground">
                      Download a ready-to-share card with your QR code and username.
                    </p>
                  </div>
                  <div className="flex flex-col items-center gap-3">
                    {isGeneratingQrCode ? (
                      <div className="h-48 w-48 rounded-xl bg-muted animate-pulse" />
                    ) : qrCodeDataUrl ? (
                      <>
                        <div className="rounded-2xl border bg-white p-4 shadow-sm">
                          <NextImage
                            src={qrCodeDataUrl}
                            alt="Linqyard profile QR code"
                            width={192}
                            height={192}
                            unoptimized
                            className="h-48 w-48"
                          />
                        </div>
                        <div className="text-center space-y-1">
                          <p className="text-sm font-semibold">@{user?.username}</p>
                          <p className="text-xs text-muted-foreground">
                            Scan to view my Linqyard profile
                          </p>
                        </div>
                      </>
                    ) : qrCodeError ? (
                      <p className="text-sm text-destructive text-center">{qrCodeError}</p>
                    ) : (
                      <p className="text-sm text-muted-foreground text-center">
                        QR code unavailable.
                      </p>
                    )}
                  </div>
                  <div className="flex flex-col gap-2">
                    <Button
                      className="w-full"
                      onClick={downloadQrCard}
                      disabled={isGeneratingQrCode || isDownloadingQrCard || !qrCodeDataUrl}
                    >
                      {isDownloadingQrCard ? "Preparing image..." : "Download QR image"}
                    </Button>
                    <Button
                      className="w-full"
                      size="sm"
                      variant="outline"
                      onClick={() => copyToClipboard('qr')}
                      disabled={!qrCodeDataUrl}
                    >
                      {copiedPlatform === 'qr' ? (
                        <Check className="h-4 w-4 text-green-600" />
                      ) : (
                        <Copy className="h-4 w-4" />
                      )}
                      <span className="ml-2">Copy QR link</span>
                    </Button>
                  </div>
                </div>
              </div>

              <div className="space-y-3">
                {/* WhatsApp */}
                <div className="flex items-center gap-3 p-3 rounded-lg border bg-card hover:bg-accent/50 transition-colors">
                  <div className="flex-1">
                    <div className="flex items-center gap-2">
                      <svg className="h-5 w-5 text-green-600" fill="currentColor" viewBox="0 0 24 24">
                        <path d="M17.472 14.382c-.297-.149-1.758-.867-2.03-.967-.273-.099-.471-.148-.67.15-.197.297-.767.966-.94 1.164-.173.199-.347.223-.644.075-.297-.15-1.255-.463-2.39-1.475-.883-.788-1.48-1.761-1.653-2.059-.173-.297-.018-.458.13-.606.134-.133.298-.347.446-.52.149-.174.198-.298.298-.497.099-.198.05-.371-.025-.52-.075-.149-.669-1.612-.916-2.207-.242-.579-.487-.5-.669-.51-.173-.008-.371-.01-.57-.01-.198 0-.52.074-.792.372-.272.297-1.04 1.016-1.04 2.479 0 1.462 1.065 2.875 1.213 3.074.149.198 2.096 3.2 5.077 4.487.709.306 1.262.489 1.694.625.712.227 1.36.195 1.871.118.571-.085 1.758-.719 2.006-1.413.248-.694.248-1.289.173-1.413-.074-.124-.272-.198-.57-.347m-5.421 7.403h-.004a9.87 9.87 0 01-5.031-1.378l-.361-.214-3.741.982.998-3.648-.235-.374a9.86 9.86 0 01-1.51-5.26c.001-5.45 4.436-9.884 9.888-9.884 2.64 0 5.122 1.03 6.988 2.898a9.825 9.825 0 012.893 6.994c-.003 5.45-4.437 9.884-9.885 9.884m8.413-18.297A11.815 11.815 0 0012.05 0C5.495 0 .16 5.335.157 11.892c0 2.096.547 4.142 1.588 5.945L.057 24l6.305-1.654a11.882 11.882 0 005.683 1.448h.005c6.554 0 11.89-5.335 11.893-11.893a11.821 11.821 0 00-3.48-8.413z"/>
                      </svg>
                      <span className="font-medium">WhatsApp</span>
                    </div>
                  </div>
                  <div className="flex items-center gap-2">
                    <Button
                      size="sm"
                      variant="ghost"
                      onClick={() => copyToClipboard('whatsapp')}
                    >
                      {copiedPlatform === 'whatsapp' ? (
                        <Check className="h-4 w-4 text-green-600" />
                      ) : (
                        <Copy className="h-4 w-4" />
                      )}
                    </Button>
                    <Button
                      size="sm"
                      onClick={() => shareOnPlatform('whatsapp')}
                    >
                      Share
                    </Button>
                  </div>
                </div>

                {/* Telegram */}
                <div className="flex items-center gap-3 p-3 rounded-lg border bg-card hover:bg-accent/50 transition-colors">
                  <div className="flex-1">
                    <div className="flex items-center gap-2">
                      <svg className="h-5 w-5 text-blue-500" fill="currentColor" viewBox="0 0 24 24">
                        <path d="M11.944 0A12 12 0 0 0 0 12a12 12 0 0 0 12 12 12 12 0 0 0 12-12A12 12 0 0 0 12 0a12 12 0 0 0-.056 0zm4.962 7.224c.1-.002.321.023.465.14a.506.506 0 0 1 .171.325c.016.093.036.306.02.472-.18 1.898-.962 6.502-1.36 8.627-.168.9-.499 1.201-.82 1.23-.696.065-1.225-.46-1.9-.902-1.056-.693-1.653-1.124-2.678-1.8-1.185-.78-.417-1.21.258-1.91.177-.184 3.247-2.977 3.307-3.23.007-.032.014-.15-.056-.212s-.174-.041-.249-.024c-.106.024-1.793 1.14-5.061 3.345-.48.33-.913.49-1.302.48-.428-.008-1.252-.241-1.865-.44-.752-.245-1.349-.374-1.297-.789.027-.216.325-.437.893-.663 3.498-1.524 5.83-2.529 6.998-3.014 3.332-1.386 4.025-1.627 4.476-1.635z"/>
                      </svg>
                      <span className="font-medium">Telegram</span>
                    </div>
                  </div>
                  <div className="flex items-center gap-2">
                    <Button
                      size="sm"
                      variant="ghost"
                      onClick={() => copyToClipboard('telegram')}
                    >
                      {copiedPlatform === 'telegram' ? (
                        <Check className="h-4 w-4 text-green-600" />
                      ) : (
                        <Copy className="h-4 w-4" />
                      )}
                    </Button>
                    <Button
                      size="sm"
                      onClick={() => shareOnPlatform('telegram')}
                    >
                      Share
                    </Button>
                  </div>
                </div>

                {/* Twitter */}
                <div className="flex items-center gap-3 p-3 rounded-lg border bg-card hover:bg-accent/50 transition-colors">
                  <div className="flex-1">
                    <div className="flex items-center gap-2">
                      <svg className="h-5 w-5" fill="currentColor" viewBox="0 0 24 24">
                        <path d="M18.244 2.25h3.308l-7.227 8.26 8.502 11.24H16.17l-5.214-6.817L4.99 21.75H1.68l7.73-8.835L1.254 2.25H8.08l4.713 6.231zm-1.161 17.52h1.833L7.084 4.126H5.117z"/>
                      </svg>
                      <span className="font-medium">Twitter / X</span>
                    </div>
                  </div>
                  <div className="flex items-center gap-2">
                    <Button
                      size="sm"
                      variant="ghost"
                      onClick={() => copyToClipboard('twitter')}
                    >
                      {copiedPlatform === 'twitter' ? (
                        <Check className="h-4 w-4 text-green-600" />
                      ) : (
                        <Copy className="h-4 w-4" />
                      )}
                    </Button>
                    <Button
                      size="sm"
                      onClick={() => shareOnPlatform('twitter')}
                    >
                      Share
                    </Button>
                  </div>
                </div>
              </div>

              {/* Direct link with copy */}
              <div className="pt-4 border-t">
                <label className="text-sm font-medium mb-2 block">Or copy direct link</label>
                <div className="flex items-center gap-2">
                  <Input
                    value={getShareUrl('direct')}
                    readOnly
                    className="text-sm"
                  />
                  <Button
                    size="sm"
                    variant="outline"
                    onClick={() => copyToClipboard('direct')}
                  >
                    {copiedPlatform === 'direct' ? (
                      <Check className="h-4 w-4 text-green-600" />
                    ) : (
                      <Copy className="h-4 w-4" />
                    )}
                  </Button>
                </div>
              </div>
            </div>
            <DialogFooter className="sm:justify-start">
              <Button
                type="button"
                variant="secondary"
                onClick={() => setIsShareModalOpen(false)}
              >
                Close
              </Button>
            </DialogFooter>
          </DialogContent>
        </Dialog>
      </div>
    </div>
  );

  // --- helpers ---
  function startEdit(link: LinkItem) {
    setEditingLinkId(link.id);
    metadataAttemptedUrlsRef.current.clear();
    setForm({
      id: link.id,
      name: link.name,
      url: link.url || "",
      description: link.description || "",
      groupId: link.groupId,
      sequence: link.sequence,
      isActive: link.isActive,
    });
    syncUrlStateFromFull(link.url || "");
    setIsCreating(false);
  }

  function handleDeleteLink(link: LinkItem) {
    deleteLink(link.id);
  }

  function startCreate(groupId: string | null = null) {
    // Check if user can create more links (free tier limit)
    if (!canCreateLink) {
      toast.error("Free tier limit reached: Maximum 12 links allowed", {
        description: "Upgrade to Plus or Pro for unlimited links"
      });
      return;
    }
    
    metadataAttemptedUrlsRef.current.clear();
    setIsCreating(true);
    setEditingLinkId(null);
    setForm({ name: "", url: "", description: "", groupId, sequence: 0, isActive: true });
    resetUrlState();
  }

  function cancel() {
    setIsCreating(false);
    setEditingLinkId(null);
    metadataAttemptedUrlsRef.current.clear();
    setForm({ name: "", url: "", description: "", groupId: null, sequence: 0, isActive: true });
    resetUrlState();
  }

  async function saveLink() {
    try {
      if (editingLinkId) {
        await post(`/link/${editingLinkId}/edit`, form as any);
        toast.success("Link updated");
      } else {
        await post("/link", form as any);
        toast.success("Link created");
      }
      await refetchLinks();
      cancel();
    } catch (err: any) {
      console.error("Save link failed", err);
      toast.error(err?.data?.title || err?.message || "Failed to save link");
    }
  }

  async function createGroup() {
    try {
      await post("/group", groupForm as any);
      toast.success("Group created");
      await refetchLinks();
      setGroupForm({ name: "", description: "", sequence: 0 });
      setIsCreatingGroup(false);
    } catch (err: any) {
      console.error("Create group failed", err);
      toast.error(err?.data?.title || err?.message || "Failed to create group");
    }
  }

  async function deleteGroup(groupId: string) {
    const group = localGroups.find(g => g.id === groupId);
    setItemToDelete({ type: 'group', id: groupId, name: group?.name || 'this group' });
    setDeleteConfirmOpen(true);
  }

  async function deleteLink(linkId: string) {
    const link = [...localUngrouped, ...localGroups.flatMap(g => g.links)].find(l => l.id === linkId);
    setItemToDelete({ type: 'link', id: linkId, name: link?.name || 'this link' });
    setDeleteConfirmOpen(true);
  }

  async function confirmDelete() {
    if (!itemToDelete) return;

    try {
      if (itemToDelete.type === 'group') {
        await post(`/group/${itemToDelete.id}/delete`, {});
        toast.success("Group deleted");
      } else {
        await post(`/link/${itemToDelete.id}/delete`, {});
        toast.success("Link deleted");
      }
      await refetchLinks();
    } catch (err: any) {
      console.error(`Delete ${itemToDelete.type} failed`, err);
      toast.error(err?.data?.title || err?.message || `Failed to delete ${itemToDelete.type}`);
    } finally {
      setDeleteConfirmOpen(false);
      setItemToDelete(null);
    }
  }

  function startEditGroup(groupId: string) {
    const group = localGroups.find(g => g.id === groupId);
    if (!group) return;
    
    setEditingGroupId(groupId);
    setGroupEditForm({
      name: group.name,
      description: group.description,
      sequence: 0, // sequence is handled automatically
      isActive: true, // groups are active by default
    });
  }

  function cancelGroupEdit() {
    setEditingGroupId(null);
    setGroupEditForm({
      name: "",
      description: "",
      sequence: 0,
      isActive: true,
    });
  }

  async function saveGroupEdit() {
    if (!editingGroupId) return;
    
    try {
      await post(`/group/${editingGroupId}/edit`, groupEditForm);
      toast.success("Group updated");
      await refetchLinks();
      cancelGroupEdit();
    } catch (err: any) {
      console.error("Save group edit failed", err);
      toast.error(err?.data?.title || err?.message || "Failed to update group");
    }
  }
}
