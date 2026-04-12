import type { VoteDto } from "./Vote";

export interface BallotDto {
  ballotGuid: string;
  ballotCode: string;
  locationGuid: string;
  locationName: string;
  ballotNumAtComputer: number;
  computerCode: string;
  statusCode: string;
  teller1?: string;
  teller2?: string;
  dateCreated?: Date | null;
  dateUpdated?: Date | null;
  voteCount: number;
  votes: VoteDto[];
}

export interface CreateBallotDto {
  electionGuid: string;
  locationGuid: string;
  computerCode: string;
  statusCode?: string;
  teller1?: string;
  teller2?: string;
}

export interface UpdateBallotDto {
  statusCode?: string;
  teller1?: string;
  teller2?: string;
}
