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

// Election Report DTOs
export interface ElectionReportDto {
  electionName: string;
  electionDate?: string;
  numToElect: number;
  totalBallots: number;
  spoiledBallots: number;
  totalVotes: number;
  elected: CandidateReportDto[];
  extra: CandidateReportDto[];
  other: CandidateReportDto[];
  ties: TieReportDto[];
}

export interface CandidateReportDto {
  rank: number;
  fullName: string;
  voteCount: number;
  section: string;
}

export interface TieReportDto {
  tieBreakGroup: number;
  section: string;
  candidateNames: string[];
}

// Report Data DTOs
export interface BallotReportDto {
  ballotGuid: string;
  locationName: string;
  status: string;
  votes: VoteReportDto[];
}

export interface VoteReportDto {
  fullName: string;
  position: number;
}

export interface VoterReportDto {
  personGuid: string;
  fullName: string;
  locationName: string;
  voted: boolean;
  voteTime?: string;
}

export interface LocationReportDto {
  locationName: string;
  totalVoters: number;
  voted: number;
  ballotsEntered: number;
  totalVotes: number;
}

export interface ReportDataResponseDto {
  reportType: string;
  data: any;
}

// Tie Management DTOs
export interface TieDetailsDto {
  tieBreakGroup: number;
  section: string;
  candidates: TieCandidateDto[];
  instructions: string;
}

export interface TieCandidateDto {
  personGuid: string;
  fullName: string;
  voteCount: number;
  tieBreakCount?: number;
}

export interface SaveTieCountsRequestDto {
  counts: TieCountDto[];
}

export interface TieCountDto {
  personGuid: string;
  tieBreakCount: number;
}

export interface SaveTieCountsResponseDto {
  success: boolean;
  message: string;
  reAnalysisTriggered: boolean;
}

// Presentation DTOs
export interface PresentationDto {
  electionName: string;
  electionDate?: string;
  numToElect: number;
  totalBallots: number;
  totalVotes: number;
  electedCandidates: PresentationCandidateDto[];
  extraCandidates: PresentationCandidateDto[];
  hasTies: boolean;
  ties: PresentationTieDto[];
  status: string;
}

export interface PresentationCandidateDto {
  rank: number;
  fullName: string;
  voteCount: number;
  isTied: boolean;
  isWinner: boolean;
}

export interface PresentationTieDto {
  tieBreakGroup: number;
  section: string;
  candidateNames: string[];
  tieBreakRequired: boolean;
}

// Monitor Info DTOs
export interface MonitorInfoDto {
  electionGuid: string;
  computers: ComputerInfoDto[];
  locations: LocationInfoDto[];
  onlineVotingInfo: OnlineVotingInfoDto;
  totalBallots: number;
  totalVotes: number;
  lastUpdated: string;
}

export interface ComputerInfoDto {
  computerCode: string;
  locationName: string;
  ballotCount: number;
  lastContact: string;
  status: string;
}

export interface LocationInfoDto {
  locationGuid: string;
  locationName: string;
  ballotCount: number;
  voteCount: number;
  voterCount: number;
  status: string;
}

export interface OnlineVotingInfoDto {
  totalOnlineVoters: number;
  votedOnline: number;
  onlineBallotsEntered: number;
  status: string;
}

// Detailed Statistics DTOs
export interface DetailedStatisticsDto {
  overview: ElectionOverviewDto;
  voteDistribution: VoteDistributionDto;
  candidatePerformance: CandidatePerformanceDto[];
  turnoutAnalysis: TurnoutAnalysisDto;
  locationStatistics: LocationStatisticsDto[];
}

export interface ElectionOverviewDto {
  electionName: string;
  electionDate?: string;
  totalRegisteredVoters: number;
  totalBallotsCast: number;
  validBallots: number;
  spoiledBallots: number;
  totalVotes: number;
  positionsToElect: number;
  overallTurnoutPercentage: number;
  electionDuration?: string;
}

export interface VoteDistributionDto {
  votesPerPosition: number[];
  voteCountDistribution: Record<string, number>;
  averageVotesPerBallot: number;
  maxVotesOnSingleBallot: number;
  minVotesOnSingleBallot: number;
  ballotLengthDistribution: Record<number, number>;
}

export interface CandidatePerformanceDto {
  personGuid: string;
  fullName: string;
  totalVotes: number;
  votePercentage: number;
  rank: number;
  isElected: boolean;
  isEliminated: boolean;
  votesByPosition: Record<number, number>;
  firstChoicePercentage: number;
  lastChoicePercentage: number;
}

export interface TurnoutAnalysisDto {
  overallTurnout: number;
  turnoutByLocation: Record<string, number>;
  turnoutTrends: Record<string, number>;
  earlyVotingCount: number;
  electionDayVotingCount: number;
  earlyVotingPercentage: number;
  demographicBreakdown: DemographicTurnoutDto[];
  timeBasedTurnout: TimeBasedTurnoutDto[];
  participationRates: ParticipationRateDto;
}

export interface DemographicTurnoutDto {
  demographicCategory: string;
  demographicValue: string;
  totalVoters: number;
  voted: number;
  turnoutPercentage: number;
}

export interface TimeBasedTurnoutDto {
  timePeriod: string;
  periodType: string;
  ballotsCast: number;
  cumulativeTurnout: number;
}

export interface ParticipationRateDto {
  firstTimeVoters: number;
  returningVoters: number;
  onlineVoters: number;
  inPersonVoters: number;
  participationByMethod: Record<string, number>;
}

export interface LocationStatisticsDto {
  locationName: string;
  registeredVoters: number;
  ballotsCast: number;
  validBallots: number;
  spoiledBallots: number;
  turnoutPercentage: number;
  totalVotes: number;
  topCandidates: Record<string, number>;
}
