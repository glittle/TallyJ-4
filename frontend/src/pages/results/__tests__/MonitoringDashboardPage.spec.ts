import { flushPromises, mount } from "@vue/test-utils";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { i18n } from "@/test/setup";
import MonitoringDashboardPage from "../MonitoringDashboardPage.vue";
import type { MonitorInfoDto } from "@/types";

const {
  mockGetSummary,
  mockAcceptAll,
  mockConfirm,
  mockShowSuccess,
  mockShowError,
} = vi.hoisted(() => ({
  mockGetSummary: vi.fn(),
  mockAcceptAll: vi.fn(),
  mockConfirm: vi.fn(),
  mockShowSuccess: vi.fn(),
  mockShowError: vi.fn(),
}));

vi.mock("vue-router", () => ({
  useRoute: () => ({ params: { id: "election-1" } }),
  useRouter: () => ({ push: vi.fn() }),
}));

vi.mock("@/domain/guestTellerAccess", () => ({
  isFullTeller: () => true,
}));

vi.mock("@/services/electionService", () => ({
  electionService: {
    getAcceptAllOnlineBallotsSummary: (...args: unknown[]) =>
      mockGetSummary(...args),
    acceptAllOnlineBallots: (...args: unknown[]) => mockAcceptAll(...args),
  },
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

vi.mock("element-plus", async (importOriginal) => {
  const actual = await importOriginal<typeof import("element-plus")>();
  return {
    ...actual,
    ElMessageBox: {
      confirm: (...args: unknown[]) => mockConfirm(...args),
    },
  };
});

const mockMonitor: MonitorInfoDto = {
  electionGuid: "election-1",
  computers: [],
  locations: [],
  onlineVotingInfo: {
    totalOnlineBallots: 4,
    processedOnlineBallots: 1,
    pendingOnlineBallots: 3,
    onlineVotingEnabled: true,
    acceptAllRuns: [],
  },
  totalBallots: 10,
  totalVotes: 20,
  lastUpdated: new Date().toISOString(),
};

vi.mock("@/stores/resultStore", () => ({
  useResultStore: () => ({
    fetchMonitorInfo: vi.fn().mockResolvedValue(mockMonitor),
  }),
}));

vi.mock("@/services/signalrService", () => ({
  signalrService: {
    connectToFrontDeskHub: vi.fn().mockResolvedValue({
      on: vi.fn(),
      off: vi.fn(),
    }),
    joinFrontDeskElection: vi.fn().mockResolvedValue(undefined),
  },
}));

const stubs = {
  ElCard: {
    template: "<div class='el-card'><slot name='header' /><slot /></div>",
  },
  ElButton: {
    template: "<button v-bind='$attrs'><slot /></button>",
  },
  ElIcon: { template: "<span />" },
  ElRow: { template: "<div><slot /></div>" },
  ElCol: { template: "<div><slot /></div>" },
  ElSkeleton: { template: "<div />" },
  ElAlert: { template: "<div><slot /></div>" },
  ElTable: { template: "<table><slot /></table>" },
  ElTableColumn: { template: "<td />" },
  ElTag: { template: "<span><slot /></span>" },
  ElDescriptions: { template: "<div><slot /></div>" },
  ElDescriptionsItem: { template: "<div><slot /></div>" },
  ElEmpty: { template: "<div />" },
};

describe("MonitoringDashboardPage Accept all", () => {
  beforeEach(() => {
    mockGetSummary.mockReset();
    mockAcceptAll.mockReset();
    mockConfirm.mockReset();
    mockShowSuccess.mockReset();
    mockShowError.mockReset();
    mockMonitor.onlineVotingInfo.pendingOnlineBallots = 3;
    mockMonitor.onlineVotingInfo.acceptAllRuns = [];
  });

  async function mountPage() {
    const wrapper = mount(MonitoringDashboardPage, {
      global: {
        plugins: [i18n],
        stubs,
      },
    });
    await flushPromises();
    return wrapper;
  }

  it("shows Accept all when there are pending online ballots", async () => {
    const wrapper = await mountPage();
    const button = wrapper.find("[data-testid='accept-all-online-ballots']");
    expect(button.exists()).toBe(true);
    expect(button.attributes("disabled")).toBeUndefined();
  });

  it("disables Accept all when nothing is pending", async () => {
    mockMonitor.onlineVotingInfo.pendingOnlineBallots = 0;
    const wrapper = await mountPage();
    const button = wrapper.find("[data-testid='accept-all-online-ballots']");
    expect(button.attributes("disabled")).toBeDefined();
  });

  it("shows the Accept-all record when runs exist", async () => {
    mockMonitor.onlineVotingInfo.acceptAllRuns = [
      {
        when: "2026-09-02T12:00:00.000Z",
        acceptedByUserId: "teller-1",
        acceptedBy: "Jane Teller",
        pendingBefore: 3,
        acceptedBefore: 1,
        pendingAfter: 0,
        acceptedAfter: 4,
      },
    ];
    const wrapper = await mountPage();
    expect(wrapper.find("[data-testid='accept-all-history']").exists()).toBe(
      true,
    );
    expect(
      wrapper.find("[data-testid='accept-all-history-empty']").exists(),
    ).toBe(false);
  });

  it("shows an empty Accept-all record when there are no runs", async () => {
    mockMonitor.onlineVotingInfo.acceptAllRuns = [];
    const wrapper = await mountPage();
    expect(wrapper.find("[data-testid='accept-all-history-empty']").exists()).toBe(
      true,
    );
    expect(wrapper.find("[data-testid='accept-all-history']").exists()).toBe(
      false,
    );
  });

  it("loads a summary and confirms before accepting", async () => {
    mockGetSummary.mockResolvedValue({ pendingCount: 3, processedCount: 1 });
    mockConfirm.mockResolvedValue("confirm");
    mockAcceptAll.mockResolvedValue({
      success: true,
      acceptedCount: 3,
      messageKey: "monitoring.acceptAll.complete",
    });

    const wrapper = await mountPage();
    await wrapper
      .find("[data-testid='accept-all-online-ballots']")
      .trigger("click");
    await flushPromises();

    expect(mockGetSummary).toHaveBeenCalledWith("election-1");
    expect(mockConfirm).toHaveBeenCalled();
    const confirmMessage = String(mockConfirm.mock.calls[0]?.[0] ?? "");
    expect(confirmMessage).toContain("3");
    expect(mockAcceptAll).toHaveBeenCalledWith("election-1");
    expect(mockShowSuccess).toHaveBeenCalled();
  });

  it("does not accept when the teller cancels the confirmation", async () => {
    mockGetSummary.mockResolvedValue({ pendingCount: 3, processedCount: 1 });
    mockConfirm.mockRejectedValue("cancel");

    const wrapper = await mountPage();
    await wrapper
      .find("[data-testid='accept-all-online-ballots']")
      .trigger("click");
    await flushPromises();

    expect(mockAcceptAll).not.toHaveBeenCalled();
  });

  it("surfaces 409 inProgress from the hey-api throwOnError body", async () => {
    mockGetSummary.mockResolvedValue({ pendingCount: 3, processedCount: 1 });
    mockConfirm.mockResolvedValue("confirm");
    mockAcceptAll.mockRejectedValue({
      messageKey: "monitoring.acceptAll.inProgress",
      alreadyInProgress: true,
    });

    const wrapper = await mountPage();
    await wrapper
      .find("[data-testid='accept-all-online-ballots']")
      .trigger("click");
    await flushPromises();

    expect(mockShowError).toHaveBeenCalledWith(
      "Another Accept all is already running for this election.",
    );
    expect(mockShowSuccess).not.toHaveBeenCalled();
  });

  it("surfaces 400 finalized from the hey-api throwOnError body", async () => {
    mockGetSummary.mockResolvedValue({ pendingCount: 3, processedCount: 1 });
    mockConfirm.mockResolvedValue("confirm");
    mockAcceptAll.mockRejectedValue({
      messageKey: "monitoring.acceptAll.finalized",
    });

    const wrapper = await mountPage();
    await wrapper
      .find("[data-testid='accept-all-online-ballots']")
      .trigger("click");
    await flushPromises();

    expect(mockShowError).toHaveBeenCalledWith(
      "Cannot accept online ballots after the election is finalized.",
    );
    expect(mockShowSuccess).not.toHaveBeenCalled();
  });

  it("still reads messageKey from axios-shaped response.data when present", async () => {
    mockGetSummary.mockResolvedValue({ pendingCount: 3, processedCount: 1 });
    mockConfirm.mockResolvedValue("confirm");
    mockAcceptAll.mockRejectedValue({
      response: {
        data: { messageKey: "monitoring.acceptAll.inProgress" },
      },
    });

    const wrapper = await mountPage();
    await wrapper
      .find("[data-testid='accept-all-online-ballots']")
      .trigger("click");
    await flushPromises();

    expect(mockShowError).toHaveBeenCalledWith(
      "Another Accept all is already running for this election.",
    );
  });
});
