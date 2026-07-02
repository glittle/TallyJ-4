import { describe, expect, it } from "vitest";
import { getBallotStatusLabel } from "../ballotStatusLabel";

const t = (key: string) => {
  const labels: Record<string, string> = {
    "ballots.statusValue.New": "New",
    "ballots.statusValue.Empty": "Empty",
    "ballots.statusValue.Ok": "Ok",
    "ballots.statusValue.Review": "Review",
  };
  return labels[key] ?? key;
};

describe("getBallotStatusLabel", () => {
  it("shows New for Empty status during a new session", () => {
    expect(getBallotStatusLabel(t, "Empty", { isNewSession: true })).toBe(
      "New",
    );
  });

  it("shows Empty for Empty status on return visits", () => {
    expect(getBallotStatusLabel(t, "Empty")).toBe("Empty");
    expect(getBallotStatusLabel(t, "Empty", { isNewSession: false })).toBe(
      "Empty",
    );
  });

  it("shows the mapped label for other statuses", () => {
    expect(getBallotStatusLabel(t, "Review")).toBe("Review");
    expect(getBallotStatusLabel(t, "Ok", { isNewSession: true })).toBe("Ok");
  });
});
