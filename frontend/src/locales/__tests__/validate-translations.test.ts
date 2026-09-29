import { spawnSync } from "child_process";
import {
  existsSync,
  mkdirSync,
  mkdtempSync,
  rmSync,
  symlinkSync,
  writeFileSync,
} from "fs";
import { tmpdir } from "os";
import { join, resolve } from "path";
import { describe, expect, it } from "vitest";
import {
  ValidationError,
  diffKeysAgainstEnglish,
  validateLocaleCatalog,
} from "../validateLocaleCatalog.js";

function resolveCliScript() {
  const candidates = [
    resolve(process.cwd(), "src/locales/validate-translations.js"),
    resolve(process.cwd(), "frontend/src/locales/validate-translations.js"),
  ];
  const found = candidates.find((candidate) => existsSync(candidate));
  if (!found) {
    throw new Error(
      `validate-translations.js not found from cwd ${process.cwd()}`,
    );
  }
  return found;
}

const SCRIPT = resolveCliScript();

const leaf = (text: string, status: "source" | "ai") => ({
  t: text,
  s: status,
  w: "2026-09-23T17:47:03Z",
});

function withTempDir(run: (dir: string) => void) {
  const dir = mkdtempSync(join(tmpdir(), "tallyj-i18n-"));
  try {
    run(dir);
  } finally {
    rmSync(dir, { recursive: true, force: true });
  }
}

function writeLocale(
  root: string,
  locale: string,
  fileName: string,
  data: Record<string, unknown>,
) {
  const dir = join(root, locale);
  mkdirSync(dir, { recursive: true });
  writeFileSync(join(dir, fileName), JSON.stringify(data, null, 2));
}

describe("diffKeysAgainstEnglish", () => {
  it("counts an English-only key as awaiting and a key absent from en as stale", () => {
    const diff = diffKeysAgainstEnglish(
      new Map<string, string[]>([
        ["en", ["common.keep", "frontDesk.onlyEnglish"]],
        ["fr", ["common.keep", "elections.stageChangeError"]],
      ]),
    );

    expect(diff.missingReference).toBe(false);
    expect(diff.awaiting).toEqual([{ locale: "fr", count: 1 }]);
    expect(diff.stale).toEqual([
      { locale: "fr", key: "elections.stageChangeError" },
    ]);
  });

  it("reports a missing en catalog without treating other keys as stale", () => {
    const diff = diffKeysAgainstEnglish(
      new Map<string, string[]>([["fr", ["common.onlyFrench"]]]),
    );

    expect(diff.missingReference).toBe(true);
    expect(diff.awaiting).toEqual([]);
    expect(diff.stale).toEqual([]);
  });
});

