#!/usr/bin/env node
import { createHash } from "node:crypto";
import { existsSync, readFileSync, readdirSync, statSync, watch, writeFileSync } from "node:fs";
import { dirname, join, relative, resolve } from "node:path";

const root = resolve(import.meta.dirname, "../..");
const skillsRoot = join(root, "skills");
const registryPath = join(skillsRoot, "registry.json");
const requiredHeadings = [
  "## Scopo",
  "## Quando usare",
  "## Quando non usare",
  "## Componenti e servizi disponibili",
  "## API e interfacce",
  "## Esempi",
  "## Dipendenze",
  "## Vincoli",
  "## Test richiesti",
  "## Checklist di aggiornamento",
  "## Changelog"
];

function normalizeText(content) {
  return content.replace(/\r\n/g, "\n");
}

function walk(directory) {
  return readdirSync(directory, { withFileTypes: true }).flatMap((entry) => {
    const path = join(directory, entry.name);
    return entry.isDirectory() ? walk(path) : [path];
  });
}

function frontmatter(content, source) {
  const match = content.match(/^---\r?\n([\s\S]*?)\r?\n---\r?\n/);
  if (!match) throw new Error(`${source}: frontmatter mancante`);
  return Object.fromEntries(match[1].split(/\r?\n/).filter(Boolean).map((line) => {
    const separator = line.indexOf(":");
    if (separator < 1) throw new Error(`${source}: metadato non valido: ${line}`);
    return [line.slice(0, separator).trim(), line.slice(separator + 1).trim().replace(/^['"]|['"]$/g, "")];
  }));
}

function loadSkills() {
  const files = walk(skillsRoot).filter((path) => path.endsWith("SKILL.md"));
  if (files.length === 0) throw new Error("Nessuna skill trovata");
  const ids = new Set();
  const catalogIds = new Set();
  return files.map((path) => {
    const content = normalizeText(readFileSync(path, "utf8"));
    const metadata = frontmatter(content, relative(root, path));
    for (const key of ["id", "name", "version", "area", "description"]) {
      if (!metadata[key]) throw new Error(`${relative(root, path)}: metadato ${key} mancante`);
    }
    if (ids.has(metadata.id)) throw new Error(`Skill duplicata: ${metadata.id}`);
    ids.add(metadata.id);
    for (const heading of requiredHeadings) {
      if (!content.includes(heading)) throw new Error(`${relative(root, path)}: sezione mancante ${heading}`);
    }
    let catalog = [];
    if (metadata.catalog) {
      const catalogPath = resolve(dirname(path), metadata.catalog);
      if (!existsSync(catalogPath)) throw new Error(`${relative(root, path)}: catalogo inesistente ${metadata.catalog}`);
      const document = JSON.parse(readFileSync(catalogPath, "utf8"));
      catalog = document.items ?? [];
      for (const item of catalog) {
        if (!item.id || !item.name || !item.source) throw new Error(`${relative(root, catalogPath)}: item incompleto`);
        const globalId = `${metadata.area}:${item.id}`;
        if (catalogIds.has(globalId)) throw new Error(`Elemento catalogo duplicato: ${globalId}`);
        catalogIds.add(globalId);
        if (!existsSync(resolve(root, item.source))) throw new Error(`${relative(root, catalogPath)}: source inesistente ${item.source}`);
      }
    }
    return {
      id: metadata.id,
      name: metadata.name,
      version: metadata.version,
      area: metadata.area,
      description: metadata.description,
      path: relative(root, path).replaceAll("\\", "/"),
      catalog: metadata.catalog ? relative(root, resolve(dirname(path), metadata.catalog)).replaceAll("\\", "/") : null,
      catalogItems: catalog.map(({ id, name, source }) => ({ id, name, source })),
      checksum: createHash("sha256").update(content).digest("hex")
    };
  }).sort((a, b) => a.id.localeCompare(b.id));
}

function registry() {
  return { schemaVersion: 1, app: "KinHub", generatedBy: "tools/skill-harness", skills: loadSkills() };
}

function serializedRegistry() {
  return `${JSON.stringify(registry(), null, 2)}\n`;
}

function build() {
  writeFileSync(registryPath, serializedRegistry(), "utf8");
  console.log(`Registry aggiornato: ${relative(root, registryPath)}`);
}

function validate() {
  const expected = serializedRegistry();
  if (!existsSync(registryPath)) throw new Error("skills/registry.json mancante: eseguire npm run skills:build");
  if (readFileSync(registryPath, "utf8") !== expected) throw new Error("skills/registry.json non aggiornato: eseguire npm run skills:build");
  console.log(`Skill valide: ${registry().skills.length}`);
}

function list() {
  for (const skill of registry().skills) console.log(`${skill.id}\t${skill.area}\t${skill.description}`);
}

function readSkill(id) {
  if (!id) throw new Error("Uso: npm run skills:read -- <skill-id-o-area>");
  const matches = registry().skills.filter((skill) => skill.id === id || skill.area === id);
  if (matches.length !== 1) throw new Error(`Skill non trovata o ambigua: ${id}`);
  process.stdout.write(readFileSync(join(root, matches[0].path), "utf8"));
}

function watchSkills() {
  build();
  let timer;
  watch(skillsRoot, { recursive: true }, (_event, file) => {
    if (!file || file === "registry.json") return;
    clearTimeout(timer);
    timer = setTimeout(() => {
      try { build(); } catch (error) { console.error(error.message); }
    }, 150);
  });
  console.log("Watch skill attivo. Ctrl+C per terminare.");
}

try {
  const [command = "validate", argument] = process.argv.slice(2);
  ({ build, validate, list, read: () => readSkill(argument), watch: watchSkills }[command] ?? (() => { throw new Error(`Comando sconosciuto: ${command}`); }))();
} catch (error) {
  console.error(error.message);
  process.exitCode = 1;
}
