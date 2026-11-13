import { flushPromises, mount } from "@vue/test-utils";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { i18n } from "@/test/setup";
import type {
  CountReconciliationReportDto,
  TallyResultDto,
  TallyStatisticsDto,
} from "@/types";
import ResultsPage from "../ResultsPage.vue";

const storeState = {
  loading: false,
  calculating: false,
  results: null as TallyResultDto | null,
  reconciliation: null as CountReconciliationReportDto | null,
  tallyProgress: null,
  fetchResults: vi.fn().mockResolvedValue(undefined),
  fetchReconciliation: vi.fn().mockResolvedValue(undefined),
  initializeSignalR: vi.fn().mockResolvedValue(undefined),
  joinTallySession: vi.fn().mockResolvedValue(undefined),
  leaveTallySession: vi.fn().mockResolvedValue(undefined),
};

vi.mock("vue-router", () => ({
  useRoute: () => ({ params: { id: "election-1" } }),
}));

vi.mock("@/composables/useApiErrorHandler", () => ({
  useApiErrorHandler: () => ({ handleApiError: vi.fn() }),
}));

vi.mock("@/stores/resultStore", () => ({
  useResultStore: () => storeState,
}));

const stubs = {
  ElCard: { template: "<div><slot /></div>" },
  ElSkeleton: { template: "<div class='skeleton' />" },
  ElAlert: {
    props: ["title"],
    template: "<div class='alert'>{{ title }}</div>",
  },
  ElProgress: { template: "<div />" },
  ElDescriptions: { template: "<div class='descriptions'><slot /></div>" },
  ElDescriptionsItem: {
    props: ["label"],
    template: "<div class='desc-item'>{{ label }} <slot /></div>",
  },
  ElTabs: { template: "<div><slot /></div>" },
  ElTabPane: { template: "<div><slot /></div>" },
  ElTable: { template: "<div />" },
  ElTableColumn: { template: "<span />" },
  ElEmpty: {
    props: ["description"],
    template: "<div class='empty'>{{ description }}</div>",
  },
  ResultsTable: {
    props: ["results"],
    template:
      "<div class='results-table'>{{ (results || []).map((row) => row.fullName).join(',') }}</div>",
  },
  TiesDisplay: { template: "<div class='ties' />" },
};

function stats(
  overrides: Partial<TallyStatisticsDto> = {},
): TallyStatisticsDto {
  return {
    totalBallots: 0,
    ballotsReceived: 0,
    spoiledBallots: 0,
    ballotsNeedingReview: 0,
    totalVotes: 0,
    validVotes: 0,
    invalidVotes: 0,
    numVoters: 2,
    numEligiblePeople: 2,
    numberToElect: 9,
    numberExtra: 0,
    ...overrides,
  };
}

const blockedReport: CountReconciliationReportDto = {
  isReconciled: false,
  frontDeskCount: 2,
  ballotCount: 3,
  pendingOnlineCount: 0,
  spoiledBallotCount: 0,
  mismatches: [
    {
      kind: "FrontDeskVsBallots",
      frontDeskCount: 2,
      ballotCount: 3,
    },
  ],
};

const readyReport: CountReconciliationReportDto = {
  isReconciled: true,
  frontDeskCount: 3,
  ballotCount: 3,
  pendingOnlineCount: 0,
  spoiledBallotCount: 0,
  mismatches: [],
};

function tally(overrides: Partial<TallyResultDto> = {}): TallyResultDto {
  return {
    electionGuid: "election-1",
    electionName: "Bot QA",
    calculatedAt: null,
    analysisBlocker: null,
    statistics: stats(),
    results: [],
    ties: [],
    ...overrides,
  };
}

describe("ResultsPage blocked tally", () => {
  beforeEach(() => {
    storeState.loading = false;
    storeState.results = null;
    storeState.reconciliation = null;
    storeState.fetchResults.mockClear();
    storeState.fetchReconciliation.mockClear();
  });

  async function mountPage() {
    const wrapper = mount(ResultsPage, {
      global: { plugins: [i18n], stubs },
    });
    await flushPromises();
    return wrapper;
  }

  it("shows the mismatch message instead of a zeroed Calculated result", async () => {
    storeState.results = tally({
      analysisBlocker:
        "elections.stageChangeError.countsDoNotReconcile|count=1",
    });
    storeState.reconciliation = blockedReport;

    const wrapper = await mountPage();

    expect(wrapper.text()).toContain(
      "1 mismatch(es) must be resolved before Analyze or Finalize.",
    );
    expect(wrapper.text()).not.toContain("Calculated at");
    expect(wrapper.text()).not.toContain("Total Ballots");
    expect(wrapper.find(".results-table").exists()).toBe(false);
    expect(wrapper.find(".empty").exists()).toBe(false);
    expect(storeState.fetchResults).toHaveBeenCalledWith("election-1");
    expect(storeState.fetchReconciliation).toHaveBeenCalledWith("election-1");
  });

  it("shows an outstanding-ballot blocker without a calculated timestamp", async () => {
    storeState.results = tally({
      analysisBlocker: "elections.stageChangeError.ballotsOutstanding|count=2",
    });
    storeState.reconciliation = readyReport;

    const wrapper = await mountPage();

    expect(wrapper.get('[data-testid="results-blocker"]').text()).toBe(
      "2 ballot(s) have outstanding issues",
    );
    expect(wrapper.text()).not.toContain("Calculated at");
    expect(wrapper.find(".results-table").exists()).toBe(false);
  });

  it("keeps a previous tally visible and still explains the block", async () => {
    storeState.results = tally({
      calculatedAt: "2026-11-10T23:42:30.000Z",
      analysisBlocker:
        "elections.stageChangeError.countsDoNotReconcile|count=1",
      statistics: stats({ totalBallots: 3, totalVotes: 4 }),
      results: [
        {
          personGuid: "p1",
          fullName: "Ada Lovelace",
          voteCount: 4,
          rank: 1,
          section: "E",
          isTied: false,
          tieBreakRequired: false,
          closeToNext: false,
          closeToPrev: false,
        },
      ],
    });
    storeState.reconciliation = blockedReport;

    const wrapper = await mountPage();

    expect(wrapper.text()).toContain(
      "1 mismatch(es) must be resolved before Analyze or Finalize.",
    );
    expect(wrapper.text()).toContain("Calculated at");
    expect(wrapper.text()).toContain("Ada Lovelace");
    expect(wrapper.text()).toContain("Total Ballots");
    expect(wrapper.text()).toContain("3");
  });

  it("asks for a calculation when nothing is stored and Analyze is allowed", async () => {
    storeState.results = tally();
    storeState.reconciliation = readyReport;

    const wrapper = await mountPage();

    expect(wrapper.find(".empty").text()).toBe(
      "No results available. Calculate tally first.",
    );
    expect(wrapper.text()).not.toContain("Calculated at");
    expect(wrapper.find('[data-testid="results-blocker"]').exists()).toBe(
      false,
    );
  });
});
