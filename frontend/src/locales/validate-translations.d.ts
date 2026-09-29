export function diffKeysAgainstEnglish(
  keysByLocale: Map<string, Iterable<string>>,
): {
  stale: { locale: string; key: string }[];
  awaiting: { locale: string; count: number }[];
  missingReference: boolean;
};
