import type { ElectionStage } from "../domain/electionStages";

export interface ElectionUpdateEvent {
  electionGuid: string;
  name?: string;
  electionStage?: ElectionStage;
  updatedAt: string;
}

/** FrontDeskHub updateOnlineElection — online window / process thin payload. */
export interface OnlineElectionUpdateEvent {
  electionGuid: string;
  onlineWhenOpen?: string | null;
  onlineWhenClose?: string | null;
  onlineCloseIsEstimate?: boolean;
  onlineSelectionProcess?: string | null;
}

/**
 * AllVotersHub updateVoters — same thin online-window fields as FrontDesk.
 * Clients re-fetch GET availableElections (do not trust as full election detail).
 */
export type UpdateVotersEvent = OnlineElectionUpdateEvent;

/**
 * VoterPersonalHub updateVoter — registration change and/or multi-device login.
 * Clients re-fetch status / election list; login:true shows a notice.
 */
export interface UpdateVoterEvent {
  updateRegistration?: boolean;
  electionGuid?: string | null;
  votingMethod?: string | null;
  registrationTime?: string | null;
  login?: boolean;
}

export interface TallyProgressEvent {
  electionGuid: string;
  totalBallots: number;
  processedBallots: number;
  totalVotes: number;
  /** i18n phrase key; translated on the client via translateTallyProgressMessage */
  message: string;
  percentComplete: number;
  isComplete: boolean;
}

export interface ImportProgressEvent {
  electionGuid: string;
  totalRows: number;
  processedRows: number;
  successCount: number;
  errorCount: number;
  currentStatus: string;
  percentComplete: number;
  isComplete: boolean;
  errors: string[];
}

export interface PersonUpdateEvent {
  electionGuid: string;
  personGuid: string;
  action: "added" | "updated" | "deleted";
  firstName?: string;
  lastName?: string;
  updatedAt: string;
}
export interface BallotUpdateEvent {
  electionGuid: string;
  ballotGuid: string;
  action: "added" | "updated" | "deleted";
  ballotCode?: string;
  statusCode?: string;
  voteCount?: number;
  updatedAt: string;
}

export interface PersonVoteCountUpdateEvent {
  electionGuid: string;
  personGuid: string;
  voteCount: number;
}

export interface PeopleImportProgressEvent {
  processed: number;
  total: number;
  status: string;
}

/** Matches backend ImportPeopleResult pushed on importComplete. */
export interface PeopleImportCompleteEvent {
  success: boolean;
  peopleAdded: number;
  peopleSkipped: number;
  totalRows: number;
  warnings?: unknown[];
  errors?: unknown[];
  timeElapsedSeconds: number;
}

/**
 * Hub event `loaderStatus` is two positional args (not an object payload):
 * `(message: string, isTemporary: boolean)`.
 * Client UI log lines after processing those args:
 */
export interface ElectionPackageLoaderLogLine {
  id: number;
  message: string;
  /** When true, the next temporary line may replace this one. */
  isTemporary: boolean;
}
