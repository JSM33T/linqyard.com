"use client"

import { ReactNode, useEffect, useMemo, useState } from "react"

import { cn } from "@/lib/utils"

export type TocHeading = {
  id: string
  title: string
  depth: number
  label?: ReactNode
}

type TableOfContentsProps = {
  headings: TocHeading[]
  className?: string
}

// Renders a scroll-spy aware table of contents for content-heavy pages.
export function TableOfContents({ headings, className }: TableOfContentsProps) {
  const [activeId, setActiveId] = useState<string | null>(() => headings[0]?.id ?? null)

  const headingIndex = useMemo(() => {
    return headings.reduce<Record<string, number>>((acc, heading, index) => {
      acc[heading.id] = index
      return acc
    }, {})
  }, [headings])

  useEffect(() => {
    if (headings.length === 0 || typeof window === "undefined") {
      return
    }

    setActiveId((previous) => previous ?? headings[0]?.id ?? null)

    const observer = new IntersectionObserver(
      (entries) => {
        const visibleHeadings = entries
          .filter((entry) => entry.isIntersecting)
          .map((entry) => entry.target.id)
          .sort((a, b) => (headingIndex[a] ?? 0) - (headingIndex[b] ?? 0))

        if (visibleHeadings.length > 0) {
          setActiveId(visibleHeadings[0])
          return
        }

        const beforeViewport = entries
          .filter((entry) => entry.boundingClientRect.top < 0)
          .map((entry) => entry.target.id)
          .sort((a, b) => (headingIndex[a] ?? 0) - (headingIndex[b] ?? 0))

        if (beforeViewport.length > 0) {
          setActiveId(beforeViewport[beforeViewport.length - 1])
        }
      },
      {
        rootMargin: "0px 0px -65% 0px",
        threshold: [0, 1],
      }
    )

    const elements = headings
      .map((heading) => document.getElementById(heading.id))
      .filter((element): element is HTMLElement => Boolean(element))

    elements.forEach((element) => observer.observe(element))

    return () => {
      elements.forEach((element) => observer.unobserve(element))
      observer.disconnect()
    }
  }, [headings, headingIndex])

  if (headings.length === 0) {
    return null
  }

  return (
    <nav
      aria-label="Table of contents"
      className={cn(
        "rounded-lg border border-primary/60 bg-muted/20 p-5 text-sm text-muted-foreground",
        className
      )}
    >
      <p className="text-xs font-semibold uppercase tracking-[0.35em] text-muted-foreground/80">
        On this page
      </p>
      <ul className="mt-4 space-y-2">
        {headings.map((heading) => {
          const isActive = activeId === heading.id
          const offset = Math.max(0, heading.depth - 2)

          return (
            <li key={heading.id} style={{ marginLeft: `${offset * 12}px` }}>
              <a
                href={`#${heading.id}`}
                className={cn(
                  "block rounded-md border border-transparent px-3 py-1.5 transition-colors",
                  isActive
                    ? "border-accent/60 bg-accent/10 text-foreground"
                    : "hover:border-accent/40 hover:text-foreground"
                )}
                onClick={() => setActiveId(heading.id)}
              >
                {heading.label ?? heading.title}
              </a>
            </li>
          )
        })}
      </ul>
    </nav>
  )
}
