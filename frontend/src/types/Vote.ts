export interface VoteDto {
  rowId: number;
  ballotGuid: string;
  positionOnBallot: number;
  personGuid?: string;
  personFullName?: string;
  statusCode: string;
  ineligibleReasonCode?: string;
  personCombinedInfo?: string;
  onlineVoteRaw?: string;
}

export interface CreateVoteDto {
  ballotGuid: string;
  positionOnBallot: number;
  personGuid?: string;
  ineligibleReasonCode?: string;
}

export interface VotePositionDto {
  rowId: number;
  positionOnBallot: number;
}

export interface VoteWithBallotStatusDto {
  vote?: VoteDto;
  ballotStatusCode?: string;
  votePositions?: VotePositionDto[];
}

export interface ReorderVotesDto {
  ballotGuid: string;
  voteRowIds: number[];
}
