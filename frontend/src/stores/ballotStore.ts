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
  ReorderVotesDto,
  VoteDto,
  VotePositionDto,
  VoteWithBallotStatusDto,
} from "../types";
import type { BallotUpdateEvent } from "../types/SignalREvents";
import {
  patchBallotSummary,
  summaryFromFullBallot,
  toBallotSummary,
  type BallotSummaryDto,
} from "../utils/ballotSummary";
import { normalizeVoteList } from "../utils/voteDtoNormalization";

function compactVotePositions(votes: VoteDto[]): VoteDto[] {
  return votes
    .slice()
    .sort((a, b) => a.positionOnBallot - b.positionOnBallot)
    .map((vote, index) => ({
      ...vote,
      positionOnBallot: index + 1,
    }));
}

function applyVotePositions(
  votes: VoteDto[],
  votePositions: VotePositionDto[],
): VoteDto[] {
  const positionByRowId = new Map(
    votePositions.map((position) => [
      position.rowId,
      position.positionOnBallot,
    ]),
  );

  return votes
    .filter((vote) => positionByRowId.has(vote.rowId))
    .map((vote) => ({
      ...vote,
      positionOnBallot: positionByRowId.get(vote.rowId)!,
    }))
    .sort((a, b) => a.positionOnBallot - b.positionOnBallot);
}

