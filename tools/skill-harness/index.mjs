#!/usr/bin/env node
import { createHash } from "node:crypto";
import { existsSync, readFileSync, readdirSync, realpathSync, statSync, watch, writeFileSync } from "node:fs";
import { dirname, isAbsolute, join, relative, resolve, sep } from "node:path";

const root = resolve(import.meta.dirname, "../..");
const realRoot = realpathSync(root);
const skillsRoot = join(root, ".agents/skills");
const registryPath = join(skillsRoot, "registry.json");
const functionsRoot = join(root, "src/backend/applications/DA.KinHub.Functions/Functions");
const apiRoutesPath = join(root, "src/backend/applications/DA.KinHub.Functions/Http/ApiRoutes.cs");
const openApiPath = join(root, "openapi.yaml");
const workflowsRoot = join(root, ".github/workflows");
const infrastructureRoot = join(root, "infra");
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

function isOutsideRoot(path) {
  return isAbsolute(path) || path === ".." || path.startsWith(`..${sep}`);
}

function repositoryFile(reference, source, kind) {
  const path = resolve(root, reference);
  const repositoryPath = relative(root, path);
  if (isOutsideRoot(repositoryPath)) throw new Error(`${source}: ${kind} fuori dal repository: ${reference}`);
  if (!existsSync(path) || !statSync(path).isFile()) throw new Error(`${source}: ${kind} inesistente: ${reference}`);
  if (isOutsideRoot(relative(realRoot, realpathSync(path)))) throw new Error(`${source}: ${kind} risolve fuori dal repository: ${reference}`);
  return { path, repositoryPath };
}

function loadReferences(metadata, source) {
  if (!metadata.references) return [];
  const seen = new Set();
  return metadata.references.split(",").map((value) => value.trim()).filter(Boolean).map((reference) => {
    if (!/\.(md|json)$/i.test(reference)) throw new Error(`${source}: reference non documentale: ${reference}`);
    const { path, repositoryPath } = repositoryFile(reference, source, "reference");
    if (seen.has(repositoryPath)) throw new Error(`${source}: reference duplicata: ${reference}`);
    seen.add(repositoryPath);
    const content = normalizeText(readFileSync(path, "utf8"));
    return {
      path: repositoryPath.replaceAll("\\", "/"),
      checksum: createHash("sha256").update(content).digest("hex")
    };
  });
}

function loadSkills() {
  // Agent skills can coexist with the repository skill schema in this directory.
  const files = walk(skillsRoot).filter((path) => path.endsWith("SKILL.md")).filter((path) => /^---\r?\nid\s*:/m.test(readFileSync(path, "utf8")));
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
    const references = loadReferences(metadata, relative(root, path));
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
        repositoryFile(item.source, relative(root, catalogPath), "source");
      }
    }
    return {
      id: metadata.id,
      name: metadata.name,
      version: metadata.version,
      area: metadata.area,
      description: metadata.description,
      path: relative(root, path).replaceAll("\\", "/"),
      references,
      catalog: metadata.catalog ? relative(root, resolve(dirname(path), metadata.catalog)).replaceAll("\\", "/") : null,
      catalogItems: catalog.map(({ id, name, source }) => ({ id, name, source })),
      checksum: createHash("sha256").update(content).digest("hex")
    };
  }).sort((a, b) => a.id.localeCompare(b.id));
}

function validateOpenApiRoutes() {
  const apiRoutes = normalizeText(readFileSync(apiRoutesPath, "utf8"));
  const routeConstants = new Map(
    [...apiRoutes.matchAll(/^    public static class (\w+)\s*\{([\s\S]*?)(?=^    public static class|^})/gm)]
      .flatMap(([, group, content]) => [...content.matchAll(/public const string (\w+) = "([^"]+)";/g)]
        .map(([, name, route]) => [`${group}.${name}`, route]))
  );
  const triggers =
    walk(functionsRoot)
      .filter((path) => path.endsWith(".cs"))
      .flatMap((path) => [...normalizeText(readFileSync(path, "utf8")).matchAll(/\[HttpTrigger\(([\s\S]*?)\)\]/g)]);
  const functionRoutes = new Set(triggers.map(([, trigger]) => {
    const route = trigger.match(/Route\s*=\s*ApiRoutes\.(\w+\.\w+)/)?.[1];
    if (!route) throw new Error("Ogni HTTP Function deve dichiarare Route = ApiRoutes.<gruppo>.<nome>");
    return routeConstants.get(route);
  }));

  if (functionRoutes.has(undefined)) throw new Error("Impossibile risolvere una route HTTP Function da ApiRoutes.cs");

  const documentedRoutes = new Set(
    [...normalizeText(readFileSync(openApiPath, "utf8")).matchAll(/^  \/(.+):\s*$/gm)]
      .map(([, route]) => route)
  );
  const missingRoutes = [...functionRoutes].filter((route) => !documentedRoutes.has(route));
  if (missingRoutes.length > 0) {
    throw new Error(`openapi.yaml non documenta le route HTTP Function: ${missingRoutes.sort().join(", ")}`);
  }
}

