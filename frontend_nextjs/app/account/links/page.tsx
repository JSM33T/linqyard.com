"use client";

import React, { useEffect, useMemo, useRef, useState } from "react";
import Link from "next/link";
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

import { ArrowLeft, Globe, Plus, GripVertical, ExternalLink, Edit3, Trash2, FolderPlus } from "lucide-react";
import { toast } from "sonner";

import { useUser } from "@/contexts/UserContext";
import { useApi, useGet } from "@/hooks/useApi";
import { GetGroupedLinksResponse, LinkItem, CreateOrEditLinkRequest, UpdateGroupRequest, GroupResequenceItemRequest } from "@/hooks/types";

/* ---------- FX presets ---------- */
const containerVariants = {
  hidden: { opacity: 0, y: 20 },
  visible: { opacity: 1, y: 0, transition: { duration: 0.6 } },
};
const cardVariants = {
  hidden: { opacity: 0, scale: 0.98 },
  visible: { opacity: 1, scale: 1, transition: { duration: 0.25 } },
};

/* ---------- Sortable row (single Link chip) ---------- */
function SortableLinkRow({ item, onEdit }: { item: LinkItem; onEdit: (l: LinkItem) => void }) {
  const { attributes, listeners, setNodeRef, transform, transition, isDragging } = useSortable({ id: item.id });
  const style = {
    transform: CSS.Transform.toString(transform),
    transition,
    zIndex: isDragging ? 50 : ("auto" as const),
    boxShadow: isDragging ? "0 8px 24px rgba(0,0,0,0.20)" : undefined,
  } as React.CSSProperties;

  return (
    <div
      ref={setNodeRef}
      style={style}
      className={"group/link flex items-center gap-3 rounded-lg border bg-background/60 hover:bg-accent/50 transition-all p-3 cursor-grab active:cursor-grabbing"}
      {...attributes}
      {...listeners}
    >
      <GripVertical className="h-4 w-4 text-muted-foreground opacity-60 group-hover/link:opacity-100 transition-opacity" />
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
    </div>
  );
}

