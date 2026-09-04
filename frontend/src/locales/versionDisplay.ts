/**
 * Latin "Beta" in `common.versionDisplay` is left untranslated on purpose
 * (copy polish is parked). Isolate it so its font weight stays the same
 * when the surrounding string uses an Arabic/Persian font stack.
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
