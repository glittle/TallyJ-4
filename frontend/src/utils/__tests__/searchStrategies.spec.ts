import { describe, it, expect } from "vitest";
import {
  normalizeSearchText,
  calculateLevenshteinDistance,
  compareSoundexCodes,
  exactMatch,
  prefixMatch,
  wordBoundaryMatch,
  substringMatch,
  otherNamesMatch,
  phoneticMatch,
  fuzzyMatch,
  applyAllStrategies,
} from "../searchStrategies";
import type { SearchablePersonDto } from "@/types/Person";

function createMockPerson(
  firstName: string,
  lastName: string,
  otherNames = "",
  otherLastNames = "",
  soundexCodes: string[] = [],
): SearchablePersonDto {
  return {
    personGuid: "test-guid",
    firstName,
    lastName,
    fullName: `${firstName} ${lastName}`,
    otherNames,
    otherLastNames,
    otherInfo: "",
    combinedSoundCodes: soundexCodes.join(","),
    voteCount: 0,
    _searchText: `${firstName} ${lastName}`,
    _soundexCodes: soundexCodes,
  };
}

describe("normalizeSearchText", () => {
  it("should convert to lowercase", () => {
    expect(normalizeSearchText("HELLO")).toBe("hello");
  });

  it("should trim whitespace", () => {
    expect(normalizeSearchText("  hello  ")).toBe("hello");
  });

  it("should collapse multiple spaces", () => {
    expect(normalizeSearchText("hello    world")).toBe("hello world");
  });

  it("should remove diacritics", () => {
    expect(normalizeSearchText("café")).toBe("cafe");
    expect(normalizeSearchText("naïve")).toBe("naive");
  });

  it("should handle empty strings", () => {
    expect(normalizeSearchText("")).toBe("");
  });

  it("should handle unicode characters", () => {
    expect(normalizeSearchText("Ñoño")).toBe("nono");
  });
});

describe("calculateLevenshteinDistance", () => {
  it("should return 0 for identical strings", () => {
    expect(calculateLevenshteinDistance("hello", "hello")).toBe(0);
  });

  it("should return length of second string when first is empty", () => {
    expect(calculateLevenshteinDistance("", "hello")).toBe(5);
  });

  it("should return length of first string when second is empty", () => {
    expect(calculateLevenshteinDistance("hello", "")).toBe(5);
  });

  it("should calculate distance for single character difference", () => {
    expect(calculateLevenshteinDistance("cat", "bat")).toBe(1);
  });

  it("should calculate distance for insertion", () => {
    expect(calculateLevenshteinDistance("cat", "cats")).toBe(1);
  });

  it("should calculate distance for deletion", () => {
    expect(calculateLevenshteinDistance("cats", "cat")).toBe(1);
  });

  it("should calculate distance for multiple changes", () => {
    expect(calculateLevenshteinDistance("kitten", "sitting")).toBe(3);
  });

  it("should handle completely different strings", () => {
    expect(calculateLevenshteinDistance("abc", "xyz")).toBe(3);
  });
});

describe("compareSoundexCodes", () => {
  it("should return 0 for empty arrays", () => {
    expect(compareSoundexCodes([], [])).toBe(0);
    expect(compareSoundexCodes(["A100"], [])).toBe(0);
    expect(compareSoundexCodes([], ["A100"])).toBe(0);
  });

  it("should return 100 for identical single codes", () => {
    expect(compareSoundexCodes(["A100"], ["A100"])).toBe(100);
  });

  it("should return 0 for completely different codes", () => {
    expect(compareSoundexCodes(["A100"], ["B200"])).toBe(0);
  });

  it("should calculate partial match for multiple codes", () => {
    expect(compareSoundexCodes(["A100", "B200"], ["A100", "C300"])).toBe(50);
  });

  it("should handle different array lengths", () => {
    expect(compareSoundexCodes(["A100"], ["A100", "B200"])).toBe(50);
  });

  it("should handle multiple matches", () => {
    const codes1 = ["A100", "B200", "C300"];
    const codes2 = ["A100", "B200", "C300"];
    expect(compareSoundexCodes(codes1, codes2)).toBe(100);
  });
});

