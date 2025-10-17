import Link from "next/link"
import { ArrowRight } from "lucide-react"

import { Badge } from "@/components/ui/badge"
import {
  Card,
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
} from "@/components/ui/card"
import { Separator } from "@/components/ui/separator"
import { getAllPosts } from "@/lib/blog"

const dateFormatter = new Intl.DateTimeFormat("en-US", {
  year: "numeric",
  month: "short",
  day: "numeric",
})

export default async function BlogPage() {
  const posts = await getAllPosts()

  return (
    <section className="container mx-auto max-w-5xl space-y-12 py-16 lg:space-y-16 lg:py-24">
      <header className="space-y-4 text-center">
        <p className="text-sm font-semibold uppercase tracking-[0.25em] text-accent">
          Blog
        </p>
        <div className="mx-auto max-w-2xl space-y-3">
          <h1 className="text-3xl font-semibold tracking-tight text-foreground sm:text-4xl">
            Insights & updates from the Linqyard team
          </h1>
          <p className="text-base text-muted-foreground">
            Explore lessons learned, product milestones, and the practices that
            keep our data workflows humming.
          </p>
        </div>
      </header>

      {posts.length === 0 ? (
        <div className="rounded-xl border border-dashed border-border bg-muted/30 px-6 py-12 text-center text-sm text-muted-foreground">
          We haven’t published any articles yet. Check back soon.
        </div>
      ) : (
        <div className="grid gap-6 md:gap-8">
          {posts.map((post) => (
            <Card
              key={post.slug}
              className="group border-border/70 transition-colors hover:border-primary/50"
            >
              <CardHeader className="gap-4 pb-0">
                <div className="flex flex-wrap items-center gap-2 text-sm text-muted-foreground">
                  <time dateTime={post.datePublished}>
                    {dateFormatter.format(new Date(post.datePublished))}
                  </time>
                  <Separator
                    orientation="vertical"
                    className="h-4 w-px bg-border/70"
                  />
                  <span>{post.readMinutes} min read</span>
                  <Separator
                    orientation="vertical"
                    className="h-4 w-px bg-border/70"
                  />
                  <Link
                    href={post.author.url}
                    className="font-medium text-foreground transition-colors hover:text-primary"
                  >
                    {post.author.name}
                  </Link>
                </div>

                <div className="space-y-2">
                  <CardTitle className="text-2xl font-semibold leading-tight sm:text-3xl">
                    <Link
                      href={`/blog/${post.slug}`}
                      className="transition-colors hover:text-primary"
                    >
                      {post.title}
                    </Link>
                  </CardTitle>
                  <CardDescription className="text-base text-muted-foreground">
                    {post.description}
                  </CardDescription>
                </div>
              </CardHeader>

              {post.tags.length > 0 && (
                <CardContent className="flex flex-wrap gap-2 pb-0 pt-6">
                  {post.tags.map((tag) => (
                    <Badge
                      key={tag}
                      className="border-transparent bg-accent text-accent-foreground shadow-sm"
                    >
                      {tag}
                    </Badge>
                  ))}
                </CardContent>
              )}

              <CardFooter className="flex items-center justify-between pt-6">
                <Link
                  href={`/blog/${post.slug}`}
                  className="inline-flex items-center gap-2 text-sm font-medium text-primary transition-colors hover:text-primary/80"
                >
                  Read article
                  <ArrowRight className="h-4 w-4 transition-transform group-hover:translate-x-1" />
                </Link>
              </CardFooter>
            </Card>
          ))}
        </div>
      )}
    </section>
  )
}
