export interface ElectionUpdateEvent {
  electionGuid: string;
  name?: string;
  tallyStatus?: string;
  electionStatus?: string;
  updatedAt: string;
}

export interface TallyProgressEvent {
  electionGuid: string;
  totalBallots: number;
  processedBallots: number;
  totalVotes: number;
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

export interface PeopleImportCompleteEvent {
  result: {
    success: boolean;
    peopleAdded: number;
    peopleSkipped: number;
    totalRows: number;
    warnings: string[];
    errors: string[];
    timeElapsedSeconds: number;
  };
}
