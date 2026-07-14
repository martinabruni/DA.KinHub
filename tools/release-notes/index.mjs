import fs from "node:fs";
const files = fs
  .readdirSync("changes")
  .filter((x) => x.endsWith(".md") && x !== "README.md");
if (process.argv[2] === "validate")
  for (const f of files) {
    const s = fs.readFileSync(`changes/${f}`, "utf8");
    if (!s.includes("type:") || !s.includes("area:"))
      throw Error(`Invalid fragment ${f}`);
  }
console.log(`Validated ${files.length} change fragments`);