/* ---------- Sortable Group Header ---------- */
function SortableGroupHeader({ id, name, description, onCreateLink, onDeleteGroup, onEditGroup }: {
  id: string;
  name: string;
  description?: string | null;
  onCreateLink: (groupId: string) => void;
  onDeleteGroup?: (groupId: string) => void;
  onEditGroup?: (groupId: string) => void;
}) {
  const { attributes, listeners, setNodeRef, transform, transition, isDragging } = useSortable({ 
    id: `group-${id}` 
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
      className={`flex items-start justify-between w-full gap-3 ${isDragging ? 'opacity-50' : ''}`}
      {...attributes}
    >
      <div className="flex-1 text-left">
        <div className="flex items-center gap-2">
          <div className="h-2 w-2 rounded-full bg-gradient-to-r from-primary to-blue-500" />
              <span className="font-semibold no-underline hover:no-underline">{name}</span>
          <div {...listeners} className="cursor-grab active:cursor-grabbing ml-1">
            <GripVertical className="h-4 w-4 text-muted-foreground opacity-60 hover:opacity-100 transition-opacity" />
          </div>
        </div>
        {description ? <p className="text-xs text-muted-foreground mt-1">{description}</p> : null}
      </div>
      <div className="flex items-center gap-1">
        <Button size="sm" variant="ghost" onClick={(e) => { e.stopPropagation(); onCreateLink(id); }}>
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
  onEmptyDropHint,
}: {
  id: string | null; // null for ungrouped
  name: string;
  description?: string | null;
  items: LinkItem[];
  onCreateLink: (groupId: string | null) => void;
  onDeleteGroup?: (groupId: string) => void;
  onEditGroup?: (groupId: string) => void;
  onEdit: (l: LinkItem) => void;
  onEmptyDropHint?: string;
}) {
  const containerId = id ?? "__ungrouped__";
  const { setNodeRef, isOver } = useDroppable({ id: containerId });

  return (
    <AccordionItem value={containerId} className="rounded-xl border bg-card">
      <AccordionTrigger className="px-4">
        {id ? (
          <SortableGroupHeader 
            id={id}
            name={name}
            description={description}
            onCreateLink={onCreateLink}
            onDeleteGroup={onDeleteGroup}
            onEditGroup={onEditGroup}
          />
        ) : (
          <div className="flex items-start justify-between w-full gap-3">
            <div className="flex-1 text-left">
              <div className="flex items-center gap-2">
                <div className="h-2 w-2 rounded-full bg-muted-foreground" />
                <span className="font-semibold no-underline hover:no-underline">{name}</span>
              </div>
              {description ? <p className="text-xs text-muted-foreground mt-1">{description}</p> : null}
            </div>
            <div className="flex items-center gap-1">
              <Button size="sm" variant="ghost" onClick={(e) => { e.stopPropagation(); onCreateLink(id); }}>
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
                  <SortableLinkRow item={link} onEdit={onEdit} />
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

  // Local optimistic structure
  const [localGroups, setLocalGroups] = useState<
    { id: string; name: string; description?: string | null; links: LinkItem[] }[]
  >([]);
  const [localUngrouped, setLocalUngrouped] = useState<LinkItem[]>([]);

  // drag state
  const [, setActiveId] = useState<string | null>(null);
  const activeItemRef = useRef<LinkItem | null>(null);

  useEffect(() => {
    if (linksError) {
      console.warn("Links API error:", linksError);
      toast.error("Could not load links.");
    }
  }, [linksError]);

  // hydrate local state from API
  useEffect(() => {
    if (!groupedData?.data) return;
    setLocalGroups(
      [...groupedData.data.groups]
        .sort((a, b) => a.sequence - b.sequence) // Sort groups by sequence
        .map((g) => ({
          id: g.id,
          name: g.name,
          description: g.description,
          links: [...g.links].sort((a, b) => a.sequence - b.sequence),
        }))
    );
    setLocalUngrouped([...groupedData.data.ungrouped.links].sort((a, b) => a.sequence - b.sequence));
  }, [groupedData]);

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
                <div className="sm:ml-auto flex items-center gap-3">
                  <Button size="sm" variant="outline" onClick={() => setIsCreatingGroup(true)}>
                    <FolderPlus className="h-4 w-4 mr-2" />
                    New Group
                  </Button>
                  <Button size="sm" onClick={() => startCreate(null)}>
                    <Plus className="h-4 w-4 mr-2" />
                    New Link
                  </Button>
                  {/* Preview button visible only on small screens (grouped with actions) */}
                    <Button size="sm" variant="outline" className="sm:hidden" onClick={() => setIsPreviewModalOpen(true)}>
                    <span className="inline-flex items-center">
                      <Globe className="h-4 w-4 mr-2" />
                      <span>Preview</span>
                    </span>
                  </Button>
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
                      <Button onClick={() => setIsCreatingGroup(true)} variant="outline">
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
                        <Accordion type="multiple" defaultValue={[...groupedData.data.groups.map((g) => g.id), "__ungrouped__"]} className="space-y-3">
                          <GroupSection
                            id={null}
                            name="Ungrouped"
                            description="Links without a group"
                            items={localUngrouped}
                            onCreateLink={startCreate}
                            onEdit={startEdit}
                            onEmptyDropHint="Drop links here"
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
                            />
                          ))}
                        </Accordion>
                      </SortableContext>

                      <DragOverlay dropAnimation={defaultDropAnimation}>
                        {dragOverlayItem ? (
                          <div className="w-full">
                            <SortableLinkRow item={dragOverlayItem} onEdit={() => {}} />
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
                                  <Input
                                    placeholder="https://..."
                                    value={form.url}
                                    onChange={(e) => setForm((f) => ({ ...f, url: e.target.value }))}
                                  />
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
                        {/* Build header node and pass into LinksPreview so it renders inside the mock */}
                        {(() => {
                          const headerNode = user ? (
                            <div>
                              <div className="h-20 w-full bg-gradient-to-r from-primary/8 via-transparent to-primary/8 rounded-t-xl flex items-center justify-center" />
                              <div className="-mt-8 flex flex-col items-center text-center px-4 pb-2">
                                <div className="h-14 w-14 rounded-full ring-4 ring-card bg-white overflow-hidden">
                                  <img src={user.avatarUrl ?? '/images/avatar-placeholder.png'} alt={user.username} className="h-full w-full object-cover" />
                                </div>
                                <h3 className="mt-2 text-sm font-semibold truncate">{[user.firstName, user.lastName].filter(Boolean).join(' ') || user.username}</h3>
                                <p className="text-xs text-muted-foreground">@{user.username}</p>
                              </div>
                            </div>
                          ) : null;

                          return <LinksPreview groups={localGroups} ungrouped={localUngrouped} header={headerNode} />;
                        })()}
                      </div>
                    </div>
                  </div>
                </motion.div>
              )}
            </AnimatePresence>

            <div className="hidden lg:block lg:col-span-1">
              <div className="sticky top-20">
              {/* Desktop preview: pass the same header into the mock so header appears inside the mock above the links */}
              {(() => {
                const headerNode = user ? (
                  <div>
                    <div className="h-20 w-full bg-gradient-to-r from-primary/8 via-transparent to-primary/8 rounded-t-xl flex items-center justify-center" />
                    <div className="-mt-8 flex flex-col items-center text-center px-4 pb-2">
                      <div className="h-14 w-14 rounded-full ring-4 ring-card bg-white overflow-hidden">
                        <img src={user.avatarUrl ?? '/images/avatar-placeholder.png'} alt={user.username} className="h-full w-full object-cover" />
                      </div>
                      <h3 className="mt-2 text-sm font-semibold truncate">{[user.firstName, user.lastName].filter(Boolean).join(' ') || user.username}</h3>
                      <p className="text-xs text-muted-foreground">@{user.username}</p>
                    </div>
                  </div>
                ) : null;

                return <LinksPreview groups={localGroups} ungrouped={localUngrouped} header={headerNode} />;
              })()}
              </div>
            </div>
          </div>
        </motion.div>
      </div>
    </div>
  );

  // --- helpers ---
  function startEdit(link: LinkItem) {
    setEditingLinkId(link.id);
    setForm({
      id: link.id,
      name: link.name,
      url: link.url,
      description: link.description || "",
      groupId: link.groupId,
      sequence: link.sequence,
      isActive: link.isActive,
    });
    setIsCreating(false);
  }

  function startCreate(groupId: string | null = null) {
    setIsCreating(true);
    setEditingLinkId(null);
    setForm({ name: "", url: "", description: "", groupId, sequence: 0, isActive: true });
  }

  function cancel() {
    setIsCreating(false);
    setEditingLinkId(null);
    setForm({ name: "", url: "", description: "", groupId: null, sequence: 0, isActive: true });
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
    if (!confirm("Delete group? Links in the group will be moved to Ungrouped.")) return;
    try {
      await post(`/group/${groupId}/delete`, {});
      toast.success("Group deleted");
      await refetchLinks();
    } catch (err: any) {
      console.error("Delete group failed", err);
      toast.error(err?.data?.title || err?.message || "Failed to delete group");
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