export const useBallotStore = defineStore("ballot", () => {
  const ballots = ref<BallotSummaryDto[]>([]);
  const currentBallot = ref<BallotDto | null>(null);
  const loading = ref(false);
  const error = ref<string | null>(null);
  const signalrInitialized = ref(false);
  const ballotFetchGeneration = new Map<string, number>();
  const ballotMutationGeneration = new Map<string, number>();

  function upsertBallotSummary(summary: BallotSummaryDto) {
    const index = ballots.value.findIndex(
      (b) => b.ballotGuid === summary.ballotGuid,
    );
    if (index !== -1) {
      ballots.value[index] = summary;
    } else {
      ballots.value.push(summary);
    }
  }

  function patchBallotSummaryByGuid(
    ballotGuid: string,
    patch: Partial<BallotSummaryDto>,
  ) {
    const index = ballots.value.findIndex((b) => b.ballotGuid === ballotGuid);
    if (index === -1) {
      return;
    }

    ballots.value[index] = patchBallotSummary(ballots.value[index], patch);
  }

  async function fetchBallots(electionGuid: string) {
    loading.value = true;
    error.value = null;
    try {
      const fetched = (await ballotService.getAll(electionGuid)) ?? [];
      ballots.value = fetched.map(toBallotSummary);
    } catch (e: any) {
      error.value = e.message || "Failed to fetch ballots";
      throw e;
    } finally {
      loading.value = false;
    }
  }

  function applyFetchedBallot(
    ballotGuid: string,
    ballot: BallotDto,
    mutationGenerationBeforeFetch: number,
    adoptAsCurrent = true,
  ) {
    let resolvedBallot = ballot;

    if (
      currentBallot.value?.ballotGuid === ballotGuid &&
      (ballotMutationGeneration.get(ballotGuid) ?? 0) >
        mutationGenerationBeforeFetch
    ) {
      resolvedBallot = {
        ...ballot,
        votes: currentBallot.value.votes,
        voteCount: currentBallot.value.voteCount,
      };
    }

    const isViewingBallot = currentBallot.value?.ballotGuid === ballotGuid;
    if (adoptAsCurrent || isViewingBallot) {
      currentBallot.value = resolvedBallot;
    }

    upsertBallotSummary(summaryFromFullBallot(resolvedBallot));
    return resolvedBallot;
  }

  async function fetchBallotById(
    ballotGuid: string,
    options?: { adoptAsCurrent?: boolean },
  ) {
    const fetchGeneration = (ballotFetchGeneration.get(ballotGuid) ?? 0) + 1;
    ballotFetchGeneration.set(ballotGuid, fetchGeneration);
    const mutationGenerationBeforeFetch =
      ballotMutationGeneration.get(ballotGuid) ?? 0;

    loading.value = true;
    error.value = null;
    try {
      const ballot = await ballotService.getById(ballotGuid);

      if (ballotFetchGeneration.get(ballotGuid) !== fetchGeneration) {
        return ballot;
      }

      return applyFetchedBallot(
        ballotGuid,
        ballot,
        mutationGenerationBeforeFetch,
        options?.adoptAsCurrent ?? true,
      );
    } catch (e: any) {
      error.value = e.message || "Failed to fetch ballot";
      throw e;
    } finally {
      if (ballotFetchGeneration.get(ballotGuid) === fetchGeneration) {
        loading.value = false;
      }
    }
  }

  async function createBallot(dto: CreateBallotDto) {
    loading.value = true;
    error.value = null;
    try {
      const ballot = await ballotService.create(dto);
      upsertBallotSummary(summaryFromFullBallot(ballot));
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
      upsertBallotSummary(summaryFromFullBallot(ballot));

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

  function resolveVoteMutationVotes(
    ballotGuid: string,
    result: VoteWithBallotStatusDto,
    options?: { deletedRowId?: number },
  ): VoteDto[] | null {
    const existingVotes =
      currentBallot.value?.ballotGuid === ballotGuid
        ? currentBallot.value.votes
        : [];

    if (result.vote) {
      let updatedVotes = existingVotes;
      if (result.votePositions?.length) {
        updatedVotes = applyVotePositions(updatedVotes, result.votePositions);
      }

      const existingIndex = updatedVotes.findIndex(
        (vote) => vote.rowId === result.vote!.rowId,
      );
      updatedVotes =
        existingIndex !== -1
          ? updatedVotes.map((vote, index) =>
              index === existingIndex ? result.vote! : vote,
            )
          : [...updatedVotes, result.vote!];

      return normalizeVoteList(
        updatedVotes.sort((a, b) => a.positionOnBallot - b.positionOnBallot),
      );
    }

    if (result.votePositions?.length) {
      let updatedVotes = existingVotes;
      if (options?.deletedRowId !== undefined) {
        updatedVotes = updatedVotes.filter(
          (vote) => vote.rowId !== options.deletedRowId,
        );
      }

      return normalizeVoteList(
        applyVotePositions(updatedVotes, result.votePositions),
      );
    }

    if (options?.deletedRowId !== undefined) {
      return normalizeVoteList(
        compactVotePositions(
          existingVotes.filter((vote) => vote.rowId !== options.deletedRowId),
        ),
      );
    }

    return null;
  }

  function applyVoteMutationResult(
    ballotGuid: string,
    result: VoteWithBallotStatusDto,
    options?: { deletedRowId?: number },
  ) {
    ballotMutationGeneration.set(
      ballotGuid,
      (ballotMutationGeneration.get(ballotGuid) ?? 0) + 1,
    );

    const isCurrentBallot = currentBallot.value?.ballotGuid === ballotGuid;
    const hasAuthoritativePositions =
      (result.votePositions?.length ?? 0) > 0 || !!result.vote;

    const normalizedVotes = resolveVoteMutationVotes(
      ballotGuid,
      result,
      options,
    );
    if (!normalizedVotes) {
      return;
    }

    const summaryPatch: Partial<BallotSummaryDto> = {};
    if (hasAuthoritativePositions || isCurrentBallot) {
      summaryPatch.voteCount = normalizedVotes.length;
    }
    if (result.ballotStatusCode) {
      summaryPatch.statusCode = String(result.ballotStatusCode);
    }
    if (Object.keys(summaryPatch).length > 0) {
      patchBallotSummaryByGuid(ballotGuid, summaryPatch);
    }

    if (!isCurrentBallot) {
      return;
    }

    const existingSummary =
      ballots.value.find((b) => b.ballotGuid === ballotGuid) ??
      summaryFromFullBallot(currentBallot.value);

    currentBallot.value = {
      ...existingSummary,
      votes: normalizedVotes,
      voteCount: normalizedVotes.length,
      ...(result.ballotStatusCode
        ? { statusCode: String(result.ballotStatusCode) }
        : {}),
    };
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
        deletedRowId: vote.rowId,
      });
    } catch (e: any) {
      error.value = e.message || "Failed to delete vote";
      throw e;
    } finally {
      loading.value = false;
    }
  }

  async function reorderVotes(dto: ReorderVotesDto) {
    loading.value = true;
    error.value = null;
    try {
      const result = await voteService.reorder(dto);
      applyVoteMutationResult(dto.ballotGuid, result);
      return result;
    } catch (e: any) {
      error.value = e.message || "Failed to reorder votes";
      throw e;
    } finally {
      loading.value = false;
    }
  }

  function setCurrentBallot(ballot: BallotDto | null) {
    currentBallot.value = ballot;
  }

  function clearCurrentBallot() {
    currentBallot.value = null;
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
        console.log("Server requested page reload for ballots");
      });

      signalrInitialized.value = true;
    } catch (e) {
      console.error("Failed to initialize SignalR for ballot store:", e);
    }
  }

  function handleBallotAdded(data: BallotUpdateEvent) {
    const exists = ballots.value.some((b) => b.ballotGuid === data.ballotGuid);
    if (!exists) {
      fetchBallotById(data.ballotGuid, { adoptAsCurrent: false }).catch(
        console.error,
      );
    }
  }

  function handleBallotUpdated(data: BallotUpdateEvent) {
    const patch: Partial<BallotSummaryDto> = {};
    if (data.statusCode !== undefined) {
      patch.statusCode = data.statusCode;
    }
    if (data.voteCount !== undefined) {
      patch.voteCount = data.voteCount;
    }
    patchBallotSummaryByGuid(data.ballotGuid, patch);

    if (currentBallot.value?.ballotGuid === data.ballotGuid) {
      fetchBallotById(data.ballotGuid).catch(console.error);
    }
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
    reorderVotes,
    setCurrentBallot,
    clearCurrentBallot,
    clearError,
    initializeSignalR,
    joinElection,
    leaveElection,
  };
});
