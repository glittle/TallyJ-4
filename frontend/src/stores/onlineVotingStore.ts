import { defineStore } from "pinia";
import { ref } from "vue";
import { onlineVotingService } from "../services/onlineVotingService";
import { useApiErrorHandler } from "../composables/useApiErrorHandler";
import type {
  RequestCodeDto,
  VerifyCodeDto,
  OnlineElectionInfo,
  OnlineCandidate,
  SubmitOnlineBallotDto,
  OnlineVoteStatus,
  GoogleAuthForVoterDto,
  FacebookAuthForVoterDto,
  KakaoAuthForVoterDto,
  TelegramAuthForVoterDto,
} from "../types";

export const useOnlineVotingStore = defineStore("onlineVoting", () => {
  const { handleApiError } = useApiErrorHandler();

  const voterToken = ref<string | null>(localStorage.getItem("voter_token"));
  const voterId = ref<string | null>(localStorage.getItem("voter_id"));
  const electionInfo = ref<OnlineElectionInfo | null>(null);
  const candidates = ref<OnlineCandidate[]>([]);
  const voteStatus = ref<OnlineVoteStatus | null>(null);
  const loading = ref(false);

  async function requestVerificationCode(data: RequestCodeDto) {
    try {
      loading.value = true;
      const response = await onlineVotingService.requestCode(data);
      return response.messageKey;
    } catch (error) {
      handleApiError(error as any);
      throw error;
    } finally {
      loading.value = false;
    }
  }

  async function verifyCode(data: VerifyCodeDto) {
    try {
      loading.value = true;
      const response = await onlineVotingService.verifyCode(data);
      voterToken.value = response.token;
      voterId.value = response.voterId;
      localStorage.setItem("voter_token", response.token);
      localStorage.setItem("voter_id", response.voterId);
      return response;
    } catch (error) {
      handleApiError(error as any);
      throw error;
    } finally {
      loading.value = false;
    }
  }

  async function googleAuth(data: GoogleAuthForVoterDto) {
    try {
      loading.value = true;
      const response = await onlineVotingService.googleAuth(data);
      voterToken.value = response.token;
      voterId.value = response.voterId;
      localStorage.setItem("voter_token", response.token);
      localStorage.setItem("voter_id", response.voterId);
      return response;
    } catch (error) {
      handleApiError(error as any);
      throw error;
    } finally {
      loading.value = false;
    }
  }

  async function facebookAuth(data: FacebookAuthForVoterDto) {
    try {
      loading.value = true;
      const response = await onlineVotingService.facebookAuth(data);
      voterToken.value = response.token;
      voterId.value = response.voterId;
      localStorage.setItem("voter_token", response.token);
      localStorage.setItem("voter_id", response.voterId);
      return response;
    } catch (error) {
      handleApiError(error as any);
      throw error;
    } finally {
      loading.value = false;
    }
  }

  async function kakaoAuth(data: KakaoAuthForVoterDto) {
    try {
      loading.value = true;
      const response = await onlineVotingService.kakaoAuth(data);
      voterToken.value = response.token;
      voterId.value = response.voterId;
      localStorage.setItem("voter_token", response.token);
      localStorage.setItem("voter_id", response.voterId);
      return response;
    } catch (error) {
      handleApiError(error as any);
      throw error;
    } finally {
      loading.value = false;
    }
  }

  async function telegramAuth(data: TelegramAuthForVoterDto) {
    try {
      loading.value = true;
      const response = await onlineVotingService.telegramAuth(data);
      voterToken.value = response.token;
      voterId.value = response.voterId;
      localStorage.setItem("voter_token", response.token);
      localStorage.setItem("voter_id", response.voterId);
      return response;
    } catch (error) {
      handleApiError(error as any);
      throw error;
    } finally {
      loading.value = false;
    }
  }

  async function loadElectionInfo(electionGuid: string) {
    try {
      loading.value = true;
      const data = await onlineVotingService.getElectionInfo(electionGuid);
      electionInfo.value = data;
      return data;
    } catch (error) {
      handleApiError(error as any);
      throw error;
    } finally {
      loading.value = false;
    }
  }

  async function loadCandidates(electionGuid: string) {
    try {
      loading.value = true;
      const data = await onlineVotingService.getCandidates(electionGuid);
      candidates.value = data;
      return data;
    } catch (error) {
      handleApiError(error as any);
      throw error;
    } finally {
      loading.value = false;
    }
  }

  async function submitBallot(
    electionGuid: string,
    data: SubmitOnlineBallotDto,
  ) {
    try {
      loading.value = true;
      const response = await onlineVotingService.submitBallot(
        electionGuid,
        data,
      );
      return response;
    } catch (error) {
      handleApiError(error as any);
      throw error;
    } finally {
      loading.value = false;
    }
  }

  async function checkVoteStatus(electionGuid: string, voterIdToCheck: string) {
    try {
      loading.value = true;
      const data = await onlineVotingService.getVoteStatus(
        electionGuid,
        voterIdToCheck,
      );
      voteStatus.value = data;
      return data;
    } catch (error) {
      handleApiError(error as any);
      throw error;
    } finally {
      loading.value = false;
    }
  }

  function logout() {
    voterToken.value = null;
    voterId.value = null;
    electionInfo.value = null;
    candidates.value = [];
    voteStatus.value = null;
    localStorage.removeItem("voter_token");
    localStorage.removeItem("voter_id");
  }

  return {
    voterToken,
    voterId,
    electionInfo,
    candidates,
    voteStatus,
    loading,
    requestVerificationCode,
    verifyCode,
    googleAuth,
    facebookAuth,
    kakaoAuth,
    telegramAuth,
    loadElectionInfo,
    loadCandidates,
    submitBallot,
    checkVoteStatus,
    logout,
  };
});
