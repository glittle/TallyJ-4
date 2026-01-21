import type { VoteDto } from './Vote';

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
  voteCount: number;
  votes: VoteDto[];
}

export interface CreateBallotDto {
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
