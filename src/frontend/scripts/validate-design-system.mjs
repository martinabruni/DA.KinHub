import { readdirSync, readFileSync } from "node:fs";
import { extname, join, relative } from "node:path";
import { fileURLToPath } from "node:url";

const sourceRootPath = fileURLToPath(new URL("../src/", import.meta.url));

const bannedPatterns = [
  {
    label: "demo route /design-system",
    test: (content) => content.includes("/design-system") || content.includes("DesignSystemPage") || content.includes("designSystem")
  },
  {
    label: "legacy design-system css namespace .ds-*",
    test: (content) => /\.ds-[\w-]+/.test(content) || /className\s*=\s*["'`][^"'`]*\bds-/.test(content)
  },
  {
    label: "legacy button utility class",
    test: (content) => hasLegacyClassToken(content, "button")
  },
  {
    label: "legacy state-card utility class",
    test: (content) => hasLegacyClassToken(content, "state-card")
  },
  {
    label: "legacy settings-card utility class",
    test: (content) => hasLegacyClassToken(content, "settings-card")
  },
  {
    label: "legacy feature-card utility class",
    test: (content) => hasLegacyClassToken(content, "feature-card")
  },
  {
    label: "legacy card-grid utility class",
    test: (content) => hasLegacyClassToken(content, "card-grid")
  },
  {
    label: "legacy control utility class",
    test: (content) => hasLegacyClassToken(content, "control")
  },
  {
    label: "parallel UI library import",
    test: (content) => /from\s+["'](@mui|antd|react-bootstrap|bootstrap|@chakra-ui|semantic-ui-react)/.test(content)
  }
];

const allowedLegacyCssFiles = new Set(["src/styles.css"]);
const allowedExtensions = new Set([".ts", ".tsx", ".json", ".css"]);
const failures = [];

scan(sourceRootPath);

if (failures.length > 0) {
  console.error("Design system validation failed:");
  for (const failure of failures) {
    console.error(`- ${failure.file}: ${failure.reason}`);
  }
  process.exit(1);
}

console.log("Design system validation passed.");

function scan(directoryPath) {
  for (const entry of readdirSync(directoryPath, { withFileTypes: true })) {
    const entryPath = join(directoryPath, entry.name);
    if (entry.isDirectory()) {
      scan(entryPath);
      continue;
    }

    if (!allowedExtensions.has(extname(entry.name))) {
      continue;
    }

    const relativePath = normalize(relative(sourceRootPath, entryPath));
    const file = `src/${relativePath}`;
    const content = readFileSync(entryPath, "utf8");

    for (const pattern of bannedPatterns) {
      if (pattern.test(content)) {
        failures.push({ file, reason: pattern.label });
      }
    }

    if (file.endsWith("styles.css") || file.endsWith(".css")) {
      validateCss(file, content);
    }
  }
}

function validateCss(file, content) {
  if (!allowedLegacyCssFiles.has(file)) {
    return;
  }

  const bannedSelectors = [".button", ".state-card", ".settings-card", ".feature-card", ".card-grid", ".control", ".ds-"];
  for (const selector of bannedSelectors) {
    if (content.includes(selector)) {
      failures.push({ file, reason: `legacy selector ${selector}` });
    }
  }
}

function normalize(value) {
  return value.replace(/\\/g, "/");
}

function hasLegacyClassToken(content, token) {
  const matches = content.matchAll(/className\s*=\s*(["'`])([^"'`]+)\1/g);
  for (const match of matches) {
    const tokens = match[2].split(/\s+/);
    if (tokens.includes(token)) {
      return true;
    }
  }

  return false;
}
