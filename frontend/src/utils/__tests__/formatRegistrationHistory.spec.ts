import { describe, it, expect } from "vitest";
import {
  parseRegistrationHistory,
  formatRegistrationHistoryDetails,
} from "../formatRegistrationHistory";

describe("parseRegistrationHistory", () => {
  it("parses PascalCase entries from stored JSON", () => {
    const json = JSON.stringify([
      {
        Timestamp: "2024-01-15T10:30:00Z",
        Action: "CheckedIn",
        VotingMethod: "InPerson",
        Teller1: "Alice",
      },
    ]);

    const entries = parseRegistrationHistory(json);

    expect(entries).toHaveLength(1);
    expect(entries[0].timestamp).toBe("2024-01-15T10:30:00Z");
    expect(entries[0].action).toBe("CheckedIn");
    expect(entries[0].votingMethod).toBe("InPerson");
    expect(entries[0].teller1).toBe("Alice");
  });

  it("returns empty array for invalid JSON", () => {
    expect(parseRegistrationHistory("not-json")).toEqual([]);
    expect(parseRegistrationHistory(null)).toEqual([]);
  });
});

describe("formatRegistrationHistoryDetails", () => {
  it("formats checked-in entries", () => {
    const result = formatRegistrationHistoryDetails(
      {
        timestamp: "2024-01-15T10:30:00Z",
        action: "CheckedIn",
        votingMethod: "InPerson",
      },
      {
        t: (key: string) => {
          if (key === "frontDesk.history.checkedIn") {
            return "Checked in via InPerson";
          }
          return key;
        },
      },
    );

    expect(result).toContain("Checked in via InPerson");
  });
});
