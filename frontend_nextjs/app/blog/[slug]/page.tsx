import type { Metadata } from "next"
import Link from "next/link"
import { notFound } from "next/navigation"
import { compileMDX } from "next-mdx-remote/rsc"
import remarkGfm from "remark-gfm"

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
    const post = await getPostBySlug(params.slug)
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
  const post = await loadPostOr404(params.slug)

  const { content } = await compileMDX({
    source: post.body,
    components: mdxComponents,
    options: {
      parseFrontmatter: false,
      mdxOptions: {
        remarkPlugins: [remarkGfm],
      },
    },
  })

  const publishedDate = new Date(post.meta.datePublished)
  return (
    <section className="container mx-auto max-w-3xl py-16 lg:py-24">
      <article className="space-y-10">
        <header className="space-y-6">
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

        <div className="space-y-6 text-foreground">{content}</div>
      </article>
    </section>
  )
}
