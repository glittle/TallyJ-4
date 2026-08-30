import { defineStore } from "pinia";
import { computed, ref } from "vue";
import { onlineVotingService } from "../services/onlineVotingService";
import { secureTokenService } from "../services/secureTokenService";
import { signalrService } from "../services/signalrService";
import { useApiErrorHandler } from "../composables/useApiErrorHandler";
import type {
  RequestCodeDto,
  VerifyCodeDto,
  OnlineElectionInfo,
  OnlinePerson,
  SubmitOnlineBallotDto,
  OnlineVoteStatus,
  GoogleAuthForVoterDto,
  FacebookAuthForVoterDto,
  KakaoAuthForVoterDto,
  TelegramAuthForVoterDto,
  AvailableElection,
} from "../types";
import type {
  UpdateVoterEvent,
  UpdateVotersEvent,
} from "../types/SignalREvents";

export const useOnlineVotingStore = defineStore("onlineVoting", () => {
  const { handleApiError } = useApiErrorHandler();

  const voterId = ref<string | null>(null);
  const electionInfo = ref<OnlineElectionInfo | null>(null);
  const votablePeople = ref<OnlinePerson[]>([]);
  const voteStatus = ref<OnlineVoteStatus | null>(null);
  const availableElections = ref<AvailableElection[]>([]);
  const loading = ref(false);
  /** True while AllVoters + VoterPersonal hubs are connected for this session. */
  const voterHubsConnected = ref(false);
  /** Multi-device login notice for the UI (cleared when dismissed or on logout). */
  const loginElsewhereNotice = ref(false);

  let voterHubHandlersBound = false;
  let restorePromise: Promise<boolean> | null = null;

  // Drop pre-#250 JWTs that may still sit in JS-readable storage on this origin.
  try {
    localStorage.removeItem("voter_token");
    localStorage.removeItem("voter_id");
  } catch {
    // Ignore quota / private-mode failures.
  }

  const isAuthenticated = computed(() => !!voterId.value);

  function persistAuth(id: string) {
    voterId.value = id;
  }

  async function applyAuthResponse(response: {
    voterId?: string | null;
  }): Promise<void> {
    if (response.voterId) {
      persistAuth(response.voterId);
      return;
    }
    await restoreSession();
  }

  /**
   * Restores voterId from the httpOnly cookie session via GET /me.
   * Uses the non-httpOnly voter_session flag to skip the call when logged out.
   */
  async function restoreSession(): Promise<boolean> {
    if (voterId.value) {
      return true;
    }
    if (!secureTokenService.isVoterAuthenticated()) {
      return false;
    }
    if (restorePromise) {
      return restorePromise;
    }

    restorePromise = (async () => {
      try {
        const session = await onlineVotingService.getSession();
        if (!session.voterId) {
          return false;
        }
        persistAuth(session.voterId);
        return true;
      } catch {
        secureTokenService.clearVoterSession();
        return false;
      } finally {
        restorePromise = null;
      }
    })();

    return restorePromise;
  }

  async function requestVerificationCode(data: RequestCodeDto) {
    try {
      loading.value = true;
      const response = await onlineVotingService.requestCode(data);
      return response.messageKey;
    } finally {
      loading.value = false;
    }
  }

  async function verifyCode(data: VerifyCodeDto) {
    try {
      loading.value = true;
      const response = await onlineVotingService.verifyCode(data);
      await applyAuthResponse(response);
      return response;
    } finally {
      loading.value = false;
    }
  }

  async function googleAuth(data: GoogleAuthForVoterDto) {
    try {
      loading.value = true;
      const response = await onlineVotingService.googleAuth(data);
      await applyAuthResponse(response);
      return response;
    } finally {
      loading.value = false;
    }
  }

  async function facebookAuth(data: FacebookAuthForVoterDto) {
    try {
      loading.value = true;
      const response = await onlineVotingService.facebookAuth(data);
      await applyAuthResponse(response);
      return response;
    } finally {
      loading.value = false;
    }
  }

  async function kakaoAuth(data: KakaoAuthForVoterDto) {
    try {
      loading.value = true;
      const response = await onlineVotingService.kakaoAuth(data);
      await applyAuthResponse(response);
      return response;
    } finally {
      loading.value = false;
    }
  }

  async function telegramAuth(data: TelegramAuthForVoterDto) {
    try {
      loading.value = true;
      const response = await onlineVotingService.telegramAuth(data);
      await applyAuthResponse(response);
      return response;
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

  async function loadVotablePeople(electionGuid: string) {
    try {
      loading.value = true;
      const data = await onlineVotingService.getVotablePeople(electionGuid);
      votablePeople.value = data;
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

  async function loadAvailableElections() {
    const authed = await restoreSession();
    if (!authed) {
      throw new Error("Voter is not authenticated.");
    }

    try {
      loading.value = true;
      const data = await onlineVotingService.getAvailableElections();
      availableElections.value = data;
      return data;
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

  function dismissLoginElsewhereNotice() {
    loginElsewhereNotice.value = false;
  }

  async function handleUpdateVoters(_payload: UpdateVotersEvent) {
    // Thin signal: re-fetch authoritative list (eligibility is server-side).
    if (!(await restoreSession())) {
      return;
    }
    try {
      await loadAvailableElections();
    } catch {
      // Errors already handled in loadAvailableElections; avoid tearing down hubs.
    }
  }

  async function handleUpdateVoter(payload: UpdateVoterEvent) {
    if (payload.login) {
      loginElsewhereNotice.value = true;
    }

    if (payload.updateRegistration && (await restoreSession())) {
      try {
        await loadAvailableElections();
        if (payload.electionGuid && voterId.value) {
          await checkVoteStatus(payload.electionGuid, voterId.value);
        }
      } catch {
        // Errors already handled; keep hub session alive.
      }
    }
  }

  /**
   * Connect AllVoters + VoterPersonal hubs for the authenticated voter session.
   * Auth is the httpOnly voter cookie (withCredentials). Safe to call multiple times.
   */
  async function ensureVoterHubsConnected(): Promise<void> {
    if (!(await restoreSession())) {
      return;
    }

    if (voterHubsConnected.value && voterHubHandlersBound) {
      return;
    }

    try {
      await signalrService.connectVoterHubs();

      if (!voterHubHandlersBound) {
        const allVoters = signalrService.getConnection("/hubs/all-voters");
        const personal = signalrService.getConnection("/hubs/voter-personal");

        allVoters?.on("updateVoters", (payload: UpdateVotersEvent) => {
          void handleUpdateVoters(payload);
        });

        personal?.on("updateVoter", (payload: UpdateVoterEvent) => {
          void handleUpdateVoter(payload);
        });

        voterHubHandlersBound = true;
      }

      voterHubsConnected.value = true;
    } catch (error) {
      console.warn("Failed to connect online voter SignalR hubs:", error);
      voterHubsConnected.value = false;
    }
  }

  async function disconnectVoterHubs(): Promise<void> {
    voterHubHandlersBound = false;
    voterHubsConnected.value = false;
    try {
      await signalrService.disconnectVoterHubs();
    } catch (error) {
      console.warn("Failed to disconnect online voter SignalR hubs:", error);
    }
  }

  async function logout() {
    await disconnectVoterHubs();
    try {
      await onlineVotingService.logout();
    } catch {
      // Still clear local UI state if the logout request fails.
    }
    secureTokenService.clearVoterSession();
    voterId.value = null;
    electionInfo.value = null;
    votablePeople.value = [];
    voteStatus.value = null;
    availableElections.value = [];
    loginElsewhereNotice.value = false;
  }

  return {
    voterId,
    isAuthenticated,
    electionInfo,
    votablePeople,
    voteStatus,
    availableElections,
    loading,
    voterHubsConnected,
    loginElsewhereNotice,
    restoreSession,
    requestVerificationCode,
    verifyCode,
    googleAuth,
    facebookAuth,
    kakaoAuth,
    telegramAuth,
    loadAvailableElections,
    loadElectionInfo,
    loadVotablePeople,
    submitBallot,
    checkVoteStatus,
    ensureVoterHubsConnected,
    disconnectVoterHubs,
    dismissLoginElsewhereNotice,
    logout,
  };
});
