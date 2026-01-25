import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import { electionService } from '../services/electionService';
import { signalrService } from '../services/signalrService';
import type { ElectionDto, CreateElectionDto, UpdateElectionDto } from '../types';
import type { ElectionUpdateEvent } from '../types/SignalREvents';

export const useElectionStore = defineStore('election', () => {
  const elections = ref<ElectionDto[]>([]);
  const currentElection = ref<ElectionDto | null>(null);
  const loading = ref(false);
  const error = ref<string | null>(null);
  const signalrInitialized = ref(false);

  const activeElections = computed(() => 
    elections.value.filter(e => e.tallyStatus !== 'Finalized' && e.tallyStatus !== 'Archived')
  );

  const finalizedElections = computed(() => 
    elections.value.filter(e => e.tallyStatus === 'Finalized')
  );

  async function fetchElections() {
    loading.value = true;
    error.value = null;
    try {
      elections.value = await electionService.getAll();
    } catch (e: any) {
      error.value = e.message || 'Failed to fetch elections';
      throw e;
    } finally {
      loading.value = false;
    }
  }

  async function fetchElectionById(electionGuid: string) {
    loading.value = true;
    error.value = null;
    try {
      const election = await electionService.getById(electionGuid);
      currentElection.value = election;
      
      const index = elections.value.findIndex(e => e.electionGuid === electionGuid);
      if (index !== -1) {
        elections.value[index] = election;
      } else {
        elections.value.push(election);
      }
      
      return election;
    } catch (e: any) {
      error.value = e.message || 'Failed to fetch election';
      throw e;
    } finally {
      loading.value = false;
    }
  }

  async function createElection(dto: CreateElectionDto) {
    loading.value = true;
    error.value = null;
    try {
      const election = await electionService.create(dto);
      elections.value.push(election);
      currentElection.value = election;
      return election;
    } catch (e: any) {
      error.value = e.message || 'Failed to create election';
      throw e;
    } finally {
      loading.value = false;
    }
  }

  async function updateElection(electionGuid: string, dto: UpdateElectionDto) {
    loading.value = true;
    error.value = null;
    try {
      const election = await electionService.update(electionGuid, dto);
      
      const index = elections.value.findIndex(e => e.electionGuid === electionGuid);
      if (index !== -1) {
        elections.value[index] = election;
      }
      
      if (currentElection.value?.electionGuid === electionGuid) {
        currentElection.value = election;
      }
      
      return election;
    } catch (e: any) {
      error.value = e.message || 'Failed to update election';
      throw e;
    } finally {
      loading.value = false;
    }
  }

  async function deleteElection(electionGuid: string) {
    loading.value = true;
    error.value = null;
    try {
      await electionService.delete(electionGuid);
      
      elections.value = elections.value.filter(e => e.electionGuid !== electionGuid);
      
      if (currentElection.value?.electionGuid === electionGuid) {
        currentElection.value = null;
      }
    } catch (e: any) {
      error.value = e.message || 'Failed to delete election';
      throw e;
    } finally {
      loading.value = false;
    }
  }

  function setCurrentElection(election: ElectionDto | null) {
    currentElection.value = election;
  }

  function clearError() {
    error.value = null;
  }

  async function initializeSignalR() {
    if (signalrInitialized.value) return;

    try {
      const connection = await signalrService.connectToMainHub();
      
      connection.on('ElectionUpdated', (data: ElectionUpdateEvent) => {
        handleElectionUpdate(data);
      });

      connection.on('ElectionStatusChanged', (data: ElectionUpdateEvent) => {
        handleElectionUpdate(data);
      });

      signalrInitialized.value = true;
    } catch (e) {
      console.error('Failed to initialize SignalR for election store:', e);
    }
  }

  function handleElectionUpdate(data: ElectionUpdateEvent) {
    const index = elections.value.findIndex(e => e.electionGuid === data.electionGuid);
    if (index !== -1) {
      const existing = elections.value[index]!;
      elections.value[index] = {
        electionGuid: existing.electionGuid,
        name: data.name ?? existing.name,
        dateOfElection: existing.dateOfElection,
        electionType: existing.electionType,
        numberToElect: existing.numberToElect,
        tallyStatus: data.tallyStatus ?? existing.tallyStatus,
        convenor: existing.convenor,
        electionMode: existing.electionMode,
        numberExtra: existing.numberExtra,
        showFullReport: existing.showFullReport,
        listForPublic: existing.listForPublic,
        showAsTest: existing.showAsTest,
        onlineWhenOpen: existing.onlineWhenOpen,
        onlineWhenClose: existing.onlineWhenClose,
        voterCount: existing.voterCount,
        ballotCount: existing.ballotCount,
        locationCount: existing.locationCount,
      };
    }

    if (currentElection.value?.electionGuid === data.electionGuid) {
      const existing = currentElection.value!;
      currentElection.value = {
        electionGuid: existing.electionGuid,
        name: data.name ?? existing.name,
        dateOfElection: existing.dateOfElection,
        electionType: existing.electionType,
        numberToElect: existing.numberToElect,
        tallyStatus: data.tallyStatus ?? existing.tallyStatus,
        convenor: existing.convenor,
        electionMode: existing.electionMode,
        numberExtra: existing.numberExtra,
        showFullReport: existing.showFullReport,
        listForPublic: existing.listForPublic,
        showAsTest: existing.showAsTest,
        onlineWhenOpen: existing.onlineWhenOpen,
        onlineWhenClose: existing.onlineWhenClose,
        voterCount: existing.voterCount,
        ballotCount: existing.ballotCount,
        locationCount: existing.locationCount,
      };
    }
  }

  async function joinElection(electionGuid: string) {
    try {
      await signalrService.joinElection(electionGuid);
    } catch (e) {
      console.error('Failed to join election group:', e);
    }
  }

  async function leaveElection(electionGuid: string) {
    try {
      await signalrService.leaveElection(electionGuid);
    } catch (e) {
      console.error('Failed to leave election group:', e);
    }
  }

  return {
    elections,
    currentElection,
    loading,
    error,
    activeElections,
    finalizedElections,
    fetchElections,
    fetchElectionById,
    createElection,
    updateElection,
    deleteElection,
    setCurrentElection,
    clearError,
    initializeSignalR,
    joinElection,
    leaveElection
  };
});
