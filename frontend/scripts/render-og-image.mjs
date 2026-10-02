/**
 * Rasterize public/og-image.svg to public/og-image.png (1200×627).
 *
 * The PNG is committed and copied into the Vite build as a stable, unhashed
 * file. Re-run after editing the SVG: npm run og-image
 *
 * Fonts are not vendored. The script loads Inter, Noto Sans, Segoe UI, or
 * Liberation Sans when those files are on the machine. Glyphs are baked into
 * the PNG, so the committed image does not depend on the viewer's fonts.
 */
import { execFileSync } from "node:child_process";
import { existsSync, readFileSync, writeFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { Resvg } from "@resvg/resvg-js";

const here = dirname(fileURLToPath(import.meta.url));
const frontendRoot = resolve(here, "..");
const svgPath = resolve(frontendRoot, "public/og-image.svg");
const pngPath = resolve(frontendRoot, "public/og-image.png");

const WIDTH = 1200;
const HEIGHT = 627;

const fontCandidates = [
  "/usr/share/fonts/truetype/macos/Inter-Regular.ttf",
  "/usr/share/fonts/truetype/macos/Inter-SemiBold.ttf",
  "/usr/share/fonts/truetype/macos/Inter-Bold.ttf",
  "/usr/share/fonts/truetype/noto/NotoSans-Regular.ttf",
  "/usr/share/fonts/truetype/noto/NotoSans-Bold.ttf",
  "/usr/share/fonts/truetype/liberation/LiberationSans-Regular.ttf",
  "/usr/share/fonts/truetype/liberation/LiberationSans-Bold.ttf",
  "C:/Windows/Fonts/segoeui.ttf",
  "C:/Windows/Fonts/segoeuib.ttf",
  "C:/Windows/Fonts/segoeuisb.ttf",
];

function fontsFromFontconfig() {
  try {
    const out = execFileSync("fc-list", [":family", "file"], {
      encoding: "utf8",
    });
    const wanted = /inter|noto sans|liberation sans|segoe ui/i;
    return out
      .split("\n")
      .map((line) => line.split(":")[0]?.trim())
      .filter((file) => file && wanted.test(file) && existsSync(file));
  } catch {
    return [];
  }
}

const fontFiles = [
  ...new Set(
    [...fontCandidates, ...fontsFromFontconfig()].filter((file) =>
      existsSync(file),
    ),
  ),
];

if (fontFiles.length === 0) {
  console.error(
    "No sans-serif font files found. Install Inter, Noto Sans, or Liberation Sans, then re-run.",
  );
  process.exit(1);
}

const svg = readFileSync(svgPath);
const resvg = new Resvg(svg, {
  fitTo: { mode: "width", value: WIDTH },
  font: {
    fontFiles,
    loadSystemFonts: false,
    defaultFontFamily: "Inter",
  },
  background: "#fdfcf4",
});

const rendered = resvg.render();
if (rendered.width !== WIDTH || rendered.height !== HEIGHT) {
  console.error(
    `Expected ${WIDTH}x${HEIGHT}, rendered ${rendered.width}x${rendered.height}`,
  );
  process.exit(1);
}

const png = rendered.asPng();
writeFileSync(pngPath, png);
console.log(
  `Wrote ${pngPath} (${rendered.width}x${rendered.height}, ${png.length} bytes)`,
);
