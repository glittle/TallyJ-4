import { defineStore } from 'pinia';
import { ref } from 'vue';
import { resultService } from '../services/resultService';
import type { TallyResultDto, TallyStatisticsDto } from '../types';

export const useResultStore = defineStore('result', () => {
  const results = ref<TallyResultDto | null>(null);
  const statistics = ref<TallyStatisticsDto | null>(null);
  const loading = ref(false);
  const calculating = ref(false);
  const error = ref<string | null>(null);

  async function calculateTally(electionGuid: string, electionType: 'Normal' | 'SingleName') {
    calculating.value = true;
    loading.value = true;
    error.value = null;
    try {
      let result: TallyResultDto;
      if (electionType === 'Normal') {
        result = await resultService.calculateNormalElection(electionGuid);
      } else {
        result = await resultService.calculateSingleNameElection(electionGuid);
      }
      
      results.value = result;
      statistics.value = result.statistics;
      return result;
    } catch (e: any) {
      error.value = e.message || 'Failed to calculate tally';
      throw e;
    } finally {
      calculating.value = false;
      loading.value = false;
    }
  }

  async function fetchResults(electionGuid: string) {
    loading.value = true;
    error.value = null;
    try {
      const result = await resultService.getResults(electionGuid);
      results.value = result;
      statistics.value = result.statistics;
      return result;
    } catch (e: any) {
      error.value = e.message || 'Failed to fetch results';
      throw e;
    } finally {
      loading.value = false;
    }
  }

  async function fetchStatistics(electionGuid: string) {
    loading.value = true;
    error.value = null;
    try {
      const stats = await resultService.getStatistics(electionGuid);
      statistics.value = stats;
      return stats;
    } catch (e: any) {
      error.value = e.message || 'Failed to fetch statistics';
      throw e;
    } finally {
      loading.value = false;
    }
  }

  function clearResults() {
    results.value = null;
    statistics.value = null;
  }

  function clearError() {
    error.value = null;
  }

  return {
    results,
    statistics,
    loading,
    calculating,
    error,
    calculateTally,
    fetchResults,
    fetchStatistics,
    clearResults,
    clearError
  };
});
