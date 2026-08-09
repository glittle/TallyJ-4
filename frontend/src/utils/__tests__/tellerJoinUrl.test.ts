import { describe, expect, it } from "vitest";
import {
  buildTellerJoinUrl,
  compactElectionGuid,
  expandElectionGuid,
  isElectionGuid,
  parseTellerJoinRoute,
} from "../tellerJoinUrl";

const GUID = "a1b2c3d4-e5f6-7890-abcd-ef1234567890";
const COMPACT = "a1b2c3d4e5f67890abcdef1234567890";

describe("tellerJoinUrl", () => {
  it("compacts and expands GUIDs", () => {
    expect(compactElectionGuid(GUID)).toBe(COMPACT);
    expect(expandElectionGuid(COMPACT)).toBe(GUID);
    expect(expandElectionGuid(GUID)).toBe(GUID);
    expect(expandElectionGuid("not-a-guid")).toBeNull();
    expect(isElectionGuid(COMPACT)).toBe(true);
    expect(isElectionGuid("secret")).toBe(false);
  });

  it("builds a short shareable URL with query passcode", () => {
    const url = buildTellerJoinUrl("https://example.com", GUID, "My Code!");
    expect(url).toBe(`https://example.com/teller-join/${COMPACT}?c=My%20Code!`);
  });

  it("parses compact guid + query code", () => {
    expect(
      parseTellerJoinRoute({
        params: { accessCode: COMPACT },
        query: { c: "secret" },
      }),
    ).toEqual({ electionGuid: GUID, accessCode: "secret" });
  });

  it("parses legacy path accessCode + guid", () => {
    expect(
      parseTellerJoinRoute({
        params: { accessCode: "plain-code", electionGuid: GUID },
        query: {},
      }),
    ).toEqual({ electionGuid: GUID, accessCode: "plain-code" });
  });

  it("prefers query code over path access code", () => {
    expect(
      parseTellerJoinRoute({
        params: { accessCode: "path-code", electionGuid: GUID },
        query: { code: "query-code" },
      }),
    ).toEqual({ electionGuid: GUID, accessCode: "query-code" });
  });
});
