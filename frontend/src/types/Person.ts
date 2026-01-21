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
  ageGroup?: string;
  ineligibleReasonGuid?: string;
  voteCount: number;
}

export interface CreatePersonDto {
  electionGuid: string;
  firstName?: string;
  lastName: string;
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
}

export interface UpdatePersonDto {
  firstName?: string;
  lastName?: string;
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
}
