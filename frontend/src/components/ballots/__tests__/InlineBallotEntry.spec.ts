import { describe, it, expect, beforeEach, vi } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { nextTick } from 'vue';
import InlineBallotEntry from '../InlineBallotEntry.vue';
import VoteEntryRow from '../VoteEntryRow.vue';
import type { BallotDto } from '@/types/Ballot';
import type { VoteDto } from '@/types/Vote';
import type { SearchablePersonDto } from '@/types/Person';
import { ElMessage, ElButton, ElSkeleton, ElAlert, ElAutocomplete, ElIcon } from 'element-plus';

const mockT = (key: string, values?: any) => {
  const translations: Record<string, string> = {
    'ballots.position': 'Position',
    'ballots.candidate': 'Candidate',
    'ballots.duplicateWarning': 'Duplicate: This person is already selected',
    'ballots.clearAll': 'Clear All',
    'ballots.saveBallot': 'Save Ballot',
    'ballots.votesEntered': '{count} of {max} votes entered',
    'ballots.cacheLoading': 'Loading candidates...',
    'ballots.cacheLoadError': 'Failed to load candidates',
    'common.clear': 'Clear'
  };
  
  let result = translations[key] || key;
  
  if (values) {
    Object.keys(values).forEach(k => {
      result = result.replace(`{${k}}`, String(values[k]));
    });
  }
  
  return result;
};

vi.mock('vue-i18n', () => ({
  useI18n: () => ({
    t: mockT
  })
}));

vi.mock('element-plus', async () => {
  const actual = await vi.importActual('element-plus');
  return {
    ...actual,
    ElMessage: {
      warning: vi.fn(),
      error: vi.fn(),
      success: vi.fn(),
      info: vi.fn()
    }
  };
});

const mockPeopleStore = {
  candidateCache: [] as SearchablePersonDto[],
  isCacheInitialized: false,
  initializeCandidateCache: vi.fn()
};

vi.mock('@/stores/peopleStore', () => ({
  usePeopleStore: () => mockPeopleStore
}));

vi.mock('@/composables/usePersonSearch', () => ({
  usePersonSearch: (searchQuery: any, candidates: any) => {
    return {
      searchResults: {
        get value() {
          const query = searchQuery.value?.toLowerCase() || '';
          if (!query) return [];
          
          return candidates.value.filter((c: SearchablePersonDto) => 
            c.fullName.toLowerCase().includes(query)
          );
        }
      }
    };
  }
}));

function createMockPerson(
  firstName: string,
  lastName: string,
  personGuid: string = `guid-${firstName}-${lastName}`
): SearchablePersonDto {
  const fullName = `${firstName} ${lastName}`;
  return {
    personGuid,
    firstName,
    lastName,
    fullName,
    _searchText: fullName.toLowerCase(),
    _soundexCodes: [],
    voteCount: 0
  };
}

function createMockBallot(votes: VoteDto[] = []): BallotDto {
  return {
    ballotGuid: 'ballot-123',
    ballotCode: 'B001',
    locationGuid: 'location-1',
    locationName: 'Main Hall',
    ballotNumAtComputer: 1,
    computerCode: 'C01',
    statusCode: 'Review',
    voteCount: votes.length,
    votes
  };
}

const VoteEntryRowStub = {
  name: 'VoteEntryRow',
  template: '<div class="vote-entry-row-stub"></div>',
  props: ['positionOnBallot', 'modelValue', 'candidates', 'duplicatePersonGuids'],
  emits: ['vote-selected', 'vote-cleared', 'update:modelValue'],
  methods: {
    focusInput() {}
  }
};

const defaultMountOptions = {
  global: {
    components: {
      VoteEntryRow,
      ElButton,
      ElSkeleton,
      ElAlert,
      ElAutocomplete,
      ElIcon
    },
    mocks: {
      $t: mockT
    },
    stubs: {
      VoteEntryRow: VoteEntryRowStub
    }
  }
};

const unstubMountOptions = {
  global: {
    components: {
      VoteEntryRow,
      ElButton,
      ElSkeleton,
      ElAlert,
      ElAutocomplete,
      ElIcon
    },
    mocks: {
      $t: mockT
    }
  }
};

