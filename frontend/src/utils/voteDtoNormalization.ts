import type { VoteDto } from "../types";

type ApiVoteDto = VoteDto & { voteStatus?: string };

export type ApiVoteLike = {
  statusCode?: string;
  voteStatus?: string;
  ineligibleReasonCode?: string;
};

/** Canonical UI vote status for a valid vote. */
export function normalizeVoteStatusCode(status?: string): string {
  if (!status) {
    return "ok";
  }
  if (status.toLowerCase() === "ok") {
    return "ok";
  }
  return status;
}

export function resolveVoteStatus(vote: ApiVoteLike): string {
  return normalizeVoteStatusCode(vote.statusCode ?? vote.voteStatus);
}

export function isVoteRaw(statusCode?: string): boolean {
  return normalizeVoteStatusCode(statusCode).toLowerCase() === "raw";
}

export function isVoteSpoiled(statusCode?: string): boolean {
  const status = normalizeVoteStatusCode(statusCode);
  return status !== "ok" && !isVoteRaw(status);
}

export function isVoteDtoSpoiled(vote: ApiVoteLike): boolean {
  if (isVoteRaw(vote.statusCode ?? vote.voteStatus)) {
    return false;
  }

  if (vote.ineligibleReasonCode) {
    return true;
  }

  const status = resolveVoteStatus(vote);
  return status !== "ok";
}

export function normalizeVoteDto(vote: ApiVoteDto): VoteDto {
  const rawStatus = vote.statusCode ?? vote.voteStatus;
  return {
    ...vote,
    statusCode: normalizeVoteStatusCode(rawStatus),
  };
}

export function normalizeVoteList(votes?: ApiVoteDto[]): VoteDto[] {
  return (votes ?? []).map((vote) => normalizeVoteDto(vote));
}
