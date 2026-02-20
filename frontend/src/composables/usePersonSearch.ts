import { computed, ref, watch, type Ref } from 'vue';
import { useDebounceFn } from '@vueuse/core';
import type { SearchablePersonDto } from '@/types/Person';
import { applyAllStrategies, type SearchResult } from '@/utils/searchStrategies';

export interface UsePersonSearchOptions {
  debounceDelay?: number;
  maxResults?: number;
  enableCache?: boolean;
}

interface SearchCache {
  query: string;
  peopleHash: string;
  results: SearchablePersonDto[];
  timestamp: number;
}

const CACHE_TTL = 60000; // 1 minute

export function usePersonSearch(
  searchQuery: Ref<string>,
  candidates: Ref<SearchablePersonDto[]>,
  options: UsePersonSearchOptions = {}
) {
  const { debounceDelay = 150, maxResults = 20, enableCache = true } = options;
  
  const cache = ref<Map<string, SearchCache>>(new Map());
  
  const getPeopleHash = (people: SearchablePersonDto[]): string => {
    return `${people.length}-${people[0]?.personGuid || ''}-${people[people.length - 1]?.personGuid || ''}`;
  };
  
  const getCacheKey = (query: string, peopleHash: string): string => {
    return `${query.toLowerCase().trim()}-${peopleHash}`;
  };
  
  const cleanExpiredCache = () => {
    const now = Date.now();
    const keysToDelete: string[] = [];
    
    cache.value.forEach((entry, key) => {
      if (now - entry.timestamp > CACHE_TTL) {
        keysToDelete.push(key);
      }
    });
    
    keysToDelete.forEach(key => cache.value.delete(key));
  };

  const performSearch = (query: string, people: SearchablePersonDto[]): SearchablePersonDto[] => {
    const trimmedQuery = query.trim();
    
    if (!trimmedQuery || people.length === 0) {
      return [];
    }

    if (enableCache) {
      const peopleHash = getPeopleHash(people);
      const cacheKey = getCacheKey(trimmedQuery, peopleHash);
      const cachedResult = cache.value.get(cacheKey);
      
      if (cachedResult && Date.now() - cachedResult.timestamp < CACHE_TTL) {
        return cachedResult.results;
      }
      
      cleanExpiredCache();
    }

    const results: SearchResult[] = [];

    for (const person of people) {
      const result = applyAllStrategies(trimmedQuery, person);
      if (result) {
        results.push(result);
      }
    }

    results.sort((a, b) => {
      const voteCountDiff = (b.person.voteCount ?? 0) - (a.person.voteCount ?? 0);
      if (voteCountDiff !== 0) {
        return voteCountDiff;
      }

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

    const finalResults = results.slice(0, maxResults).map(r => r.person);
    
    if (enableCache) {
      const peopleHash = getPeopleHash(people);
      const cacheKey = getCacheKey(trimmedQuery, peopleHash);
      cache.value.set(cacheKey, {
        query: trimmedQuery,
        peopleHash,
        results: finalResults,
        timestamp: Date.now()
      });
    }

    return finalResults;
  };

  const debouncedSearch = useDebounceFn((query: string, people: SearchablePersonDto[]) => {
    return performSearch(query, people);
  }, debounceDelay);

  const searchResults = computed(() => {
    return performSearch(searchQuery.value, candidates.value);
  });
  
  watch(candidates, () => {
    cache.value.clear();
  });

  return {
    searchResults,
    performSearch,
    debouncedSearch,
    clearCache: () => cache.value.clear()
  };
}
