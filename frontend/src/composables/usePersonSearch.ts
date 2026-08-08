import { computed, ref, watch, type Ref } from "vue";
import { useDebounceFn } from "@/utils/debounce";
import type { SearchablePersonDto } from "@/types/Person";
import {
  applyAllStrategies,
  type SearchResult,
} from "@/utils/searchStrategies";

export interface UsePersonSearchOptions {
  debounceDelay?: number;
  maxResults?: number;
  enableCache?: boolean;
}

/** Person plus the strategy weight from the last search (debug / ranking). */
export type RankedSearchPerson = SearchablePersonDto & {
  _searchWeight: number;
  _matchedStrategy?: string;
};

interface SearchCache {
  query: string;
  peopleHash: string;
  results: RankedSearchPerson[];
  timestamp: number;
}

const CACHE_TTL = 60000; // 1 minute

/**
 * Groups strategy weights into bands so that "similar ranking" is explicit.
 * Within a band, popularity (voteCount) decides order; then alphabetical.
 */
function getMatchBand(weight: number): number {
  if (weight >= 90) return 4; // exact / strong prefix
  if (weight >= 80) return 3; // wordBoundary / multiToken / substring
  if (weight >= 60) return 2; // otherNames / phonetic
  if (weight >= 40) return 1; // fuzzy
  return 0;
}

export function usePersonSearch(
  searchQuery: Ref<string>,
  searchablePeople: Ref<SearchablePersonDto[]>,
  options: UsePersonSearchOptions = {},
) {
  const { debounceDelay = 150, maxResults = 20, enableCache = true } = options;

  const cache = ref<Map<string, SearchCache>>(new Map());

  const getPeopleHash = (people: SearchablePersonDto[]): string => {
    // Include a cheap popularity fingerprint so voteCount updates invalidate cache order
    let voteSum = 0;
    for (const p of people) {
      voteSum += p.voteCount ?? 0;
    }
    return `${people.length}-${people[0]?.personGuid || ""}-${people[people.length - 1]?.personGuid || ""}-v${voteSum}`;
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

    keysToDelete.forEach((key) => cache.value.delete(key));
  };

  const performSearch = (
    query: string,
    people: SearchablePersonDto[],
  ): RankedSearchPerson[] => {
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

    // 1. Match-quality band (primary)
    // 2. Popularity within the same band (secondary)
    // 3. Higher exact weight within band
    // 4. Alphabetical (tertiary)
    results.sort((a, b) => {
      const bandDiff = getMatchBand(b.weight) - getMatchBand(a.weight);
      if (bandDiff !== 0) {
        return bandDiff;
      }

      const voteCountDiff =
        (b.person.voteCount ?? 0) - (a.person.voteCount ?? 0);
      if (voteCountDiff !== 0) {
        return voteCountDiff;
      }

      if (b.weight !== a.weight) {
        return b.weight - a.weight;
      }

      const lastNameCompare = a.person.lastName.localeCompare(
        b.person.lastName,
      );
      if (lastNameCompare !== 0) {
        return lastNameCompare;
      }

      const firstNameA = a.person.firstName || "";
      const firstNameB = b.person.firstName || "";
      return firstNameA.localeCompare(firstNameB);
    });

    const finalResults: RankedSearchPerson[] = results
      .slice(0, maxResults)
      .map((r) => ({
        ...r.person,
        _searchWeight: r.weight,
        _matchedStrategy: r.matchedStrategy,
      }));

    if (enableCache) {
      const peopleHash = getPeopleHash(people);
      const cacheKey = getCacheKey(trimmedQuery, peopleHash);
      cache.value.set(cacheKey, {
        query: trimmedQuery,
        peopleHash,
        results: finalResults,
        timestamp: Date.now(),
      });
    }

    return finalResults;
  };

  const debouncedSearch = useDebounceFn(
    (query: string, people: SearchablePersonDto[]) => {
      return performSearch(query, people);
    },
    debounceDelay,
  );

  const searchResults = computed(() => {
    return performSearch(searchQuery.value, searchablePeople.value);
  });

  watch(searchablePeople, () => {
    cache.value.clear();
  });

  return {
    searchResults,
    performSearch,
    debouncedSearch,
    clearCache: () => cache.value.clear(),
  };
}
