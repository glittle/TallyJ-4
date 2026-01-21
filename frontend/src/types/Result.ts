export interface TallyResultDto {
  electionGuid: string;
  electionName: string;
  calculatedAt: string;
  statistics: TallyStatisticsDto;
  results: CandidateResultDto[];
  ties: TieInfoDto[];
}

export interface CandidateResultDto {
  personGuid: string;
  fullName: string;
  voteCount: number;
  rank: number;
  section: string;
  isTied: boolean;
  tieBreakGroup?: number;
  tieBreakRequired: boolean;
  closeToNext: boolean;
  closeToPrev: boolean;
}

export interface TieInfoDto {
  tieBreakGroup: number;
  voteCount: number;
  tieBreakRequired: boolean;
  section: string;
  candidateNames: string[];
}

export interface TallyStatisticsDto {
  totalBallots: number;
  ballotsReceived: number;
  spoiledBallots: number;
  ballotsNeedingReview: number;
  totalVotes: number;
  validVotes: number;
  invalidVotes: number;
  numVoters: number;
  numEligibleCandidates: number;
  numberToElect: number;
  numberExtra: number;
}
