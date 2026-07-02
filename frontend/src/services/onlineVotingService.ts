import {
  getApiOnlineVotingAvailableElections,
  getApiOnlineVotingByElectionGuidByVoterIdVoteStatus,
  getApiOnlineVotingByElectionGuidVotablePeople,
  getApiOnlineVotingByElectionGuidElectionInfo,
  postApiOnlineVotingByElectionGuidSubmitBallot,
  postApiOnlineVotingFacebookAuth,
  postApiOnlineVotingGoogleAuth,
  postApiOnlineVotingKakaoAuth,
  postApiOnlineVotingRequestCode,
  postApiOnlineVotingTelegramAuth,
  postApiOnlineVotingVerifyCode,
} from "@/api/gen/configService";
import type {
  OnlineVotingAvailableElectionDto,
  OnlineVotingFacebookAuthForVoterDto,
  OnlineVotingGoogleAuthForVoterDto,
  OnlineVotingKakaoAuthForVoterDto,
  OnlineVotingOnlinePersonDto,
  OnlineVotingOnlineElectionInfoDto,
  OnlineVotingOnlineVoteStatusDto,
  OnlineVotingOnlineVoterAuthResponse,
  OnlineVotingRequestCodeDto,
  OnlineVotingSubmitOnlineBallotDto,
  OnlineVotingTelegramAuthForVoterDto,
  OnlineVotingVerifyCodeDto,
} from "@/api/gen/configService/types.gen";

function requireData<T>(data: T | undefined, context: string): T {
  if (data === undefined || data === null) {
    throw new Error(`${context}: empty API response`);
  }
  return data;
}

export const onlineVotingService = {
  async requestCode(
    data: OnlineVotingRequestCodeDto,
  ): Promise<{ messageKey: string }> {
    const response = await postApiOnlineVotingRequestCode({ body: data });
    const payload = requireData(response.data, "requestCode");
    return { messageKey: payload.messageKey ?? "" };
  },

  async verifyCode(
    data: OnlineVotingVerifyCodeDto,
  ): Promise<OnlineVotingOnlineVoterAuthResponse> {
    const response = await postApiOnlineVotingVerifyCode({ body: data });
    return requireData(response.data, "verifyCode");
  },

  async googleAuth(
    data: OnlineVotingGoogleAuthForVoterDto,
  ): Promise<OnlineVotingOnlineVoterAuthResponse> {
    const response = await postApiOnlineVotingGoogleAuth({ body: data });
    return requireData(response.data, "googleAuth");
  },

  async facebookAuth(
    data: OnlineVotingFacebookAuthForVoterDto,
  ): Promise<OnlineVotingOnlineVoterAuthResponse> {
    const response = await postApiOnlineVotingFacebookAuth({ body: data });
    return requireData(response.data, "facebookAuth");
  },

  async kakaoAuth(
    data: OnlineVotingKakaoAuthForVoterDto,
  ): Promise<OnlineVotingOnlineVoterAuthResponse> {
    const response = await postApiOnlineVotingKakaoAuth({ body: data });
    return requireData(response.data, "kakaoAuth");
  },

  async telegramAuth(
    data: OnlineVotingTelegramAuthForVoterDto,
  ): Promise<OnlineVotingOnlineVoterAuthResponse> {
    const response = await postApiOnlineVotingTelegramAuth({ body: data });
    return requireData(response.data, "telegramAuth");
  },

  async getAvailableElections(
    voterId: string,
  ): Promise<OnlineVotingAvailableElectionDto[]> {
    const response = await getApiOnlineVotingAvailableElections({
      query: { voterId },
    });
    return requireData(response.data, "getAvailableElections");
  },

  async getElectionInfo(
    electionGuid: string,
  ): Promise<OnlineVotingOnlineElectionInfoDto> {
    const response = await getApiOnlineVotingByElectionGuidElectionInfo({
      path: { electionGuid },
    });
    return requireData(response.data, "getElectionInfo");
  },

  async getVotablePeople(
    electionGuid: string,
  ): Promise<OnlineVotingOnlinePersonDto[]> {
    const response = await getApiOnlineVotingByElectionGuidVotablePeople({
      path: { electionGuid },
    });
    return requireData(response.data, "getVotablePeople");
  },

  async submitBallot(
    electionGuid: string,
    data: OnlineVotingSubmitOnlineBallotDto,
  ): Promise<{ message: string }> {
    const response = await postApiOnlineVotingByElectionGuidSubmitBallot({
      path: { electionGuid },
      body: data,
    });
    const payload = requireData(response.data, "submitBallot");
    return { message: payload.message ?? "" };
  },

  async getVoteStatus(
    electionGuid: string,
    voterId: string,
  ): Promise<OnlineVotingOnlineVoteStatusDto> {
    const response = await getApiOnlineVotingByElectionGuidByVoterIdVoteStatus({
      path: { electionGuid, voterId },
    });
    return requireData(response.data, "getVoteStatus");
  },
};