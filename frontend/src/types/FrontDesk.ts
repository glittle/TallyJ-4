export interface FrontDeskVoterDto {
  personGuid: string;
  fullName: string;
  bahaiId?: string;
  area?: string;
  canVote?: boolean;
  votingMethod?: string;
  envNum?: number;
  registrationTime?: string;
  votingLocationGuid?: string;
  teller1?: string;
  teller2?: string;
  isCheckedIn: boolean;
}

export interface CheckInVoterDto {
  personGuid: string;
  votingMethod: string;
  tellerName?: string;
  votingLocationGuid?: string;
}

export interface FrontDeskStatsDto {
  totalEligible: number;
  checkedIn: number;
  notYetCheckedIn: number;
  checkInPercentage: number;
}

export interface RollCallDto {
  voters: FrontDeskVoterDto[];
  stats: FrontDeskStatsDto;
}
