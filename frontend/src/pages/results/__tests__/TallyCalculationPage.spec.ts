import { flushPromises, mount } from "@vue/test-utils";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { i18n } from "@/test/setup";
import TallyCalculationPage from "../TallyCalculationPage.vue";
import type { CountReconciliationReportDto } from "@/types";

const {
  mockFetchReconciliation,
  mockCalculateTally,
  mockShowError,
  mockShowSuccess,
} = vi.hoisted(() => ({
  mockFetchReconciliation: vi.fn(),
  mockCalculateTally: vi.fn(),
  mockShowError: vi.fn(),
  mockShowSuccess: vi.fn(),
}));

vi.mock("vue-router", () => ({
  useRoute: () => ({ params: { id: "election-1" } }),
  useRouter: () => ({ push: vi.fn() }),
}));

vi.mock("@/composables/useNotifications", () => ({
  useNotifications: () => ({
    showSuccessMessage: mockShowSuccess,
    showErrorMessage: mockShowError,
  }),
}));

vi.mock("@/composables/useApiErrorHandler", () => ({
  useApiErrorHandler: () => ({ handleApiError: vi.fn() }),
}));

const blockedReport: CountReconciliationReportDto = {
  isReconciled: false,
  frontDeskCount: 2,
  ballotCount: 1,
  pendingOnlineCount: 1,
  spoiledBallotCount: 0,
  mismatches: [
    {
      kind: "PendingOnline",
      personName: "Ada Lovelace",
      onlineStatus: "Submitted",
    },
  ],
};

const readyReport: CountReconciliationReportDto = {
  isReconciled: true,
  frontDeskCount: 1,
  ballotCount: 1,
  pendingOnlineCount: 0,
  spoiledBallotCount: 0,
  mismatches: [],
};

const storeState = {
  calculating: false,
  loading: false,
  results: null,
  tallyProgress: null,
  reconciliation: blockedReport as CountReconciliationReportDto | null,
  currentElection: { numberToElect: 9 },
  fetchElectionById: vi.fn().mockResolvedValue(undefined),
  initializeSignalR: vi.fn().mockResolvedValue(undefined),
  joinTallySession: vi.fn().mockResolvedValue(undefined),
  leaveTallySession: vi.fn().mockResolvedValue(undefined),
  fetchResults: vi.fn().mockResolvedValue(undefined),
  fetchReconciliation: mockFetchReconciliation,
  calculateTally: mockCalculateTally,
  clearError: vi.fn(),
};

vi.mock("@/stores/resultStore", () => ({
  useResultStore: () => storeState,
}));

vi.mock("@/stores/electionStore", () => ({
  useElectionStore: () => ({
    currentElection: storeState.currentElection,
    fetchElectionById: storeState.fetchElectionById,
  }),
}));

const stubs = {
  ReconciliationReportPanel: {
    props: ["report"],
    template:
      "<div class='recon-panel'>{{ report && report.isReconciled ? 'ready' : 'blocked' }}</div>",
  },
  ElCard: { template: "<div><slot /></div>" },
  ElAlert: { template: "<div><slot /></div>" },
  ElForm: { template: "<form><slot /></form>" },
  ElFormItem: { template: "<div><slot /></div>" },
  ElButton: {
    props: ["disabled", "loading"],
    template:
      "<button :disabled='disabled' @click='$emit(\"click\")'><slot /></button>",
  },
  ElIcon: { template: "<span />" },
  ElDivider: { template: "<hr />" },
  ElProgress: { template: "<div />" },
  ElDescriptions: { template: "<div />" },
  ElDescriptionsItem: { template: "<div />" },
  ElTable: { template: "<div />" },
  ElTableColumn: { template: "<div />" },
  ElTag: { template: "<span />" },
  Operation: { template: "<span />" },
};

describe("TallyCalculationPage reconciliation gate", () => {
  beforeEach(() => {
    mockFetchReconciliation.mockReset();
    mockCalculateTally.mockReset();
    mockShowError.mockReset();
    mockShowSuccess.mockReset();
    mockFetchReconciliation.mockResolvedValue(blockedReport);
    storeState.reconciliation = blockedReport;
    storeState.calculating = false;
  });

  it("loads the report and disables Analyze when mismatches exist", async () => {
    const wrapper = mount(TallyCalculationPage, {
      global: { plugins: [i18n], stubs },
    });
    await flushPromises();

    expect(mockFetchReconciliation).toHaveBeenCalledWith("election-1");
    expect(wrapper.find(".recon-panel").text()).toBe("blocked");
    expect(wrapper.find("button").attributes("disabled")).toBeDefined();
  });

  it("does not call calculate when Analyze is clicked while blocked", async () => {
    const wrapper = mount(TallyCalculationPage, {
      global: { plugins: [i18n], stubs },
    });
    await flushPromises();

    await wrapper.find("button").trigger("click");
    expect(wrapper.find("button").attributes("disabled")).toBeDefined();
    expect(mockCalculateTally).not.toHaveBeenCalled();
  });

  it("enables Analyze when the report is reconciled", async () => {
    storeState.reconciliation = readyReport;
    mockFetchReconciliation.mockResolvedValue(readyReport);

    const wrapper = mount(TallyCalculationPage, {
      global: { plugins: [i18n], stubs },
    });
    await flushPromises();

    expect(wrapper.find("button").attributes("disabled")).toBeUndefined();
  });

  it("translates hey-api stage-change errors when Calculate fails", async () => {
    storeState.reconciliation = readyReport;
    mockFetchReconciliation.mockResolvedValue(readyReport);
    mockCalculateTally.mockRejectedValue({
      message: "elections.stageChangeError.ballotsOutstanding|count=3",
    });

    const wrapper = mount(TallyCalculationPage, {
      global: { plugins: [i18n], stubs },
    });
    await flushPromises();

    await wrapper.find("button").trigger("click");
    await flushPromises();

    expect(mockShowError).toHaveBeenCalledWith(
      "3 ballot(s) have outstanding issues",
    );
  });
});