describe('InlineBallotEntry', () => {
  let mockCandidates: SearchablePersonDto[];

  beforeEach(() => {
    vi.clearAllMocks();
    
    mockCandidates = [
      createMockPerson('John', 'Doe'),
      createMockPerson('Jane', 'Smith'),
      createMockPerson('Bob', 'Johnson'),
      createMockPerson('Alice', 'Williams'),
      createMockPerson('Charlie', 'Brown')
    ];

    mockPeopleStore.candidateCache = mockCandidates;
    mockPeopleStore.isCacheInitialized = true;
    mockPeopleStore.initializeCandidateCache.mockResolvedValue(undefined);
  });

  describe('initialization', () => {
    it('should initialize candidate cache on mount', async () => {
      const ballot = createMockBallot();
      
      mount(InlineBallotEntry, {
        props: {
          electionGuid: 'election-123',
          ballot,
          maxVotes: 9
        },
        ...defaultMountOptions
      });

      await flushPromises();

      expect(mockPeopleStore.initializeCandidateCache).toHaveBeenCalledWith('election-123');
    });

    it('should show loading skeleton during cache initialization', async () => {
      const ballot = createMockBallot();
      
      let resolveInit: () => void;
      const initPromise = new Promise<void>(resolve => {
        resolveInit = resolve;
      });
      
      mockPeopleStore.initializeCandidateCache.mockReturnValue(initPromise);

      const wrapper = mount(InlineBallotEntry, {
        props: {
          electionGuid: 'election-123',
          ballot,
          maxVotes: 9
        },
        ...defaultMountOptions
      });

      await nextTick();

      expect(wrapper.find('.inline-ballot-entry__loading').exists()).toBe(true);
      expect(wrapper.findComponent(ElSkeleton).exists()).toBe(true);
      
      resolveInit!();
      await flushPromises();
    });

    it('should hide loading skeleton after cache loads', async () => {
      const ballot = createMockBallot();

      const wrapper = mount(InlineBallotEntry, {
        props: {
          electionGuid: 'election-123',
          ballot,
          maxVotes: 9
        },
        ...defaultMountOptions
      });

      await flushPromises();

      expect(wrapper.find('.inline-ballot-entry__loading').exists()).toBe(false);
      expect(wrapper.find('.inline-ballot-entry__content').exists()).toBe(true);
    });

    it('should show error message if cache initialization fails', async () => {
      const ballot = createMockBallot();
      
      mockPeopleStore.initializeCandidateCache.mockRejectedValue(
        new Error('Network error')
      );

      const wrapper = mount(InlineBallotEntry, {
        props: {
          electionGuid: 'election-123',
          ballot,
          maxVotes: 9
        },
        ...defaultMountOptions
      });

      await flushPromises();

      expect(wrapper.find('.inline-ballot-entry__error').exists()).toBe(true);
      expect(wrapper.findComponent(ElAlert).exists()).toBe(true);
      expect(ElMessage.error).toHaveBeenCalled();
    });
  });

  describe('rendering', () => {
    it('should render correct number of vote rows based on maxVotes', async () => {
      const ballot = createMockBallot();

      const wrapper = mount(InlineBallotEntry, {
        props: {
          electionGuid: 'election-123',
          ballot,
          maxVotes: 9
        },
        ...defaultMountOptions
      });

      await flushPromises();

      const rows = wrapper.findAllComponents({ name: 'VoteEntryRow' });
      expect(rows.length).toBe(9);
    });

    it('should display vote status correctly', async () => {
      const votes: VoteDto[] = [
        {
          ballotGuid: 'ballot-123',
          positionOnBallot: 1,
          personGuid: mockCandidates[0].personGuid,
          personFullName: mockCandidates[0].fullName,
          statusCode: 'Ok'
        },
        {
          ballotGuid: 'ballot-123',
          positionOnBallot: 2,
          personGuid: mockCandidates[1].personGuid,
          personFullName: mockCandidates[1].fullName,
          statusCode: 'Ok'
        }
      ];
      const ballot = createMockBallot(votes);

      const wrapper = mount(InlineBallotEntry, {
        props: {
          electionGuid: 'election-123',
          ballot,
          maxVotes: 9
        },
        ...defaultMountOptions
      });

      await flushPromises();

      const statusText = wrapper.find('.inline-ballot-entry__status-text');
      expect(statusText.text()).toContain('2');
      expect(statusText.text()).toContain('9');
    });

    it('should show Clear All button', async () => {
      const ballot = createMockBallot();

      const wrapper = mount(InlineBallotEntry, {
        props: {
          electionGuid: 'election-123',
          ballot,
          maxVotes: 9
        },
        ...defaultMountOptions
      });

      await flushPromises();

      const clearButton = wrapper.find('button');
      expect(clearButton.text()).toContain('Clear All');
    });
  });

  describe('vote management', () => {
    it('should emit vote-added when a vote is selected', async () => {
      const ballot = createMockBallot();

      const wrapper = mount(InlineBallotEntry, {
        props: {
          electionGuid: 'election-123',
          ballot,
          maxVotes: 9
        },
        ...unstubMountOptions
      });

      await flushPromises();

      const voteRow = wrapper.findComponent(VoteEntryRow);
      const vote: VoteDto = {
        ballotGuid: 'ballot-123',
        positionOnBallot: 1,
        personGuid: mockCandidates[0].personGuid,
        personFullName: mockCandidates[0].fullName,
        statusCode: 'Ok'
      };

      await voteRow.vm.$emit('vote-selected', vote);
      await nextTick();

      expect(wrapper.emitted('vote-added')).toBeTruthy();
      const emittedEvents = wrapper.emitted('vote-added') as any[];
      expect(emittedEvents[0][0]).toMatchObject(vote);
    });

    it('should emit vote-removed when a vote is cleared', async () => {
      const ballot = createMockBallot();

      const wrapper = mount(InlineBallotEntry, {
        props: {
          electionGuid: 'election-123',
          ballot,
          maxVotes: 9
        },
        ...unstubMountOptions
      });

      await flushPromises();

      const voteRow = wrapper.findComponent(VoteEntryRow);
      await voteRow.vm.$emit('vote-cleared', 1);
      await nextTick();

      expect(wrapper.emitted('vote-removed')).toBeTruthy();
      const emittedEvents = wrapper.emitted('vote-removed') as any[];
      expect(emittedEvents[0][0]).toBe(1);
    });

    it('should show duplicate warning when same person is selected twice', async () => {
      const ballot = createMockBallot();

      const wrapper = mount(InlineBallotEntry, {
        props: {
          electionGuid: 'election-123',
          ballot,
          maxVotes: 9
        },
        ...unstubMountOptions
      });

      await flushPromises();

      const voteRows = wrapper.findAllComponents(VoteEntryRow);
      
      const vote1: VoteDto = {
        ballotGuid: 'ballot-123',
        positionOnBallot: 1,
        personGuid: mockCandidates[0].personGuid,
        personFullName: mockCandidates[0].fullName,
        statusCode: 'Ok'
      };

      const vote2: VoteDto = {
        ballotGuid: 'ballot-123',
        positionOnBallot: 2,
        personGuid: mockCandidates[0].personGuid,
        personFullName: mockCandidates[0].fullName,
        statusCode: 'Ok'
      };

      await voteRows[0].vm.$emit('vote-selected', vote1);
      await nextTick();

      await voteRows[1].vm.$emit('vote-selected', vote2);
      await nextTick();

      expect(ElMessage.warning).toHaveBeenCalled();
    });
  });

  describe('Clear All functionality', () => {
    it('should clear all votes when Clear All is clicked', async () => {
      const votes: VoteDto[] = [
        {
          ballotGuid: 'ballot-123',
          positionOnBallot: 1,
          personGuid: mockCandidates[0].personGuid,
          personFullName: mockCandidates[0].fullName,
          statusCode: 'Ok'
        },
        {
          ballotGuid: 'ballot-123',
          positionOnBallot: 2,
          personGuid: mockCandidates[1].personGuid,
          personFullName: mockCandidates[1].fullName,
          statusCode: 'Ok'
        }
      ];
      const ballot = createMockBallot(votes);

      const wrapper = mount(InlineBallotEntry, {
        props: {
          electionGuid: 'election-123',
          ballot,
          maxVotes: 9
        },
        ...defaultMountOptions
      });

      await flushPromises();

      const clearButton = wrapper.findComponent(ElButton);
      await clearButton.trigger('click');
      await nextTick();

      expect(wrapper.emitted('vote-removed')).toBeTruthy();
      const emittedEvents = wrapper.emitted('vote-removed') as any[];
      expect(emittedEvents.length).toBe(2);
      expect(emittedEvents[0][0]).toBe(1);
      expect(emittedEvents[1][0]).toBe(2);
    });

    it('should disable Clear All button when no votes are entered', async () => {
      const ballot = createMockBallot();

      const wrapper = mount(InlineBallotEntry, {
        props: {
          electionGuid: 'election-123',
          ballot,
          maxVotes: 9
        },
        ...defaultMountOptions
      });

      await flushPromises();

      const clearButton = wrapper.findComponent(ElButton);
      expect(clearButton.attributes('disabled')).toBeDefined();
    });

    it('should enable Clear All button when votes are entered', async () => {
      const votes: VoteDto[] = [
        {
          ballotGuid: 'ballot-123',
          positionOnBallot: 1,
          personGuid: mockCandidates[0].personGuid,
          personFullName: mockCandidates[0].fullName,
          statusCode: 'Ok'
        }
      ];
      const ballot = createMockBallot(votes);

      const wrapper = mount(InlineBallotEntry, {
        props: {
          electionGuid: 'election-123',
          ballot,
          maxVotes: 9
        },
        ...defaultMountOptions
      });

      await flushPromises();

      const clearButton = wrapper.findComponent(ElButton);
      expect(clearButton.attributes('disabled')).toBeUndefined();
    });
  });

  describe('status indicator', () => {
    it('should update status when votes change', async () => {
      const ballot = createMockBallot();

      const wrapper = mount(InlineBallotEntry, {
        props: {
          electionGuid: 'election-123',
          ballot,
          maxVotes: 9
        },
        ...unstubMountOptions
      });

      await flushPromises();

      let statusText = wrapper.find('.inline-ballot-entry__status-text');
      expect(statusText.text()).toContain('0');

      const voteRow = wrapper.findComponent(VoteEntryRow);
      const vote: VoteDto = {
        ballotGuid: 'ballot-123',
        positionOnBallot: 1,
        personGuid: mockCandidates[0].personGuid,
        personFullName: mockCandidates[0].fullName,
        statusCode: 'Ok'
      };

      await voteRow.vm.$emit('vote-selected', vote);
      await nextTick();

      statusText = wrapper.find('.inline-ballot-entry__status-text');
      expect(statusText.text()).toContain('1');
    });

    it('should correctly count only votes with personGuid', async () => {
      const votes: VoteDto[] = [
        {
          ballotGuid: 'ballot-123',
          positionOnBallot: 1,
          personGuid: mockCandidates[0].personGuid,
          personFullName: mockCandidates[0].fullName,
          statusCode: 'Ok'
        },
        {
          ballotGuid: 'ballot-123',
          positionOnBallot: 2,
          statusCode: 'Ok'
        }
      ];
      const ballot = createMockBallot(votes);

      const wrapper = mount(InlineBallotEntry, {
        props: {
          electionGuid: 'election-123',
          ballot,
          maxVotes: 9
        },
        ...defaultMountOptions
      });

      await flushPromises();

      const statusText = wrapper.find('.inline-ballot-entry__status-text');
      expect(statusText.text()).toContain('1');
    });
  });

  describe('duplicate detection', () => {
    it('should detect duplicates across multiple votes', async () => {
      const votes: VoteDto[] = [
        {
          ballotGuid: 'ballot-123',
          positionOnBallot: 1,
          personGuid: mockCandidates[0].personGuid,
          personFullName: mockCandidates[0].fullName,
          statusCode: 'Ok'
        },
        {
          ballotGuid: 'ballot-123',
          positionOnBallot: 3,
          personGuid: mockCandidates[0].personGuid,
          personFullName: mockCandidates[0].fullName,
          statusCode: 'Ok'
        }
      ];
      const ballot = createMockBallot(votes);

      const wrapper = mount(InlineBallotEntry, {
        props: {
          electionGuid: 'election-123',
          ballot,
          maxVotes: 9
        },
        ...unstubMountOptions
      });

      await flushPromises();

      const voteRows = wrapper.findAllComponents(VoteEntryRow);
      expect(voteRows[0].props('duplicatePersonGuids')).toContain(mockCandidates[0].personGuid);
      expect(voteRows[2].props('duplicatePersonGuids')).toContain(mockCandidates[0].personGuid);
    });

    it('should not mark non-duplicate votes as duplicates', async () => {
      const votes: VoteDto[] = [
        {
          ballotGuid: 'ballot-123',
          positionOnBallot: 1,
          personGuid: mockCandidates[0].personGuid,
          personFullName: mockCandidates[0].fullName,
          statusCode: 'Ok'
        },
        {
          ballotGuid: 'ballot-123',
          positionOnBallot: 2,
          personGuid: mockCandidates[1].personGuid,
          personFullName: mockCandidates[1].fullName,
          statusCode: 'Ok'
        }
      ];
      const ballot = createMockBallot(votes);

      const wrapper = mount(InlineBallotEntry, {
        props: {
          electionGuid: 'election-123',
          ballot,
          maxVotes: 9
        },
        ...unstubMountOptions
      });

      await flushPromises();

      const voteRows = wrapper.findAllComponents(VoteEntryRow);
      expect(voteRows[0].props('duplicatePersonGuids')).toEqual([]);
      expect(voteRows[1].props('duplicatePersonGuids')).toEqual([]);
    });
  });
});
