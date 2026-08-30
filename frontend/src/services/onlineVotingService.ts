import { client } from "@/api/gen/configService/client.gen";
import {
  getApiOnlineVotingAvailableElections,
  getApiOnlineVotingByElectionGuidByVoterIdVoteStatus,
  getApiOnlineVotingByElectionGuidPeople,
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

  async getAvailableElections(): Promise<OnlineVotingAvailableElectionDto[]> {
    const response = await getApiOnlineVotingAvailableElections();
    return requireData(response.data, "getAvailableElections");
  },

  async getSession(): Promise<{ voterId: string; voterIdType: string }> {
    const response = await client.get({ url: "/api/online-voting/me" });
    const payload = requireData(
      response.data as { voterId?: string; voterIdType?: string } | undefined,
      "getSession",
    );
    return {
      voterId: payload.voterId ?? "",
      voterIdType: payload.voterIdType ?? "",
    };
  },

  async logout(): Promise<void> {
    await client.post({ url: "/api/online-voting/logout" });
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
    const response = await getApiOnlineVotingByElectionGuidPeople({
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
