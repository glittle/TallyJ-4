import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { setActivePinia, createPinia } from 'pinia';
import { usePeopleStore } from './peopleStore';
import type { PersonDto } from '../types';

vi.mock('../services/peopleService', () => ({
  peopleService: {
    getAll: vi.fn(),
    getById: vi.fn(),
    create: vi.fn(),
    update: vi.fn(),
    delete: vi.fn(),
    search: vi.fn(),
    getVoters: vi.fn(),
    getCandidates: vi.fn()
  }
}));

vi.mock('../services/signalrService', () => ({
  signalrService: {
    connectToFrontDeskHub: vi.fn(),
    joinElection: vi.fn(),
    leaveElection: vi.fn()
  }
}));

import { peopleService } from '../services/peopleService';

describe('People Store - Candidate Cache', () => {
  let store: ReturnType<typeof usePeopleStore>;

  const mockPerson: PersonDto = {
    personGuid: '123e4567-e89b-12d3-a456-426614174000',
    firstName: 'John',
    lastName: 'Smith',
    fullName: 'John Smith',
    canReceiveVotes: true,
    canVote: true,
    voteCount: 0,
    combinedSoundCodes: 'J500,S530'
  };

  const mockPerson2: PersonDto = {
    personGuid: '123e4567-e89b-12d3-a456-426614174001',
    firstName: 'Jane',
    lastName: 'Doe',
    fullName: 'Jane Doe',
    canReceiveVotes: true,
    canVote: true,
    voteCount: 0,
    combinedSoundCodes: 'J500,D000',
    otherNames: 'Janet',
    otherLastNames: 'Dough'
  };

  beforeEach(() => {
    setActivePinia(createPinia());
    store = usePeopleStore();
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.clearAllMocks();
  });

  describe('enrichPersonForSearch', () => {
    it('should add _searchText and _soundexCodes to person', () => {
      const enriched = store.enrichPersonForSearch(mockPerson);

      expect(enriched._searchText).toBe('john smith');
      expect(enriched._soundexCodes).toEqual(['J500', 'S530']);
      expect(enriched.personGuid).toBe(mockPerson.personGuid);
      expect(enriched.firstName).toBe(mockPerson.firstName);
    });

    it('should include otherNames and otherLastNames in _searchText', () => {
      const enriched = store.enrichPersonForSearch(mockPerson2);

      expect(enriched._searchText).toBe('jane doe janet dough');
      expect(enriched._soundexCodes).toEqual(['J500', 'D000']);
    });

    it('should handle missing combinedSoundCodes', () => {
      const personWithoutCodes = { ...mockPerson, combinedSoundCodes: undefined };
      const enriched = store.enrichPersonForSearch(personWithoutCodes);

      expect(enriched._soundexCodes).toEqual([]);
    });

    it('should handle empty combinedSoundCodes', () => {
      const personWithEmptyCodes = { ...mockPerson, combinedSoundCodes: '' };
      const enriched = store.enrichPersonForSearch(personWithEmptyCodes);

      expect(enriched._soundexCodes).toEqual([]);
    });

    it('should trim and filter empty strings from search text', () => {
      const personWithMissingFields = {
        ...mockPerson,
        firstName: undefined,
        otherNames: undefined,
        otherLastNames: undefined
      };
      const enriched = store.enrichPersonForSearch(personWithMissingFields);

      expect(enriched._searchText).toBe('smith');
    });
  });

  describe('initializeCandidateCache', () => {
    it('should fetch candidates and enrich them', async () => {
      vi.mocked(peopleService.getCandidates).mockResolvedValue([mockPerson, mockPerson2]);

      await store.initializeCandidateCache('election-123');

      expect(peopleService.getCandidates).toHaveBeenCalledWith('election-123');
      expect(store.candidateCache).toHaveLength(2);
      expect(store.candidateCache[0]._searchText).toBe('john smith');
      expect(store.candidateCache[1]._searchText).toBe('jane doe janet dough');
      expect(store.isCacheInitialized).toBe(true);
    });

    it('should not fetch candidates if already initialized', async () => {
      vi.mocked(peopleService.getCandidates).mockResolvedValue([mockPerson]);

      await store.initializeCandidateCache('election-123');
      await store.initializeCandidateCache('election-123');

      expect(peopleService.getCandidates).toHaveBeenCalledTimes(1);
    });

    it('should throw error if fetch fails', async () => {
      const error = new Error('Network error');
      vi.mocked(peopleService.getCandidates).mockRejectedValue(error);

      await expect(store.initializeCandidateCache('election-123')).rejects.toThrow('Network error');
      expect(store.isCacheInitialized).toBe(false);
    });
  });

  describe('SignalR handlePersonAdded', () => {
    it('should add eligible person to cache', async () => {
      store.isCacheInitialized = true;
      store.candidateCache = [];

      vi.mocked(peopleService.getById).mockResolvedValue(mockPerson);

      await store.handlePersonAdded({
        electionGuid: 'election-123',
        personGuid: mockPerson.personGuid,
        action: 'added',
        firstName: mockPerson.firstName,
        lastName: mockPerson.lastName,
        updatedAt: new Date().toISOString()
      });

      expect(store.candidateCache).toHaveLength(1);
      expect(store.candidateCache[0].personGuid).toBe(mockPerson.personGuid);
      expect(store.candidateCache[0]._searchText).toBe('john smith');
    });

    it('should not add ineligible person to cache', async () => {
      store.isCacheInitialized = true;
      store.candidateCache = [];

      const ineligiblePerson = { ...mockPerson, canReceiveVotes: false };
      vi.mocked(peopleService.getById).mockResolvedValue(ineligiblePerson);

      await store.handlePersonAdded({
        electionGuid: 'election-123',
        personGuid: mockPerson.personGuid,
        action: 'added',
        firstName: mockPerson.firstName,
        lastName: mockPerson.lastName,
        updatedAt: new Date().toISOString()
      });

      expect(store.candidateCache).toHaveLength(0);
    });

    it('should not add to cache if not initialized', async () => {
      store.isCacheInitialized = false;
      store.candidateCache = [];

      vi.mocked(peopleService.getById).mockResolvedValue(mockPerson);

      await store.handlePersonAdded({
        electionGuid: 'election-123',
        personGuid: mockPerson.personGuid,
        action: 'added',
        firstName: mockPerson.firstName,
        lastName: mockPerson.lastName,
        updatedAt: new Date().toISOString()
      });

      expect(store.candidateCache).toHaveLength(0);
    });
  });

  describe('SignalR handlePersonUpdated', () => {
    it('should update existing person in cache', async () => {
      const initialEnriched = store.enrichPersonForSearch(mockPerson);
      store.isCacheInitialized = true;
      store.candidateCache = [initialEnriched];

      const updatedPerson = { ...mockPerson, firstName: 'Johnny' };
      vi.mocked(peopleService.getById).mockResolvedValue(updatedPerson);

      await store.handlePersonUpdated({
        electionGuid: 'election-123',
        personGuid: mockPerson.personGuid,
        action: 'updated',
        firstName: 'Johnny',
        lastName: mockPerson.lastName,
        updatedAt: new Date().toISOString()
      });

      expect(store.candidateCache).toHaveLength(1);
      expect(store.candidateCache[0].firstName).toBe('Johnny');
      expect(store.candidateCache[0]._searchText).toBe('johnny smith');
    });

    it('should add person to cache if they become eligible', async () => {
      store.isCacheInitialized = true;
      store.candidateCache = [];

      const nowEligiblePerson = { ...mockPerson, canReceiveVotes: true };
      vi.mocked(peopleService.getById).mockResolvedValue(nowEligiblePerson);

      await store.handlePersonUpdated({
        electionGuid: 'election-123',
        personGuid: mockPerson.personGuid,
        action: 'updated',
        firstName: mockPerson.firstName,
        lastName: mockPerson.lastName,
        updatedAt: new Date().toISOString()
      });

      expect(store.candidateCache).toHaveLength(1);
      expect(store.candidateCache[0].personGuid).toBe(mockPerson.personGuid);
    });

    it('should remove person from cache if they become ineligible', async () => {
      const initialEnriched = store.enrichPersonForSearch(mockPerson);
      store.isCacheInitialized = true;
      store.candidateCache = [initialEnriched];

      const ineligiblePerson = { ...mockPerson, canReceiveVotes: false };
      vi.mocked(peopleService.getById).mockResolvedValue(ineligiblePerson);

      await store.handlePersonUpdated({
        electionGuid: 'election-123',
        personGuid: mockPerson.personGuid,
        action: 'updated',
        firstName: mockPerson.firstName,
        lastName: mockPerson.lastName,
        updatedAt: new Date().toISOString()
      });

      expect(store.candidateCache).toHaveLength(0);
    });
  });

  describe('SignalR handlePersonDeleted', () => {
    it('should remove person from cache', () => {
      const enriched = store.enrichPersonForSearch(mockPerson);
      store.isCacheInitialized = true;
      store.candidateCache = [enriched];

      store.handlePersonDeleted({
        electionGuid: 'election-123',
        personGuid: mockPerson.personGuid,
        action: 'deleted',
        firstName: mockPerson.firstName,
        lastName: mockPerson.lastName,
        updatedAt: new Date().toISOString()
      });

      expect(store.candidateCache).toHaveLength(0);
    });

    it('should not fail if cache is not initialized', () => {
      store.isCacheInitialized = false;
      store.candidateCache = [];

      expect(() => {
        store.handlePersonDeleted({
          electionGuid: 'election-123',
          personGuid: mockPerson.personGuid,
          action: 'deleted',
          firstName: mockPerson.firstName,
          lastName: mockPerson.lastName,
          updatedAt: new Date().toISOString()
        });
      }).not.toThrow();
    });
  });
});
