import { defineStore } from "pinia";
import { ref } from "vue";
import { electionService } from "../services/electionService";
import type { ElectionStats } from "../types";
import { extractApiErrorMessage } from "../utils/errorHandler";

export const useElectionStatsStore = defineStore("electionStats", () => {
  const statsByElection = ref<Record<string, ElectionStats>>({});
  const loading = ref(false);
  const error = ref<string | null>(null);

  function getCached(electionGuid: string): ElectionStats | undefined {
    return statsByElection.value[electionGuid];
  }

  function invalidate(electionGuid: string) {
    const { [electionGuid]: _removed, ...rest } = statsByElection.value;
    statsByElection.value = rest;
  }

  async function fetchStats(
    electionGuid: string,
    { force = false }: { force?: boolean } = {},
  ): Promise<ElectionStats> {
    if (!force) {
      const cached = getCached(electionGuid);
      if (cached) {
        return cached;
      }
    }

    pendingRequests.value += 1;
    loading.value = true;
    error.value = null;
    try {
      const stats = await electionService.getStats(electionGuid);
      statsByElection.value = {
        ...statsByElection.value,
        [electionGuid]: stats,
      };
      return stats;
    } catch (e: unknown) {
      error.value = extractApiErrorMessage(e);
      throw e;
    } finally {
      pendingRequests.value = Math.max(0, pendingRequests.value - 1);
      loading.value = pendingRequests.value > 0;
    }
  }

  return {
    statsByElection,
    loading,
    error,
    getCached,
    invalidate,
    fetchStats,
  };
});