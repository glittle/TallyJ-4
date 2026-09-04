/**
 * Isolate Latin "Beta" when it still appears in `common.versionDisplay`
 * so its weight uses the Latin primary stack. Persian now uses بتا
 * (no Latin mark). Other locales, including Arabic, still use Latin Beta.
 */
export const VERSION_BETA_MARK = "Beta";

export type VersionDisplayParts = {
  before: string;
  mark: string;
  after: string;
};

export function splitVersionDisplay(display: string): VersionDisplayParts {
  const index = display.indexOf(VERSION_BETA_MARK);
  if (index === -1) {
    return { before: display, mark: "", after: "" };
  }

  return {
    before: display.slice(0, index),
    mark: VERSION_BETA_MARK,
    after: display.slice(index + VERSION_BETA_MARK.length),
  };
}
