import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import { peopleService } from '../services/peopleService';
import type { PersonDto, CreatePersonDto, UpdatePersonDto } from '../types';

export const usePeopleStore = defineStore('people', () => {
  const people = ref<PersonDto[]>([]);
  const loading = ref(false);
  const error = ref<string | null>(null);

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

  return {
    people,
    loading,
    error,
    voters,
    candidates,
    fetchPeople,
    fetchPersonById,
    createPerson,
    updatePerson,
    deletePerson,
    searchPeople,
    clearError
  };
});
