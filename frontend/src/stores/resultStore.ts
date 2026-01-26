import { defineStore } from 'pinia';
import { ref } from 'vue';
import { resultService } from '../services/resultService';
import { signalrService } from '../services/signalrService';
import type {
  TallyResultDto,
  TallyStatisticsDto,
  ElectionReportDto,
  ReportDataResponseDto,
  TieDetailsDto,
  PresentationDto,
  MonitorInfoDto,
  DetailedStatisticsDto
} from '../types';
import type { TallyProgressEvent } from '../types/SignalREvents';

export const useResultStore = defineStore('result', () => {
  const results = ref<TallyResultDto | null>(null);
  const statistics = ref<TallyStatisticsDto | null>(null);
  const electionReport = ref<ElectionReportDto | null>(null);
  const reportData = ref<ReportDataResponseDto | null>(null);
  const tieDetails = ref<TieDetailsDto[]>([]);
  const presentationData = ref<PresentationDto | null>(null);
  const monitorInfo = ref<MonitorInfoDto | null>(null);
  const detailedStatistics = ref<DetailedStatisticsDto | null>(null);
  const loading = ref(false);
  const calculating = ref(false);
  const error = ref<string | null>(null);
  const signalrInitialized = ref(false);
  const tallyProgress = ref<TallyProgressEvent | null>(null);

  async function calculateTally(electionGuid: string, electionType: 'normal' | 'singlename' = 'normal') {
    calculating.value = true;
    loading.value = true;
    error.value = null;
    try {
      const result = await resultService.calculateTally(electionGuid, electionType);

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
    electionReport.value = null;
    reportData.value = null;
    tieDetails.value = [];
    presentationData.value = null;
    monitorInfo.value = null;
    detailedStatistics.value = null;
  }

  function clearError() {
    error.value = null;
  }

  // Monitoring
  async function fetchMonitorInfo(electionGuid: string) {
    loading.value = true;
    error.value = null;
    try {
      const info = await resultService.getMonitorInfo(electionGuid);
      monitorInfo.value = info;
      return info;
    } catch (e: any) {
      error.value = e.message || 'Failed to fetch monitor info';
      throw e;
    } finally {
      loading.value = false;
    }
  }

  // Reports
  async function fetchElectionReport(electionGuid: string) {
    loading.value = true;
    error.value = null;
    try {
      const report = await resultService.getElectionReport(electionGuid);
      electionReport.value = report;
      return report;
    } catch (e: any) {
      error.value = e.message || 'Failed to fetch election report';
      throw e;
    } finally {
      loading.value = false;
    }
  }

  async function fetchReportData(electionGuid: string, reportCode: string) {
    loading.value = true;
    error.value = null;
    try {
      const data = await resultService.getReportData(electionGuid, reportCode);
      reportData.value = data;
      return data;
    } catch (e: any) {
      error.value = e.message || 'Failed to fetch report data';
      throw e;
    } finally {
      loading.value = false;
    }
  }

  // Tie Management
  async function fetchTieDetails(electionGuid: string) {
    loading.value = true;
    error.value = null;
    try {
      const details = await resultService.getTieDetails(electionGuid);
      tieDetails.value = details;
      return details;
    } catch (e: any) {
      error.value = e.message || 'Failed to fetch tie details';
      throw e;
    } finally {
      loading.value = false;
    }
  }

  async function saveTieCounts(electionGuid: string, counts: { personGuid: string; tieBreakCount: number }[]) {
    loading.value = true;
    error.value = null;
    try {
      const request = { counts };
      const response = await resultService.saveTieCounts(electionGuid, request);
      return response;
    } catch (e: any) {
      error.value = e.message || 'Failed to save tie counts';
      throw e;
    } finally {
      loading.value = false;
    }
  }

  // Presentation
  async function fetchPresentationData(electionGuid: string) {
    loading.value = true;
    error.value = null;
    try {
      const data = await resultService.getPresentationData(electionGuid);
      presentationData.value = data;
      return data;
    } catch (e: any) {
      error.value = e.message || 'Failed to fetch presentation data';
      throw e;
    } finally {
      loading.value = false;
    }
  }

  async function fetchDetailedStatistics(electionGuid: string) {
    loading.value = true;
    error.value = null;
    try {
      const data = await resultService.getDetailedStatistics(electionGuid);
      detailedStatistics.value = data;
      return data;
    } catch (e: any) {
      error.value = e.message || 'Failed to fetch detailed statistics';
      throw e;
    } finally {
      loading.value = false;
    }
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
    // Reactive state
    results,
    statistics,
    electionReport,
    reportData,
    tieDetails,
    presentationData,
    monitorInfo,
    detailedStatistics,
    loading,
    calculating,
    error,
    tallyProgress,

    // Methods
    calculateTally,
    fetchResults,
    fetchStatistics,
    fetchMonitorInfo,
    fetchElectionReport,
    fetchReportData,
    fetchTieDetails,
    saveTieCounts,
    fetchPresentationData,
    fetchDetailedStatistics,
    clearResults,
    clearError,
    initializeSignalR,
    joinTallySession,
    leaveTallySession
  };
});
