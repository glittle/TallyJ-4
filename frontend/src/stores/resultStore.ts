import { defineStore } from 'pinia';
import { ref } from 'vue';
import { resultService } from '../services/resultService';
import { signalrService } from '../services/signalrService';
import type { TallyResultDto, TallyStatisticsDto } from '../types';
import type { TallyProgressEvent } from '../types/SignalREvents';

export const useResultStore = defineStore('result', () => {
  const results = ref<TallyResultDto | null>(null);
  const statistics = ref<TallyStatisticsDto | null>(null);
  const loading = ref(false);
  const calculating = ref(false);
  const error = ref<string | null>(null);
  const signalrInitialized = ref(false);
  const tallyProgress = ref<TallyProgressEvent | null>(null);

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

  async function initializeSignalR() {
    if (signalrInitialized.value) return;

    try {
      const connection = await signalrService.connectToAnalyzeHub();
      
      connection.on('TallyProgress', (data: TallyProgressEvent) => {
        handleTallyProgress(data);
      });

      connection.on('TallyComplete', (data: TallyProgressEvent) => {
        handleTallyComplete(data);
      });

      signalrInitialized.value = true;
    } catch (e) {
      console.error('Failed to initialize SignalR for result store:', e);
    }
  }

  function handleTallyProgress(data: TallyProgressEvent) {
    tallyProgress.value = data;
  }

  function handleTallyComplete(data: TallyProgressEvent) {
    tallyProgress.value = data;
    calculating.value = false;
    if (data.electionGuid) {
      fetchResults(data.electionGuid).catch(console.error);
    }
  }

  async function joinTallySession(electionGuid: string) {
    try {
      await signalrService.joinTallySession(electionGuid);
    } catch (e) {
      console.error('Failed to join tally session:', e);
    }
  }

  async function leaveTallySession(electionGuid: string) {
    try {
      await signalrService.leaveTallySession(electionGuid);
    } catch (e) {
      console.error('Failed to leave tally session:', e);
    }
  }

  return {
    results,
    statistics,
    loading,
    calculating,
    error,
    tallyProgress,
    calculateTally,
    fetchResults,
    fetchStatistics,
    clearResults,
    clearError,
    initializeSignalR,
    joinTallySession,
    leaveTallySession
  };
});
