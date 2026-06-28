import type { ComposerTranslation } from "vue-i18n";

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
  t: ComposerTranslation,
): string {
  const [key, ...paramParts] = serverMessage.split("|");
  const params = parseMessageParams(paramParts);
  const translated = t(key, params);

  if (translated !== key) {
    return translated;
  }

  return t(FALLBACK_KEY);
}

export function translateElectionStageChangeError(
  serverMessage: string,
  t: ComposerTranslation,
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
