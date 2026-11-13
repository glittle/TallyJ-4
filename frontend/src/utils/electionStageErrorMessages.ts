type PhraseTranslator = (
  key: string,
  named?: Record<string, unknown>,
) => string;

const FALLBACK_KEY = "elections.stageChangeError.generic";

function parseMessageParams(
  paramParts: string[],
): Record<string, string | number> {
  const params: Record<string, string | number> = {};
  for (const part of paramParts) {
    const eq = part.indexOf("=");
    if (eq > 0) {
      const name = part.slice(0, eq);
      const raw = part.slice(eq + 1);
      const asNumber = Number(raw);
      params[name] = Number.isNaN(asNumber) ? raw : asNumber;
    }
  }
  return params;
}

function translateSingleMessage(
  serverMessage: string,
  t: PhraseTranslator,
): string {
  const [key, ...paramParts] = serverMessage.split("|");
  const params = parseMessageParams(paramParts);
  const translated = t(key, params);

  if (translated !== key) {
    return translated;
  }

  return t(FALLBACK_KEY);
}

const COUNTS_DO_NOT_RECONCILE =
  "elections.stageChangeError.countsDoNotReconcile";

/**
 * Message for a blocked Analyze. Count mismatches use the Calculate screen's
 * reconciliation sentence. Other blockers use the stage-change phrase.
 */
export function translateAnalysisBlocker(
  serverMessage: string | null | undefined,
  t: PhraseTranslator,
): string {
  const trimmed = serverMessage?.trim();
  if (!trimmed) {
    return "";
  }

  if (
    trimmed === COUNTS_DO_NOT_RECONCILE ||
    trimmed.startsWith(`${COUNTS_DO_NOT_RECONCILE}|`)
  ) {
    const params = parseMessageParams(trimmed.split("|").slice(1));
    const count = typeof params.count === "number" ? params.count : 0;
    return t("tally.reconciliation.blocked", { count });
  }

  return translateElectionStageChangeError(trimmed, t);
}

export function translateElectionStageChangeError(
  serverMessage: string,
  t: PhraseTranslator,
): string {
  const trimmed = serverMessage?.trim();
  if (!trimmed || trimmed === "An unknown error occurred") {
    return t(FALLBACK_KEY);
  }

  if (trimmed.includes(";")) {
    return trimmed
      .split(";")
      .map((part) => translateSingleMessage(part.trim(), t))
      .join("; ");
  }

  return translateSingleMessage(trimmed, t);
}
