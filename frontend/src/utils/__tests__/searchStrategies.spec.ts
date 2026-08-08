import type { SearchablePersonDto } from "@/types/Person";
import { describe, expect, it } from "vitest";
import {
  applyAllStrategies,
  exactMatch,
  matchesFrontDeskVoterSearch,
  multiTokenCoverageMatch,
  normalizeSearchText,
  phoneticMatch,
  tokenizeNameForSearch,
  wordBoundaryMatch,
} from "../searchStrategies";

function createMockPerson(
  firstName: string,
  lastName: string,
  otherNames = "",
  otherLastNames = "",
  soundexCodes: string[] = [],
  otherInfo = "",
  area = "",
): SearchablePersonDto {
  const searchText = [
    firstName,
    lastName,
    otherNames,
    otherLastNames,
    otherInfo,
  ]
    .filter(Boolean)
    .join(" ");
  return {
    personGuid: "test-guid",
    firstName,
    lastName,
    fullName: `${firstName} ${lastName}`,
    otherNames,
    otherLastNames,
    otherInfo,
    area,
    combinedSoundCodes: soundexCodes.join(","),
    voteCount: 0,
    _searchText: searchText,
    _soundexCodes: soundexCodes,
  };
}

describe("matchesFrontDeskVoterSearch", () => {
  const smithAnthony = {
    fullName: "Smith, Anthony",
    bahaiId: "12345",
    area: "North",
  };

  it("matches a single substring within the full name", () => {
    expect(matchesFrontDeskVoterSearch(smithAnthony, "mit")).toBe(true);
  });

  it("matches multiple name terms against separate name parts", () => {
    expect(matchesFrontDeskVoterSearch(smithAnthony, "sm an")).toBe(true);
  });

  it("requires every term to match", () => {
    expect(matchesFrontDeskVoterSearch(smithAnthony, "sm he")).toBe(false);
  });

  it("matches bahai id for single-term searches", () => {
    expect(matchesFrontDeskVoterSearch(smithAnthony, "12345")).toBe(true);
  });

  it("does not match area alone", () => {
    expect(matchesFrontDeskVoterSearch(smithAnthony, "north")).toBe(false);
  });

  it("allows area as a secondary term when a primary field also matches", () => {
    expect(matchesFrontDeskVoterSearch(smithAnthony, "smith north")).toBe(true);
    expect(matchesFrontDeskVoterSearch(smithAnthony, "north anthony")).toBe(
      true,
    );
  });

  it("matches otherInfo as a primary field", () => {
    const voter = {
      fullName: "Smith, Anthony",
      otherInfo: "choir member",
      area: "North",
    };
    expect(matchesFrontDeskVoterSearch(voter, "choir")).toBe(true);
    expect(matchesFrontDeskVoterSearch(voter, "smith choir")).toBe(true);
  });

  it("tokenizes comma-separated names for multi-term search", () => {
    expect(tokenizeNameForSearch("Smith, Anthony")).toEqual([
      "smith",
      "anthony",
    ]);
  });

  it("handles voters with a missing full name", () => {
    expect(matchesFrontDeskVoterSearch({ fullName: undefined }, "smith")).toBe(
      false,
    );
    expect(matchesFrontDeskVoterSearch({ fullName: undefined }, "")).toBe(true);
  });
});

describe("area is not used in ballot ranking", () => {
  it("should not match on area alone via applyAllStrategies", () => {
    // Use an area token that will not fuzzy-match name tokens (e.g. "North" ≈ "Smith").
    const person = createMockPerson(
      "John",
      "Smith",
      "",
      "",
      [],
      "",
      "North District",
    );
    person._searchText = "John Smith";
    expect(applyAllStrategies("District", person)).toBeNull();
  });

  it("should not change weight when area matches a search term", () => {
    const person = createMockPerson("John", "Smith", "", "", [], "", "North");
    person._searchText = "John Smith";
    const withoutArea = applyAllStrategies("Smith", {
      ...person,
      area: "",
    });
    const withArea = applyAllStrategies("Smith", person);
    expect(withoutArea).not.toBeNull();
    expect(withArea).not.toBeNull();
    expect(withArea!.weight).toBe(withoutArea!.weight);
  });
});

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

describe("multiTokenCoverageMatch", () => {
  it("should rank full coverage above partial coverage (glenn li)", () => {
    const littleGlen = createMockPerson("Glen", "Little");
    const leeLinda = createMockPerson("Linda", "Lee");

    const littleScore = multiTokenCoverageMatch("glenn li", littleGlen);
    const leeScore = multiTokenCoverageMatch("glenn li", leeLinda);

    expect(littleScore).not.toBeNull();
    expect(leeScore).not.toBeNull();
    expect(littleScore!).toBeGreaterThan(leeScore!);
  });

  it("should return null for single-term queries", () => {
    const person = createMockPerson("Glen", "Little");
    expect(multiTokenCoverageMatch("glen", person)).toBeNull();
  });
});

describe("exactMatch", () => {
  it("should return 100 for exact match", () => {
    const person = createMockPerson("John", "Smith");
    expect(exactMatch("John Smith", person)).toBe(100);
  });

  it("should handle diacritics", () => {
    const person = createMockPerson("José", "García");
    person._searchText = "José García";
    expect(exactMatch("Jose Garcia", person)).toBe(100);
  });
});

describe("wordBoundaryMatch", () => {
  it("should return 85 for word boundary match", () => {
    const person = createMockPerson("John", "Smith");
    expect(wordBoundaryMatch("J S", person)).toBe(85);
  });
});

describe("phoneticMatch", () => {
  it("should match names that sound alike (Mary / Mehri both M600)", () => {
    const person = createMockPerson("Mary", "Smith");
    person._soundexCodes = [];
    expect(phoneticMatch("Mehri", person)).toBe(75);
  });

  it("should generate Soundex from name tokens when precomputed codes are empty", () => {
    const person = createMockPerson("John", "Smith");
    person._soundexCodes = [];
    expect(phoneticMatch("Jon", person)).toBe(75);
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
});
