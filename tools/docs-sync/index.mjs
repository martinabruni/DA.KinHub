#!/usr/bin/env node
import { existsSync, mkdirSync, readFileSync, readdirSync, rmSync, writeFileSync } from "node:fs";
import { join, relative, resolve } from "node:path";

const root = resolve(import.meta.dirname, "../..");
const docsRoot = join(root, "docs/user-guide");
const outputRoot = join(root, "src/frontend/src/generated/docs");
const locales = ["it", "en"];

function walk(directory) {
  return readdirSync(directory, { withFileTypes: true }).flatMap((entry) => entry.isDirectory() ? walk(join(directory, entry.name)) : [join(directory, entry.name)]);
}

function parse(path) {
  const content = readFileSync(path, "utf8");
  const match = content.match(/^---\r?\n([\s\S]*?)\r?\n---\r?\n([\s\S]*)$/);
  if (!match) throw new Error(`${relative(root, path)}: frontmatter mancante`);
  const metadata = Object.fromEntries(match[1].split(/\r?\n/).filter(Boolean).map((line) => {
    const index = line.indexOf(":");
    return [line.slice(0, index).trim(), line.slice(index + 1).trim().replace(/^['"]|['"]$/g, "")];
  }));
  for (const key of ["slug", "locale", "title", "description"]) if (!metadata[key]) throw new Error(`${relative(root, path)}: ${key} mancante`);
  return { ...metadata, content: match[2].trim(), source: relative(root, path).replaceAll("\\", "/") };
}

function collect() {
  const byLocale = Object.fromEntries(locales.map((locale) => {
    const directory = join(docsRoot, locale);
    if (!existsSync(directory)) throw new Error(`Directory mancante: docs/user-guide/${locale}`);
    const pages = walk(directory).filter((file) => file.endsWith(".md")).map(parse);
    for (const page of pages) if (page.locale !== locale) throw new Error(`${page.source}: locale incoerente`);
    const slugs = new Set();
    for (const page of pages) {
      if (slugs.has(page.slug)) throw new Error(`Slug duplicato ${locale}/${page.slug}`);
      slugs.add(page.slug);
    }
    return [locale, pages.sort((a, b) => a.slug.localeCompare(b.slug))];
  }));
  const it = new Set(byLocale.it.map((page) => page.slug));
  const en = new Set(byLocale.en.map((page) => page.slug));
  const missing = [...new Set([...it, ...en])].filter((slug) => !it.has(slug) || !en.has(slug));
  if (missing.length) throw new Error(`Guide non allineate it/en: ${missing.join(", ")}`);
  return byLocale;
}

function validateRoutes(byLocale) {
  const registryPath = join(root, "src/frontend/src/routes/route-registry.json");
  if (!existsSync(registryPath)) throw new Error("Route registry mancante");
  const routes = JSON.parse(readFileSync(registryPath, "utf8"));
  const slugs = new Set(byLocale.it.map((page) => page.slug));
  for (const route of routes) {
    for (const field of ["id", "path", "titleKey", "helpKey", "guideSlug"]) if (!route[field]) throw new Error(`Route incompleta: ${JSON.stringify(route)}`);
    if (!slugs.has(route.guideSlug)) throw new Error(`Guida mancante per route ${route.path}: ${route.guideSlug}`);
  }
}

function validate() {
  const pages = collect();
  validateRoutes(pages);
  console.log(`Documentazione valida: ${pages.it.length} pagine × 2 lingue`);
  return pages;
}

function sync() {
  const pages = validate();
  rmSync(outputRoot, { recursive: true, force: true });
  mkdirSync(outputRoot, { recursive: true });
  writeFileSync(join(outputRoot, "index.json"), `${JSON.stringify({ generatedBy: "tools/docs-sync", locales, pages }, null, 2)}\n`, "utf8");
  console.log(`Documentazione sincronizzata in ${relative(root, outputRoot)}`);
}

try {
  const command = process.argv[2] ?? "validate";
  if (command === "validate") validate();
  else if (command === "sync") sync();
  else throw new Error(`Comando sconosciuto: ${command}`);
} catch (error) {
  console.error(error.message);
  process.exitCode = 1;
}
