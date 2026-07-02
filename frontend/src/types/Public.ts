export interface PublicDisplayDto {
  electionGuid: string;
  electionName: string;
  dateOfElection: string | null;
  convenor: string;
  electionType: string;
  tallyStatus: string;
  numberToElect: number;
  numberExtra: number;
  electedPeople: PublicPersonDto[];
  additionalPeople: PublicPersonDto[];
  statistics: PublicDisplayStatsDto;
  lastUpdated: string;
  isFinalized: boolean;
}

export interface PublicPersonDto {
  rank: number;
  fullName: string;
  voteCount: number;
  isTied: boolean;
  tieBreakRequired: boolean;
}

export interface PublicDisplayStatsDto {
  totalBallots: number;
  validBallots: number;
  spoiledBallots: number;
  totalVotes: number;
  registeredVoters: number;
  turnoutPercentage: number;
}

export interface PublicDisplayOptions {
  showVoteCounts: boolean;
  showStatistics: boolean;
  showAdditionalNames: boolean;
  theme: "light" | "dark";
  refreshInterval: number;
  autoRefresh: boolean;
}