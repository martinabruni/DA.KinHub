import crypto from "node:crypto";
import fs from "node:fs";
import path from "node:path";

export const REQUIRED_METADATA = ["id", "name", "area", "description", "version"];
export const REQUIRED_SECTIONS = [
  "Scopo", "Quando usare", "Quando non usare", "Componenti e servizi disponibili",
  "API e interfacce", "Esempi", "Dipendenze", "Vincoli", "Test richiesti",
  "Checklist di aggiornamento", "Changelog",
];

const normalize = (value) => value.replaceAll("\\", "/");
const compare = (a, b) => a.localeCompare(b, "en");

export function parseSkill(file, skillsRoot) {
  const source = fs.readFileSync(file, "utf8").replace(/^\uFEFF/, "");
  const match = source.match(/^---\r?\n([\s\S]*?)\r?\n---\r?\n/);
  const errors = [];
  const metadata = {};
  if (!match) errors.push("front matter YAML mancante");
  else {
    for (const line of match[1].split(/\r?\n/)) {
      if (!line.trim() || line.trimStart().startsWith("#")) continue;
      const field = line.match(/^([a-z][a-z0-9_-]*):\s*(.*?)\s*$/);
      if (!field) errors.push(`metadato non valido: ${line}`);
      else metadata[field[1]] = field[2].replace(/^(["'])(.*)\1$/, "$2");
    }
  }
  for (const key of REQUIRED_METADATA) if (!metadata[key]) errors.push(`metadato obbligatorio mancante: ${key}`);
  if (metadata.id && !/^[a-z0-9]+(?:-[a-z0-9]+)*$/.test(metadata.id)) errors.push("id deve essere kebab-case");
  if (metadata.area && !/^[a-z0-9]+(?:-[a-z0-9]+)*$/.test(metadata.area)) errors.push("area deve essere kebab-case");
  if (metadata.version && !/^\d+\.\d+\.\d+$/.test(metadata.version)) errors.push("version deve essere SemVer");
  const headings = new Set([...source.matchAll(/^##\s+(.+?)\s*$/gm)].map((x) => x[1].trim().toLocaleLowerCase("it")));
  for (const section of REQUIRED_SECTIONS) if (!headings.has(section.toLocaleLowerCase("it"))) errors.push(`sezione obbligatoria mancante: ${section}`);

  const skillDir = path.dirname(file);
  const references = [];
  for (const link of source.matchAll(/\[[^\]]*\]\(([^)]+)\)/g)) {
    const target = link[1].split("#", 1)[0].trim();
    if (!target || /^(https?:|mailto:)/i.test(target)) continue;
    const resolved = path.resolve(skillDir, decodeURIComponent(target));
    references.push(normalize(path.relative(skillsRoot, resolved)));
    if (!fs.existsSync(resolved)) errors.push(`riferimento locale non valido: ${target}`);
  }
  const catalog = path.join(skillDir, "catalog.json");
  let catalogPath = null;
  if (fs.existsSync(catalog)) {
    catalogPath = normalize(path.relative(skillsRoot, catalog));
    try {
      const data = JSON.parse(fs.readFileSync(catalog, "utf8"));
      if (!Array.isArray(data.items)) errors.push("catalog.json deve contenere un array items");
      else {
        const ids = new Set();
        for (const item of data.items) {
          if (!item?.id || !item?.name || !item?.description) errors.push("ogni elemento del catalogo richiede id, name e description");
          else if (ids.has(item.id)) errors.push(`id catalogo duplicato: ${item.id}`);
          else ids.add(item.id);
        }
      }
    } catch (error) { errors.push(`catalog.json non valido: ${error.message}`); }
  }
  return {
    metadata, errors, references: [...new Set(references)].sort(compare), catalogPath,
    path: normalize(path.relative(skillsRoot, file)),
    checksum: `sha256:${crypto.createHash("sha256").update(source).digest("hex")}`,
  };
}

export function scanSkills(skillsRoot) {
  if (!fs.existsSync(skillsRoot)) return { skills: [], errors: ["cartella skills mancante"] };
  const files = [];
  const visit = (directory) => {
    for (const entry of fs.readdirSync(directory, { withFileTypes: true }).sort((a, b) => compare(a.name, b.name))) {
      const item = path.join(directory, entry.name);
      if (entry.isDirectory()) visit(item);
      else if (entry.name === "SKILL.md") files.push(item);
    }
  };
  visit(skillsRoot);
  const skills = files.map((file) => parseSkill(file, skillsRoot));
  const errors = [];
  const ids = new Map();
  const areas = new Map();
  for (const skill of skills) {
    for (const error of skill.errors) errors.push(`${skill.path}: ${error}`);
    for (const [key, map] of [[skill.metadata.id, ids], [skill.metadata.area, areas]]) {
      if (!key) continue;
      if (map.has(key)) errors.push(`${skill.path}: valore duplicato '${key}' (già in ${map.get(key)})`);
      else map.set(key, skill.path);
    }
  }
  if (!skills.length) errors.push("nessuna skill trovata");
  return { skills, errors };
}

export function makeRegistry(skills) {
  return {
    schemaVersion: 1,
    skills: skills.map((skill) => ({
      id: skill.metadata.id, name: skill.metadata.name, area: skill.metadata.area,
      description: skill.metadata.description, version: skill.metadata.version,
      path: `skills/${skill.path}`, ...(skill.catalogPath ? { catalog: `skills/${skill.catalogPath}` } : {}),
      checksum: skill.checksum, references: skill.references.map((x) => `skills/${x}`),
    })).sort((a, b) => compare(a.id, b.id)),
  };
}

export const serializeRegistry = (registry) => `${JSON.stringify(registry, null, 2)}\n`;

export function validateRepository(repoRoot, { checkRegistry = true } = {}) {
  const skillsRoot = path.join(repoRoot, "skills");
  const result = scanSkills(skillsRoot);
  const expected = serializeRegistry(makeRegistry(result.skills));
  const registryPath = path.join(skillsRoot, "registry.json");
  if (checkRegistry && (!fs.existsSync(registryPath) || fs.readFileSync(registryPath, "utf8").replaceAll("\r\n", "\n") !== expected)) {
    result.errors.push("skills/registry.json non aggiornato; eseguire skills:build");
  }
  return { ...result, expected, registryPath };
}
