import { describe, it, expect, beforeEach, vi } from 'vitest';
import { mount } from '@vue/test-utils';
import { nextTick } from 'vue';
import VoteEntryRow from '../VoteEntryRow.vue';
import type { VoteDto } from '@/types/Vote';
import type { SearchablePersonDto } from '@/types/Person';
import { ElAutocomplete, ElButton, ElIcon } from 'element-plus';

const mockT = (key: string) => {
  const translations: Record<string, string> = {
    'ballots.position': 'Position',
    'ballots.candidate': 'Candidate',
    'ballots.duplicateWarning': 'Duplicate: This person is already selected',
    'common.clear': 'Clear'
  };
  return translations[key] || key;
};

vi.mock('vue-i18n', () => ({
  useI18n: () => ({
    t: mockT
  })
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
    _searchText: fullName,
    _soundexCodes: [],
    voteCount: 0
  };
}

const defaultMountOptions = {
  global: {
    components: {
      ElAutocomplete,
      ElButton,
      ElIcon
    },
    mocks: {
      $t: mockT
    }
  }
};

describe('VoteEntryRow', () => {
  let mockCandidates: SearchablePersonDto[];

  beforeEach(() => {
    mockCandidates = [
      createMockPerson('John', 'Doe'),
      createMockPerson('Jane', 'Smith'),
      createMockPerson('Bob', 'Johnson'),
      createMockPerson('Alice', 'Williams'),
      createMockPerson('Charlie', 'Brown')
    ];
  });

  describe('rendering', () => {
    it('should render with position number', () => {
      const wrapper = mount(VoteEntryRow, {
        props: {
          positionOnBallot: 1,
          candidates: mockCandidates,
          duplicatePersonGuids: []
        },
        ...defaultMountOptions
      });

      expect(wrapper.find('.vote-entry-row__position').text()).toBe('1');
    });

    it('should render autocomplete input', () => {
      const wrapper = mount(VoteEntryRow, {
        props: {
          positionOnBallot: 1,
          candidates: mockCandidates,
          duplicatePersonGuids: []
        },
        ...defaultMountOptions
      });

      expect(wrapper.findComponent(ElAutocomplete).exists()).toBe(true);
    });

    it('should not show clear button initially', () => {
      const wrapper = mount(VoteEntryRow, {
        props: {
          positionOnBallot: 1,
          candidates: mockCandidates,
          duplicatePersonGuids: []
        },
        ...defaultMountOptions
      });

      expect(wrapper.find('.vote-entry-row__clear').exists()).toBe(false);
    });

    it('should not show duplicate warning initially', () => {
      const wrapper = mount(VoteEntryRow, {
        props: {
          positionOnBallot: 1,
          candidates: mockCandidates,
          duplicatePersonGuids: []
        },
        ...defaultMountOptions
      });

      expect(wrapper.find('.vote-entry-row__warning').exists()).toBe(false);
    });
  });

  describe('search functionality', () => {
    it('should show filtered results when searching', async () => {
      const wrapper = mount(VoteEntryRow, {
        props: {
          positionOnBallot: 1,
          candidates: mockCandidates,
          duplicatePersonGuids: []
        },
        ...defaultMountOptions
      });

      const autocomplete = wrapper.findComponent(ElAutocomplete);
      const fetchSuggestions = autocomplete.props('fetchSuggestions') as Function;

      const mockCallback = vi.fn();
      fetchSuggestions('John', mockCallback);

      await nextTick();

      expect(mockCallback).toHaveBeenCalled();
      const results = mockCallback.mock.calls[0][0];
      expect(results.length).toBeGreaterThan(0);
      expect(results[0].value).toContain('John');
    });

    it('should return empty array for empty query', async () => {
      const wrapper = mount(VoteEntryRow, {
        props: {
          positionOnBallot: 1,
          candidates: mockCandidates,
          duplicatePersonGuids: []
        },
        ...defaultMountOptions
      });

      const autocomplete = wrapper.findComponent(ElAutocomplete);
      const fetchSuggestions = autocomplete.props('fetchSuggestions') as Function;

      const mockCallback = vi.fn();
      fetchSuggestions('', mockCallback);

      await nextTick();

      expect(mockCallback).toHaveBeenCalledWith([]);
    });

    it('should find multiple matching results', async () => {
      const wrapper = mount(VoteEntryRow, {
        props: {
          positionOnBallot: 1,
          candidates: mockCandidates,
          duplicatePersonGuids: []
        },
        ...defaultMountOptions
      });

      const autocomplete = wrapper.findComponent(ElAutocomplete);
      const fetchSuggestions = autocomplete.props('fetchSuggestions') as Function;

      const mockCallback = vi.fn();
      fetchSuggestions('o', mockCallback);

      await nextTick();

      const results = mockCallback.mock.calls[0][0];
      expect(results.length).toBeGreaterThan(1);
    });
  });

  describe('vote selection', () => {
    it('should emit vote-selected when person is selected', async () => {
      const wrapper = mount(VoteEntryRow, {
        props: {
          positionOnBallot: 1,
          modelValue: null,
          candidates: mockCandidates,
          duplicatePersonGuids: []
        },
        ...defaultMountOptions
      });

      const autocomplete = wrapper.findComponent(ElAutocomplete);
      
      await autocomplete.vm.$emit('select', {
        value: 'John Doe',
        person: mockCandidates[0]
      });

      await nextTick();

      expect(wrapper.emitted('vote-selected')).toBeTruthy();
      expect(wrapper.emitted('update:modelValue')).toBeTruthy();
      
      const voteSelectedEvents = wrapper.emitted('vote-selected') as any[];
      expect(voteSelectedEvents[0][0]).toMatchObject({
        positionOnBallot: 1,
        personGuid: mockCandidates[0].personGuid,
        personFullName: mockCandidates[0].fullName
      });
    });

    it('should show clear button after selection', async () => {
      const wrapper = mount(VoteEntryRow, {
        props: {
          positionOnBallot: 1,
          modelValue: null,
          candidates: mockCandidates,
          duplicatePersonGuids: []
        },
        ...defaultMountOptions
      });

      const autocomplete = wrapper.findComponent(ElAutocomplete);
      
      await autocomplete.vm.$emit('select', {
        value: 'John Doe',
        person: mockCandidates[0]
      });

      await nextTick();

      expect(wrapper.find('.vote-entry-row__clear').exists()).toBe(true);
    });

    it('should disable input after selection', async () => {
      const wrapper = mount(VoteEntryRow, {
        props: {
          positionOnBallot: 1,
          modelValue: null,
          candidates: mockCandidates,
          duplicatePersonGuids: []
        },
        ...defaultMountOptions
      });

      const autocomplete = wrapper.findComponent(ElAutocomplete);
      
      await autocomplete.vm.$emit('select', {
        value: 'John Doe',
        person: mockCandidates[0]
      });

      await nextTick();

      expect(wrapper.findComponent(ElAutocomplete).props('disabled')).toBe(true);
    });
  });

  describe('clear functionality', () => {
    it('should clear selection when clear button is clicked', async () => {
      const vote: VoteDto = {
        ballotGuid: 'ballot-123',
        positionOnBallot: 1,
        personGuid: mockCandidates[0].personGuid,
        personFullName: mockCandidates[0].fullName,
        statusCode: 'Ok'
      };

      const wrapper = mount(VoteEntryRow, {
        props: {
          positionOnBallot: 1,
          modelValue: vote,
          candidates: mockCandidates,
          duplicatePersonGuids: []
        },
        ...defaultMountOptions
      });

      await nextTick();

      const clearButton = wrapper.find('.vote-entry-row__clear');
      expect(clearButton.exists()).toBe(true);

      await clearButton.trigger('click');
      await nextTick();

      expect(wrapper.emitted('vote-cleared')).toBeTruthy();
      expect(wrapper.emitted('update:modelValue')).toBeTruthy();
      
      const clearedEvents = wrapper.emitted('vote-cleared') as any[];
      expect(clearedEvents[0][0]).toBe(1);
      
      const updateEvents = wrapper.emitted('update:modelValue') as any[];
      expect(updateEvents[updateEvents.length - 1][0]).toBeNull();
    });

    it('should re-enable input after clearing', async () => {
      const vote: VoteDto = {
        ballotGuid: 'ballot-123',
        positionOnBallot: 1,
        personGuid: mockCandidates[0].personGuid,
        personFullName: mockCandidates[0].fullName,
        statusCode: 'Ok'
      };

      const wrapper = mount(VoteEntryRow, {
        props: {
          positionOnBallot: 1,
          modelValue: vote,
          candidates: mockCandidates,
          duplicatePersonGuids: []
        },
        ...defaultMountOptions
      });

      await nextTick();

      const clearButton = wrapper.find('.vote-entry-row__clear');
      await clearButton.trigger('click');
      await nextTick();

      expect(wrapper.findComponent(ElAutocomplete).props('disabled')).toBe(false);
    });
  });

  describe('keyboard navigation', () => {
    it('should clear search on Escape when typing', async () => {
      const wrapper = mount(VoteEntryRow, {
        props: {
          positionOnBallot: 1,
          modelValue: null,
          candidates: mockCandidates,
          duplicatePersonGuids: []
        },
        ...defaultMountOptions
      });

      const autocomplete = wrapper.findComponent(ElAutocomplete);
      
      await autocomplete.vm.$emit('keydown', new KeyboardEvent('keydown', { key: 'Escape' }));
      await nextTick();

      const input = autocomplete.find('input');
      expect(input.element.value).toBe('');
    });

    it('should clear selection on Escape when selected', async () => {
      const vote: VoteDto = {
        ballotGuid: 'ballot-123',
        positionOnBallot: 1,
        personGuid: mockCandidates[0].personGuid,
        personFullName: mockCandidates[0].fullName,
        statusCode: 'Ok'
      };

      const wrapper = mount(VoteEntryRow, {
        props: {
          positionOnBallot: 1,
          modelValue: vote,
          candidates: mockCandidates,
          duplicatePersonGuids: []
        },
        ...defaultMountOptions
      });

      await nextTick();

      const autocomplete = wrapper.findComponent(ElAutocomplete);
      await autocomplete.vm.$emit('keydown', new KeyboardEvent('keydown', { key: 'Escape' }));
      await nextTick();

      expect(wrapper.emitted('vote-cleared')).toBeTruthy();
    });
  });

  describe('duplicate detection', () => {
    it('should show duplicate warning when person is in duplicatePersonGuids', async () => {
      const vote: VoteDto = {
        ballotGuid: 'ballot-123',
        positionOnBallot: 1,
        personGuid: mockCandidates[0].personGuid,
        personFullName: mockCandidates[0].fullName,
        statusCode: 'Ok'
      };

      const wrapper = mount(VoteEntryRow, {
        props: {
          positionOnBallot: 1,
          modelValue: vote,
          candidates: mockCandidates,
          duplicatePersonGuids: [mockCandidates[0].personGuid]
        },
        ...defaultMountOptions
      });

      await nextTick();

      expect(wrapper.find('.vote-entry-row__warning').exists()).toBe(true);
      expect(wrapper.find('.vote-entry-row__warning').text()).toContain('Duplicate');
    });

    it('should add is-duplicate class when duplicate detected', async () => {
      const vote: VoteDto = {
        ballotGuid: 'ballot-123',
        positionOnBallot: 1,
        personGuid: mockCandidates[0].personGuid,
        personFullName: mockCandidates[0].fullName,
        statusCode: 'Ok'
      };

      const wrapper = mount(VoteEntryRow, {
        props: {
          positionOnBallot: 1,
          modelValue: vote,
          candidates: mockCandidates,
          duplicatePersonGuids: [mockCandidates[0].personGuid]
        },
        ...defaultMountOptions
      });

      await nextTick();

      expect(wrapper.find('.vote-entry-row.is-duplicate').exists()).toBe(true);
    });

    it('should not show warning when person is not duplicate', async () => {
      const vote: VoteDto = {
        ballotGuid: 'ballot-123',
        positionOnBallot: 1,
        personGuid: mockCandidates[0].personGuid,
        personFullName: mockCandidates[0].fullName,
        statusCode: 'Ok'
      };

      const wrapper = mount(VoteEntryRow, {
        props: {
          positionOnBallot: 1,
          modelValue: vote,
          candidates: mockCandidates,
          duplicatePersonGuids: []
        },
        ...defaultMountOptions
      });

      await nextTick();

      expect(wrapper.find('.vote-entry-row__warning').exists()).toBe(false);
      expect(wrapper.find('.vote-entry-row.is-duplicate').exists()).toBe(false);
    });
  });

  describe('ARIA attributes', () => {
    it('should have proper aria-label on input', () => {
      const wrapper = mount(VoteEntryRow, {
        props: {
          positionOnBallot: 3,
          candidates: mockCandidates,
          duplicatePersonGuids: []
        },
        ...defaultMountOptions
      });

      const autocomplete = wrapper.findComponent(ElAutocomplete);
      expect(autocomplete.props('ariaLabel')).toContain('Position 3');
      expect(autocomplete.props('ariaLabel')).toContain('Candidate');
    });

    it('should have aria-describedby when duplicate', async () => {
      const vote: VoteDto = {
        ballotGuid: 'ballot-123',
        positionOnBallot: 2,
        personGuid: mockCandidates[0].personGuid,
        personFullName: mockCandidates[0].fullName,
        statusCode: 'Ok'
      };

      const wrapper = mount(VoteEntryRow, {
        props: {
          positionOnBallot: 2,
          modelValue: vote,
          candidates: mockCandidates,
          duplicatePersonGuids: [mockCandidates[0].personGuid]
        },
        ...defaultMountOptions
      });

      await nextTick();

      const warning = wrapper.find('.vote-entry-row__warning');
      expect(warning.exists()).toBe(true);
      expect(warning.attributes('id')).toBe('duplicate-warning-2');
    });

    it('should have role="alert" on warning', async () => {
      const vote: VoteDto = {
        ballotGuid: 'ballot-123',
        positionOnBallot: 1,
        personGuid: mockCandidates[0].personGuid,
        personFullName: mockCandidates[0].fullName,
        statusCode: 'Ok'
      };

      const wrapper = mount(VoteEntryRow, {
        props: {
          positionOnBallot: 1,
          modelValue: vote,
          candidates: mockCandidates,
          duplicatePersonGuids: [mockCandidates[0].personGuid]
        },
        ...defaultMountOptions
      });

      await nextTick();

      const warning = wrapper.find('.vote-entry-row__warning');
      expect(warning.attributes('role')).toBe('alert');
      expect(warning.attributes('aria-live')).toBe('polite');
    });

    it('should have aria-label on clear button', async () => {
      const vote: VoteDto = {
        ballotGuid: 'ballot-123',
        positionOnBallot: 1,
        personGuid: mockCandidates[0].personGuid,
        personFullName: mockCandidates[0].fullName,
        statusCode: 'Ok'
      };

      const wrapper = mount(VoteEntryRow, {
        props: {
          positionOnBallot: 1,
          modelValue: vote,
          candidates: mockCandidates,
          duplicatePersonGuids: []
        },
        ...defaultMountOptions
      });

      await nextTick();

      const clearButton = wrapper.find('.vote-entry-row__clear');
      expect(clearButton.attributes('aria-label')).toBe('Clear');
    });
  });

  describe('model value updates', () => {
    it('should sync with modelValue prop changes', async () => {
      const vote: VoteDto = {
        ballotGuid: 'ballot-123',
        positionOnBallot: 1,
        personGuid: mockCandidates[0].personGuid,
        personFullName: mockCandidates[0].fullName,
        statusCode: 'Ok'
      };

      const wrapper = mount(VoteEntryRow, {
        props: {
          positionOnBallot: 1,
          modelValue: null,
          candidates: mockCandidates,
          duplicatePersonGuids: []
        },
        ...defaultMountOptions
      });

      await wrapper.setProps({ modelValue: vote });
      await nextTick();

      expect(wrapper.find('.vote-entry-row__clear').exists()).toBe(true);
      expect(wrapper.findComponent(ElAutocomplete).props('disabled')).toBe(true);
    });

    it('should clear display when modelValue is set to null', async () => {
      const vote: VoteDto = {
        ballotGuid: 'ballot-123',
        positionOnBallot: 1,
        personGuid: mockCandidates[0].personGuid,
        personFullName: mockCandidates[0].fullName,
        statusCode: 'Ok'
      };

      const wrapper = mount(VoteEntryRow, {
        props: {
          positionOnBallot: 1,
          modelValue: vote,
          candidates: mockCandidates,
          duplicatePersonGuids: []
        },
        ...defaultMountOptions
      });

      await nextTick();
      expect(wrapper.find('.vote-entry-row__clear').exists()).toBe(true);

      await wrapper.setProps({ modelValue: null });
      await nextTick();

      expect(wrapper.find('.vote-entry-row__clear').exists()).toBe(false);
      expect(wrapper.findComponent(ElAutocomplete).props('disabled')).toBe(false);
    });
  });

  describe('exposed methods', () => {
    it('should expose focusInput method', () => {
      const wrapper = mount(VoteEntryRow, {
        props: {
          positionOnBallot: 1,
          candidates: mockCandidates,
          duplicatePersonGuids: []
        },
        ...defaultMountOptions
      });

      expect(wrapper.vm.focusInput).toBeDefined();
      expect(typeof wrapper.vm.focusInput).toBe('function');
    });
  });
});
