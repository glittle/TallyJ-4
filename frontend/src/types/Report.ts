export interface ReportListItem {
  code: string;
  name: string;
  category: string;
}

export interface MainReport {
  electionName: string;
  convenor?: string;
  dateOfElection?: string;
  numEligibleToVote: number;
  sumOfEnvelopesCollected: number;
  numBallotsWithManual: number;
  percentParticipation: number;
  inPersonBallots: number;
  mailedInBallots: number;
  droppedOffBallots: number;
  onlineBallots: number;
  importedBallots: number;
  calledInBallots: number;
  custom1Ballots: number;
  custom2Ballots: number;
  custom3Ballots: number;
  custom1Name?: string;
  custom2Name?: string;
  custom3Name?: string;
  spoiledBallots: number;
  spoiledVotes: number;
  spoiledBallotReasons: SpoiledBallotGroup[];
  spoiledVoteReasons: SpoiledVoteGroup[];
  elected: ElectedPerson[];
  hasTies: boolean;
}

export interface SpoiledBallotGroup {
  reason: string;
  ballotCount: number;
}

export interface SpoiledVoteGroup {
  reason: string;
  voteCount: number;
}

export interface ElectedPerson {
  rank: string;
  name: string;
  bahaiId?: string;
  voteCountDisplay: string;
  section: string;
}

export interface VotesByNumReport {
  electionName: string;
  dateOfElection?: string;
  people: VotePerson[];
}

export interface VotePerson {
  personName: string;
  voteCount: number;
  tieBreakCount?: number;
  tieBreakRequired: boolean;
  section: string;
  showBreak: boolean;
}

export interface VotesByNameReport {
  electionName: string;
  dateOfElection?: string;
  people: VotePerson[];
}

export interface BallotReportItem {
  ballotCode: string;
  location: string;
  isOnline: boolean;
  isImported: boolean;
  ballotId: number;
  locationId: number;
  statusCode: string;
  spoiled: boolean;
  votes: BallotVote[];
}

export interface BallotVote {
  personName: string;
  singleNameElectionCount?: number;
  onlineVoteRaw?: string;
  spoiled: boolean;
  tieBreakRequired: boolean;
  invalidReasonDesc?: string;
}

export interface BallotsReport {
  electionName: string;
  dateOfElection?: string;
  isSingleNameElection: boolean;
  ballots: BallotReportItem[];
}

export interface SpoiledVotesReport {
  electionName: string;
  dateOfElection?: string;
  people: SpoiledVoteItem[];
}

export interface SpoiledVoteItem {
  personName: string;
  voteCount: number;
  invalidReasonDesc: string;
}

export interface BallotAlignmentReport {
  electionName: string;
  dateOfElection?: string;
  numToElect: number;
  isSingleNameElection: boolean;
  rows: AlignmentRow[];
}

export interface AlignmentRow {
  matchingNames: number;
  ballotCount: number;
}

export interface BallotsSameReport {
  electionName: string;
  dateOfElection?: string;
  isSingleNameElection: boolean;
  groups: DuplicateGroup[];
}

export interface DuplicateGroup {
  groupNumber: number;
  ballots: BallotReportItem[];
}

export interface BallotsSummaryReport {
  electionName: string;
  dateOfElection?: string;
  ballots: BallotSummaryItem[];
}

export interface BallotSummaryItem {
  ballotCode: string;
  location: string;
  locationId: number;
  ballotId: number;
  statusCode: string;
  spoiled: boolean;
  spoiledVotes: number;
  teller1?: string;
  teller2?: string;
}

export interface AllCanReceiveReport {
  electionName: string;
  dateOfElection?: string;
  people: string[];
}

export interface VotersReport {
  electionName: string;
  dateOfElection?: string;
  hasMultipleLocations: boolean;
  totalCount: number;
  people: VoterItem[];
}

export interface VoterItem {
  personName: string;
  votingMethod: string;
  bahaiId?: string;
  location?: string;
  registrationTime?: string;
  teller1?: string;
  teller2?: string;
  registrationLog?: string;
}

export interface FlagsReport {
  electionName: string;
  dateOfElection?: string;
  hasMultipleLocations: boolean;
  flagNames: string[];
  people: FlagPerson[];
}

export interface FlagPerson {
  rowId: number;
  personName: string;
  location?: string;
  flags: string[];
}

export interface VotersOnlineReport {
  electionName: string;
  dateOfElection?: string;
  people: OnlineVoterItem[];
}

export interface OnlineVoterItem {
  personId: number;
  fullName: string;
  votingMethodDisplay: string;
  status?: string;
  whenStatus?: string;
  email?: string;
  whenEmail?: string;
  phone?: string;
  whenPhone?: string;
}

export interface VotersByAreaReport {
  electionName: string;
  dateOfElection?: string;
  custom1Name?: string;
  custom2Name?: string;
  custom3Name?: string;
  areas: AreaRow[];
  total: AreaRow;
}

export interface AreaRow {
  areaName: string;
  totalEligible: number;
  voted: number;
  inPerson: number;
  mailedIn: number;
  droppedOff: number;
  calledIn: number;
  custom1: number;
  custom2: number;
  custom3: number;
  online: number;
  onlineKiosk: number;
  imported: number;
}

export interface VotersByLocationReport {
  electionName: string;
  dateOfElection?: string;
  custom1Name?: string;
  custom2Name?: string;
  custom3Name?: string;
  locations: LocationRow[];
  total: LocationRow;
}

export interface LocationRow {
  locationName: string;
  totalVoters: number;
  inPerson: number;
  mailedIn: number;
  droppedOff: number;
  calledIn: number;
  custom1: number;
  custom2: number;
  custom3: number;
  online: number;
  onlineKiosk: number;
  imported: number;
}

export interface VotersByLocationAreaReport {
  electionName: string;
  dateOfElection?: string;
  locations: LocationAreaGroup[];
}

export interface LocationAreaGroup {
  locationName: string;
  areas: AreaCount[];
  totalCount: number;
}

export interface AreaCount {
  areaName: string;
  count: number;
}

export interface ChangedPeopleReport {
  electionName: string;
  dateOfElection?: string;
  people: ChangedPerson[];
}

export interface ChangedPerson {
  change: string;
  firstName?: string;
  lastName?: string;
  otherNames?: string;
  otherLastNames?: string;
  otherInfo?: string;
  bahaiId?: string;
  canVote: boolean;
  canReceiveVotes: boolean;
  invalidReasonDesc?: string;
}

export interface AllNonEligibleReport {
  electionName: string;
  dateOfElection?: string;
  people: NonEligiblePerson[];
}

export interface NonEligiblePerson {
  personName: string;
  canReceiveVotes: boolean;
  canVote: boolean;
  invalidReasonDesc?: string;
  votingMethod?: string;
}

export interface VoterEmailsReport {
  electionName: string;
  dateOfElection?: string;
  people: VoterEmailItem[];
}

export interface VoterEmailItem {
  fullName: string;
  bahaiId?: string;
  email?: string;
  phone?: string;
  canVote: boolean;
  hasSignedInEmail: boolean;
  hasSignedInPhone: boolean;
  votingMethod?: string;
}
