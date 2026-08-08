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

/** Resolve eligibility reason code to localized text (falls back to the code). */
export function getIneligibleReasonLabel(
  t: TranslateFn,
  code: string | null | undefined,
): string {
  const normalized = code?.trim().toUpperCase();
  if (!normalized) {
    return t("ballots.spoiled");
  }

  const key = `eligibility.${normalized}`;
  const translated = t(key);
  return translated === key ? normalized : translated;
}

export function getVoteSpoiledLabel(t: TranslateFn, vote: VoteDto): string {
  return getIneligibleReasonLabel(t, resolveIneligibleReasonCode(vote));
}
