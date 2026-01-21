import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import { electionService } from '../services/electionService';
import type { ElectionDto, CreateElectionDto, UpdateElectionDto } from '../types';

export const useElectionStore = defineStore('election', () => {
  const elections = ref<ElectionDto[]>([]);
  const currentElection = ref<ElectionDto | null>(null);
  const loading = ref(false);
  const error = ref<string | null>(null);

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
    clearError
  };
});
