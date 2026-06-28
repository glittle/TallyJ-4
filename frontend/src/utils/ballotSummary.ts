import type { BallotDto } from "@/types/Ballot";

export type BallotSummaryDto = Omit<BallotDto, "votes">;

export function toBallotSummary(ballot: BallotDto): BallotSummaryDto {
  const { votes: _votes, ...summary } = ballot;
  return summary;
}

export function patchBallotSummary(
  summary: BallotSummaryDto,
  patch: Partial<BallotSummaryDto>,
): BallotSummaryDto {
  return {
    ...summary,
    ...patch,
  };
}

export function summaryFromFullBallot(ballot: BallotDto): BallotSummaryDto {
  return toBallotSummary(ballot);
}
