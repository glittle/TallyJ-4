import { describe, expect, it } from "vitest";
import {
  electionSupportsKiosk,
  getVotingMethodLabel,
  isRecordedOtherThanOnline,
  parseElectionVotingMethods,
  parseFrontDeskCheckInMethods,
} from "../votingMethodLabels";

const t = ((key: string) => key) as any;

describe("votingMethodLabels", () => {
  it("maps Person.VotingMethod letters, not Imported as In Person", () => {
    expect(getVotingMethodLabel("P", t)).toBe("frontDesk.votingMethod.inPerson");
    expect(getVotingMethodLabel("I", t)).toBe("frontDesk.votingMethod.imported");
    expect(getVotingMethodLabel("D", t)).toBe("people.votingMethod.droppedOff");
    expect(getVotingMethodLabel("K", t)).toBe("people.votingMethod.kiosk");
  });

  it("parses concatenated letters and comma-separated aliases", () => {
    expect(parseElectionVotingMethods("PDM")).toEqual(["P", "M", "D"]);
    expect(parseElectionVotingMethods("IP,OL")).toEqual(["P", "O"]);
    expect(parseElectionVotingMethods("PMDKO")).toEqual([
      "P",
      "M",
      "D",
      "K",
      "O",
    ]);
    expect(parseElectionVotingMethods(null)).toEqual(["P", "M", "D"]);
  });

  it("treats paper, mailed, dropped off, and kiosk as other-than-online", () => {
    expect(isRecordedOtherThanOnline("P")).toBe(true);
    expect(isRecordedOtherThanOnline("M")).toBe(true);
    expect(isRecordedOtherThanOnline("D")).toBe(true);
    expect(isRecordedOtherThanOnline("K")).toBe(true);
    expect(isRecordedOtherThanOnline("O")).toBe(false);
    expect(isRecordedOtherThanOnline(undefined)).toBe(false);
  });

  it("detects kiosk from election VotingMethods", () => {
    expect(electionSupportsKiosk("PMDK")).toBe(true);
    expect(electionSupportsKiosk("IP,K")).toBe(true);
    expect(electionSupportsKiosk("PDM")).toBe(false);
  });

  it("omits Online from Front Desk check-in methods", () => {
    expect(parseFrontDeskCheckInMethods("PMDKO")).toEqual([
      "P",
      "M",
      "D",
      "K",
    ]);
    expect(parseFrontDeskCheckInMethods("IP,OL")).toEqual(["P"]);
  });
});
