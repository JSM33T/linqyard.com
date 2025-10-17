import * as React from "react"

import { cn } from "@/lib/utils"

type HeadingProps = React.HTMLAttributes<HTMLHeadingElement>
type ParagraphProps = React.HTMLAttributes<HTMLParagraphElement>
type ListProps = React.HTMLAttributes<HTMLUListElement | HTMLOListElement>
type ListItemProps = React.LiHTMLAttributes<HTMLLIElement>
type AnchorProps = React.AnchorHTMLAttributes<HTMLAnchorElement>
type BlockquoteProps = React.BlockquoteHTMLAttributes<HTMLQuoteElement>
type CodeProps = React.HTMLAttributes<HTMLElement>
type PreProps = React.HTMLAttributes<HTMLPreElement>
type TableProps = React.TableHTMLAttributes<HTMLTableElement>

const createHeading =
  (component: `h${1 | 2 | 3 | 4 | 5 | 6}`, className: string) =>
  ({ className: customClassName, ...props }: HeadingProps) =>
    React.createElement(component, {
      className: cn(
        "scroll-m-20 font-semibold tracking-tight text-foreground",
        className,
        customClassName
      ),
      ...props,
    })

const components = {
  h1: createHeading("h1", "text-4xl sm:text-5xl"),
  h2: createHeading("h2", "text-3xl sm:text-4xl"),
  h3: createHeading("h3", "text-2xl sm:text-3xl"),
  h4: createHeading("h4", "text-xl sm:text-2xl"),
  h5: createHeading("h5", "text-lg"),
  h6: createHeading("h6", "text-base uppercase tracking-[0.3em] text-muted-foreground"),
  p: ({ className, ...props }: ParagraphProps) => (
    <p
      className={cn(
        "leading-7 text-muted-foreground [&:not(:first-child)]:mt-6",
        className
      )}
      {...props}
    />
  ),
  ul: ({ className, ...props }: ListProps) => (
    <ul
      className={cn(
        "my-6 ml-6 list-disc space-y-2 marker:text-accent",
        className
      )}
      {...props}
    />
  ),
  ol: ({ className, ...props }: ListProps) => (
    <ol
      className={cn(
        "my-6 ml-6 list-decimal space-y-2 marker:text-accent",
        className
      )}
      {...props}
    />
  ),
  li: ({ className, ...props }: ListItemProps) => (
    <li className={cn("pl-2 text-muted-foreground", className)} {...props} />
  ),
  a: ({ className, ...props }: AnchorProps) => (
    <a
      className={cn(
        "font-medium text-primary underline-offset-[6px] transition hover:underline",
        className
      )}
      {...props}
    />
  ),
  blockquote: ({ className, ...props }: BlockquoteProps) => (
    <blockquote
      className={cn(
        "border-l-4 border-accent/60 bg-muted/40 px-6 py-3 text-lg italic text-foreground",
        className
      )}
      {...props}
    />
  ),
  code: ({ className, ...props }: CodeProps) => (
    <code
      className={cn(
        "relative rounded-md bg-muted px-1.5 py-0.5 font-mono text-sm",
        className
      )}
      {...props}
    />
  ),
  pre: ({ className, ...props }: PreProps) => (
    <pre
      className={cn(
        "mb-6 mt-6 overflow-x-auto rounded-lg border border-border bg-muted/50 p-4 font-mono text-sm",
        className
      )}
      {...props}
    />
  ),
  table: ({ className, ...props }: TableProps) => (
    <div className="my-8 overflow-x-auto">
      <table
        className={cn(
          "w-full min-w-max border-collapse text-left text-sm text-foreground",
          className
        )}
        {...props}
      />
    </div>
  ),
  th: ({ className, ...props }: React.ThHTMLAttributes<HTMLTableCellElement>) => (
    <th
      className={cn(
        "border-b border-border bg-muted/60 px-3 py-2 text-left text-sm font-semibold text-foreground",
        className
      )}
      {...props}
    />
  ),
  td: ({ className, ...props }: React.TdHTMLAttributes<HTMLTableCellElement>) => (
    <td
      className={cn(
        "border-b border-border px-3 py-2 text-sm text-muted-foreground",
        className
      )}
      {...props}
    />
  ),
}

export type MDXComponents = typeof components

export const mdxComponents: MDXComponents = components
