export interface EligibilityReasonDto {
  reasonGuid: string;
  code: string;
  description: string;
  canVote: boolean;
  canReceiveVotes: boolean;
  internalOnly: boolean;
}