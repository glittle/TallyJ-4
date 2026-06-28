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

export function getVoteSpoiledLabel(t: TranslateFn, vote: VoteDto): string {
  const code = resolveIneligibleReasonCode(vote);
  if (!code) {
    return t("ballots.spoiled");
  }

  const key = `eligibility.${code}`;
  const translated = t(key);
  return translated === key ? code : translated;
}
