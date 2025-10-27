import type { Metadata } from "next"
import Link from "next/link"
import { notFound } from "next/navigation"
import { compileMDX } from "next-mdx-remote/rsc"
import remarkGfm from "remark-gfm"

import { TableOfContents, type TocHeading } from "@/components/blog/table-of-contents"
import { mdxComponents } from "@/components/mdx/mdx-components"
import { Badge } from "@/components/ui/badge"
import { Separator } from "@/components/ui/separator"
import { getBlogSlugs, getPostBySlug, type BlogPost } from "@/lib/blog"

type BlogPageParams = {
  params: {
    slug: string
  }
}

const dateFormatter = new Intl.DateTimeFormat("en-US", {
  year: "numeric",
  month: "long",
  day: "numeric",
})

const loadPostOr404 = async (slug: string): Promise<BlogPost> => {
  const post = await getPostBySlug(slug).catch((error) => {
    if (process.env.NODE_ENV !== "production") {
      console.error(`Failed to load post "${slug}":`, error)
    }
    return null
  })

  if (!post) {
    notFound()
  }

  return post
}

export const generateStaticParams = async () => {
  const slugs = await getBlogSlugs()
  return slugs.map((slug) => ({ slug }))
}

export const generateMetadata = async ({
  params,
}: BlogPageParams): Promise<Metadata> => {
  try {
    // `params` can be a thenable in Next.js — await it before accessing properties
    // See: https://nextjs.org/docs/messages/sync-dynamic-apis
    const awaitedParams = (await params) as BlogPageParams["params"]
    const post = await getPostBySlug(awaitedParams.slug)
    const publishedDate = new Date(post.meta.datePublished)

    return {
      title: post.meta.title,
      description: post.meta.description,
      authors: [
        {
          name: post.meta.author.name,
          url: post.meta.author.url,
        },
      ],
      openGraph: {
        title: post.meta.title,
        description: post.meta.description,
        type: "article",
        publishedTime: publishedDate.toISOString(),
        authors: [post.meta.author.name],
        tags: post.meta.tags,
      },
      twitter: {
        card: "summary_large_image",
        title: post.meta.title,
        description: post.meta.description,
      },
    }
  } catch {
    return {
      title: "Article not found",
    }
  }
}

export default async function BlogViewPage({ params }: BlogPageParams) {
  // `params` can be a thenable in Next.js — await it before accessing properties
  const awaitedParams = (await params) as BlogPageParams["params"]
  const post = await loadPostOr404(awaitedParams.slug)

  const headings: TocHeading[] = []

  const headingPlugin = () => {
    const slugCounts = new Map<string, number>()

    const getTextContent = (node: any): string => {
      if (!node) {
        return ""
      }

      if (typeof node.value === "string") {
        return node.value
      }

      if (Array.isArray(node.children)) {
        return node.children.map((child: unknown) => getTextContent(child)).join("")
      }

      return ""
    }

    const slugify = (value: string) => {
      const normalized = value
        .toLowerCase()
        .trim()
        .replace(/[^a-z0-9\s-]/g, "")
        .replace(/\s+/g, "-")

      const base = normalized.length > 0 ? normalized : `section-${headings.length + 1}`
      const occurrence = slugCounts.get(base) ?? 0
      slugCounts.set(base, occurrence + 1)

      return occurrence === 0 ? base : `${base}-${occurrence}`
    }

    const visitTree = (node: any) => {
      if (!node || typeof node !== "object") {
        return
      }

      if (node.type === "heading" && typeof node.depth === "number" && node.depth >= 2 && node.depth <= 4) {
        const title = getTextContent(node).trim()

        if (title.length > 0) {
          const id = slugify(title)
          const data =
            typeof node.data === "object" && node.data !== null ? node.data : {}
          const dataRecord = data as Record<string, unknown>

          const properties =
            typeof dataRecord.hProperties === "object" && dataRecord.hProperties !== null
              ? (dataRecord.hProperties as Record<string, unknown>)
              : {}

          dataRecord.id = id
          properties.id = id

          dataRecord.hProperties = properties
          node.data = dataRecord

          headings.push({
            id,
            title,
            depth: node.depth,
          })
        }
      }

      if (Array.isArray(node.children)) {
        node.children.forEach((child: unknown) => visitTree(child))
      }
    }

    return (tree: any) => {
      visitTree(tree)
    }
  }

  const { content } = await compileMDX({
    source: post.body,
    components: mdxComponents,
    options: {
      parseFrontmatter: false,
      mdxOptions: {
        remarkPlugins: [remarkGfm, headingPlugin],
      },
    },
  })

  const publishedDate = new Date(post.meta.datePublished)
  // normalize cover image URL: if it's not an absolute URL, prefix with base + /cover
  const getCoverSrc = (src?: string) => {
    if (!src) return undefined
    if (/^https?:\/\//i.test(src)) return src
    const path = src.startsWith("/") ? src : `/${src}`
    return `${path}`
  }

  return (
    <section className="container mx-auto max-w-6xl px-4 sm:px-6 py-16 lg:py-24">
      <div className="lg:flex lg:items-start lg:gap-12">
        <article className="flex-1 space-y-10 lg:max-w-3xl">
          <header className="space-y-6">
          <div>
            <Link
              href="/blog"
              className="text-sm text-muted-foreground transition-colors hover:text-primary"
            >
              ← Back to blogs
            </Link>
          </div>
          <div className="flex flex-wrap items-center gap-2 text-sm text-muted-foreground">
            <time dateTime={post.meta.datePublished}>
              {dateFormatter.format(publishedDate)}
            </time>
            <Separator
              orientation="vertical"
              className="h-4 w-px bg-border/70"
            />
            <span>{post.meta.readMinutes} min read</span>
          </div>

          <div className="space-y-3">
            <h1 className="text-4xl font-semibold tracking-tight text-foreground sm:text-5xl">
              {post.meta.title}
            </h1>
            <p className="text-lg text-muted-foreground">
              {post.meta.description}
            </p>
          </div>

          {post.meta.coverImage && (
            <div className="mt-6 overflow-hidden rounded-md">
              <img
                src={getCoverSrc(post.meta.coverImage)}
                alt={`Cover image for ${post.meta.title}`}
                className="w-full h-64 sm:h-72 md:h-80 object-cover rounded-md"
              />
            </div>
          )}

          <div className="flex flex-wrap items-center gap-3 text-sm text-muted-foreground">
            <span className="uppercase tracking-[0.2em] text-xs text-accent">
              Author
            </span>
            <Link
              href={post.meta.author.url}
              className="font-medium text-foreground transition-colors hover:text-primary"
            >
              {post.meta.author.name}
            </Link>
            {post.meta.tags.length > 0 && (
              <>
                <Separator
                  orientation="vertical"
                  className="h-4 w-px bg-border/70"
                />
                <div className="flex flex-wrap gap-2">
                  {post.meta.tags.map((tag) => (
                    <Badge
                      key={tag}
                      className="border-transparent bg-accent text-accent-foreground shadow-sm"
                    >
                      {tag}
                    </Badge>
                  ))}
                </div>
              </>
            )}
          </div>
          </header>

          <Separator className="bg-border/70" />

          {headings.length > 0 && (
            <div className="lg:hidden">
              <TableOfContents headings={headings} className="mt-6" />
            </div>
          )}

          <div className="space-y-6 text-foreground">{content}</div>
        </article>

        {headings.length > 0 && (
          <aside className="sticky top-28 mt-10 hidden w-[260px] flex-shrink-0 lg:block">
            <TableOfContents headings={headings} />
          </aside>
        )}
      </div>
    </section>
  )
}
