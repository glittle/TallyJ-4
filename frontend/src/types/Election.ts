import type { ElectionStage } from "../domain/electionStages";

export interface ElectionDto {
  electionGuid: string;
  name: string;
  dateOfElection?: string;
  electionType?: string;
  numberToElect?: number;
  electionStage: ElectionStage;
  convenor?: string;
  electionMode?: string;
  numberExtra?: number;
  showFullReport?: boolean;
  listForPublic?: boolean;
  showAsTest?: boolean | null;
  useOnlineVoting?: boolean;
  onlineWhenOpen?: string;
  onlineWhenClose?: string;
  electionPasscode?: string;
  linkedElectionGuid?: string;
  linkedElectionKind?: string;
  useCallInButton?: boolean;
  hidePreBallotPages?: boolean;
  maskVotingMethod?: boolean;
  guestTellersCanAddPeople?: boolean;
  onlineCloseIsEstimate?: boolean;
  onlineSelectionProcess?: string;
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
  tellerLoginLockedUntil?: string;
  paidChannelBlockReason?: string | null;
  paidSendsUsed?: number;
  paidSendAllowance?: number;
  onlineVotingSuspended?: boolean;
}

export interface DuplicateElectionDto {
  name?: string;
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
  showAsTest?: boolean | null;
  electionPasscode?: string;
  linkedElectionGuid?: string;
  linkedElectionKind?: string;
  useCallInButton?: boolean;
  hidePreBallotPages?: boolean;
  maskVotingMethod?: boolean;
  guestTellersCanAddPeople?: boolean;
  useOnlineVoting?: boolean;
  onlineWhenOpen?: string;
  onlineWhenClose?: string;
  onlineCloseIsEstimate?: boolean;
  onlineSelectionProcess?: string;
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
  showAsTest?: boolean | null;
  electionStage?: ElectionStage;
  useOnlineVoting?: boolean;
  onlineWhenOpen?: string;
  onlineWhenClose?: string;
  electionPasscode?: string;
  linkedElectionGuid?: string;
  linkedElectionKind?: string;
  useCallInButton?: boolean;
  hidePreBallotPages?: boolean;
  maskVotingMethod?: boolean;
  guestTellersCanAddPeople?: boolean;
  onlineCloseIsEstimate?: boolean;
  onlineSelectionProcess?: string;
  emailFromAddress?: string;
  emailFromName?: string;
  emailText?: string;
  emailSubject?: string;
  smsText?: string;
  customMethods?: string;
  votingMethods?: string;
  flags?: string;
}

export interface ElectionStats {
  voterCount: number;
  ballotCount: number;
  locationCount: number;
}

/** Lightweight status for a joined full or guest teller. */
export interface ElectionStatus {
  electionGuid: string;
  name: string;
  dateOfElection?: string | null;
  electionType?: string;
  electionStage?: ElectionStage;
  isActive: boolean;
  registeredVoters: number;
  ballotsSubmitted: number;
}

/** Linked-election dropdown only reads the guid and name. */
export interface LinkedElectionOption {
  electionGuid: string;
  name: string;
}

export interface ElectionSummaryDto {
  electionGuid: string;
  name: string;
  dateOfElection?: string;
  electionType?: string;
  electionStage?: ElectionStage;
  voterCount: number;
  ballotCount: number;
  isTellerAccessOpen?: boolean;
  isOnlineVotingEnabled?: boolean;
  showAsTest?: boolean | null;
  /** Positions to elect; mapped from API summary field `toElect`. */
  numberToElect?: number;
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
