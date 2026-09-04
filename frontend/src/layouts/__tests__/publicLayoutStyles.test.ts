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

  it("styles the Beta mark with the Latin primary stack", () => {
    expect(source).toContain(".version-beta");
    expect(source).toContain("font-family: var(--font-family-primary)");
    expect(source).toContain("font-weight: var(--font-weight-semibold)");
  });
});
