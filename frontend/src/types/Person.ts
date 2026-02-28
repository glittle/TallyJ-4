export interface PersonDto {
  personGuid: string;
  firstName?: string;
  lastName: string;
  fullName: string;
  email?: string;
  phone?: string;
  canReceiveVotes?: boolean;
  canVote?: boolean;
  area?: string;
  bahaiId?: string;
  otherLastNames?: string;
  otherNames?: string;
  otherInfo?: string;
  combinedSoundCodes?: string;
  ageGroup?: string;
  ineligibleReasonGuid?: string;
  ineligibleReasonCode?: string;
  voteCount: number;
}

export interface PersonListDto {
  personGuid: string;
  fullName: string;
  email?: string;
  phone?: string;
  area?: string;
  canVote?: boolean;
  canReceiveVotes?: boolean;
  ineligibleReasonCode?: string;
}

export interface PersonDetailDto {
  personGuid: string;
  electionGuid: string;
  firstName?: string;
  lastName: string;
  fullName: string;
  email?: string;
  phone?: string;
  canReceiveVotes?: boolean;
  canVote?: boolean;
  area?: string;
  bahaiId?: string;
  otherLastNames?: string;
  otherNames?: string;
  otherInfo?: string;
  ageGroup?: string;
  ineligibleReasonGuid?: string;
  ineligibleReasonCode?: string;
  registrationTime?: string;
  votingLocationGuid?: string;
  votingMethod?: string;
  envNum?: number;
  teller1?: string;
  teller2?: string;
  hasOnlineBallot?: boolean;
  registrationHistory?: string;
  voteCount: number;
}

export interface SearchablePersonDto extends PersonDto {
  _searchText: string;
  _soundexCodes: string[];
}

export interface CreatePersonDto {
  electionGuid: string;
  firstName?: string;
  lastName: string;
  email?: string;
  phone?: string;
  area?: string;
  bahaiId?: string;
  otherLastNames?: string;
  otherNames?: string;
  otherInfo?: string;
  ageGroup?: string;
  ineligibleReasonGuid?: string;
}

export interface UpdatePersonDto {
  firstName?: string;
  lastName?: string;
  email?: string;
  phone?: string;
  area?: string;
  bahaiId?: string;
  otherLastNames?: string;
  otherNames?: string;
  otherInfo?: string;
  ageGroup?: string;
  ineligibleReasonGuid?: string;
}
