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
}

export interface VoteWithBallotStatusDto {
  vote?: VoteDto;
  ballotStatusCode?: string;
  votes?: VoteDto[];
}

export interface ReorderVotesDto {
  ballotGuid: string;
  voteRowIds: number[];
}