describe("exactMatch", () => {
  it("should return 100 for exact match", () => {
    const person = createMockPerson("John", "Smith");
    expect(exactMatch("John Smith", person)).toBe(100);
  });

  it("should be case insensitive", () => {
    const person = createMockPerson("John", "Smith");
    expect(exactMatch("john smith", person)).toBe(100);
    expect(exactMatch("JOHN SMITH", person)).toBe(100);
  });

  it("should handle extra whitespace", () => {
    const person = createMockPerson("John", "Smith");
    expect(exactMatch("  John   Smith  ", person)).toBe(100);
  });

  it("should return null for non-match", () => {
    const person = createMockPerson("John", "Smith");
    expect(exactMatch("Jane Doe", person)).toBeNull();
  });

  it("should handle diacritics", () => {
    const person = createMockPerson("José", "García");
    person._searchText = "José García";
    expect(exactMatch("Jose Garcia", person)).toBe(100);
  });
});

describe("prefixMatch", () => {
  it("should return 90 for prefix match", () => {
    const person = createMockPerson("John", "Smith");
    expect(prefixMatch("John", person)).toBe(90);
  });

  it("should match partial first name", () => {
    const person = createMockPerson("Jonathan", "Smith");
    expect(prefixMatch("Jon", person)).toBe(90);
  });

  it("should be case insensitive", () => {
    const person = createMockPerson("John", "Smith");
    expect(prefixMatch("john", person)).toBe(90);
  });

  it("should return null for non-prefix", () => {
    const person = createMockPerson("John", "Smith");
    expect(prefixMatch("Smith", person)).toBeNull();
  });

  it("should return null for empty search", () => {
    const person = createMockPerson("John", "Smith");
    expect(prefixMatch("", person)).toBeNull();
  });
});

describe("wordBoundaryMatch", () => {
  it("should return 85 for word boundary match", () => {
    const person = createMockPerson("John", "Smith");
    expect(wordBoundaryMatch("J S", person)).toBe(85);
  });

  it("should match multiple word prefixes", () => {
    const person = createMockPerson("Jonathan", "Smith");
    expect(wordBoundaryMatch("Jon Sm", person)).toBe(85);
  });

  it("should match first and last name separately", () => {
    const person = createMockPerson("Mary", "Jane");
    expect(wordBoundaryMatch("M J", person)).toBe(85);
  });

  it("should return null when not all words match", () => {
    const person = createMockPerson("John", "Smith");
    expect(wordBoundaryMatch("John Doe", person)).toBeNull();
  });

  it("should handle single word search", () => {
    const person = createMockPerson("John", "Smith");
    expect(wordBoundaryMatch("John", person)).toBe(85);
  });

  it("should return null for empty search", () => {
    const person = createMockPerson("John", "Smith");
    expect(wordBoundaryMatch("", person)).toBeNull();
  });
});

describe("substringMatch", () => {
  it("should return 80 for substring match", () => {
    const person = createMockPerson("Jonathan", "Smith");
    expect(substringMatch("athan", person)).toBe(80);
  });

  it("should match middle of name", () => {
    const person = createMockPerson("Alexander", "McFarland");
    expect(substringMatch("alex", person)).toBe(80);
  });

  it("should be case insensitive", () => {
    const person = createMockPerson("John", "Smith");
    expect(substringMatch("OHN", person)).toBe(80);
  });

  it("should return null for non-match", () => {
    const person = createMockPerson("John", "Smith");
    expect(substringMatch("xyz", person)).toBeNull();
  });

  it("should match last name substring", () => {
    const person = createMockPerson("John", "Smith");
    expect(substringMatch("mit", person)).toBe(80);
  });
});

