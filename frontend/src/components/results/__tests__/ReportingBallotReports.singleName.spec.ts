import { mount } from "@vue/test-utils";
import { describe, expect, it } from "vitest";
import { i18n } from "@/test/setup";
import type { BallotsReport } from "@/types";
import ReportingBallotReports from "../ReportingBallotReports.vue";

function ballotsReport(overrides: Partial<BallotsReport> = {}): BallotsReport {
  return {
    electionName: "Single Name",
    isConventionOrNsa: false,
    isSingleNameTally: false,
    ballots: [
      {
        ballotCode: "A1",
        location: "",
        isOnline: false,
        isImported: false,
        ballotId: 1,
        locationId: 1,
        statusCode: "Ok",
        spoiled: false,
        votes: [
          {
            personName: "Ann Aar",
            singleNameElectionCount: 7,
            spoiled: false,
            tieBreakRequired: false,
          },
        ],
      },
    ],
    ...overrides,
  };
}

function mountReport(reportData: BallotsReport) {
  return mount(ReportingBallotReports, {
    props: {
      selectedReport: "Ballots",
      selectedReportName: "Ballots",
      reportData,
    },
    global: { plugins: [i18n] },
  });
}

describe("ReportingBallotReports single-name counts", () => {
  it("shows the per-name count for a single-name tally", () => {
    const wrapper = mountReport(ballotsReport({ isSingleNameTally: true }));

    expect(wrapper.find(".sne-count").text()).toBe("7");
    expect(wrapper.text()).toContain("Ann Aar");
  });

  it("hides the per-name count for Convention and NSA", () => {
    const wrapper = mountReport(
      ballotsReport({ isSingleNameTally: false, isConventionOrNsa: true }),
    );

    expect(wrapper.find(".sne-count").exists()).toBe(false);
    expect(wrapper.text()).toContain("Ann Aar");
    expect(wrapper.text()).not.toContain("7");
  });
});
