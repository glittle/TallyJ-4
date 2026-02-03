import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import { frontDeskService } from '../services/frontDeskService';
import { signalrService } from '../services/signalrService';
import type { FrontDeskVoterDto, CheckInVoterDto, FrontDeskStatsDto } from '../types/FrontDesk';

export const useFrontDeskStore = defineStore('frontDesk', () => {
  const voters = ref<FrontDeskVoterDto[]>([]);
  const stats = ref<FrontDeskStatsDto | null>(null);
  const loading = ref(false);
  const error = ref<string | null>(null);
  const signalrInitialized = ref(false);
  const searchQuery = ref('');

  const filteredVoters = computed(() => {
    if (!searchQuery.value.trim()) {
      return voters.value;
    }
    
    const query = searchQuery.value.toLowerCase();
    return voters.value.filter(voter => 
      voter.fullName.toLowerCase().includes(query) ||
      voter.bahaiId?.toLowerCase().includes(query) ||
      voter.area?.toLowerCase().includes(query)
    );
  });

  const checkedInVoters = computed(() => 
    filteredVoters.value.filter(v => v.isCheckedIn)
  );

  const notCheckedInVoters = computed(() => 
    filteredVoters.value.filter(v => !v.isCheckedIn)
  );

  async function fetchEligibleVoters(electionGuid: string) {
    loading.value = true;
    error.value = null;
    try {
      voters.value = await frontDeskService.getEligibleVoters(electionGuid);
      await fetchStats(electionGuid);
    } catch (e: any) {
      error.value = e.message || 'Failed to fetch eligible voters';
      throw e;
    } finally {
      loading.value = false;
    }
  }

  async function checkInVoter(electionGuid: string, checkInDto: CheckInVoterDto) {
    loading.value = true;
    error.value = null;
    try {
      const updatedVoter = await frontDeskService.checkInVoter(electionGuid, checkInDto);
      
      const index = voters.value.findIndex(v => v.personGuid === updatedVoter.personGuid);
      if (index !== -1) {
        voters.value[index] = updatedVoter;
      }
      
      return updatedVoter;
    } catch (e: any) {
      error.value = e.message || 'Failed to check in voter';
      throw e;
    } finally {
      loading.value = false;
    }
  }

  async function fetchRollCall(electionGuid: string) {
    loading.value = true;
    error.value = null;
    try {
      const rollCall = await frontDeskService.getRollCall(electionGuid);
      voters.value = rollCall.voters;
      stats.value = rollCall.stats;
      return rollCall;
    } catch (e: any) {
      error.value = e.message || 'Failed to fetch roll call';
      throw e;
    } finally {
      loading.value = false;
    }
  }

  async function fetchStats(electionGuid: string) {
    try {
      stats.value = await frontDeskService.getStats(electionGuid);
    } catch (e: any) {
      console.error('Failed to fetch stats:', e);
    }
  }

  function clearError() {
    error.value = null;
  }

  function setSearchQuery(query: string) {
    searchQuery.value = query;
  }

  async function initializeSignalR() {
    if (signalrInitialized.value) return;

    try {
      const connection = await signalrService.connectToFrontDeskHub();

      connection.on('PersonCheckedIn', (voter: FrontDeskVoterDto) => {
        const index = voters.value.findIndex(v => v.personGuid === voter.personGuid);
        if (index !== -1) {
          voters.value[index] = voter;
        }
      });

      connection.on('VoterCountUpdated', (updatedStats: FrontDeskStatsDto) => {
        stats.value = updatedStats;
      });

      signalrInitialized.value = true;
    } catch (e) {
      console.error('Failed to initialize SignalR for front desk store:', e);
    }
  }

  async function joinElection(electionGuid: string) {
    try {
      await signalrService.joinFrontDeskElection(electionGuid);
    } catch (e) {
      console.error('Failed to join election group for front desk updates:', e);
    }
  }

  async function leaveElection(electionGuid: string) {
    try {
      await signalrService.leaveFrontDeskElection(electionGuid);
    } catch (e) {
      console.error('Failed to leave election group for front desk updates:', e);
    }
  }

  return {
    voters,
    stats,
    loading,
    error,
    searchQuery,
    filteredVoters,
    checkedInVoters,
    notCheckedInVoters,
    fetchEligibleVoters,
    checkInVoter,
    fetchRollCall,
    fetchStats,
    clearError,
    setSearchQuery,
    initializeSignalR,
    joinElection,
    leaveElection
  };
});
