import { describe, expect, it } from "vitest";
import {
  formatBallotDisplayCode,
  formatComputerCodeLabel,
  isImportedComputerCode,
  isOnlineComputerCode,
  isOnlineOrImportedComputerCode,
} from "../ballotDisplayCode";

const t = (key: string, values?: Record<string, unknown>) => {
  if (key === "ballots.onlineCode") {
    return `Online ${values?.n}`;
  }
  if (key === "ballots.importedCode") {
    return `Imported ${values?.n}`;
  }
  if (key === "ballots.onlineComputer") {
    return "Online";
  }
  if (key === "ballots.importedComputer") {
    return "Imported";
  }
  return key;
};

describe("ballotDisplayCode", () => {
  it("recognizes reserved online and imported codes including leftovers", () => {
    expect(isOnlineComputerCode("OL")).toBe(true);
    expect(isOnlineComputerCode("ww")).toBe(true);
    expect(isImportedComputerCode("IM")).toBe(true);
    expect(isImportedComputerCode("IMPORT")).toBe(true);
    expect(isOnlineComputerCode("A")).toBe(false);
    expect(isOnlineOrImportedComputerCode("OL")).toBe(true);
    expect(isOnlineOrImportedComputerCode("IM")).toBe(true);
    expect(isOnlineOrImportedComputerCode("A")).toBe(false);
  });

  it("formats online and imported ballots as words plus number", () => {
    expect(
      formatBallotDisplayCode(t, {
        computerCode: "WW",
        ballotNumAtComputer: 3,
        ballotCode: "WW3",
      }),
    ).toBe("Online 3");
    expect(
      formatBallotDisplayCode(t, {
        computerCode: "IM",
        ballotNumAtComputer: 12,
        ballotCode: "IM12",
      }),
    ).toBe("Imported 12");
    expect(
      formatBallotDisplayCode(t, {
        computerCode: "A",
        ballotNumAtComputer: 4,
        ballotCode: "A4",
      }),
    ).toBe("A4");
  });

  it("formats computer filter labels", () => {
    expect(formatComputerCodeLabel(t, "OL")).toBe("Online");
    expect(formatComputerCodeLabel(t, "IM")).toBe("Imported");
    expect(formatComputerCodeLabel(t, "B")).toBe("B");
  });
});
