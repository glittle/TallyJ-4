import { describe, it, expect, vi, beforeEach } from "vitest";
import { mount } from "@vue/test-utils";
import { setActivePinia, createPinia } from "pinia";
import DashboardPage from "../DashboardPage.vue";

vi.mock("vue-i18n", () => ({
  useI18n: () => ({ t: (key: string) => key }),
}));

const mockPush = vi.fn();
vi.mock("vue-router", () => ({
  useRouter: () => ({ push: mockPush }),
}));

const mockElections = vi.fn(() => []);
const mockActiveElections = vi.fn(() => []);

vi.mock("@/stores/electionStore", () => ({
  useElectionStore: () => ({
    get elections() {
      return mockElections();
    },
    get activeElections() {
      return mockActiveElections();
    },
    loading: false,
    fetchElections: vi.fn().mockResolvedValue(undefined),
    initializeSignalR: vi.fn().mockResolvedValue(undefined),
    joinDashboardElections: vi.fn().mockResolvedValue(undefined),
    leaveDashboardElections: vi.fn().mockResolvedValue(undefined),
    duplicateElection: vi.fn().mockResolvedValue(undefined),
  }),
}));

vi.mock("@/composables/useApiErrorHandler", () => ({
  useApiErrorHandler: () => ({ handleApiError: vi.fn() }),
}));

vi.mock("@/composables/useNotifications", () => ({
  useNotifications: () => ({
    showSuccessMessage: vi.fn(),
    showErrorMessage: vi.fn(),
  }),
}));

vi.mock("@/utils/activeElectionHubStorage", () => ({
  getActiveElectionHubGuid: vi.fn(() => null),
}));

const globalConfig = {
  stubs: {
    ElCard: {
      template: "<div class='el-card'><slot name='header' /><slot /></div>",
    },
    ElRow: { template: "<div><slot /></div>" },
    ElCol: { template: "<div><slot /></div>" },
    ElButton: { template: "<button><slot /></button>" },
    ElIcon: { template: "<span />" },
    ElInput: { template: "<input />" },
    ElSelect: { template: "<select><slot /></select>" },
    ElOption: { template: "<option />" },
    ElDatePicker: { template: "<input />" },
    ElSpace: { template: "<div><slot /></div>" },
    ElTable: { template: "<table class='el-table'><slot /></table>" },
    ElTableColumn: {
      props: ["label"],
      template: "<th class='table-col'>{{ label }}</th>",
    },
    ElPagination: { template: "<div />" },
    ElSkeleton: { template: "<div />" },
    ElEmpty: { template: "<div class='el-empty'><slot /></div>" },
    ElTag: { template: "<span><slot /></span>" },
    Plus: { template: "<span />" },
    CopyDocument: { template: "<span />" },
    Upload: { template: "<span />" },
    Document: { template: "<span />" },
    CircleCheck: { template: "<span />" },
    Search: { template: "<span />" },
  },
  mocks: {
    $t: (key: string) => key,
  },
  directives: {
    loading: {},
  },
};

describe("DashboardPage", () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    mockPush.mockReset();
    mockElections.mockReturnValue([]);
    mockActiveElections.mockReturnValue([]);
  });

  it("does not render the removed Resume or Setup Tips dashboard rail", () => {
    mockElections.mockReturnValue([
      {
        electionGuid: "abc",
        name: "Test Election",
        dateOfElection: "2026-01-01",
        voterCount: 10,
        ballotCount: 5,
        tallyStatus: "Setup",
      },
    ]);
    const wrapper = mount(DashboardPage, { global: globalConfig });
    expect(wrapper.find(".dashboard-rail").exists()).toBe(false);
    expect(wrapper.find(".resume-election-card").exists()).toBe(false);
    expect(wrapper.find(".setup-tips-card").exists()).toBe(false);
  });

  it("renders the elections list section", () => {
    const wrapper = mount(DashboardPage, { global: globalConfig });
    expect(wrapper.find(".dashboard-page").exists()).toBe(true);
    expect(wrapper.find(".elections-section").exists()).toBe(true);
  });

  it("renders the participation column when elections exist", () => {
    mockElections.mockReturnValue([
      {
        electionGuid: "abc",
        name: "Test",
        dateOfElection: "2026-01-01",
        voterCount: 10,
        ballotCount: 5,
        tallyStatus: "Setup",
      },
    ]);
    const wrapper = mount(DashboardPage, { global: globalConfig });
    expect(wrapper.html()).toContain("elections.participation");
  });
});
