import { readFileSync, readdirSync } from "node:fs";
import { join, resolve } from "node:path";

const root = resolve(import.meta.dirname, "../src/locales");
const flatten = (value, prefix = "") => Object.entries(value).flatMap(([key, child]) => {
  const path = prefix ? `${prefix}.${key}` : key;
  return child && typeof child === "object" && !Array.isArray(child) ? flatten(child, path) : [path];
});

const files = readdirSync(join(root, "it")).filter((name) => name.endsWith(".json")).sort();
const enFiles = readdirSync(join(root, "en")).filter((name) => name.endsWith(".json")).sort();
if (JSON.stringify(files) !== JSON.stringify(enFiles)) throw new Error("Namespace i18n diversi tra it ed en");
for (const file of files) {
  const it = flatten(JSON.parse(readFileSync(join(root, "it", file), "utf8"))).sort();
  const en = flatten(JSON.parse(readFileSync(join(root, "en", file), "utf8"))).sort();
  if (JSON.stringify(it) !== JSON.stringify(en)) throw new Error(`Chiavi i18n non allineate in ${file}`);
}
console.log(`Traduzioni allineate: ${files.length} namespace`);
