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

function hexChannelToLinear(channel: number): number {
  const c = channel / 255;
  return c <= 0.04045 ? c / 12.92 : ((c + 0.055) / 1.055) ** 2.4;
}

function relativeLuminance(hex: string): number {
  if (!/^#[0-9a-fA-F]{6}$/.test(hex)) {
    throw new Error(`Expected #rrggbb, got ${hex}`);
  }
  const n = Number.parseInt(hex.slice(1), 16);
  const r = hexChannelToLinear((n >> 16) & 255);
  const g = hexChannelToLinear((n >> 8) & 255);
  const b = hexChannelToLinear(n & 255);
  return 0.2126 * r + 0.7152 * g + 0.0722 * b;
}

function contrastRatio(foreground: string, background: string): number {
  const a = relativeLuminance(foreground);
  const b = relativeLuminance(background);
  const [hi, lo] = a > b ? [a, b] : [b, a];
  return (hi + 0.05) / (lo + 0.05);
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

  it("pairs the current-page chip fill with light text that meets WCAG AA", () => {
    const activeFill = tokenValue(dark, "--color-sidebar-active");
    const activeText = tokenValue(dark, "--color-sidebar-text-active");
    expect(activeFill).toBe("#2563a8");
    expect(activeText).toBe("#eef2f9");
    expect(activeFill).not.toBe(tokenValue(dark, "--color-sidebar-border"));
    expect(activeFill).not.toBe(tokenValue(dark, "--color-sidebar-hover"));
    expect(contrastRatio(activeText, activeFill)).toBeGreaterThanOrEqual(4.5);
  });

  it("keeps public header text light on the navy public background", () => {
    expect(tokenValue(dark, "--color-public-header-text")).toBe(
      "var(--color-primary-200)",
    );
    expect(tokenValue(dark, "--color-public-text")).toBe(
      "var(--color-primary-200)",
    );
    expect(tokenValue(light, "--color-public-header-text")).toBe(
      "var(--color-primary-700)",
    );
  });

  it("brightens link/name text in dark while light stays primary-500", () => {
    expect(tokenValue(light, "--color-text-link")).toBe(
      "var(--color-primary-500)",
    );
    expect(tokenValue(dark, "--color-text-link")).toBe(
      "var(--color-primary-200)",
    );
  });
});
