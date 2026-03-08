export interface RequestCodeDto {
  voterId: string;
  voterIdType: "E" | "P" | "C";
  deliveryMethod: "email" | "sms" | "voice" | "whatsapp";
}

export interface VerifyCodeDto {
  voterId: string;
  verifyCode: string;
}

export interface OnlineVoterAuthResponse {
  token: string;
  voterId: string;
  voterIdType: string;
  expiresAt: string;
}

export interface OnlineElectionInfo {
  electionGuid: string;
  name: string;
  convenor?: string;
  dateOfElection?: string;
  numberToElect?: number;
  onlineWhenOpen?: string;
  onlineWhenClose?: string;
  isOpen: boolean;
  instructions?: string;
}

export interface OnlineCandidate {
  personGuid: string;
  fullName: string;
  area?: string;
  otherInfo?: string;
}

export interface OnlineVote {
  personGuid?: string;
  voteName?: string;
  positionOnBallot: number;
}

export interface SubmitOnlineBallotDto {
  electionGuid: string;
  voterId: string;
  votes: OnlineVote[];
}

export interface OnlineVoteStatus {
  hasVoted: boolean;
  whenSubmitted?: string;
  message?: string;
}

export interface GoogleAuthForVoterDto {
  credential: string;
}

export interface FacebookAuthForVoterDto {
  accessToken: string;
}

export interface KakaoAuthForVoterDto {
  accessToken: string;
}
