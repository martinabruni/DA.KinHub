#!/usr/bin/env node
import { execFileSync } from "node:child_process";
import { existsSync, mkdirSync, readFileSync, readdirSync, writeFileSync } from "node:fs";
import { basename, join, relative, resolve } from "node:path";

const root = resolve(import.meta.dirname, "../..");
const changesRoot = join(root, "changes");
const allowedTypes = ["added", "changed", "deprecated", "removed", "fixed", "security"];

function parse(path) {
  const content = readFileSync(path, "utf8");
  const match = content.match(/^---\r?\n([\s\S]*?)\r?\n---\r?\n([\s\S]*)$/);
  if (!match) throw new Error(`${relative(root, path)}: frontmatter mancante`);
  const metadata = Object.fromEntries(match[1].split(/\r?\n/).filter(Boolean).map((line) => {
    const index = line.indexOf(":");
    return [line.slice(0, index).trim(), line.slice(index + 1).trim().replace(/^['"]|['"]$/g, "")];
  }));
  if (!allowedTypes.includes(metadata.type)) throw new Error(`${relative(root, path)}: type non valido`);
  if (!metadata.area || !["true", "false"].includes(metadata.breaking)) throw new Error(`${relative(root, path)}: area/breaking non validi`);
  const it = match[2].match(/## it\r?\n([\s\S]*?)(?=\r?\n## en)/)?.[1]?.trim();
  const en = match[2].match(/## en\r?\n([\s\S]*)$/)?.[1]?.trim();
  if (!it || !en) throw new Error(`${relative(root, path)}: descrizioni it/en mancanti`);
  return { id: basename(path, ".md"), ...metadata, breaking: metadata.breaking === "true", descriptions: { it, en } };
}

function fragments() {
  return readdirSync(changesRoot).filter((name) => name.endsWith(".md") && name !== "README.md").map((name) => parse(join(changesRoot, name))).sort((a, b) => a.id.localeCompare(b.id));
}

function metadata(items) {
  const version = readFileSync(join(root, "VERSION"), "utf8").trim();
  let commit = process.env.GITHUB_SHA ?? process.env.COMMIT_SHA;
  if (!commit) {
    try { commit = execFileSync("git", ["rev-parse", "--short", "HEAD"], { cwd: root, encoding: "utf8" }).trim(); }
    catch { commit = "local"; }
  }
  return {
    appName: "KinHub",
    version,
    commit,
    buildDate: process.env.BUILD_DATE ?? new Date().toISOString(),
    environment: process.env.BUILD_ENVIRONMENT ?? "Development",
    entries: items
  };
}

function validate() {
  const items = fragments();
  if (items.length === 0) throw new Error("Almeno un change fragment è richiesto");
  console.log(`Change fragments validi: ${items.length}`);
  return items;
}

function generate() {
  const document = metadata(validate());
  const publicDir = join(root, "src/frontend/public");
  mkdirSync(publicDir, { recursive: true });
  writeFileSync(join(publicDir, "release-notes.json"), `${JSON.stringify(document, null, 2)}\n`, "utf8");
  for (const locale of ["it", "en"]) {
    const dir = join(root, `docs/patch-notes/${locale}`);
    mkdirSync(dir, { recursive: true });
    const title = locale === "it" ? `KinHub ${document.version} — Note di rilascio` : `KinHub ${document.version} — Release notes`;
    const body = document.entries.map((entry) => `- **${entry.type} / ${entry.area}**${entry.breaking ? " ⚠️" : ""}: ${entry.descriptions[locale]}`).join("\n");
    writeFileSync(join(dir, `${document.version}.md`), `---\nversion: ${document.version}\nlocale: ${locale}\ndate: ${document.buildDate.slice(0, 10)}\n---\n\n# ${title}\n\n${body}\n`, "utf8");
  }
  console.log(`Release metadata generati per ${document.version}`);
  return document;
}

function release() {
  const document = generate();
  const changelogPath = join(root, "CHANGELOG.md");
  const changelog = readFileSync(changelogPath, "utf8");
  const marker = `## [${document.version}]`;
  if (!changelog.includes(marker)) {
    const grouped = Object.fromEntries(allowedTypes.map((type) => [type, document.entries.filter((entry) => entry.type === type)]));
    const labels = { added: "Added", changed: "Changed", deprecated: "Deprecated", removed: "Removed", fixed: "Fixed", security: "Security" };
    const sections = allowedTypes.filter((type) => grouped[type].length).map((type) => `### ${labels[type]}\n\n${grouped[type].map((entry) => `- ${entry.descriptions.en}`).join("\n")}`).join("\n\n");
    writeFileSync(changelogPath, changelog.replace("## [Unreleased]", `## [Unreleased]\n\n${marker} - ${document.buildDate.slice(0, 10)}\n\n${sections}`), "utf8");
  }
}

try {
  const command = process.argv[2] ?? "validate";
  if (command === "validate") validate();
  else if (command === "generate") generate();
  else if (command === "release") release();
  else throw new Error(`Comando sconosciuto: ${command}`);
} catch (error) {
  console.error(error.message);
  process.exitCode = 1;
}
