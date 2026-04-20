import { readFileSync, writeFileSync, readdirSync, statSync } from "fs";
import { join, dirname } from "path";
import { fileURLToPath } from "url";

const __dirname = dirname(fileURLToPath(import.meta.url));

function sortObject(obj) {
  if (obj === null || typeof obj !== "object" || Array.isArray(obj)) {
    return obj;
  }
  const sorted = {};
  for (const key of Object.keys(obj).sort()) {
    sorted[key] = sortObject(obj[key]);
  }
  return sorted;
}

function sortFile(filePath) {
  const raw = readFileSync(filePath, "utf-8");
  const data = JSON.parse(raw);
  const sorted = sortObject(data);
  writeFileSync(filePath, JSON.stringify(sorted, null, 2) + "\n", "utf-8");
}

const entries = readdirSync(__dirname);
let count = 0;

for (const entry of entries) {
  const full = join(__dirname, entry);
  const stat = statSync(full);

  if (stat.isFile() && entry.endsWith(".json")) {
    sortFile(full);
    count++;
    console.log(`sorted: ${entry}`);
    continue;
  }

  if (stat.isDirectory()) {
    const files = readdirSync(full).filter((f) => f.endsWith(".json"));
    for (const f of files) {
      sortFile(join(full, f));
      count++;
      console.log(`sorted: ${entry}/${f}`);
    }
  }
}

console.log(`\nDone. Sorted ${count} file(s).`);
