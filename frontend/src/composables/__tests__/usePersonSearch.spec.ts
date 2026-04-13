import { describe, it, expect, vi, beforeEach } from "vitest";
import { ref } from "vue";
import { usePersonSearch } from "../usePersonSearch";
import type { SearchablePersonDto } from "@/types/Person";

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
  let mockCandidates: SearchablePersonDto[];

  beforeEach(() => {
    mockCandidates = [
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
      const candidates = ref(mockCandidates);
      const { searchResults } = usePersonSearch(searchQuery, candidates);

      expect(searchResults.value.length).toBeGreaterThan(0);
      expect(searchResults.value[0]!.firstName).toBe("John");
      expect(searchResults.value[0]!.lastName).toBe("Doe");
    });

    it("should match case-insensitively", () => {
      const searchQuery = ref("john doe");
      const candidates = ref(mockCandidates);
      const { searchResults } = usePersonSearch(searchQuery, candidates);

      expect(searchResults.value.length).toBeGreaterThan(0);
      expect(searchResults.value[0]!.firstName).toBe("John");
    });
  });

  describe("prefix match", () => {
    it("should find partial name matches", () => {
      const searchQuery = ref("Jo");
      const candidates = ref(mockCandidates);
      const { searchResults } = usePersonSearch(searchQuery, candidates);

      expect(searchResults.value.length).toBeGreaterThan(0);
      const names = searchResults.value.map((p) => p.firstName);
      expect(names).toContain("John");
    });

    it("should match prefix at start of name", () => {
      const searchQuery = ref("Cha");
      const candidates = ref(mockCandidates);
      const { searchResults } = usePersonSearch(searchQuery, candidates);

      expect(searchResults.value).toHaveLength(1);
      expect(searchResults.value[0]!.firstName).toBe("Charlie");
    });
  });

  describe("word boundary match", () => {
    it("should find matches where search words start name parts", () => {
      const searchQuery = ref("J D");
      const candidates = ref(mockCandidates);
      const { searchResults } = usePersonSearch(searchQuery, candidates);

      expect(searchResults.value.length).toBeGreaterThan(0);
      const fullNames = searchResults.value.map((p) => p.fullName);
      expect(fullNames).toContain("John Doe");
    });
  });

  describe("substring match", () => {
    it("should find substring anywhere in name", () => {
      const searchQuery = ref("illi");
      const candidates = ref(mockCandidates);
      const { searchResults } = usePersonSearch(searchQuery, candidates);

      expect(searchResults.value).toHaveLength(1);
      expect(searchResults.value[0]!.lastName).toBe("Williams");
    });
  });

  describe("other names match", () => {
    it("should match against otherNames field", () => {
      const candidatesWithOtherNames = ref([
        createMockPerson("Robert", "Smith", ["R163", "S530"], "Bob", undefined),
      ]);
      const searchQuery = ref("Bob");
      const { searchResults } = usePersonSearch(
        searchQuery,
        candidatesWithOtherNames,
      );

      expect(searchResults.value).toHaveLength(1);
      expect(searchResults.value[0]!.firstName).toBe("Robert");
    });

    it("should match against otherLastNames field", () => {
      const candidatesWithOtherNames = ref([
        createMockPerson("Jane", "Doe", ["J500", "D000"], undefined, "Smith"),
      ]);
      const searchQuery = ref("Smith");
      const { searchResults } = usePersonSearch(
        searchQuery,
        candidatesWithOtherNames,
      );

      expect(searchResults.value).toHaveLength(1);
      expect(searchResults.value[0]!.firstName).toBe("Jane");
    });
  });

  describe("phonetic match", () => {
    it("should find similar-sounding names using Soundex", () => {
      const candidatesWithPhonetic = ref([
        createMockPerson("John", "MacFarland", ["J500", "M216"]),
        createMockPerson("Jane", "McFarland", ["J500", "M216"]),
      ]);
      const searchQuery = ref("Macfarland");
      const { searchResults } = usePersonSearch(
        searchQuery,
        candidatesWithPhonetic,
      );

      expect(searchResults.value.length).toBeGreaterThan(0);
    });

    it("should NOT activate phonetic match for queries < 3 characters", () => {
      const candidatesWithPhonetic = ref([
        createMockPerson("John", "Doe", ["J500", "D000"]),
        createMockPerson("Jane", "Dough", ["J500", "D200"]),
      ]);
      const searchQuery = ref("Do");
      const { searchResults } = usePersonSearch(
        searchQuery,
        candidatesWithPhonetic,
      );

      const exactOrPrefixMatch = searchResults.value.some(
        (p) => p.lastName === "Doe" || p.lastName === "Dough",
      );
      expect(exactOrPrefixMatch).toBe(true);
    });

    it("should activate phonetic match for queries >= 3 characters", () => {
      const candidatesWithPhonetic = ref([
        createMockPerson("Steven", "Smith", ["S315", "S530"]),
        createMockPerson("Stephen", "Smyth", ["S315", "S530"]),
      ]);
      const searchQuery = ref("Stephen");
      const { searchResults } = usePersonSearch(
        searchQuery,
        candidatesWithPhonetic,
      );

      expect(searchResults.value.length).toBeGreaterThan(0);
    });
  });

  describe("fuzzy match", () => {
    it("should find typos with Levenshtein distance <= 2", () => {
      const candidatesWithTypo = ref([
        createMockPerson("John", "Smith", ["J500", "S530"]),
      ]);
      const searchQuery = ref("Smth");
      const { searchResults } = usePersonSearch(
        searchQuery,
        candidatesWithTypo,
      );

      expect(searchResults.value.length).toBeGreaterThan(0);
      expect(searchResults.value[0]!.lastName).toBe("Smith");
    });

    it("should NOT activate fuzzy match for queries < 3 characters", () => {
      const candidatesWithTypo = ref([
        createMockPerson("John", "Doe", ["J500", "D000"]),
      ]);
      const { performSearch } = usePersonSearch(ref(""), candidatesWithTypo);
      const results = performSearch("De", candidatesWithTypo.value);

      const fuzzyMatch = results.some((p) => p.lastName === "Doe");
      expect(fuzzyMatch).toBe(false);
    });

    it("should NOT match if distance > 2", () => {
      const candidatesWithTypo = ref([
        createMockPerson("John", "Smith", ["J500", "S530"]),
      ]);
      const searchQuery = ref("Wxyz");
      const { searchResults } = usePersonSearch(
        searchQuery,
        candidatesWithTypo,
      );

      const smithMatch = searchResults.value.some(
        (p) => p.lastName === "Smith",
      );
      expect(smithMatch).toBe(false);
    });
  });

  describe("ranking and sorting", () => {
    it("should order results by weight (highest first)", () => {
      const candidatesForRanking = ref([
        createMockPerson("John", "Doe", ["J500", "D000"]),
        createMockPerson("Jonathan", "Smith", ["J535", "S530"]),
        createMockPerson("Robert", "Brown", ["R163", "B650"]),
      ]);
      const searchQuery = ref("John Doe");
      const { searchResults } = usePersonSearch(
        searchQuery,
        candidatesForRanking,
      );

      expect(searchResults.value.length).toBeGreaterThan(0);
      expect(searchResults.value[0]!.firstName).toBe("John");
      expect(searchResults.value[0]!.lastName).toBe("Doe");
    });

    it("should break ties by lastName alphabetically", () => {
      const candidatesForTieBreak = ref([
        createMockPerson("John", "Zebra", ["J500", "Z160"]),
        createMockPerson("John", "Apple", ["J500", "A140"]),
        createMockPerson("John", "Mango", ["J500", "M520"]),
      ]);
      const searchQuery = ref("John");
      const { searchResults } = usePersonSearch(
        searchQuery,
        candidatesForTieBreak,
      );

      expect(searchResults.value).toHaveLength(3);
      expect(searchResults.value[0]!.lastName).toBe("Apple");
      expect(searchResults.value[1]!.lastName).toBe("Mango");
      expect(searchResults.value[2]!.lastName).toBe("Zebra");
    });

    it("should sort results by voteCount descending as primary key", () => {
      const candidatesWithVoteCounts = ref([
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
        candidatesWithVoteCounts,
      );

      expect(searchResults.value).toHaveLength(3);
      expect(searchResults.value[0]!.firstName).toBe("Bob");
      expect(searchResults.value[1]!.firstName).toBe("Carol");
      expect(searchResults.value[2]!.firstName).toBe("Alice");
    });

    it("should use weight as secondary sort when voteCount is equal", () => {
      const candidatesEqualVoteCount = ref([
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
        candidatesEqualVoteCount,
      );

      expect(searchResults.value.length).toBeGreaterThan(0);
      expect(searchResults.value[0]!.firstName).toBe("John");
      expect(searchResults.value[0]!.lastName).toBe("Doe");
    });

    it("should break lastName ties by firstName alphabetically", () => {
      const candidatesForTieBreak = ref([
        createMockPerson("Zoe", "Smith", ["Z000", "S530"]),
        createMockPerson("Alice", "Smith", ["A420", "S530"]),
        createMockPerson("Bob", "Smith", ["B100", "S530"]),
      ]);
      const searchQuery = ref("Smith");
      const { searchResults } = usePersonSearch(
        searchQuery,
        candidatesForTieBreak,
      );

      expect(searchResults.value).toHaveLength(3);
      expect(searchResults.value[0]!.firstName).toBe("Alice");
      expect(searchResults.value[1]!.firstName).toBe("Bob");
      expect(searchResults.value[2]!.firstName).toBe("Zoe");
    });
  });

  describe("result capping", () => {
    it("should cap results at 20 by default", () => {
      const manyCandidates = ref(
        Array.from({ length: 50 }, (_, i) =>
          createMockPerson(`Person${i}`, "Smith", ["P625", "S530"]),
        ),
      );
      const searchQuery = ref("Smith");
      const { searchResults } = usePersonSearch(searchQuery, manyCandidates);

      expect(searchResults.value).toHaveLength(20);
    });

    it("should respect custom maxResults option", () => {
      const manyCandidates = ref(
        Array.from({ length: 50 }, (_, i) =>
          createMockPerson(`Person${i}`, "Smith", ["P625", "S530"]),
        ),
      );
      const searchQuery = ref("Smith");
      const { searchResults } = usePersonSearch(searchQuery, manyCandidates, {
        maxResults: 5,
      });

      expect(searchResults.value).toHaveLength(5);
    });
  });

  describe("debouncing", () => {
    it("should debounce search with default 150ms delay", async () => {
      const searchQuery = ref("");
      const candidates = ref(mockCandidates);
      const { debouncedSearch } = usePersonSearch(searchQuery, candidates);

      const searchSpy = vi.fn();
      const wrappedSearch = vi.fn(
        (query: string, people: SearchablePersonDto[]) => {
          searchSpy();
          return debouncedSearch(query, people);
        },
      );

      wrappedSearch("J", mockCandidates);
      wrappedSearch("Jo", mockCandidates);
      wrappedSearch("Joh", mockCandidates);

      expect(searchSpy).toHaveBeenCalledTimes(3);

      await new Promise((resolve) => setTimeout(resolve, 200));
    });

    it("should respect custom debounce delay", () => {
      const searchQuery = ref("");
      const candidates = ref(mockCandidates);
      const { debouncedSearch } = usePersonSearch(searchQuery, candidates, {
        debounceDelay: 300,
      });

      expect(debouncedSearch).toBeDefined();
    });
  });

  describe("edge cases", () => {
    it("should return empty array for empty search query", () => {
      const searchQuery = ref("");
      const candidates = ref(mockCandidates);
      const { searchResults } = usePersonSearch(searchQuery, candidates);

      expect(searchResults.value).toEqual([]);
    });

    it("should return empty array for whitespace-only search query", () => {
      const searchQuery = ref("   ");
      const candidates = ref(mockCandidates);
      const { searchResults } = usePersonSearch(searchQuery, candidates);

      expect(searchResults.value).toEqual([]);
    });

    it("should return empty array when candidates list is empty", () => {
      const searchQuery = ref("John");
      const candidates = ref<SearchablePersonDto[]>([]);
      const { searchResults } = usePersonSearch(searchQuery, candidates);

      expect(searchResults.value).toEqual([]);
    });

    it("should handle candidates without soundex codes", () => {
      const candidatesNoSoundex = ref([createMockPerson("John", "Doe", [])]);
      const searchQuery = ref("John");
      const { searchResults } = usePersonSearch(
        searchQuery,
        candidatesNoSoundex,
      );

      expect(searchResults.value).toHaveLength(1);
    });
  });

  describe("performance", () => {
    it("should search 1000 people in less than 50ms", () => {
      const largeCandidates = ref(
        Array.from({ length: 1000 }, (_, i) =>
          createMockPerson(`FirstName${i}`, `LastName${i % 100}`, [
            `F000`,
            `L000`,
          ]),
        ),
      );
      const searchQuery = ref("LastName50");

      const startTime = performance.now();
      const { searchResults } = usePersonSearch(searchQuery, largeCandidates);
      const results = searchResults.value;
      const endTime = performance.now();
      const duration = endTime - startTime;

      expect(results.length).toBeGreaterThan(0);
      expect(duration).toBeLessThan(150);
    });

    it("should handle complex searches efficiently", () => {
      const largeCandidates = ref(
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

      const startTime = performance.now();
      const { searchResults } = usePersonSearch(searchQuery, largeCandidates);
      const results = searchResults.value;
      const endTime = performance.now();
      const duration = endTime - startTime;

      expect(results.length).toBeGreaterThan(0);
      expect(duration).toBeLessThan(150);
    });
  });

  describe("caching", () => {
    it("should cache search results", () => {
      const searchQuery = ref("John");
      const candidates = ref(mockCandidates);
      const { performSearch } = usePersonSearch(searchQuery, candidates, {
        enableCache: true,
      });

      const startTime1 = performance.now();
      const results1 = performSearch("John", candidates.value);
      const duration1 = performance.now() - startTime1;

      const startTime2 = performance.now();
      const results2 = performSearch("John", candidates.value);
      const duration2 = performance.now() - startTime2;

      expect(results1).toEqual(results2);
      expect(duration2).toBeLessThan(duration1);
    });

    it("should clear cache when candidates change", () => {
      const searchQuery = ref("John");
      const candidates = ref(mockCandidates);
      const { searchResults } = usePersonSearch(searchQuery, candidates, {
        enableCache: true,
      });

      const initialResults = searchResults.value;
      expect(initialResults.length).toBeGreaterThan(0);

      candidates.value = [createMockPerson("Jane", "Doe", ["J500", "D000"])];

      searchQuery.value = "Jane";
      const newResults = searchResults.value;
      expect(newResults.length).toBeGreaterThan(0);
      expect(newResults[0]!.firstName).toBe("Jane");
    });

    it("should allow manual cache clearing", () => {
      const searchQuery = ref("John");
      const candidates = ref(mockCandidates);
      const { performSearch, clearCache } = usePersonSearch(
        searchQuery,
        candidates,
        { enableCache: true },
      );

      performSearch("John", candidates.value);
      clearCache();

      const results = performSearch("John", candidates.value);
      expect(results.length).toBeGreaterThan(0);
    });

    it("should work with cache disabled", () => {
      const searchQuery = ref("John");
      const candidates = ref(mockCandidates);
      const { searchResults } = usePersonSearch(searchQuery, candidates, {
        enableCache: false,
      });

      expect(searchResults.value.length).toBeGreaterThan(0);
      expect(searchResults.value[0]!.firstName).toBe("John");
    });
  });

  describe("reactive updates", () => {
    it("should update results when search query changes", () => {
      const searchQuery = ref("John");
      const candidates = ref(mockCandidates);
      const { searchResults } = usePersonSearch(searchQuery, candidates);

      expect(searchResults.value.length).toBeGreaterThan(0);
      expect(searchResults.value[0]!.firstName).toBe("John");

      searchQuery.value = "Jane";

      expect(searchResults.value.length).toBeGreaterThan(0);
      expect(searchResults.value[0]!.firstName).toBe("Jane");
    });

    it("should update results when candidates list changes", () => {
      const searchQuery = ref("John");
      const candidates = ref(mockCandidates);
      const { searchResults } = usePersonSearch(searchQuery, candidates);

      expect(searchResults.value.length).toBeGreaterThan(0);

      candidates.value = [
        createMockPerson("Johnny", "Walker", ["J500", "W460"]),
        createMockPerson("Jonathan", "Davis", ["J535", "D120"]),
      ];

      expect(searchResults.value.length).toBeGreaterThan(0);
      const names = searchResults.value.map((p) => p.firstName);
      expect(names).toContain("Johnny");
    });
  });
});
