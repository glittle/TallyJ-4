import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import { peopleService } from '../services/peopleService';
import { signalrService } from '../services/signalrService';
import type { PersonDto, CreatePersonDto, UpdatePersonDto, SearchablePersonDto } from '../types';
import type { PersonUpdateEvent } from '../types/SignalREvents';

export const usePeopleStore = defineStore('people', () => {
  const people = ref<PersonDto[]>([]);
  const loading = ref(false);
  const error = ref<string | null>(null);
  const signalrInitialized = ref(false);
  const candidateCache = ref<SearchablePersonDto[]>([]);
  const isCacheInitialized = ref(false);

  const voters = computed(() => 
    people.value.filter(p => p.canVote === true)
  );

  const candidates = computed(() => 
    people.value.filter(p => p.canReceiveVotes === true)
  );

  async function fetchPeople(electionGuid: string) {
    loading.value = true;
    error.value = null;
    try {
      people.value = await peopleService.getAll(electionGuid);
    } catch (e: any) {
      error.value = e.message || 'Failed to fetch people';
      throw e;
    } finally {
      loading.value = false;
    }
  }

  async function fetchPersonById(personGuid: string) {
    loading.value = true;
    error.value = null;
    try {
      const person = await peopleService.getById(personGuid);
      
      const index = people.value.findIndex(p => p.personGuid === personGuid);
      if (index !== -1) {
        people.value[index] = person;
      } else {
        people.value.push(person);
      }
      
      return person;
    } catch (e: any) {
      error.value = e.message || 'Failed to fetch person';
      throw e;
    } finally {
      loading.value = false;
    }
  }

  async function createPerson(dto: CreatePersonDto) {
    loading.value = true;
    error.value = null;
    try {
      const person = await peopleService.create(dto);
      people.value.push(person);
      return person;
    } catch (e: any) {
      error.value = e.message || 'Failed to create person';
      throw e;
    } finally {
      loading.value = false;
    }
  }

  async function updatePerson(personGuid: string, dto: UpdatePersonDto) {
    loading.value = true;
    error.value = null;
    try {
      const person = await peopleService.update(personGuid, dto);
      
      const index = people.value.findIndex(p => p.personGuid === personGuid);
      if (index !== -1) {
        people.value[index] = person;
      }
      
      return person;
    } catch (e: any) {
      error.value = e.message || 'Failed to update person';
      throw e;
    } finally {
      loading.value = false;
    }
  }

  async function deletePerson(personGuid: string) {
    loading.value = true;
    error.value = null;
    try {
      await peopleService.delete(personGuid);
      people.value = people.value.filter(p => p.personGuid !== personGuid);
    } catch (e: any) {
      error.value = e.message || 'Failed to delete person';
      throw e;
    } finally {
      loading.value = false;
    }
  }

  async function searchPeople(electionGuid: string, query: string) {
    loading.value = true;
    error.value = null;
    try {
      return await peopleService.search(electionGuid, query);
    } catch (e: any) {
      error.value = e.message || 'Failed to search people';
      throw e;
    } finally {
      loading.value = false;
    }
  }

  function clearError() {
    error.value = null;
  }

  function enrichPersonForSearch(person: PersonDto): SearchablePersonDto {
    const searchText = [
      person.firstName || '',
      person.lastName || '',
      person.otherNames || '',
      person.otherLastNames || ''
    ]
      .filter(Boolean)
      .join(' ')
      .toLowerCase()
      .trim();

    const soundexCodes = person.combinedSoundCodes
      ? person.combinedSoundCodes.split(',').map(code => code.trim()).filter(Boolean)
      : [];

    return {
      ...person,
      _searchText: searchText,
      _soundexCodes: soundexCodes
    };
  }

  async function initializeCandidateCache(electionGuid: string) {
    if (isCacheInitialized.value) return;

    try {
      const candidates = await peopleService.getCandidates(electionGuid);
      candidateCache.value = candidates.map(enrichPersonForSearch);
      isCacheInitialized.value = true;
    } catch (e) {
      console.error('Failed to initialize candidate cache:', e);
      throw e;
    }
  }

  async function initializeSignalR() {
    if (signalrInitialized.value) return;

    try {
      const connection = await signalrService.connectToFrontDeskHub();

      connection.on('updatePeople', (data: any) => {
        // FrontDeskHub sends updatePeople with person update information
        // The data structure should contain action, personGuid, and other person details
        if (data && typeof data === 'object') {
          const updateEvent: PersonUpdateEvent = {
            electionGuid: data.electionGuid || '',
            personGuid: data.personGuid || '',
            action: data.action || 'updated',
            firstName: data.firstName,
            lastName: data.lastName,
            updatedAt: data.updatedAt || new Date().toISOString()
          };

          switch (updateEvent.action) {
            case 'added':
              handlePersonAdded(updateEvent);
              break;
            case 'updated':
              handlePersonUpdated(updateEvent);
              break;
            case 'deleted':
              handlePersonDeleted(updateEvent);
              break;
          }
        }
      });

      connection.on('reloadPage', () => {
        // Handle page reload command from server
        console.log('Server requested page reload');
        // Could trigger a page refresh or data reload
        window.location.reload();
      });

      signalrInitialized.value = true;
    } catch (e) {
      console.error('Failed to initialize SignalR for people store:', e);
    }
  }

  async function handlePersonAdded(data: PersonUpdateEvent) {
    const exists = people.value.some(p => p.personGuid === data.personGuid);
    if (!exists) {
      try {
        const person = await fetchPersonById(data.personGuid);
        if (isCacheInitialized.value && person?.canReceiveVotes) {
          const searchablePerson = enrichPersonForSearch(person);
          candidateCache.value.push(searchablePerson);
        }
      } catch (e) {
        console.error('Failed to handle person added:', e);
      }
    }
  }

  async function handlePersonUpdated(data: PersonUpdateEvent) {
    try {
      const person = await fetchPersonById(data.personGuid);
      
      if (isCacheInitialized.value) {
        const index = candidateCache.value.findIndex(p => p.personGuid === data.personGuid);
        
        if (person?.canReceiveVotes) {
          const searchablePerson = enrichPersonForSearch(person);
          if (index !== -1) {
            candidateCache.value[index] = searchablePerson;
          } else {
            candidateCache.value.push(searchablePerson);
          }
        } else if (index !== -1) {
          candidateCache.value.splice(index, 1);
        }
      }
    } catch (e) {
      console.error('Failed to handle person updated:', e);
    }
  }

  function handlePersonDeleted(data: PersonUpdateEvent) {
    people.value = people.value.filter(p => p.personGuid !== data.personGuid);
    
    if (isCacheInitialized.value) {
      candidateCache.value = candidateCache.value.filter(p => p.personGuid !== data.personGuid);
    }
  }

  async function joinElection(electionGuid: string) {
    try {
      await signalrService.joinElection(electionGuid);
    } catch (e) {
      console.error('Failed to join election group for people updates:', e);
    }
  }

  async function leaveElection(electionGuid: string) {
    try {
      await signalrService.leaveElection(electionGuid);
    } catch (e) {
      console.error('Failed to leave election group for people updates:', e);
    }
  }

  return {
    people,
    loading,
    error,
    voters,
    candidates,
    candidateCache,
    isCacheInitialized,
    fetchPeople,
    fetchPersonById,
    createPerson,
    updatePerson,
    deletePerson,
    searchPeople,
    clearError,
    enrichPersonForSearch,
    initializeCandidateCache,
    initializeSignalR,
    joinElection,
    leaveElection,
    handlePersonAdded,
    handlePersonUpdated,
    handlePersonDeleted
  };
});
