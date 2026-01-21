export interface VoteDto {
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
  statusCode?: string;
}
