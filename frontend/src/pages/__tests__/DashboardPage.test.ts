import { flushPromises, mount } from "@vue/test-utils";
import { ElMessageBox } from "element-plus";
import { createPinia, setActivePinia } from "pinia";
import { beforeEach, describe, expect, it, vi } from "vitest";
import electionsEn from "@/locales/en/elections.json";
import DashboardPage from "../DashboardPage.vue";

const passcodeNotCopiedKey = "elections.form.electionPasscodeNotCopied";
const passcodeNotCopiedText = (electionsEn as Record<string, { t: string }>)[
  passcodeNotCopiedKey
].t;

const {
  mockDuplicateElection,
  mockShowSuccessMessage,
  mockShowWarningMessage,
  mockShowErrorMessage,
  mockImportElectionFromFile,
} = vi.hoisted(() => ({
  mockDuplicateElection: vi.fn(),
  mockShowSuccessMessage: vi.fn(),
  mockShowWarningMessage: vi.fn(),
  mockShowErrorMessage: vi.fn(),
  mockImportElectionFromFile: vi.fn(),
}));

vi.mock("vue-i18n", async (importOriginal) => {
  const actual = await importOriginal<typeof import("vue-i18n")>();
  return {
    ...actual,
    useI18n: () => ({
      t: (key: string) =>
        key === "elections.form.electionPasscodeNotCopied"
          ? "The teller passcode was shorter than 6 characters and was not copied."
          : key,
      te: (key: string) => key === "elections.form.electionPasscodeNotCopied",
    }),
  };
});

const mockPush = vi.fn();
vi.mock("vue-router", () => ({
  useRouter: () => ({ push: mockPush }),
}));

const mockElections = vi.fn(
  (): Array<{
    electionGuid: string;
    name: string;
    dateOfElection?: string;
    voterCount?: number;
    ballotCount?: number;
    tallyStatus?: string;
  }> => [],
);
const mockActiveElections = vi.fn(
  (): Array<{ electionGuid: string; name: string }> => [],
);

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
    duplicateElection: mockDuplicateElection,
  }),
}));

vi.mock("@/services/electionService", () => ({
  electionService: {
    importElectionFromFile: mockImportElectionFromFile,
    importTallyJv3ElectionFromFile: vi.fn(),
  },
}));

vi.mock("@/services/signalrService", () => ({
  signalrService: {
    connectToElectionPackageImportHub: vi.fn().mockResolvedValue({
      on: vi.fn(),
      off: vi.fn(),
    }),
    joinElectionPackageImportSession: vi.fn().mockResolvedValue(undefined),
    leaveElectionPackageImportSession: vi.fn().mockResolvedValue(undefined),
    getConnection: vi.fn(() => null),
  },
}));

vi.mock("@/composables/useApiErrorHandler", () => ({
  useApiErrorHandler: () => ({ handleApiError: vi.fn() }),
}));

vi.mock("@/composables/useNotifications", () => ({
  useNotifications: () => ({
    showSuccessMessage: mockShowSuccessMessage,
    showWarningMessage: mockShowWarningMessage,
    showErrorMessage: mockShowErrorMessage,
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
    ElButton: {
      template: "<button v-bind='$attrs'><slot /></button>",
    },
    ElIcon: { template: "<span />" },
    ElInput: { template: "<input />" },
    ElSelect: { template: "<select><slot /></select>" },
    ElOption: { template: "<option />" },
    ElDatePicker: { template: "<input />" },
    ElSpace: { template: "<div><slot /></div>" },
    ElTable: { template: "<table class='el-table'><slot /></table>" },
    ElTableColumn: {
      props: ["label"],
      setup() {
        return {
          dummyRow: { electionGuid: "abc", name: "Test" },
        };
      },
      template:
        "<th class='table-col'>{{ label }}<slot name='default' :row='dummyRow' /></th>",
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
    mockDuplicateElection.mockReset();
    mockDuplicateElection.mockResolvedValue({ warning: null });
    mockShowSuccessMessage.mockReset();
    mockShowWarningMessage.mockReset();
    mockShowErrorMessage.mockReset();
    mockImportElectionFromFile.mockReset();
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

  it("renders a duplicate control when elections exist", () => {
    mockElections.mockReturnValue([
      {
        electionGuid: "abc",
        name: "Test",
        dateOfElection: "2026-01-01",
        voterCount: 10,
        ballotCount: 5,
      },
    ]);
    const wrapper = mount(DashboardPage, { global: globalConfig });
    expect(wrapper.html()).toContain("elections.duplicate.action");
  });

  it("shows the translated warning when duplicate drops a short passcode", async () => {
    mockElections.mockReturnValue([
      {
        electionGuid: "abc",
        name: "Test",
        dateOfElection: "2026-01-01",
        voterCount: 10,
        ballotCount: 5,
      },
    ]);
    mockDuplicateElection.mockResolvedValue({
      election: { electionGuid: "copy-id" },
      warning: passcodeNotCopiedKey,
    });
    vi.spyOn(ElMessageBox, "prompt").mockResolvedValue({
      value: "Copy of Test",
      action: "confirm",
    } as never);

    const wrapper = mount(DashboardPage, { global: globalConfig });
    await flushPromises();
    await wrapper
      .find("[aria-label='elections.duplicate.action']")
      .trigger("click");
    await flushPromises();

    expect(mockShowSuccessMessage).toHaveBeenCalledWith(
      "elections.duplicate.success",
    );
    expect(passcodeNotCopiedText).not.toBe(passcodeNotCopiedKey);
    expect(mockShowWarningMessage).toHaveBeenCalledWith(passcodeNotCopiedText);
  });

  it("shows the translated warning when JSON import drops a short passcode", async () => {
    mockImportElectionFromFile.mockResolvedValue({
      election: { electionGuid: "imported-id" },
      warnings: [passcodeNotCopiedKey],
    });
    const created: HTMLElement[] = [];
    const originalCreate = document.createElement.bind(document);
    vi.spyOn(document, "createElement").mockImplementation(
      (tagName, options) => {
        const element = originalCreate(
          tagName as keyof HTMLElementTagNameMap,
          options,
        );
        if (tagName === "input") {
          created.push(element);
        }
        return element;
      },
    );

    const wrapper = mount(DashboardPage, { global: globalConfig });
    await flushPromises();
    const importButton = wrapper
      .findAll("button")
      .find((button) => button.text().includes("elections.importElection"));
    expect(importButton).toBeTruthy();
    await importButton!.trigger("click");

    const input = created.at(-1) as HTMLInputElement;
    const file = new File(["{}"], "election.json", {
      type: "application/json",
    });
    Object.defineProperty(input, "files", { value: [file] });
    input.dispatchEvent(new Event("change"));
    await flushPromises();

    expect(mockImportElectionFromFile).toHaveBeenCalledWith(file);
    expect(mockShowSuccessMessage).toHaveBeenCalledWith(
      "elections.importElectionSuccess",
    );
    expect(mockShowWarningMessage).toHaveBeenCalledWith(passcodeNotCopiedText);
    expect(mockPush).toHaveBeenCalledWith("/elections/imported-id");
    vi.mocked(document.createElement).mockRestore();
  });
});
