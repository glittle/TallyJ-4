import { beforeEach, describe, expect, it } from "vitest";
import {
  electionGuidFromKioskVoterId,
  readRememberedVoterElection,
  readRouteElectionId,
  rememberVoterElection,
} from "../voterElectionContext";

const GUID = "f3f1ef0b-1d76-4117-82f4-b72b99a28fb1";
const HEX = "f3f1ef0b1d76411782f4b72b99a28fb1";

describe("voterElectionContext", () => {
  beforeEach(() => {
    sessionStorage.clear();
  });

  it("reads a route election id and ignores empty placeholders", () => {
    expect(readRouteElectionId(GUID)).toBe(GUID);
    expect(readRouteElectionId(["election-1"])).toBe("election-1");
    expect(readRouteElectionId("")).toBe("");
    expect(readRouteElectionId("undefined")).toBe("");
    expect(readRouteElectionId("  null  ")).toBe("");
    expect(readRouteElectionId(undefined)).toBe("");
  });

  it("parses the election embedded in a kiosk voter id", () => {
    expect(electionGuidFromKioskVoterId(`SMART.${HEX}`)).toBe(GUID);
    expect(electionGuidFromKioskVoterId(`smart.${HEX.toUpperCase()}`)).toBe(
      GUID,
    );
    expect(electionGuidFromKioskVoterId("voter@example.com")).toBe("");
    expect(electionGuidFromKioskVoterId("SMART")).toBe("");
    expect(electionGuidFromKioskVoterId(`SMART.${HEX.slice(0, 31)}`)).toBe("");
  });

  it("remembers an election id across a reload of this tab", () => {
    expect(readRememberedVoterElection()).toBe("");
    rememberVoterElection(GUID);
    expect(readRememberedVoterElection()).toBe(GUID);
  });
});
