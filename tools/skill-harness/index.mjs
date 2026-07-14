#!/usr/bin/env node
import fs from "node:fs";
import path from "node:path";
import { scanSkills, makeRegistry, serializeRegistry, validateRepository } from "./lib.mjs";

const args = process.argv.slice(2);
const command = args.shift() ?? "list";
const repoArg = args.find((x) => x.startsWith("--root="))?.slice(7) ?? process.env.KINHUB_ROOT ?? process.cwd();
const repoRoot = path.resolve(repoArg);
const skillsRoot = path.join(repoRoot, "skills");
const fail = (message, code = 1) => { console.error(message); process.exitCode = code; };

function validated() {
  const result = scanSkills(skillsRoot);
  if (result.errors.length) { fail(result.errors.map((x) => `- ${x}`).join("\n")); return null; }
  return result;
}

if (command === "list") {
  const result = validated();
  if (result) for (const skill of makeRegistry(result.skills).skills) console.log(`${skill.id}\t${skill.name}\t${skill.description}`);
} else if (command === "read") {
  const selector = args.find((x) => !x.startsWith("--"));
  if (!selector) fail("Uso: skills:read -- <id|area>", 2);
  else {
    const result = validated();
    const matches = result?.skills.filter((x) => x.metadata.id === selector || x.metadata.area === selector) ?? [];
    if (matches.length !== 1) fail(matches.length ? `Selettore ambiguo: ${selector}` : `Skill non trovata: ${selector}`, 2);
    else process.stdout.write(fs.readFileSync(path.join(skillsRoot, matches[0].path), "utf8"));
  }
} else if (command === "build") {
  const result = validated();
  if (result) {
    const target = path.join(skillsRoot, "registry.json");
    fs.writeFileSync(target, serializeRegistry(makeRegistry(result.skills)), "utf8");
    console.log(`Registry aggiornato: ${path.relative(repoRoot, target)} (${result.skills.length} skill)`);
  }
} else if (command === "validate") {
  const result = validateRepository(repoRoot);
  if (result.errors.length) fail(result.errors.map((x) => `- ${x}`).join("\n"));
  else console.log(`Validate ${result.skills.length} skill e registry: OK`);
} else if (command === "watch") {
  let fingerprint = "";
  const rebuild = () => {
    const result = scanSkills(skillsRoot);
    const next = result.skills.map((x) => x.checksum).join(":");
    if (next === fingerprint) return;
    fingerprint = next;
    if (result.errors.length) console.error(result.errors.map((x) => `- ${x}`).join("\n"));
    else {
      fs.writeFileSync(path.join(skillsRoot, "registry.json"), serializeRegistry(makeRegistry(result.skills)), "utf8");
      console.log(`[${new Date().toISOString()}] Registry aggiornato (${result.skills.length} skill)`);
    }
  };
  rebuild();
  console.log("Watch attivo; Ctrl+C per terminare.");
  setInterval(rebuild, 750);
} else fail(`Comando sconosciuto: ${command}. Comandi: list, read, build, validate, watch`, 2);
