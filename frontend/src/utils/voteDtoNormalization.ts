import type { VoteDto } from "../types";

type ApiVoteDto = VoteDto & { voteStatus?: string };

export type ApiVoteLike = {
  statusCode?: string;
  voteStatus?: string;
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

export function isVoteSpoiled(statusCode?: string): boolean {
  return normalizeVoteStatusCode(statusCode) !== "ok";
}

export function isVoteDtoSpoiled(vote: ApiVoteLike): boolean {
  return resolveVoteStatus(vote) !== "ok";
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