function validateInfrastructureContracts() {
  const workflowFiles = readdirSync(workflowsRoot).filter((file) => file.endsWith(".yml") || file.endsWith(".yaml"));
  const allowed = new Set(["ci.yml", "infrastructure.yml", "release.yml"]);
  const unexpected = workflowFiles.filter((file) => !allowed.has(file));
  if (unexpected.length > 0) throw new Error(`Workflow non ammessi: ${unexpected.join(", ")}`);
  if (workflowFiles.length !== allowed.size) throw new Error("Mancano workflow infrastrutturali obbligatori");

  const workflowContents = workflowFiles.map((file) => readFileSync(join(workflowsRoot, file), "utf8"));
  for (const [index, content] of workflowContents.entries()) {
    if (/pull_request_target/i.test(content)) throw new Error(`${workflowFiles[index]} usa pull_request_target`);
    for (const match of content.matchAll(/uses:\s*([^\s#]+)@([^\s#]+)/g)) {
      const reference = match[2];
      if (!/^[0-9a-f]{40}$/i.test(reference)) throw new Error(`${workflowFiles[index]}: action non fissata a SHA completo: ${match[1]}`);
    }
  }

  const bicepFiles = walk(infrastructureRoot).filter((path) => path.endsWith(".bicep"));
  const bicep = bicepFiles.map((path) => normalizeText(readFileSync(path, "utf8"))).join("\n");
  if (/uniqueString\s*\(/.test(bicep)) throw new Error("Bicep non deve usare uniqueString");
  if (/namingPrefix/.test(bicep)) throw new Error("Bicep non deve usare namingPrefix");
  const staticWebApp = normalizeText(readFileSync(join(infrastructureRoot, "modules/static-web-app.bicep"), "utf8"));
  if (!/sku:\s*\{\s*name:\s*'Standard',\s*tier:\s*'Standard'/s.test(staticWebApp)) throw new Error("Static Web Apps deve usare SKU Standard");
  const infrastructure = normalizeText(readFileSync(join(workflowsRoot, "infrastructure.yml"), "utf8"));
  if (!/what-if/.test(infrastructure) || !/--mode\s+Incremental/.test(infrastructure)) throw new Error("Infrastructure workflow deve eseguire what-if e deployment incremental");
  for (const file of ["infrastructure.yml", "release.yml"]) {
    const content = normalizeText(readFileSync(join(workflowsRoot, file), "utf8"));
    if (!/^concurrency:\s*$/m.test(content) || !/cancel-in-progress:\s*false/.test(content)) throw new Error(`${file}: concurrency senza cancel-in-progress false`);
  }
  const release = normalizeText(readFileSync(join(workflowsRoot, "release.yml"), "utf8"));
  if (!/actions:\s*read/.test(release) || !/actions\/workflows\/infrastructure\.yml\/runs\?head_sha=\$\{GITHUB_SHA\}/.test(release)) {
    throw new Error("Release workflow deve attendere il run Infrastructure dello stesso SHA");
  }
  if (!/completed:success/.test(release) || !/completed:failure\|completed:cancelled/.test(release) || !/in_progress/.test(release)) {
    throw new Error("Release workflow deve gestire success, failure, cancelled e in_progress dell'infrastruttura");
  }
  if (!/az staticwebapp secrets list/.test(release) || !/steps\.static_web_app_token\.outputs\.deployment_token/.test(release)) {
    throw new Error("Release workflow deve recuperare il token Static Web Apps tramite Azure OIDC");
  }
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
  validateOpenApiRoutes();
  validateInfrastructureContracts();
  const expected = serializedRegistry();
  if (!existsSync(registryPath)) throw new Error(".agents/skills/registry.json mancante: eseguire npm run skills:build");
  if (readFileSync(registryPath, "utf8") !== expected) throw new Error(".agents/skills/registry.json non aggiornato: eseguire npm run skills:build");
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
