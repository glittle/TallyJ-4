import type { VoteDto } from "@/types/Vote";
import { resolveVoteStatus } from "@/utils/voteDtoNormalization";

type TranslateFn = (key: string) => string;

const ELIGIBILITY_CODE_PATTERN = /^[RVXU]\d{2}$/i;

function resolveIneligibleReasonCode(vote: VoteDto): string | undefined {
  if (vote.ineligibleReasonCode) {
    return vote.ineligibleReasonCode;
  }

  const status = resolveVoteStatus(vote);
  if (ELIGIBILITY_CODE_PATTERN.test(status)) {
    return status;
  }

  return undefined;
}

/**
 * Resolve eligibility reason code to localized text.
 * When code is missing, uses `emptyFallbackKey` (default: ballots.spoiled for votes;
 * pass ballots.ineligible for person search badges).
 * When a code has no translation, falls back to the code itself.
 */
export function getIneligibleReasonLabel(
  t: TranslateFn,
  code: string | null | undefined,
  emptyFallbackKey = "ballots.spoiled",
): string {
  const normalized = code?.trim().toUpperCase();
  if (!normalized) {
    return t(emptyFallbackKey);
  }

  const key = `eligibility.${normalized}`;
  const translated = t(key);
  return translated === key ? normalized : translated;
}

export function getVoteSpoiledLabel(t: TranslateFn, vote: VoteDto): string {
  return getIneligibleReasonLabel(t, resolveIneligibleReasonCode(vote));
}
