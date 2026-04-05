/**
 * Version information
 * This reads from version.json at the repository root.
 */

import versionJson from "../../../version.json";
import { i18n } from "../locales";
const versionData = versionJson as VersionData;

interface VersionData {
  version: string;
  buildDate: string;
  buildDateBadi: string;
}

export const VERSION = versionData.version;
export const getBuildDate = () => {
  const locale = i18n.global.locale.value;
  const date = new Date(versionData.buildDate);
  const formatted = new Intl.DateTimeFormat(locale, {
    year: "numeric",
    month: "long",
    day: "numeric",
  }).format(date);
  return formatted;
};
export const getBuildDateBadi = () => {
  const raw = versionData.buildDateBadi; // e.g. "183.01.16"
  const parts = raw.split(".");
  if (parts.length !== 3) {
    return raw; // Fallback to raw string if parsing fails
  }
  const [year, month, day] = parts.map(Number);
  if (isNaN(year!) || isNaN(month!) || isNaN(day!)) {
    return raw; // Fallback to raw string if parsing fails
  }
  // Get localized month names from i18n - the first is the intercalary month, so we skip it for 1-based month indexing
  const monthNames = i18n.global.t("common.badi.monthNames").split(",");
  const monthName = monthNames[month!] || `Month ${month}`;
  return `${day} ${monthName} ${year}`;
};
