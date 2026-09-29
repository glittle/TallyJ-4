import { spawnSync } from "child_process";
import {
  existsSync,
  mkdirSync,
  mkdtempSync,
  readFileSync,
  rmSync,
  writeFileSync,
} from "fs";
import { tmpdir } from "os";
import { join, resolve } from "path";
import { describe, expect, it } from "vitest";
import {
  UnexpectedLocaleFolderError,
  mergeLocaleBundles,
} from "../../../mergeLocaleBundles.js";
import {
  LOCALE_DIRECTORY_SOURCE,
  isLocaleDirectoryName,
} from "../localeDirectory.js";

const MERGE_SCRIPT = resolve(process.cwd(), "merge-locales.js");

const leaf = (text: string, status: "source" | "ai") => ({
  t: text,
  s: status,
  w: "2026-09-23T17:47:03Z",
});

function withTempDir(run: (dir: string) => void) {
  const dir = mkdtempSync(join(tmpdir(), "tallyj-merge-"));
  try {
    run(dir);
  } finally {
    rmSync(dir, { recursive: true, force: true });
  }
}

function writeJson(dir: string, name: string, data: unknown) {
  mkdirSync(dir, { recursive: true });
  writeFileSync(join(dir, name), JSON.stringify(data, null, 2));
}

describe("isLocaleDirectoryName", () => {
  it("accepts BCP-47 language, script, and region folders", () => {
    for (const name of ["en", "fil", "zh-Hans", "pt-BR", "en-US", "es-419"]) {
      expect(isLocaleDirectoryName(name), name).toBe(true);
    }
  });

  it("rejects non-locale folders and non-canonical codes", () => {
    for (const name of [
      "__tests__",
      "bundled",
      "node_modules",
      "english",
      "en-us",
    ]) {
      expect(isLocaleDirectoryName(name), name).toBe(false);
    }
  });
});

describe("vite locale chunk names", () => {
  const bundled = new RegExp(
    `/locales/bundled/(${LOCALE_DIRECTORY_SOURCE})\\.json`,
  );

  it("keeps the hyphen in codes such as zh-Hans and pt-BR", () => {
    expect(bundled.exec("/src/locales/bundled/zh-Hans.json")?.[1]).toBe(
      "zh-Hans",
    );
    expect(bundled.exec("/src/locales/bundled/pt-BR.json")?.[1]).toBe("pt-BR");
    expect(bundled.exec("/src/locales/bundled/fil.json")?.[1]).toBe("fil");
    expect(bundled.exec("/src/locales/bundled/en.json")?.[1]).toBe("en");
  });
});

describe("mergeLocaleBundles", () => {
  it("bundles locale folders and deletes stale JSON in the output directory", () => {
    withTempDir((dir) => {
      const localesDir = join(dir, "locales");
      const outputDir = join(dir, "out");
      writeJson(join(localesDir, "en"), "common.json", {
        "common.hi": leaf("Hello", "source"),
      });
      writeJson(join(localesDir, "fil"), "common.json", {
        "common.hi": leaf("Kumusta", "ai"),
      });
      writeJson(join(localesDir, "__tests__"), "fixture.json", {
        "fixture.nope": leaf("Nope", "ai"),
      });
      mkdirSync(join(localesDir, "notes"));
      writeFileSync(join(localesDir, "notes", "readme.txt"), "no json");
      writeJson(outputDir, "__tests__.json", { stale: "yes" });
      writeJson(outputDir, "retired.json", { stale: "yes" });

      mergeLocaleBundles(localesDir, outputDir);

      expect(existsSync(join(outputDir, "__tests__.json"))).toBe(false);
      expect(existsSync(join(outputDir, "retired.json"))).toBe(false);
      expect(
        JSON.parse(readFileSync(join(outputDir, "en.json"), "utf8")),
      ).toEqual({
        "common.hi": "Hello",
      });
      expect(
        JSON.parse(readFileSync(join(outputDir, "fil.json"), "utf8")),
      ).toEqual({
        "common.hi": "Kumusta",
      });
    });
  });

  it("throws before deleting existing bundles when a JSON folder is not a locale code", () => {
    withTempDir((dir) => {
      const localesDir = join(dir, "locales");
      const outputDir = join(dir, "out");
      writeJson(join(localesDir, "en"), "common.json", {
        "common.hi": leaf("Hello", "source"),
      });
      writeJson(join(localesDir, "custom"), "extra.json", {
        "custom.hi": leaf("Extra", "ai"),
      });
      writeJson(outputDir, "__tests__.json", { stale: "keep until failure" });

      expect(() => mergeLocaleBundles(localesDir, outputDir)).toThrow(
        UnexpectedLocaleFolderError,
      );
      try {
        mergeLocaleBundles(localesDir, outputDir);
      } catch (error) {
        expect((error as UnexpectedLocaleFolderError).folders).toEqual([
          "custom",
        ]);
      }
      expect(existsSync(join(outputDir, "__tests__.json"))).toBe(true);
      expect(existsSync(join(outputDir, "en.json"))).toBe(false);
    });
  });

  it("exits non-zero from merge-locales.js for an unexpected JSON folder", () => {
    withTempDir((dir) => {
      const localesDir = join(dir, "locales");
      const outputDir = join(dir, "out");
      writeJson(join(localesDir, "english"), "common.json", {
        "common.hi": leaf("Hello", "source"),
      });

      const result = spawnSync(
        process.execPath,
        [MERGE_SCRIPT, localesDir, outputDir],
        { encoding: "utf8" },
      );

      expect(result.status).toBe(1);
      expect(result.stderr).toContain("english");
      expect(result.stderr).toContain("zh-Hans");
    });
  });
});
