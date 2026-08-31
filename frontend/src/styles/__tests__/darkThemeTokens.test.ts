import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { describe, expect, it } from "vitest";

const here = dirname(fileURLToPath(import.meta.url));
const dark = readFileSync(resolve(here, "../tokens-dark.less"), "utf8");
const light = readFileSync(resolve(here, "../tokens.less"), "utf8");
const elementPlus = readFileSync(resolve(here, "../element-plus.less"), "utf8");

function tokenValue(source: string, name: string): string {
  const match = source.match(new RegExp(`${name}:\\s*([^;]+);`));
  if (!match?.[1]) {
    throw new Error(`Token ${name} not found`);
  }
  return match[1].trim();
}

describe("dark theme tokens (dashboard/setup polish)", () => {
  it("keeps light-mode borders on the previous gray-200 card hairline", () => {
    expect(tokenValue(light, "--color-border")).toBe("var(--color-gray-200)");
    expect(elementPlus).toContain("border: 1px solid var(--color-border)");
    expect(elementPlus).not.toContain(
      "border: 1px solid var(--color-gray-200)",
    );
  });

  it("uses low-contrast navy hairlines in dark mode instead of inverted gray-400/500", () => {
    expect(tokenValue(dark, "--color-border")).toMatch(
      /rgba\(\s*168,\s*191,\s*225/i,
    );
    expect(tokenValue(dark, "--el-border-color")).toBe("var(--color-border)");
    expect(tokenValue(dark, "--el-border-color-extra-light")).not.toBe(
      "var(--color-gray-400)",
    );
    expect(dark).not.toMatch(
      /--el-border-color-extra-light:\s*var\(--color-gray-400\)/,
    );
  });

  it("makes the current sidebar item stronger than hover/border", () => {
    expect(tokenValue(dark, "--color-sidebar-active")).toBe("#1e4f8c");
    expect(tokenValue(dark, "--color-sidebar-active")).not.toBe(
      tokenValue(dark, "--color-sidebar-border"),
    );
    expect(tokenValue(dark, "--color-sidebar-active")).not.toBe(
      tokenValue(dark, "--color-sidebar-hover"),
    );
  });

  it("brightens link/name text in dark while light stays primary-500", () => {
    expect(tokenValue(light, "--color-text-link")).toBe(
      "var(--color-primary-500)",
    );
    expect(tokenValue(dark, "--color-text-link")).toBe(
      "var(--color-primary-300)",
    );
  });
});
