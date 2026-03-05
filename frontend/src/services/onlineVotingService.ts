import {
  postApiOnlineVotingRequestCode,
  postApiOnlineVotingVerifyCode,
  postApiOnlineVotingGoogleAuth,
  postApiOnlineVotingFacebookAuth,
  postApiOnlineVotingKakaoAuth,
  getApiOnlineVotingAvailableElections,
  getApiOnlineVotingByElectionGuidElectionInfo,
  getApiOnlineVotingByElectionGuidCandidates,
  postApiOnlineVotingByElectionGuidSubmitBallot,
  getApiOnlineVotingByElectionGuidByVoterIdVoteStatus,
} from "../api/gen/configService/sdk.gen";
import type {
  RequestCodeDto,
  VerifyCodeDto,
  OnlineVoterAuthResponse,
  OnlineElectionInfo,
  OnlineCandidate,
  SubmitOnlineBallotDto,
  OnlineVoteStatus,
  GoogleAuthForVoterDto,
  FacebookAuthForVoterDto,
  KakaoAuthForVoterDto,
} from "../types";

export const onlineVotingService = {
  async requestCode(data: RequestCodeDto): Promise<{ messageKey: string }> {
    const response = await postApiOnlineVotingRequestCode({
      body: data,
    });
    return response.data as { messageKey: string };
  },

  async verifyCode(data: VerifyCodeDto): Promise<OnlineVoterAuthResponse> {
    const response = await postApiOnlineVotingVerifyCode({
      body: data,
    });
    return response.data as OnlineVoterAuthResponse;
  },

  async googleAuth(
    data: GoogleAuthForVoterDto,
  ): Promise<OnlineVoterAuthResponse> {
    const response = await postApiOnlineVotingGoogleAuth({
      body: data,
    });
    return response.data as OnlineVoterAuthResponse;
  },

  async facebookAuth(
    data: FacebookAuthForVoterDto,
  ): Promise<OnlineVoterAuthResponse> {
    const response = await postApiOnlineVotingFacebookAuth({
      body: data,
    });
    return response.data as OnlineVoterAuthResponse;
  },

  async kakaoAuth(
    data: KakaoAuthForVoterDto,
  ): Promise<OnlineVoterAuthResponse> {
    const response = await postApiOnlineVotingKakaoAuth({
      body: data,
    });
    return response.data as OnlineVoterAuthResponse;
  },

  async getAvailableElections(voterId: string): Promise<OnlineElectionInfo[]> {
    const response = await getApiOnlineVotingAvailableElections({
      query: { voterId },
    });
    return response.data as OnlineElectionInfo[];
  },

  async getElectionInfo(electionGuid: string): Promise<OnlineElectionInfo> {
    const response = await getApiOnlineVotingByElectionGuidElectionInfo({
      path: { electionGuid },
    });
    return response.data as OnlineElectionInfo;
  },

  async getCandidates(electionGuid: string): Promise<OnlineCandidate[]> {
    const response = await getApiOnlineVotingByElectionGuidCandidates({
      path: { electionGuid },
    });
    return response.data as OnlineCandidate[];
  },

  async submitBallot(
    electionGuid: string,
    data: SubmitOnlineBallotDto,
  ): Promise<{ message: string }> {
    const response = await postApiOnlineVotingByElectionGuidSubmitBallot({
      path: { electionGuid },
      body: data,
    });
    return response.data as { message: string };
  },

  async getVoteStatus(
    electionGuid: string,
    voterId: string,
  ): Promise<OnlineVoteStatus> {
    const response = await getApiOnlineVotingByElectionGuidByVoterIdVoteStatus({
      path: { electionGuid, voterId },
    });
    return response.data as OnlineVoteStatus;
  },
};
