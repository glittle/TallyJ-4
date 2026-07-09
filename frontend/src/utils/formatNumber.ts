import { i18n } from "@/locales";

/**
 * Format a number for display using the current i18n locale
 * (thousands separators, decimal style, etc.).
 *
 * Pass `locale` to override the active language (useful in tests).
 * Optional `options` are forwarded to `Intl.NumberFormat`.
 */
export function formatNumber(
  value: number | undefined | null,
  options?: Intl.NumberFormatOptions,
  locale?: string,
): string {
  if (value === null || value === undefined) {
    return "-";
  }
  if (!Number.isFinite(value)) {
    return String(value);
  }

  const resolvedLocale =
    locale ??
    (typeof i18n.global.locale === "string"
      ? i18n.global.locale
      : i18n.global.locale.value);

  return new Intl.NumberFormat(resolvedLocale, options).format(value);
}
