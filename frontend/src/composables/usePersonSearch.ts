import { computed, type Ref } from 'vue';
import { useDebounceFn } from '@vueuse/core';
import type { SearchablePersonDto } from '@/types/Person';
import { applyAllStrategies, type SearchResult } from '@/utils/searchStrategies';

export interface UsePersonSearchOptions {
  debounceDelay?: number;
  maxResults?: number;
}

export function usePersonSearch(
  searchQuery: Ref<string>,
  candidates: Ref<SearchablePersonDto[]>,
  options: UsePersonSearchOptions = {}
) {
  const { debounceDelay = 150, maxResults = 20 } = options;

  const performSearch = (query: string, people: SearchablePersonDto[]): SearchablePersonDto[] => {
    const trimmedQuery = query.trim();
    
    if (!trimmedQuery || people.length === 0) {
      return [];
    }

    const results: SearchResult[] = [];

    for (const person of people) {
      const result = applyAllStrategies(trimmedQuery, person);
      if (result) {
        results.push(result);
      }
    }

    results.sort((a, b) => {
      if (b.weight !== a.weight) {
        return b.weight - a.weight;
      }
      
      const lastNameCompare = a.person.lastName.localeCompare(b.person.lastName);
      if (lastNameCompare !== 0) {
        return lastNameCompare;
      }
      
      const firstNameA = a.person.firstName || '';
      const firstNameB = b.person.firstName || '';
      return firstNameA.localeCompare(firstNameB);
    });

    return results.slice(0, maxResults).map(r => r.person);
  };

  const debouncedSearch = useDebounceFn((query: string, people: SearchablePersonDto[]) => {
    return performSearch(query, people);
  }, debounceDelay);

  const searchResults = computed(() => {
    return performSearch(searchQuery.value, candidates.value);
  });

  return {
    searchResults,
    performSearch,
    debouncedSearch
  };
}
