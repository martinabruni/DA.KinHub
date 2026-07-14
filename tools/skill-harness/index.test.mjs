import assert from "node:assert/strict";
import fs from "node:fs";
import os from "node:os";
import path from "node:path";
import test from "node:test";
import { makeRegistry, scanSkills, validateRepository } from "./lib.mjs";

const sections = ["Scopo", "Quando usare", "Quando non usare", "Componenti e servizi disponibili", "API e interfacce", "Esempi", "Dipendenze", "Vincoli", "Test richiesti", "Checklist di aggiornamento", "Changelog"];
const createRepo = () => {
  const root = fs.mkdtempSync(path.join(os.tmpdir(), "kinhub-skills-"));
  const dir = path.join(root, "skills", "frontend");
  fs.mkdirSync(dir, { recursive: true });
  fs.writeFileSync(path.join(dir, "SKILL.md"), `---\nid: frontend\nname: Frontend\narea: frontend\ndescription: Pattern UI condivisi\nversion: 1.0.0\n---\n# Frontend\n${sections.map((x) => `## ${x}\nContenuto.`).join("\n")}\n`);
  return root;
};

test("scansiona una skill valida e genera un registry deterministico", () => {
  const root = createRepo();
  const result = scanSkills(path.join(root, "skills"));
  assert.deepEqual(result.errors, []);
  assert.equal(makeRegistry(result.skills).skills[0].id, "frontend");
});

test("segnala sezioni mancanti, riferimenti rotti e duplicati", () => {
  const root = createRepo();
  const first = path.join(root, "skills", "frontend", "SKILL.md");
  fs.appendFileSync(first, "\n[rotto](missing.md)\n");
  const second = path.join(root, "skills", "other");
  fs.mkdirSync(second);
  fs.copyFileSync(first, path.join(second, "SKILL.md"));
  const errors = scanSkills(path.join(root, "skills")).errors.join("\n");
  assert.match(errors, /riferimento locale non valido/);
  assert.match(errors, /valore duplicato/);
});

test("validate richiede un registry sincronizzato", () => {
  const root = createRepo();
  assert.match(validateRepository(root).errors.join("\n"), /registry\.json non aggiornato/);
});
