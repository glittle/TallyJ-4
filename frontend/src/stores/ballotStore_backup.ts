import { defineStore } from "pinia";
import { ref } from "vue";
import { ballotService } from "../services/ballotService";
import { voteService } from "../services/voteService";
import type {
  BallotDto,
  CreateBallotDto,
  UpdateBallotDto,
  CreateVoteDto,
} from "../types";

export const useBallotStore = defineStore("ballot", () => {
  const ballots = ref<BallotDto[]>([]);
  const currentBallot = ref<BallotDto | null>(null);
  const loading = ref(false);
  const error = ref<string | null>(null);

  async function fetchBallots(electionGuid: string) {
    loading.value = true;
    error.value = null;
    try {
      ballots.value = await ballotService.getAll(electionGuid);
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

  async function createVote(dto: CreateVoteDto) {
    loading.value = true;
    error.value = null;
    try {
      const vote = await voteService.create(dto);

      if (currentBallot.value?.ballotGuid === dto.ballotGuid) {
        currentBallot.value.votes.push(vote);
        currentBallot.value.voteCount = currentBallot.value.votes.length;
      }

      return vote;
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

      await voteService.delete(vote.rowId);

      if (currentBallot.value?.ballotGuid === ballotGuid) {
        currentBallot.value.votes = currentBallot.value.votes.filter(
          (v) => v.positionOnBallot !== positionOnBallot,
        );
        currentBallot.value.voteCount = currentBallot.value.votes.length;
      }
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
  };
});
