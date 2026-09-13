export interface PersonPhoneSmsHintDto {
  hasPhoneRow: boolean;
  whenRegistered?: string | Date | null;
  smsStatus?: string | null;
}

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
  ineligibleReasonCode?: string;
  voteCount: number;
  phoneOnlineVoter?: PersonPhoneSmsHintDto | null;
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
  unitName?: string;
  phoneOnlineVoter?: PersonPhoneSmsHintDto | null;
}

export interface PersonSmsLogDto {
  sentDate: string | Date;
  lastDate?: string | Date | null;
  lastStatus?: string | null;
  errorCode?: number | null;
}

export interface PersonPhoneOnlineVoterDto extends PersonPhoneSmsHintDto {
  whenLastLogin?: string | Date | null;
  whatsAppStatus?: string | null;
  recentSmsLogs?: PersonSmsLogDto[];
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
  ineligibleReasonCode?: string;
  registrationTime?: string;
  votingLocationGuid?: string;
  votingMethod?: string;
  envNum?: number;
  teller1?: string;
  teller2?: string;
  hasOnlineBallot?: boolean;
  hasAcceptedBallot?: boolean;
  onlineBallotStatus?: string;
  registrationHistory?: string;
  kioskCode?: string;
  kioskCodeExpiresAt?: string | Date | null;
  kioskCodeConsumed?: boolean;
  unitName?: string;
  voteCount: number;
  canDelete?: boolean;
  phoneOnlineVoter?: PersonPhoneOnlineVoterDto | null;
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
  ineligibleReasonCode?: string;
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
  ineligibleReasonCode?: string;
}

export interface SetPersonPhoneSmsStatusDto {
  smsStatus: string;
}

export interface CheckSelectedWhatsAppPersonResultDto {
  personGuid: string;
  outcome: string;
}

export interface CheckSelectedWhatsAppResultDto {
  cancelled: boolean;
  checked: number;
  ok: number;
  noWa: number;
  failed: number;
  skipped: number;
  results: CheckSelectedWhatsAppPersonResultDto[];
}

export interface WhatsAppNotifyPersonResultDto {
  personGuid: string;
  outcome: string;
}

export interface WhatsAppNotifyStatusDto {
  queueToken: string;
  running: boolean;
  cancelled: boolean;
  queued: number;
  sent: number;
  skipped: number;
  failed: number;
  results: WhatsAppNotifyPersonResultDto[];
}
