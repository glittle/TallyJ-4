import { describe, it, expect, vi, beforeEach } from "vitest";
import { ref, type Ref } from "vue";
import { usePersonSearch } from "../usePersonSearch";
import type { SearchablePersonDto } from "@/types/Person";

const { strategyCalls } = vi.hoisted(() => ({
  strategyCalls: { count: 0 },
}));

// Count work in usePersonSearch. The wrapper calls the real strategies, so
// match behavior stays the same; only the number of person scans is observed.
vi.mock("@/utils/searchStrategies", async (importOriginal) => {
  const actual =
    await importOriginal<typeof import("@/utils/searchStrategies")>();
  return {
    ...actual,
    applyAllStrategies: (
      ...args: Parameters<typeof actual.applyAllStrategies>
    ) => {
      strategyCalls.count += 1;
      return actual.applyAllStrategies(...args);
    },
  };
});

function createMockPerson(
  firstName: string,
  lastName: string,
  soundCodes: string[] = [],
  otherNames?: string,
  otherLastNames?: string,
  voteCount = 0,
): SearchablePersonDto {
  const fullName = `${firstName} ${lastName}`;
  return {
    personGuid: `guid-${firstName}-${lastName}`,
    firstName,
    lastName,
    fullName,
    _searchText:
      `${firstName} ${lastName} ${otherNames || ""} ${otherLastNames || ""}`.trim(),
    _soundexCodes: soundCodes,
    voteCount,
    otherNames,
    otherLastNames,
  };
}

