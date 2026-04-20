export interface ElectionDto {
  electionGuid: string;
  name: string;
  dateOfElection?: string;
  electionType?: string;
  numberToElect?: number;
  tallyStatus?: string;
  electionStatus?: string;
  convenor?: string;
  electionMode?: string;
  numberExtra?: number;
  showFullReport?: boolean;
  listForPublic?: boolean;
  showAsTest?: boolean;
  onlineWhenOpen?: string;
  onlineWhenClose?: string;
  voterCount: number;
  ballotCount: number;
  locationCount: number;
  electionPasscode?: string;
  linkedElectionGuid?: string;
  linkedElectionKind?: string;
  useCallInButton?: boolean;
  hidePreBallotPages?: boolean;
  maskVotingMethod?: boolean;
  onlineCloseIsEstimate?: boolean;
  onlineSelectionProcess?: string;
  onlineAnnounced?: string;
  emailFromAddress?: string;
  emailFromName?: string;
  emailText?: string;
  emailSubject?: string;
  smsText?: string;
  customMethods?: string;
  votingMethods?: string;
  flags?: string;
  isTellerAccessOpen?: boolean;
  tellerAccessOpenedAt?: string;
}

export interface CreateElectionDto {
  name: string;
  dateOfElection?: string;
  electionType?: string;
  numberToElect?: number;
  convenor?: string;
  electionMode?: string;
  numberExtra?: number;
  showFullReport?: boolean;
  listForPublic?: boolean;
  showAsTest?: boolean;
  electionPasscode?: string;
  linkedElectionGuid?: string;
  linkedElectionKind?: string;
  useCallInButton?: boolean;
  hidePreBallotPages?: boolean;
  maskVotingMethod?: boolean;
  onlineWhenOpen?: string;
  onlineWhenClose?: string;
  onlineCloseIsEstimate?: boolean;
  onlineSelectionProcess?: string;
  onlineAnnounced?: string;
  emailFromAddress?: string;
  emailFromName?: string;
  emailText?: string;
  emailSubject?: string;
  smsText?: string;
  customMethods?: string;
  votingMethods?: string;
  flags?: string;
}

export interface UpdateElectionDto {
  name?: string;
  dateOfElection?: string;
  electionType?: string;
  numberToElect?: number;
  convenor?: string;
  electionMode?: string;
  numberExtra?: number;
  showFullReport?: boolean;
  listForPublic?: boolean;
  showAsTest?: boolean;
  tallyStatus?: string;
  onlineWhenOpen?: string;
  onlineWhenClose?: string;
  electionPasscode?: string;
  linkedElectionGuid?: string;
  linkedElectionKind?: string;
  useCallInButton?: boolean;
  hidePreBallotPages?: boolean;
  maskVotingMethod?: boolean;
  onlineCloseIsEstimate?: boolean;
  onlineSelectionProcess?: string;
  onlineAnnounced?: string;
  emailFromAddress?: string;
  emailFromName?: string;
  emailText?: string;
  emailSubject?: string;
  smsText?: string;
  customMethods?: string;
  votingMethods?: string;
  flags?: string;
}

export interface ElectionSummaryDto {
  electionGuid: string;
  name: string;
  dateOfElection?: string;
  electionType?: string;
  tallyStatus?: string;
  voterCount: number;
  ballotCount: number;
  isTellerAccessOpen?: boolean;
  isOnlineVotingEnabled?: boolean;
  showAsTest?: boolean;
}

export interface ImportResultDto {
  success: boolean;
  errors: string[];
  warnings: string[];
  ballotsCreated: number;
  votesCreated: number;
  totalRows: number;
  skippedRows: number;
}
