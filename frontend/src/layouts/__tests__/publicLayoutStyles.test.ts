import { describe, expect, it } from "vitest";
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const source = readFileSync(
  resolve(dirname(fileURLToPath(import.meta.url)), "../PublicLayout.vue"),
  "utf8",
);

describe("PublicLayout styles", () => {
  it("pins the theme control to the inline end so RTL mirrors English", () => {
    expect(source).toContain("justify-self: end");
    expect(source).toContain("text-align: end");
    expect(source).toContain("justify-self: start");
  });

  it("does not special-case Beta with a separate mark span", () => {
    expect(source).not.toContain("version-beta");
    expect(source).not.toContain("splitVersionDisplay");
    expect(source).toContain('$t("common.versionDisplay")');
  });

  it("switches to the compact language control when the header stacks at 600px", () => {
    expect(source).toContain('class="language-flags"');
    expect(source).toContain('class="language-compact"');
    expect(source).toMatch(
      /\.language-compact \{\s*display: none;[\s\S]*@media \(max-width: 600px\) \{\s*\.language-flags \{\s*display: none;\s*\}\s*\.language-compact \{\s*display: inline-block;/,
    );
  });
});
