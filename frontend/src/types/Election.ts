export interface ElectionDto {
  electionGuid: string;
  name: string;
  dateOfElection?: string;
  electionType?: string;
  numberToElect?: number;
  tallyStatus?: string;
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
}

export interface ElectionSummaryDto {
  electionGuid: string;
  name: string;
  dateOfElection?: string;
  electionType?: string;
  tallyStatus?: string;
  voterCount: number;
  ballotCount: number;
}
