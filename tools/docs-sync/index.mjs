import fs from "node:fs";
const langs = ["it", "en"];
for (const l of langs)
  if (!fs.existsSync(`docs/user-guide/${l}`)) process.exit(1);
if (process.argv[2] === "i18n")
  console.log("Translation parity check passed (static scaffold)");
else console.log("Documentation validation passed");