describe("validateLocaleCatalog", () => {
  it("returns MISSING_REFERENCE_LOCALE when en is absent", () => {
    withTempDir((dir) => {
      writeLocale(dir, "fr", "common.json", {
        "common.hello": leaf("Bonjour", "ai"),
      });

      const report = validateLocaleCatalog(dir);
      expect(report.errors.map((error) => error.type)).toEqual([
        "MISSING_REFERENCE_LOCALE",
      ]);
      expect(report.warnings).toEqual([]);
    });
  });

  it("returns STALE_KEY for a key in a translation file that also exists in en", () => {
    withTempDir((dir) => {
      writeLocale(dir, "en", "common.json", {
        "common.keep": leaf("Keep", "source"),
      });
      writeLocale(dir, "fr", "common.json", {
        "common.keep": leaf("Garder", "ai"),
        "common.old": leaf("Ancien", "ai"),
      });

      const report = validateLocaleCatalog(dir);
      expect(report.errors.map((error) => error.type)).toEqual(["STALE_KEY"]);
      expect(report.errors[0]?.details).toMatchObject({
        locale: "fr",
        key: "common.old",
      });
      expect(report.warnings).toEqual([]);
    });
  });

  it("returns STALE_KEY for a translation file that has no en counterpart", () => {
    withTempDir((dir) => {
      writeLocale(dir, "en", "common.json", {
        "common.keep": leaf("Keep", "source"),
      });
      writeLocale(dir, "fr", "common.json", {
        "common.keep": leaf("Garder", "ai"),
      });
      writeLocale(dir, "fr", "onlyFrench.json", {
        "onlyFrench.hello": leaf("Bonjour", "ai"),
      });

      const report = validateLocaleCatalog(dir);
      expect(report.errors.map((error) => error.type)).toEqual(["STALE_KEY"]);
      expect(report.errors[0]?.details).toMatchObject({
        locale: "fr",
        key: "onlyFrench.hello",
      });
      expect(report.warnings).toEqual([]);
    });
  });

  it("counts an English-only key as awaiting translation and not an error", () => {
    withTempDir((dir) => {
      writeLocale(dir, "en", "common.json", {
        "common.keep": leaf("Keep", "source"),
      });
      writeLocale(dir, "en", "frontDesk.json", {
        "frontDesk.title": leaf("Front Desk", "source"),
      });
      writeLocale(dir, "fr", "common.json", {
        "common.keep": leaf("Garder", "ai"),
      });

      const report = validateLocaleCatalog(dir);
      expect(report.errors).toEqual([]);
      expect(report.warnings).toEqual([{ locale: "fr", count: 1 }]);
    });
  });

  it("throws FILE_READ_ERROR for unreadable JSON", () => {
    withTempDir((dir) => {
      const localeDir = join(dir, "en");
      mkdirSync(localeDir);
      writeFileSync(join(localeDir, "common.json"), "{ not json");

      let thrown: unknown;
      try {
        validateLocaleCatalog(dir);
      } catch (error) {
        thrown = error;
      }
      expect(thrown).toBeInstanceOf(ValidationError);
      expect((thrown as ValidationError).type).toBe("FILE_READ_ERROR");
    });
  });
});

describe("validate-translations.js exit", () => {
  function run(args: string[]) {
    return spawnSync(process.execPath, [SCRIPT, ...args], {
      encoding: "utf8",
    });
  }

  it("exits 0 for a catalog whose only gap is an untranslated English key", () => {
    withTempDir((dir) => {
      writeLocale(dir, "en", "common.json", {
        "common.keep": leaf("Keep", "source"),
        "frontDesk.title": leaf("Front Desk", "source"),
      });
      writeLocale(dir, "fr", "common.json", {
        "common.keep": leaf("Garder", "ai"),
      });

      const result = run([dir]);
      expect(result.status).toBe(0);
      expect(result.stdout).toContain("fr: 1 keys awaiting translation");
      expect(result.stdout).toContain("All translation files are valid");
    });
  });

  it("exits 1 for a stale key and for unreadable JSON", () => {
    withTempDir((dir) => {
      writeLocale(dir, "en", "common.json", {
        "common.keep": leaf("Keep", "source"),
      });
      writeLocale(dir, "fr", "common.json", {
        "common.old": leaf("Ancien", "ai"),
      });

      const stale = run([dir]);
      expect(stale.status).toBe(1);
      expect(stale.stdout).toContain("STALE_KEY");
    });

    withTempDir((dir) => {
      mkdirSync(join(dir, "en"));
      writeFileSync(join(dir, "en", "common.json"), "{");

      const broken = run([dir]);
      expect(broken.status).toBe(1);
      expect(broken.stderr).toContain("Failed to read");
    });
  });

  it("still validates when node is started through a symlink to the script", (ctx) => {
    withTempDir((dir) => {
      const link = join(dir, "validate-translations.js");
      try {
        symlinkSync(SCRIPT, link);
      } catch (error) {
        const code =
          error && typeof error === "object" && "code" in error
            ? String(error.code)
            : "";
        if (code === "EPERM" || code === "EACCES") {
          ctx.skip();
          return;
        }
        throw error;
      }

      const localesDir = join(dir, "locales");
      mkdirSync(join(localesDir, "en"), { recursive: true });
      writeFileSync(join(localesDir, "en", "common.json"), "{");

      const result = spawnSync(process.execPath, [link, localesDir], {
        encoding: "utf8",
      });
      expect(result.status).toBe(1);
      expect(result.stderr).toContain("Failed to read");
    });
  });
});
