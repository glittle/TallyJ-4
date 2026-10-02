import { describe, expect, it } from "vitest";
import { peopleCountFromBody } from "../peopleImportService";

describe("peopleCountFromBody", () => {
  it("reads the { count } object the people-count action returns", () => {
    expect(peopleCountFromBody({ count: 12 })).toBe(12);
  });

  it("reads a bare number when the declared integer contract is honored", () => {
    expect(peopleCountFromBody(7)).toBe(7);
  });

  it("returns 0 for an empty or unexpected body", () => {
    expect(peopleCountFromBody(undefined)).toBe(0);
    expect(peopleCountFromBody({ count: "12" })).toBe(0);
  });
});