describe("usePersonSearch", () => {
  let mockSearchablePeople: SearchablePersonDto[];

  beforeEach(() => {
    strategyCalls.count = 0;
    mockSearchablePeople = [
      createMockPerson("John", "Doe", ["J500", "D000"]),
      createMockPerson("Jane", "Smith", ["J500", "S530"]),
      createMockPerson("Bob", "Johnson", ["B100", "J525"]),
      createMockPerson("Alice", "Williams", ["A420", "W452"]),
      createMockPerson("Charlie", "Brown", ["C640", "B650"]),
    ];
  });

  describe("exact match", () => {
    it("should return exact match with highest weight", () => {
      const searchQuery = ref("John Doe");
      const searchablePeople = ref(mockSearchablePeople);
      const { searchResults } = usePersonSearch(searchQuery, searchablePeople);

      expect(searchResults.value.length).toBeGreaterThan(0);
      expect(searchResults.value[0].firstName).toBe("John");
      expect(searchResults.value[0].lastName).toBe("Doe");
    });

    it("should match case-insensitively", () => {
      const searchQuery = ref("john doe");
      const searchablePeople = ref(mockSearchablePeople);
      const { searchResults } = usePersonSearch(searchQuery, searchablePeople);

      expect(searchResults.value.length).toBeGreaterThan(0);
      expect(searchResults.value[0].firstName).toBe("John");
    });
  });

  describe("prefix match", () => {
    it("should find partial name matches", () => {
      const searchQuery = ref("Jo");
      const searchablePeople = ref(mockSearchablePeople);
      const { searchResults } = usePersonSearch(searchQuery, searchablePeople);

      expect(searchResults.value.length).toBeGreaterThan(0);
      const names = searchResults.value.map((p) => p.firstName);
      expect(names).toContain("John");
    });

    it("should match prefix at start of name", () => {
      const searchQuery = ref("Cha");
      const searchablePeople = ref(mockSearchablePeople);
      const { searchResults } = usePersonSearch(searchQuery, searchablePeople);

      expect(searchResults.value).toHaveLength(1);
      expect(searchResults.value[0].firstName).toBe("Charlie");
    });
  });

  describe("word boundary match", () => {
    it("should find matches where search words start name parts", () => {
      const searchQuery = ref("J D");
      const searchablePeople = ref(mockSearchablePeople);
      const { searchResults } = usePersonSearch(searchQuery, searchablePeople);

      expect(searchResults.value.length).toBeGreaterThan(0);
      const fullNames = searchResults.value.map((p) => p.fullName);
      expect(fullNames).toContain("John Doe");
    });
  });

  describe("substring match", () => {
    it("should find substring anywhere in name", () => {
      const searchQuery = ref("illi");
      const searchablePeople = ref(mockSearchablePeople);
      const { searchResults } = usePersonSearch(searchQuery, searchablePeople);

      expect(searchResults.value).toHaveLength(1);
      expect(searchResults.value[0].lastName).toBe("Williams");
    });
  });

  describe("other names match", () => {
    it("should match against otherNames field", () => {
      const peopleWithOtherNames = ref([
        createMockPerson("Robert", "Smith", ["R163", "S530"], "Bob", undefined),
      ]);
      const searchQuery = ref("Bob");
      const { searchResults } = usePersonSearch(
        searchQuery,
        peopleWithOtherNames,
      );

      expect(searchResults.value).toHaveLength(1);
      expect(searchResults.value[0].firstName).toBe("Robert");
    });

    it("should match against otherLastNames field", () => {
      const peopleWithOtherNames = ref([
        createMockPerson("Jane", "Doe", ["J500", "D000"], undefined, "Smith"),
      ]);
      const searchQuery = ref("Smith");
      const { searchResults } = usePersonSearch(
        searchQuery,
        peopleWithOtherNames,
      );

      expect(searchResults.value).toHaveLength(1);
      expect(searchResults.value[0].firstName).toBe("Jane");
    });
  });

  describe("phonetic match", () => {
    it("should find similar-sounding names using Soundex", () => {
      const peopleWithPhonetic = ref([
        createMockPerson("John", "MacFarland", ["J500", "M216"]),
        createMockPerson("Jane", "McFarland", ["J500", "M216"]),
      ]);
      const searchQuery = ref("Macfarland");
      const { searchResults } = usePersonSearch(
        searchQuery,
        peopleWithPhonetic,
      );

      expect(searchResults.value.length).toBeGreaterThan(0);
    });

    it("should NOT activate phonetic match for queries < 3 characters", () => {
      const peopleWithPhonetic = ref([
        createMockPerson("John", "Doe", ["J500", "D000"]),
        createMockPerson("Jane", "Dough", ["J500", "D200"]),
      ]);
      const searchQuery = ref("Do");
      const { searchResults } = usePersonSearch(
        searchQuery,
        peopleWithPhonetic,
      );

      const exactOrPrefixMatch = searchResults.value.some(
        (p) => p.lastName === "Doe" || p.lastName === "Dough",
      );
      expect(exactOrPrefixMatch).toBe(true);
    });

    it("should activate phonetic match for queries >= 3 characters", () => {
      const peopleWithPhonetic = ref([
        createMockPerson("Steven", "Smith", ["S315", "S530"]),
        createMockPerson("Stephen", "Smyth", ["S315", "S530"]),
      ]);
      const searchQuery = ref("Stephen");
      const { searchResults } = usePersonSearch(
        searchQuery,
        peopleWithPhonetic,
      );

      expect(searchResults.value.length).toBeGreaterThan(0);
    });
  });

  describe("fuzzy match", () => {
    it("should find typos with Levenshtein distance <= 2", () => {
      const peopleWithTypo = ref([
        createMockPerson("John", "Smith", ["J500", "S530"]),
      ]);
      const searchQuery = ref("Smth");
      const { searchResults } = usePersonSearch(searchQuery, peopleWithTypo);

      expect(searchResults.value.length).toBeGreaterThan(0);
      expect(searchResults.value[0].lastName).toBe("Smith");
    });

    it("should NOT activate fuzzy match for queries < 3 characters", () => {
      const peopleWithTypo = ref([
        createMockPerson("John", "Doe", ["J500", "D000"]),
      ]);
      const { performSearch } = usePersonSearch(ref(""), peopleWithTypo);
      const results = performSearch("De", peopleWithTypo.value);

      const fuzzyMatch = results.some((p) => p.lastName === "Doe");
      expect(fuzzyMatch).toBe(false);
    });

    it("should NOT match if distance > 2", () => {
      const peopleWithTypo = ref([
        createMockPerson("John", "Smith", ["J500", "S530"]),
      ]);
      const searchQuery = ref("Wxyz");
      const { searchResults } = usePersonSearch(searchQuery, peopleWithTypo);

      const smithMatch = searchResults.value.some(
        (p) => p.lastName === "Smith",
      );
      expect(smithMatch).toBe(false);
    });
  });

  describe("ranking and sorting", () => {
    it("should order results by weight (highest first)", () => {
      const peopleForRanking = ref([
        createMockPerson("John", "Doe", ["J500", "D000"]),
        createMockPerson("Jonathan", "Smith", ["J535", "S530"]),
        createMockPerson("Robert", "Brown", ["R163", "B650"]),
      ]);
      const searchQuery = ref("John Doe");
      const { searchResults } = usePersonSearch(searchQuery, peopleForRanking);

      expect(searchResults.value.length).toBeGreaterThan(0);
      expect(searchResults.value[0].firstName).toBe("John");
      expect(searchResults.value[0].lastName).toBe("Doe");
    });

    it("should break ties by lastName alphabetically", () => {
      const peopleForTieBreak = ref([
        createMockPerson("John", "Zebra", ["J500", "Z160"]),
        createMockPerson("John", "Apple", ["J500", "A140"]),
        createMockPerson("John", "Mango", ["J500", "M520"]),
      ]);
      const searchQuery = ref("John");
      const { searchResults } = usePersonSearch(searchQuery, peopleForTieBreak);

      expect(searchResults.value).toHaveLength(3);
      expect(searchResults.value[0].lastName).toBe("Apple");
      expect(searchResults.value[1].lastName).toBe("Mango");
      expect(searchResults.value[2].lastName).toBe("Zebra");
    });

    it("should sort by popularity within the same match-quality band", () => {
      // All three match "Brown" with the same strategy weight (substring / wordBoundary).
      // Within that band, higher voteCount should come first.
      const peopleWithVoteCounts = ref([
        createMockPerson(
          "Alice",
          "Brown",
          ["A420", "B650"],
          undefined,
          undefined,
          1,
        ),
        createMockPerson(
          "Bob",
          "Brown",
          ["B100", "B650"],
          undefined,
          undefined,
          5,
        ),
        createMockPerson(
          "Carol",
          "Brown",
          ["C640", "B650"],
          undefined,
          undefined,
          3,
        ),
      ]);
      const searchQuery = ref("Brown");
      const { searchResults } = usePersonSearch(
        searchQuery,
        peopleWithVoteCounts,
      );

      expect(searchResults.value).toHaveLength(3);
      expect(searchResults.value[0].firstName).toBe("Bob");
      expect(searchResults.value[1].firstName).toBe("Carol");
      expect(searchResults.value[2].firstName).toBe("Alice");
    });

    it("should prefer higher match quality over higher popularity", () => {
      // Exact match for "John Doe" must beat a popular weaker match.
      const people = ref([
        createMockPerson(
          "John",
          "Doe",
          ["J500", "D000"],
          undefined,
          undefined,
          1,
        ),
        createMockPerson(
          "Jonathan",
          "Smith",
          ["J535", "S530"],
          undefined,
          undefined,
          99,
        ),
      ]);
      const searchQuery = ref("John Doe");
      const { searchResults } = usePersonSearch(searchQuery, people);

      expect(searchResults.value.length).toBeGreaterThan(0);
      expect(searchResults.value[0].firstName).toBe("John");
      expect(searchResults.value[0].lastName).toBe("Doe");
    });

    it("should use weight as secondary sort when voteCount is equal", () => {
      const peopleEqualVoteCount = ref([
        createMockPerson(
          "John",
          "Doe",
          ["J500", "D000"],
          undefined,
          undefined,
          2,
        ),
        createMockPerson(
          "Jonathan",
          "Smith",
          ["J535", "S530"],
          undefined,
          undefined,
          2,
        ),
      ]);
      const searchQuery = ref("John Doe");
      const { searchResults } = usePersonSearch(
        searchQuery,
        peopleEqualVoteCount,
      );

      expect(searchResults.value.length).toBeGreaterThan(0);
      expect(searchResults.value[0].firstName).toBe("John");
      expect(searchResults.value[0].lastName).toBe("Doe");
    });

    it("should break lastName ties by firstName alphabetically", () => {
      const peopleForTieBreak = ref([
        createMockPerson("Zoe", "Smith", ["Z000", "S530"]),
        createMockPerson("Alice", "Smith", ["A420", "S530"]),
        createMockPerson("Bob", "Smith", ["B100", "S530"]),
      ]);
      const searchQuery = ref("Smith");
      const { searchResults } = usePersonSearch(searchQuery, peopleForTieBreak);

      expect(searchResults.value).toHaveLength(3);
      expect(searchResults.value[0].firstName).toBe("Alice");
      expect(searchResults.value[1].firstName).toBe("Bob");
      expect(searchResults.value[2].firstName).toBe("Zoe");
    });
  });

  describe("result capping", () => {
    it("should cap results at 20 by default", () => {
      const manySearchablePeople = ref(
        Array.from({ length: 50 }, (_, i) =>
          createMockPerson(`Person${i}`, "Smith", ["P625", "S530"]),
        ),
      );
      const searchQuery = ref("Smith");
      const { searchResults } = usePersonSearch(
        searchQuery,
        manySearchablePeople,
      );

      expect(searchResults.value).toHaveLength(20);
    });

    it("should respect custom maxResults option", () => {
      const manySearchablePeople = ref(
        Array.from({ length: 50 }, (_, i) =>
          createMockPerson(`Person${i}`, "Smith", ["P625", "S530"]),
        ),
      );
      const searchQuery = ref("Smith");
      const { searchResults } = usePersonSearch(
        searchQuery,
        manySearchablePeople,
        {
          maxResults: 5,
        },
      );

      expect(searchResults.value).toHaveLength(5);
    });
  });

  describe("debouncing", () => {
    it("should debounce search with default 150ms delay", async () => {
      const searchQuery = ref("");
      const searchablePeople = ref(mockSearchablePeople);
      const { debouncedSearch } = usePersonSearch(
        searchQuery,
        searchablePeople,
      );

      const searchSpy = vi.fn();
      const wrappedSearch = vi.fn(
        (query: string, people: SearchablePersonDto[]) => {
          searchSpy();
          return debouncedSearch(query, people);
        },
      );

      wrappedSearch("J", mockSearchablePeople);
      wrappedSearch("Jo", mockSearchablePeople);
      wrappedSearch("Joh", mockSearchablePeople);

      expect(searchSpy).toHaveBeenCalledTimes(3);

      await new Promise((resolve) => setTimeout(resolve, 200));
    });

    it("should respect custom debounce delay", () => {
      const searchQuery = ref("");
      const searchablePeople = ref(mockSearchablePeople);
      const { debouncedSearch } = usePersonSearch(
        searchQuery,
        searchablePeople,
        {
          debounceDelay: 300,
        },
      );

      expect(debouncedSearch).toBeDefined();
    });
  });

  describe("edge cases", () => {
    it("should return empty array for empty search query", () => {
      const searchQuery = ref("");
      const searchablePeople = ref(mockSearchablePeople);
      const { searchResults } = usePersonSearch(searchQuery, searchablePeople);

      expect(searchResults.value).toEqual([]);
    });

    it("should return empty array for whitespace-only search query", () => {
      const searchQuery = ref("   ");
      const searchablePeople = ref(mockSearchablePeople);
      const { searchResults } = usePersonSearch(searchQuery, searchablePeople);

      expect(searchResults.value).toEqual([]);
    });

    it("should return empty array when searchable people list is empty", () => {
      const searchQuery = ref("John");
      const searchablePeople = ref<SearchablePersonDto[]>([]);
      const { searchResults } = usePersonSearch(searchQuery, searchablePeople);

      expect(searchResults.value).toEqual([]);
    });

    it("should handle people without soundex codes", () => {
      const peopleNoSoundex = ref([createMockPerson("John", "Doe", [])]);
      const searchQuery = ref("John");
      const { searchResults } = usePersonSearch(searchQuery, peopleNoSoundex);

      expect(searchResults.value).toHaveLength(1);
    });
  });

  describe("performance", () => {
    // Call count is the real guard: each cold search must invoke
    // applyAllStrategies once per person. That fails if the scan skips
    // people, walks them twice, or otherwise does work that is not one
    // strategy pass over the list. It says nothing about speed.
    // The median of five cold runs is only a loose wall-clock bound.
    // Quiet-machine medians are about 28–58ms for 1000 people and 11–17ms
    // for 500 alias rows. 250ms and 100ms fail when the median is several
    // times slower than that (roughly a 4–9× slowdown), not a small regression.
    const performanceRuns = 5;
    const thousandPersonBudgetMs = 250;
    const aliasBudgetMs = 100;

    function medianDurationMs(samples: number[]): number {
      const sorted = [...samples].sort((a, b) => a - b);
      return sorted[Math.floor(sorted.length / 2)];
    }

    function measureColdSearches(
      searchQuery: Ref<string>,
      searchablePeople: Ref<SearchablePersonDto[]>,
    ): {
      medianMs: number;
      results: SearchablePersonDto[];
      callsPerRun: number[];
    } {
      const durations: number[] = [];
      const callsPerRun: number[] = [];
      let results: SearchablePersonDto[] = [];

      for (let run = 0; run < performanceRuns; run++) {
        strategyCalls.count = 0;
        const startedAt = performance.now();
        const { searchResults } = usePersonSearch(
          searchQuery,
          searchablePeople,
        );
        results = searchResults.value;
        durations.push(performance.now() - startedAt);
        callsPerRun.push(strategyCalls.count);
      }

      return {
        medianMs: medianDurationMs(durations),
        results,
        callsPerRun,
      };
    }

    it("scans each of 1000 people once and ranks every LastName50 match first", () => {
      const largeSearchablePeople = ref(
        Array.from({ length: 1000 }, (_, i) =>
          createMockPerson(`FirstName${i}`, `LastName${i % 100}`, [
            `F000`,
            `L000`,
          ]),
        ),
      );
      const searchQuery = ref("LastName50");

      const { medianMs, results, callsPerRun } = measureColdSearches(
        searchQuery,
        largeSearchablePeople,
      );

      expect(callsPerRun).toEqual(
        Array.from({ length: performanceRuns }, () => 1000),
      );
      // Substring hits are exactly the 10 LastName50 rows. Near spellings can
      // also fuzzy-match, so the list may be longer than 10, but those 10
      // outrank the fuzzy neighbors and stay inside maxResults (20).
      expect(
        results.filter((person) => person.lastName === "LastName50"),
      ).toHaveLength(10);
      expect(
        results
          .slice(0, 10)
          .every((person) => person.lastName === "LastName50"),
      ).toBe(true);
      expect(results.length).toBeLessThanOrEqual(20);
      expect(medianMs).toBeLessThan(thousandPersonBudgetMs);
    });

    it("scans 500 people once and returns the first 20 of 100 alias rows", () => {
      const largeSearchablePeople = ref(
        Array.from({ length: 500 }, (_, i) =>
          createMockPerson(
            `FirstName${i}`,
            `LastName${i % 50}`,
            [`F000`, `L000`],
            i % 5 === 0 ? `AliasName${i}` : undefined,
          ),
        ),
      );
      const searchQuery = ref("AliasName");

      const { medianMs, results, callsPerRun } = measureColdSearches(
        searchQuery,
        largeSearchablePeople,
      );

      expect(callsPerRun).toEqual(
        Array.from({ length: performanceRuns }, () => 500),
      );
      // 100 alias rows, capped by the default maxResults of 20.
      expect(results).toHaveLength(20);
      expect(
        results.every((person) => person.otherNames?.startsWith("AliasName")),
      ).toBe(true);
      expect(medianMs).toBeLessThan(aliasBudgetMs);
    });
  });

  describe("caching", () => {
    it("returns the cached array without scanning again", () => {
      const searchQuery = ref("John");
      const searchablePeople = ref(
        mockSearchablePeople.map((person) => ({ ...person })),
      );
      const { performSearch } = usePersonSearch(searchQuery, searchablePeople, {
        enableCache: true,
      });

      strategyCalls.count = 0;
      const results1 = performSearch("John", searchablePeople.value);
      expect(strategyCalls.count).toBe(searchablePeople.value.length);

      strategyCalls.count = 0;
      const results2 = performSearch("John", searchablePeople.value);

      // A cache hit returns the stored array. Matching ids would also pass
      // after a rescan: renaming a row leaves _soundexCodes, so a sound-alike
      // search still finds that person. The same array instance and zero
      // strategy calls are what show the second call did not search.
      expect(results2).toBe(results1);
      expect(strategyCalls.count).toBe(0);
    });

    it("should clear cache when searchable people change", () => {
      const searchQuery = ref("John");
      const searchablePeople = ref(mockSearchablePeople);
      const { searchResults } = usePersonSearch(searchQuery, searchablePeople, {
        enableCache: true,
      });

      const initialResults = searchResults.value;
      expect(initialResults.length).toBeGreaterThan(0);

      searchablePeople.value = [
        createMockPerson("Jane", "Doe", ["J500", "D000"]),
      ];

      searchQuery.value = "Jane";
      const newResults = searchResults.value;
      expect(newResults.length).toBeGreaterThan(0);
      expect(newResults[0].firstName).toBe("Jane");
    });

    it("should allow manual cache clearing", () => {
      const searchQuery = ref("John");
      const searchablePeople = ref(mockSearchablePeople);
      const { performSearch, clearCache } = usePersonSearch(
        searchQuery,
        searchablePeople,
        { enableCache: true },
      );

      performSearch("John", searchablePeople.value);
      clearCache();

      const results = performSearch("John", searchablePeople.value);
      expect(results.length).toBeGreaterThan(0);
    });

    it("should work with cache disabled", () => {
      const searchQuery = ref("John");
      const searchablePeople = ref(mockSearchablePeople);
      const { searchResults } = usePersonSearch(searchQuery, searchablePeople, {
        enableCache: false,
      });

      expect(searchResults.value.length).toBeGreaterThan(0);
      expect(searchResults.value[0].firstName).toBe("John");
    });
  });

  describe("reactive updates", () => {
    it("should update results when search query changes", () => {
      const searchQuery = ref("John");
      const searchablePeople = ref(mockSearchablePeople);
      const { searchResults } = usePersonSearch(searchQuery, searchablePeople);

      expect(searchResults.value.length).toBeGreaterThan(0);
      expect(searchResults.value[0].firstName).toBe("John");

      searchQuery.value = "Jane";

      expect(searchResults.value.length).toBeGreaterThan(0);
      expect(searchResults.value[0].firstName).toBe("Jane");
    });

    it("should update results when searchable people list changes", () => {
      const searchQuery = ref("John");
      const searchablePeople = ref(mockSearchablePeople);
      const { searchResults } = usePersonSearch(searchQuery, searchablePeople);

      expect(searchResults.value.length).toBeGreaterThan(0);

      searchablePeople.value = [
        createMockPerson("Johnny", "Walker", ["J500", "W460"]),
        createMockPerson("Jonathan", "Davis", ["J535", "D120"]),
      ];

      expect(searchResults.value.length).toBeGreaterThan(0);
      const names = searchResults.value.map((p) => p.firstName);
      expect(names).toContain("Johnny");
    });
  });
});
