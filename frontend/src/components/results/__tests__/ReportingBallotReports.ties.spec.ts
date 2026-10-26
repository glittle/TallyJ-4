import { mount } from "@vue/test-utils";
import { describe, expect, it } from "vitest";
import { i18n } from "@/test/setup";
import type { MainReport, VotesByNameReport, VotesByNumReport } from "@/types";
import ReportingBallotReports from "../ReportingBallotReports.vue";

function mainReport(overrides: Partial<MainReport> = {}): MainReport {
  return {
    electionName: "QA Ties",
    numEligibleToVote: 10,
    sumOfEnvelopesCollected: 8,
    numBallotsWithManual: 8,
    percentParticipation: 80,
    inPersonBallots: 8,
    mailedInBallots: 0,
    droppedOffBallots: 0,
    onlineBallots: 0,
    importedBallots: 0,
    calledInBallots: 0,
    custom1Ballots: 0,
    custom2Ballots: 0,
    custom3Ballots: 0,
    spoiledBallots: 0,
    spoiledVotes: 0,
    spoiledBallotReasons: [],
    spoiledVoteReasons: [],
    elected: [
      {
        rank: "1",
        name: "Best, Pat",
        voteCountDisplay: "10",
        section: "E",
      },
    ],
    hasTies: false,
    ...overrides,
  };
}

function mountReport(selectedReport: string, reportData: unknown) {
  return mount(ReportingBallotReports, {
    props: {
      selectedReport,
      selectedReportName: selectedReport,
      reportData,
    },
    global: { plugins: [i18n] },
  });
}

describe("ReportingBallotReports unresolved ties", () => {
  it("names an unresolved tie on the main report", () => {
    const wrapper = mountReport("Main", mainReport({ hasTies: true }));

    expect(wrapper.text()).toContain("Tie-break Required");
    expect(wrapper.text()).not.toContain("/ Tie Break");
    expect(wrapper.text()).not.toContain("Votes / tie-break");
  });

  it("does not name a tie on the main report once the flag is clear", () => {
    const wrapper = mountReport(
      "Main",
      mainReport({
        hasTies: false,
        elected: [
          {
            rank: "1",
            name: "Best, Pat",
            voteCountDisplay: "10 / 5",
            section: "E",
          },
        ],
      }),
    );

    expect(wrapper.text()).not.toContain("Tie-break Required");
    expect(wrapper.text()).toContain("10 / 5");
    expect(wrapper.text()).toContain("Votes / tie-break");
    expect(wrapper.text()).toContain("Votes / tie-break count");
    expect(wrapper.text()).not.toContain("/ Tie Break");
  });

  it("names an unresolved tie on votes by number", () => {
    const report: VotesByNumReport = {
      electionName: "QA Ties",
      hasUnresolvedTie: true,
      people: [
        {
          personName: "Best, Pat",
          voteCount: 10,
          tieBreakCount: null,
          tieBreakRequired: true,
          section: "E",
          showBreak: true,
        },
      ],
    };

    const wrapper = mountReport("VotesByNum", report);
    expect(wrapper.text()).toContain("Tie-break Required");
    expect(wrapper.text()).toContain("10 -");
    expect(wrapper.text()).not.toContain("10 /");
    expect(wrapper.text()).not.toContain("Votes / tie-break count");
  });

  it("shows a resolved runoff count on votes by name without the required flag", () => {
    const report: VotesByNameReport = {
      electionName: "QA Ties",
      hasUnresolvedTie: false,
      people: [
        {
          personName: "Best, Pat",
          voteCount: 5,
          tieBreakCount: 3,
          tieBreakRequired: false,
          section: "E",
          showBreak: false,
        },
      ],
    };

    const wrapper = mountReport("VotesByName", report);
    expect(wrapper.text()).not.toContain("Tie-break Required");
    expect(wrapper.text()).toContain("5 / 3");
    expect(wrapper.text()).toContain("Votes / tie-break count");
  });
});