describe("otherNamesMatch", () => {
  it("should return 70 for otherNames match", () => {
    const person = createMockPerson("John", "Smith", "Johnny, Jack");
    expect(otherNamesMatch("Johnny", person)).toBe(70);
  });

  it("should match otherLastNames", () => {
    const person = createMockPerson("John", "Smith", "", "Smythe, Smithson");
    expect(otherNamesMatch("Smythe", person)).toBe(70);
  });

  it("should be case insensitive", () => {
    const person = createMockPerson("John", "Smith", "Johnny");
    expect(otherNamesMatch("johnny", person)).toBe(70);
  });

  it("should return null when no other names", () => {
    const person = createMockPerson("John", "Smith");
    expect(otherNamesMatch("Johnny", person)).toBeNull();
  });

  it("should return null for non-match in other names", () => {
    const person = createMockPerson("John", "Smith", "Johnny");
    expect(otherNamesMatch("Jack", person)).toBeNull();
  });

  it("should handle partial matches in otherNames", () => {
    const person = createMockPerson("John", "Smith", "Jonathan");
    expect(otherNamesMatch("Jon", person)).toBe(70);
  });
});

describe("phoneticMatch", () => {
  it("should return null for searches less than 3 characters", () => {
    const person = createMockPerson("John", "Smith", "", "", ["J500", "S530"]);
    expect(phoneticMatch("Jo", person)).toBeNull();
  });

  it("should return null when person has no soundex codes", () => {
    const person = createMockPerson("John", "Smith");
    person._soundexCodes = [];
    expect(phoneticMatch("John", person)).toBeNull();
  });

  it("should return 75 for high similarity (>= 75%)", () => {
    const person = createMockPerson("John", "Smith", "", "", ["J500", "S530"]);
    expect(phoneticMatch("Jon Smyth", person)).toBe(75);
  });

  it("should return 65 for medium similarity (>= 50%)", () => {
    const person = createMockPerson("Alexander", "McFarland", "", "", [
      "A425",
      "M216",
    ]);
    expect(phoneticMatch("Alexander", person)).toBe(65);
  });

  it("should return 65 for medium similarity (50% match)", () => {
    const person = createMockPerson("John", "Smith", "", "", ["J500", "S530"]);
    expect(phoneticMatch("John Doe", person)).toBe(65);
  });

  it("should return null for no phonetic similarity", () => {
    const person = createMockPerson("John", "Smith", "", "", ["J500", "S530"]);
    expect(phoneticMatch("Alexander McFarland", person)).toBeNull();
  });
});

describe("fuzzyMatch", () => {
  it("should return null for searches less than 3 characters", () => {
    const person = createMockPerson("John", "Smith");
    expect(fuzzyMatch("Jo", person)).toBeNull();
  });

  it("should return 50 for Levenshtein distance <= 2", () => {
    const person = createMockPerson("John", "Smith");
    expect(fuzzyMatch("Jon Smith", person)).toBe(50);
  });

  it("should match with single character typo", () => {
    const person = createMockPerson("Alexander", "McFarland");
    expect(fuzzyMatch("Alexnder McFarland", person)).toBe(50);
  });

  it("should match individual words with typos", () => {
    const person = createMockPerson("Jonathan", "Smith");
    expect(fuzzyMatch("Jonathon", person)).toBe(50);
  });

  it("should return null for distance > 2", () => {
    const person = createMockPerson("John", "Smith");
    expect(fuzzyMatch("Jane Doe", person)).toBeNull();
  });

  it("should handle transpositions", () => {
    const person = createMockPerson("Smith", "John");
    expect(fuzzyMatch("Smtih John", person)).toBe(50);
  });
});

