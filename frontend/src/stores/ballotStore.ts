import { defineStore } from "pinia";
import { ref } from "vue";
import { ballotService } from "../services/ballotService";
import { voteService } from "../services/voteService";
import { signalrService } from "../services/signalrService";

import type {
  BallotDto,
  CreateBallotDto,
  UpdateBallotDto,
  CreateVoteDto,
  VoteWithBallotStatusDto,
} from "../types";
import type { BallotUpdateEvent } from "../types/SignalREvents";
import { normalizeVoteList } from "../utils/voteDtoNormalization";

export const useBallotStore = defineStore("ballot", () => {
  const ballots = ref<BallotDto[]>([]);
  const currentBallot = ref<BallotDto | null>(null);
  const loading = ref(false);
  const error = ref<string | null>(null);
  const signalrInitialized = ref(false);

  async function fetchBallots(electionGuid: string) {
    loading.value = true;
    error.value = null;
    try {
      ballots.value = (await ballotService.getAll(electionGuid)) ?? [];
    } catch (e: any) {
      error.value = e.message || "Failed to fetch ballots";
      throw e;
    } finally {
      loading.value = false;
    }
  }

  async function fetchBallotById(ballotGuid: string) {
    loading.value = true;
    error.value = null;
    try {
      const ballot = await ballotService.getById(ballotGuid);
      currentBallot.value = ballot;

      const index = ballots.value.findIndex((b) => b.ballotGuid === ballotGuid);
      if (index !== -1) {
        ballots.value[index] = ballot;
      } else {
        ballots.value.push(ballot);
      }

      return ballot;
    } catch (e: any) {
      error.value = e.message || "Failed to fetch ballot";
      throw e;
    } finally {
      loading.value = false;
    }
  }

  async function createBallot(dto: CreateBallotDto) {
    loading.value = true;
    error.value = null;
    try {
      const ballot = await ballotService.create(dto);
      ballots.value.push(ballot);
      currentBallot.value = ballot;
      return ballot;
    } catch (e: any) {
      error.value = e.message || "Failed to create ballot";
      throw e;
    } finally {
      loading.value = false;
    }
  }

  async function updateBallot(ballotGuid: string, dto: UpdateBallotDto) {
    loading.value = true;
    error.value = null;
    try {
      const ballot = await ballotService.update(ballotGuid, dto);

      const index = ballots.value.findIndex((b) => b.ballotGuid === ballotGuid);
      if (index !== -1) {
        ballots.value[index] = ballot;
      }

      if (currentBallot.value?.ballotGuid === ballotGuid) {
        currentBallot.value = ballot;
      }

      return ballot;
    } catch (e: any) {
      error.value = e.message || "Failed to update ballot";
      throw e;
    } finally {
      loading.value = false;
    }
  }

  async function deleteBallot(ballotGuid: string) {
    loading.value = true;
    error.value = null;
    try {
      await ballotService.delete(ballotGuid);

      ballots.value = ballots.value.filter((b) => b.ballotGuid !== ballotGuid);

      if (currentBallot.value?.ballotGuid === ballotGuid) {
        currentBallot.value = null;
      }
    } catch (e: any) {
      error.value = e.message || "Failed to delete ballot";
      throw e;
    } finally {
      loading.value = false;
    }
  }

  function applyVoteMutationResult(
    ballotGuid: string,
    result: VoteWithBallotStatusDto,
    options?: { removedPosition?: number },
  ) {
    if (currentBallot.value?.ballotGuid !== ballotGuid) {
      return;
    }

    let updatedVotes = result.votes;
    if (updatedVotes == null) {
      if (result.vote) {
        const existingIndex = currentBallot.value.votes.findIndex(
          (v) => v.positionOnBallot === result.vote!.positionOnBallot,
        );
        updatedVotes =
          existingIndex !== -1
            ? currentBallot.value.votes.map((v, i) =>
                i === existingIndex ? result.vote! : v,
              )
            : [...currentBallot.value.votes, result.vote!];
      } else if (options?.removedPosition != null) {
        updatedVotes = currentBallot.value.votes.filter(
          (v) => v.positionOnBallot !== options.removedPosition,
        );
      } else {
        return;
      }
    }

    const normalizedVotes = normalizeVoteList(updatedVotes);

    const updatedBallot: BallotDto = {
      ...currentBallot.value,
      votes: normalizedVotes,
      voteCount: normalizedVotes.length,
      statusCode: String(
        result.ballotStatusCode ?? currentBallot.value.statusCode,
      ),
    };

    currentBallot.value = updatedBallot;

    const ballotIndex = ballots.value.findIndex(
      (b) => b.ballotGuid === ballotGuid,
    );
    if (ballotIndex !== -1) {
      ballots.value[ballotIndex] = updatedBallot;
    }
  }

  async function createVote(
    dto: CreateVoteDto,
  ): Promise<VoteWithBallotStatusDto> {
    loading.value = true;
    error.value = null;
    try {
      const result = await voteService.create(dto);
      applyVoteMutationResult(dto.ballotGuid, result);

      return result;
    } catch (e: any) {
      error.value = e.message || "Failed to create vote";
      throw e;
    } finally {
      loading.value = false;
    }
  }

  async function deleteVote(ballotGuid: string, positionOnBallot: number) {
    loading.value = true;
    error.value = null;
    try {
      const vote = currentBallot.value?.votes.find(
        (v) =>
          v.ballotGuid === ballotGuid &&
          v.positionOnBallot === positionOnBallot,
      );
      if (!vote) {
        throw new Error("Vote not found");
      }

      const result = await voteService.delete(vote.rowId);
      applyVoteMutationResult(ballotGuid, result, {
        removedPosition: positionOnBallot,
      });
    } catch (e: any) {
      error.value = e.message || "Failed to delete vote";
      throw e;
    } finally {
      loading.value = false;
    }
  }

  function setCurrentBallot(ballot: BallotDto | null) {
    currentBallot.value = ballot;
  }

  function clearError() {
    error.value = null;
  }

  async function initializeSignalR() {
    if (signalrInitialized.value) {
      return;
    }

    try {
      const connection = await signalrService.connectToFrontDeskHub();

      connection.on("updateBallots", (data: any) => {
        if (data && typeof data === "object" && data.ballotGuid) {
          const updateEvent: BallotUpdateEvent = {
            electionGuid: data.electionGuid || "",
            ballotGuid: data.ballotGuid,
            action: data.action || "updated",
            ballotCode: data.ballotCode,
            statusCode: data.statusCode,
            voteCount: data.voteCount,
            updatedAt: data.updatedAt || new Date().toISOString(),
          };

          switch (updateEvent.action) {
            case "added":
              handleBallotAdded(updateEvent);
              break;
            case "updated":
              handleBallotUpdated(updateEvent);
              break;
            case "deleted":
              handleBallotDeleted(updateEvent);
              break;
          }
        }
      });

      connection.on("reloadPage", () => {
        // Handle page reload command from server
        console.log("Server requested page reload for ballots");
        // Could trigger a data reload instead of full page refresh
      });

      signalrInitialized.value = true;
    } catch (e) {
      console.error("Failed to initialize SignalR for ballot store:", e);
    }
  }

  function handleBallotAdded(data: BallotUpdateEvent) {
    const exists = ballots.value.some((b) => b.ballotGuid === data.ballotGuid);
    if (!exists) {
      // Fetch the new ballot to get full details
      fetchBallotById(data.ballotGuid).catch(console.error);
    }
  }

  function handleBallotUpdated(data: BallotUpdateEvent) {
    // Refresh the ballot data
    fetchBallotById(data.ballotGuid).catch(console.error);
  }

  function handleBallotDeleted(data: BallotUpdateEvent) {
    ballots.value = ballots.value.filter(
      (b) => b.ballotGuid !== data.ballotGuid,
    );

    if (currentBallot.value?.ballotGuid === data.ballotGuid) {
      currentBallot.value = null;
    }
  }

  async function joinElection(electionGuid: string) {
    try {
      await signalrService.joinElection(electionGuid);
    } catch (e) {
      console.error("Failed to join election group for ballot updates:", e);
    }
  }

  async function leaveElection(electionGuid: string) {
    try {
      await signalrService.leaveElection(electionGuid);
    } catch (e) {
      console.error("Failed to leave election group for ballot updates:", e);
    }
  }

  return {
    ballots,
    currentBallot,
    loading,
    error,
    fetchBallots,
    fetchBallotById,
    createBallot,
    updateBallot,
    deleteBallot,
    createVote,
    deleteVote,
    setCurrentBallot,
    clearError,
    initializeSignalR,
    joinElection,
    leaveElection,
  };
});
