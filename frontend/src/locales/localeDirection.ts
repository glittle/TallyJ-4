/** Locales that use a right-to-left writing direction. */
export const rtlLocales = ["ar", "fa"] as const;

export type TextDirection = "ltr" | "rtl";

export function isRtlLocale(locale: string): boolean {
  return (rtlLocales as readonly string[]).includes(locale);
}

export function localeTextDirection(locale: string): TextDirection {
  return isRtlLocale(locale) ? "rtl" : "ltr";
}

/** Keep `<html lang>` and `<html dir>` in sync with the active locale. */
export function applyDocumentLocale(locale: string): void {
  const root = document.documentElement;
  root.lang = locale;
  root.dir = localeTextDirection(locale);
}
