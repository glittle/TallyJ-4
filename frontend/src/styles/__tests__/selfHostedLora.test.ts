import { readdirSync, readFileSync, statSync } from "node:fs";
import { dirname, join, relative, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { describe, expect, it } from "vitest";

const frontendRoot = resolve(
  dirname(fileURLToPath(import.meta.url)),
  "../../..",
);
const googleFontsHost = ["fonts", "googleapis.com"].join(".");
const gstaticFontsHost = ["fonts", "gstatic.com"].join(".");

function sourceFiles(dir: string, found: string[] = []): string[] {
  for (const entry of readdirSync(dir)) {
    if (entry === "node_modules" || entry === "dist") {
      continue;
    }
    const full = join(dir, entry);
    if (statSync(full).isDirectory()) {
      sourceFiles(full, found);
      continue;
    }
    if (/\.(vue|ts|js|css|less|html|json)$/.test(entry)) {
      found.push(full);
    }
  }
  return found;
}

describe("self-hosted Lora", () => {
  it("does not request Google font hosts from frontend source", () => {
    const files = [
      ...sourceFiles(join(frontendRoot, "src")),
      join(frontendRoot, "index.html"),
    ];
    const hits = files.flatMap((file) => {
      const text = readFileSync(file, "utf8");
      if (text.includes(googleFontsHost) || text.includes(gstaticFontsHost)) {
        return [relative(frontendRoot, file)];
      }
      return [];
    });

    expect(hits).toEqual([]);
  });

  it("loads Lora 400-700 normal and italic from @fontsource", () => {
    const main = readFileSync(join(frontendRoot, "src/main.ts"), "utf8");
    for (const weight of [400, 500, 600, 700]) {
      expect(main).toContain(`@fontsource/lora/${weight}.css`);
      expect(main).toContain(`@fontsource/lora/${weight}-italic.css`);
    }
  });
});
