export { isLocaleDirectoryName } from "./localeDirectory.js";

export class ValidationError {
  type: string;
  message: string;
  details: Record<string, unknown>;
  constructor(type: string, message: string, details?: Record<string, unknown>);
}

export function diffKeysAgainstEnglish(
  keysByLocale: Map<string, Iterable<string>>,
): {
  stale: { locale: string; key: string }[];
  awaiting: { locale: string; count: number }[];
  missingReference: boolean;
};

export function validateLocaleCatalog(localesDir: string): {
  files: string[];
  rootFiles: string[];
  localeFiles: Map<string, { original: string; fileName: string }[]>;
  errors: ValidationError[];
  warnings: { locale: string; count: number }[];
};
