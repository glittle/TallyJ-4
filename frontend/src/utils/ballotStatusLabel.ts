export interface BallotStatusLabelOptions {
  /** Show "New" instead of "Empty" for a ballot opened immediately after creation. */
  isNewSession?: boolean;
}

/**
 * Resolves the user-facing ballot status label.
 * "New" is a display-only label for empty ballots in a first-entry session.
 */
export function getBallotStatusLabel(
  t: (key: string, values?: Record<string, unknown>) => string,
  statusCode: string | undefined,
  options?: BallotStatusLabelOptions,
): string {
  if (!statusCode) {
    return "-";
  }

  if (options?.isNewSession && statusCode === "Empty") {
    return t("ballots.statusValue.New");
  }

  const key = `ballots.statusValue.${statusCode}`;
  const translated = t(key);
  return translated === key ? statusCode : translated;
}
