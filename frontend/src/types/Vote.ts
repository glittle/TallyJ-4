export interface VoteDto {
  rowId: number;
  ballotGuid: string;
  positionOnBallot: number;
  personGuid?: string;
  personFullName?: string;
  statusCode: string;
  personCombinedInfo?: string;
  onlineVoteRaw?: string;
}

export interface CreateVoteDto {
  ballotGuid: string;
  positionOnBallot: number;
  personGuid?: string;
}

export interface VoteWithBallotStatusDto {
  vote: VoteDto;
  ballotStatusCode: string;
}
