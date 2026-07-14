import fs from "node:fs";
import path from "node:path";
const root = path.resolve("skills");
const items = fs.existsSync(root)
  ? fs
      .readdirSync(root, { withFileTypes: true })
      .filter((x) => x.isDirectory())
      .map((x) => x.name)
  : [];
const cmd = process.argv[2] || "list";
if (cmd === "list") {
  console.log(items.join("\n"));
  process.exit(0);
}
if (cmd === "validate") {
  for (const x of items)
    if (!fs.existsSync(path.join(root, x, "SKILL.md")))
      throw Error(`Missing SKILL.md: ${x}`);
  console.log(`Validated ${items.length} skills`);
  process.exit(0);
}
console.error("Unknown command");
process.exit(2);
