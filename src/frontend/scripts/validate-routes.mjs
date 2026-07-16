import { readFileSync } from "node:fs";
import { join, resolve } from "node:path";

const root = resolve(import.meta.dirname, "../src");
const routes = JSON.parse(readFileSync(join(root, "routes/route-registry.json"), "utf8"));
const get = (object, path) => path.split(".").reduce((value, key) => value?.[key], object);
for (const locale of ["it", "en"]) {
  const pages = JSON.parse(readFileSync(join(root, `locales/${locale}/pages.json`), "utf8"));
  const help = JSON.parse(readFileSync(join(root, `locales/${locale}/help.json`), "utf8"));
  for (const route of routes) {
    if (typeof get(pages, route.titleKey) !== "string") throw new Error(`${locale}: titolo mancante per ${route.path}`);
    const entry = get(help, route.helpKey);
    for (const field of ["summary", "purpose", "actions", "prerequisites", "fields", "limits"]) {
      if (typeof entry?.[field] !== "string") throw new Error(`${locale}: help ${field} mancante per ${route.path}`);
    }
  }
}
console.log(`Route documentate: ${routes.length}`);
