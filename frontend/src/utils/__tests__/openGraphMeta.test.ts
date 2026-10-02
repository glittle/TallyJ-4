import { execFileSync } from "node:child_process";
import { mkdtempSync, readFileSync, writeFileSync } from "node:fs";
import { tmpdir } from "node:os";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { describe, expect, it } from "vitest";

const here = dirname(fileURLToPath(import.meta.url));
const frontendRoot = resolve(here, "../../..");

const DESCRIPTION =
  "TallyJ is a secure and easy-to-use election management system built primarily for Bahá'í communities. It provides a complete platform to manage elections, people, ballots, and real-time tallying.";
const TITLE = "TallyJ v4 - Election Management System";
const IMAGE_ALT = "TallyJ logo and the words Bahá’í Election System";
const PRODUCTION_ORIGIN = "https://v4.tallyj.com";
const IMAGE_URL = `${PRODUCTION_ORIGIN}/og-image.png`;

function loadIndex(): Document {
  const html = readFileSync(resolve(frontendRoot, "index.html"), "utf8");
  return new DOMParser().parseFromString(html, "text/html");
}

function meta(
  doc: Document,
  attr: "property" | "name",
  key: string,
): string | null {
  return (
    doc.querySelector(`meta[${attr}="${key}"]`)?.getAttribute("content") ?? null
  );
}

describe("Open Graph and Twitter card tags", () => {
  const doc = loadIndex();

  it("matches the meta description to og:description and twitter:description", () => {
    expect(meta(doc, "name", "description")).toBe(DESCRIPTION);
    expect(meta(doc, "property", "og:description")).toBe(DESCRIPTION);
    expect(meta(doc, "name", "twitter:description")).toBe(DESCRIPTION);
  });

  it("includes the required Open Graph tags with an absolute production image", () => {
    expect(meta(doc, "property", "og:title")).toBe(TITLE);
    expect(meta(doc, "property", "og:type")).toBe("website");
    expect(meta(doc, "property", "og:site_name")).toBe("TallyJ");
    expect(meta(doc, "property", "og:url")).toBe(`${PRODUCTION_ORIGIN}/`);
    expect(meta(doc, "property", "og:image")).toBe(IMAGE_URL);
    expect(meta(doc, "property", "og:image:width")).toBe("1200");
    expect(meta(doc, "property", "og:image:height")).toBe("627");
    expect(meta(doc, "property", "og:image:alt")).toBe(IMAGE_ALT);
    expect(IMAGE_URL).toMatch(/^https:\/\//);
    expect(meta(doc, "property", "og:url")).toMatch(/^https:\/\//);
  });

  it("includes the required Twitter card tags", () => {
    expect(meta(doc, "name", "twitter:card")).toBe("summary_large_image");
    expect(meta(doc, "name", "twitter:title")).toBe(TITLE);
    expect(meta(doc, "name", "twitter:image")).toBe(IMAGE_URL);
    expect(meta(doc, "name", "twitter:image:alt")).toBe(IMAGE_ALT);
    expect(meta(doc, "name", "twitter:creator")).toBe("@glenlittle");
    expect(meta(doc, "name", "twitter:image")).toMatch(/^https:\/\//);
  });

  it("ships a 1200x627 PNG at the stable public path, under 300 KB", () => {
    const pngPath = resolve(frontendRoot, "public/og-image.png");
    const png = readFileSync(pngPath);
    expect(png.subarray(0, 8).toString("hex")).toBe("89504e470d0a1a0a");
    expect(png.readUInt32BE(16)).toBe(1200);
    expect(png.readUInt32BE(20)).toBe(627);
    expect(png.length).toBeLessThan(300 * 1024);

    const svg = readFileSync(
      resolve(frontendRoot, "public/og-image.svg"),
      "utf8",
    );
    expect(svg).toContain("Bahá’í Election System");
    expect(svg).toContain(
      "Helping tellers run Bahá’í elections since 2001 · tallyj.com",
    );
  });

  it("rewrites only social URL attributes whose origin is production", () => {
    const dir = mkdtempSync(join(tmpdir(), "tallyj-og-"));
    const file = join(dir, "index.html");
    const lookalike = "https://v4.tallyj.com.evil.example/og-image.png";
    writeFileSync(
      file,
      `<!doctype html>
<html><head>
  <!-- production origin stays in this comment: ${PRODUCTION_ORIGIN} -->
  <meta name="description" content="Bahá'í elections at ${PRODUCTION_ORIGIN}" />
  <meta property="og:title" content="${TITLE}" />
  <meta property="og:url" content="${PRODUCTION_ORIGIN}/" />
  <meta property="og:image" content="${IMAGE_URL}" />
  <meta name="twitter:image" content="${IMAGE_URL}" />
  <meta name="twitter:url" content="${PRODUCTION_ORIGIN}/login" />
  <meta property="og:image" content="${lookalike}" />
  <p>See ${PRODUCTION_ORIGIN}/og-image.png</p>
</head></html>
`,
      "utf8",
    );
    const script = resolve(frontendRoot, "scripts/set-og-origin.mjs");
    execFileSync(
      process.execPath,
      [script, "https://uat.v4.tallyj.com", file],
      {
        encoding: "utf8",
      },
    );
    const rewritten = readFileSync(file, "utf8");
    const doc = new DOMParser().parseFromString(rewritten, "text/html");
    const contents = (attr: "property" | "name", key: string) =>
      [...doc.querySelectorAll(`meta[${attr}="${key}"]`)].map((el) =>
        el.getAttribute("content"),
      );

    expect(contents("property", "og:url")).toEqual([
      "https://uat.v4.tallyj.com/",
    ]);
    expect(contents("property", "og:image")).toEqual([
      "https://uat.v4.tallyj.com/og-image.png",
      lookalike,
    ]);
    expect(contents("name", "twitter:image")).toEqual([
      "https://uat.v4.tallyj.com/og-image.png",
    ]);
    expect(contents("name", "twitter:url")).toEqual([
      "https://uat.v4.tallyj.com/login",
    ]);
    expect(contents("name", "description")).toEqual([
      `Bahá'í elections at ${PRODUCTION_ORIGIN}`,
    ]);
    expect(contents("property", "og:title")).toEqual([TITLE]);
    expect(rewritten).toContain(
      `<!-- production origin stays in this comment: ${PRODUCTION_ORIGIN} -->`,
    );
    expect(rewritten).toContain(`<p>See ${PRODUCTION_ORIGIN}/og-image.png</p>`);

    execFileSync(
      process.execPath,
      [script, "https://uat.v4.tallyj.com/", file],
      { encoding: "utf8" },
    );
    expect(readFileSync(file, "utf8")).toBe(rewritten);
  });

  it("fails when no social URL uses the production or target origin", () => {
    const dir = mkdtempSync(join(tmpdir(), "tallyj-og-"));
    const file = join(dir, "index.html");
    writeFileSync(
      file,
      `<meta property="og:title" content="${TITLE}" />\n<!-- ${PRODUCTION_ORIGIN} -->\n`,
      "utf8",
    );
    const script = resolve(frontendRoot, "scripts/set-og-origin.mjs");
    expect(() =>
      execFileSync(
        process.execPath,
        [script, "https://uat.v4.tallyj.com", file],
        {
          encoding: "utf8",
        },
      ),
    ).toThrow();
    expect(readFileSync(file, "utf8")).toContain(PRODUCTION_ORIGIN);
  });
});
