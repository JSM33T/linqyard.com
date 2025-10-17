import fs from "fs/promises";
import path from "path";
import matter from "gray-matter";

const BLOG_DIRECTORY = path.join(process.cwd(), "content", "blog");

export type BlogFrontMatter = {
  title: string;
  description: string;
  readMinutes: number;
  author: {
    name: string;
    url: string;
  };
  tags: string[];
  datePublished: string;
  isActive?: boolean;
};

export type BlogSummary = BlogFrontMatter & {
  slug: string;
};

export type BlogPost = {
  meta: BlogSummary;
  body: string;
};

const ensureBlogDirectoryExists = async () => {
  try {
    await fs.access(BLOG_DIRECTORY);
  } catch {
    await fs.mkdir(BLOG_DIRECTORY, { recursive: true });
  }
};

const validateFrontMatter = (slug: string, data: any): BlogFrontMatter => {
  const errors: string[] = [];

  if (typeof data.title !== "string" || data.title.trim().length === 0) {
    errors.push("title must be a non-empty string");
  }

  if (
    typeof data.description !== "string" ||
    data.description.trim().length === 0
  ) {
    errors.push("description must be a non-empty string");
  }

  if (
    typeof data.readMinutes !== "number" ||
    !Number.isFinite(data.readMinutes)
  ) {
    errors.push("readMinutes must be a finite number");
  }

  if (
    typeof data.author !== "object" ||
    data.author === null ||
    typeof data.author.name !== "string" ||
    data.author.name.trim().length === 0 ||
    typeof data.author.url !== "string" ||
    data.author.url.trim().length === 0
  ) {
    errors.push("author must include name and url");
  }

  if (
    !Array.isArray(data.tags) ||
    data.tags.some((tag: unknown) => typeof tag !== "string")
  ) {
    errors.push("tags must be an array of strings");
  }

  if (typeof data.datePublished !== "string") {
    errors.push("datePublished must be an ISO date string");
  } else {
    const date = new Date(data.datePublished);
    if (Number.isNaN(date.valueOf())) {
      errors.push("datePublished must be a valid date");
    }
  }

  if (
    typeof data.isActive !== "undefined" &&
    typeof data.isActive !== "boolean"
  ) {
    errors.push("isActive must be a boolean when provided");
  }

  if (errors.length > 0) {
    throw new Error(
      `Invalid front matter in blog post "${slug}": ${errors.join(", ")}`
    );
  }

  return {
    title: data.title,
    description: data.description,
    readMinutes: data.readMinutes,
    author: {
      name: data.author.name,
      url: data.author.url,
    },
    tags: data.tags,
    datePublished: data.datePublished,
    isActive: typeof data.isActive === "boolean" ? data.isActive : true,
  };
};

const getSlugFromFilename = (filename: string) =>
  filename.replace(/\.mdx?$/, "");

export const getBlogSlugs = async (): Promise<string[]> => {
  await ensureBlogDirectoryExists();
  const entries = await fs.readdir(BLOG_DIRECTORY, { withFileTypes: true });

  return entries
    .filter((entry) => entry.isFile() && /\.mdx?$/.test(entry.name))
    .map((entry) => getSlugFromFilename(entry.name));
};

export const getPostBySlug = async (slug: string): Promise<BlogPost> => {
  await ensureBlogDirectoryExists();
  const filePath = path.join(BLOG_DIRECTORY, `${slug}.mdx`);
  const raw = await fs.readFile(filePath, "utf8");
  const { data, content } = matter(raw);

  const frontMatter = validateFrontMatter(slug, data);

  return {
    meta: {
      ...frontMatter,
      slug,
    },
    body: content,
  };
};

export const getAllPosts = async (): Promise<BlogSummary[]> => {
  const slugs = await getBlogSlugs();
  const posts = await Promise.all(slugs.map((slug) => getPostBySlug(slug)));

  return posts
    .map((post) => post.meta)
    // filter out posts that explicitly set isActive: false
    .filter((meta) => meta.isActive !== false)
    .sort((a, b) => {
      const dateA = new Date(a.datePublished).valueOf();
      const dateB = new Date(b.datePublished).valueOf();
      return dateB - dateA;
    });
};