describe("applyAllStrategies", () => {
  it("should return highest weight match", () => {
    const person = createMockPerson("John", "Smith");
    const result = applyAllStrategies("John Smith", person);
    expect(result).not.toBeNull();
    expect(result?.weight).toBe(100);
    expect(result?.matchedStrategy).toBe("exact");
  });

  it("should prefer exact over prefix", () => {
    const person = createMockPerson("John", "Smith");
    const result = applyAllStrategies("John Smith", person);
    expect(result?.matchedStrategy).toBe("exact");
  });

  it("should fall back to prefix when exact fails", () => {
    const person = createMockPerson("Jonathan", "Smith");
    const result = applyAllStrategies("Jon", person);
    expect(result?.weight).toBe(90);
    expect(result?.matchedStrategy).toBe("prefix");
  });

  it("should return null when no strategy matches", () => {
    const person = createMockPerson("John", "Smith");
    const result = applyAllStrategies("xyz", person);
    expect(result).toBeNull();
  });

  it("should include person in result", () => {
    const person = createMockPerson("John", "Smith");
    const result = applyAllStrategies("John", person);
    expect(result?.person).toBe(person);
  });

  it("should apply phonetic match when other strategies fail", () => {
    const person = createMockPerson("McFarland", "Alexander", "", "", [
      "M216",
      "A425",
    ]);
    const result = applyAllStrategies("Macfarland", person);
    expect(result).not.toBeNull();
    expect(result?.matchedStrategy).toBe("phonetic");
  });

  it("should apply fuzzy match for typos", () => {
    const person = createMockPerson("Smith", "John");
    person._searchText = "Smith John";
    const result = applyAllStrategies("Smtih John", person);
    expect(result).not.toBeNull();
    expect(result?.matchedStrategy).toBe("fuzzy");
  });
});

describe("Edge Cases", () => {
  it("should handle empty search terms gracefully", () => {
    const person = createMockPerson("John", "Smith");
    expect(exactMatch("", person)).toBeNull();
    expect(prefixMatch("", person)).toBeNull();
    expect(wordBoundaryMatch("", person)).toBeNull();
  });

  it("should handle special characters", () => {
    const person = createMockPerson("O'Brien", "Smith-Jones");
    person._searchText = "O'Brien Smith-Jones";
    expect(exactMatch("O'Brien Smith-Jones", person)).toBe(100);
  });

  it("should handle very long names", () => {
    const person = createMockPerson(
      "Wolfeschlegelsteinhausenbergerdorff",
      "Jr",
    );
    person._searchText = "Wolfeschlegelsteinhausenbergerdorff Jr";
    expect(prefixMatch("Wolf", person)).toBe(90);
  });

  it("should handle single character names", () => {
    const person = createMockPerson("A", "B");
    person._searchText = "A B";
    expect(exactMatch("A B", person)).toBe(100);
  });

  it("should handle numeric characters in names", () => {
    const person = createMockPerson("John", "Smith3");
    person._searchText = "John Smith3";
    expect(exactMatch("John Smith3", person)).toBe(100);
  });
});

describe("Performance", () => {
  it("should execute each strategy in < 5ms", () => {
    const person = createMockPerson("Alexander", "McFarland", "", "", [
      "A425",
      "M216",
    ]);

    const strategies = [
      { name: "exact", fn: exactMatch },
      { name: "prefix", fn: prefixMatch },
      { name: "wordBoundary", fn: wordBoundaryMatch },
      { name: "substring", fn: substringMatch },
      { name: "otherNames", fn: otherNamesMatch },
      { name: "phonetic", fn: phoneticMatch },
      { name: "fuzzy", fn: fuzzyMatch },
    ];

    for (const strategy of strategies) {
      const start = performance.now();
      strategy.fn("Alexander", person);
      const end = performance.now();
      expect(end - start).toBeLessThan(5);
    }
  });

  it("should execute applyAllStrategies in < 5ms", () => {
    const person = createMockPerson("Alexander", "McFarland", "", "", [
      "A425",
      "M216",
    ]);

    const start = performance.now();
    applyAllStrategies("Alexander", person);
    const end = performance.now();

    expect(end - start).toBeLessThan(5);
  });
});
