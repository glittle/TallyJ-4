import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import { electionService } from '../services/electionService';
import { signalrService } from '../services/signalrService';

import type { ElectionDto, CreateElectionDto, UpdateElectionDto } from '../types';
import type { ElectionUpdateEvent } from '../types/SignalREvents';
import { extractApiErrorMessage } from '../utils/errorHandler';

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
      error.value = extractApiErrorMessage(e);
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
      error.value = extractApiErrorMessage(e);
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
      error.value = extractApiErrorMessage(e);
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
      error.value = extractApiErrorMessage(e);
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
      error.value = extractApiErrorMessage(e);
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

      connection.on('statusChanged', (data: any) => {
        // MainHub sends statusChanged with infoForKnown and infoForGuest
        // For now, we'll handle the known user info
        if (data && typeof data === 'object') {
          const updateEvent: ElectionUpdateEvent = {
            electionGuid: data.electionGuid || '',
            name: data.name,
            tallyStatus: data.tallyStatus,
            electionStatus: data.electionStatus,
            updatedAt: data.updatedAt || new Date().toISOString()
          };
          handleElectionUpdate(updateEvent);
        }
      });

      connection.on('electionClosed', (electionGuid: string) => {
        // Handle election closed event
        const updateEvent: ElectionUpdateEvent = {
          electionGuid,
          electionStatus: 'Closed',
          updatedAt: new Date().toISOString()
        };
        handleElectionUpdate(updateEvent);
      });

      signalrInitialized.value = true;
    } catch (e) {
      console.error('Failed to initialize SignalR for election store:', e);
    }
  }

  function handleElectionUpdate(data: ElectionUpdateEvent) {
    const index = elections.value.findIndex(e => e.electionGuid === data.electionGuid);
    if (index !== -1) {
      const existingElection = elections.value[index]!;
      const oldTallyStatus = existingElection.tallyStatus;
      const oldElectionStatus = existingElection.electionStatus;

      elections.value[index] = {
        ...existingElection,
        name: data.name ?? existingElection.name,
        tallyStatus: data.tallyStatus ?? existingElection.tallyStatus,
        electionStatus: data.electionStatus ?? existingElection.electionStatus,
      } as ElectionDto;

      // Show notifications for status changes
      if (data.tallyStatus && data.tallyStatus !== oldTallyStatus) {
        showElectionStatusNotification(data.name || 'Election', 'tally', data.tallyStatus);
      }
      if (data.electionStatus && data.electionStatus !== oldElectionStatus) {
        showElectionStatusNotification(data.name || 'Election', 'election', data.electionStatus);
      }
    }

    if (currentElection.value?.electionGuid === data.electionGuid) {
      const existingCurrentElection = currentElection.value!;
      const oldTallyStatus = existingCurrentElection.tallyStatus;
      const oldElectionStatus = existingCurrentElection.electionStatus;

      currentElection.value = {
        ...existingCurrentElection,
        name: data.name ?? existingCurrentElection.name,
        tallyStatus: data.tallyStatus ?? existingCurrentElection.tallyStatus,
        electionStatus: data.electionStatus ?? existingCurrentElection.electionStatus,
      } as ElectionDto;

      // Show notifications for status changes
      if (data.tallyStatus && data.tallyStatus !== oldTallyStatus) {
        showElectionStatusNotification(data.name || 'Election', 'tally', data.tallyStatus);
      }
      if (data.electionStatus && data.electionStatus !== oldElectionStatus) {
        showElectionStatusNotification(data.name || 'Election', 'election', data.electionStatus);
      }
    }
  }

  function showElectionStatusNotification(electionName: string, statusType: 'tally' | 'election', newStatus: string) {
    const message = `${electionName} ${statusType} status changed to: ${newStatus}`;


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
