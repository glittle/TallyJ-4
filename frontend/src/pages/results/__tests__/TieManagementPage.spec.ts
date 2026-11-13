import { flushPromises, mount } from "@vue/test-utils";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { i18n } from "@/test/setup";
import TieManagementPage from "../TieManagementPage.vue";
import type { TieDetailsDto } from "@/types";

const {
  mockFetchTieDetails,
  mockSaveTieCounts,
  mockFetchResults,
  mockConfirm,
  mockShowSuccess,
  mockShowError,
  mockShowInfo,
  electionStage,
} = vi.hoisted(() => ({
  mockFetchTieDetails: vi.fn(),
  mockSaveTieCounts: vi.fn(),
  mockFetchResults: vi.fn(),
  mockConfirm: vi.fn(),
  mockShowSuccess: vi.fn(),
  mockShowError: vi.fn(),
  mockShowInfo: vi.fn(),
  electionStage: { current: "ProcessingBallots" },
}));

vi.mock("vue-router", () => ({
  useRoute: () => ({ params: { id: "election-1" } }),
  useRouter: () => ({ push: vi.fn() }),
}));

vi.mock("@/composables/useNotifications", () => ({
  useNotifications: () => ({
    showSuccessMessage: mockShowSuccess,
    showErrorMessage: mockShowError,
    showInfoMessage: mockShowInfo,
  }),
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

const sampleTies: TieDetailsDto[] = [
  {
    tieBreakGroup: 1,
    section: "E",
    instructions: "",
    people: [
      {
        personGuid: "person-a",
        fullName: "Ada",
        voteCount: 4,
        tieBreakCount: 5,
      },
      {
        personGuid: "person-b",
        fullName: "Bob",
        voteCount: 4,
        tieBreakCount: null,
      },
    ],
  },
];

vi.mock("@/stores/resultStore", () => ({
  useResultStore: () => ({
    fetchTieDetails: (...args: unknown[]) => mockFetchTieDetails(...args),
    saveTieCounts: (...args: unknown[]) => mockSaveTieCounts(...args),
    fetchResults: (...args: unknown[]) => mockFetchResults(...args),
  }),
}));

vi.mock("@/stores/electionStore", () => ({
  useElectionStore: () => ({
    get currentStage() {
      return electionStage.current;
    },
  }),
}));

const stubs = {
  ElCard: { template: "<div><slot name='header' /><slot /></div>" },
  ElAlert: {
    props: ["title"],
    template: "<div class='alert'>{{ title }}</div>",
  },
  ElButton: {
    props: ["disabled", "loading"],
    template:
      "<button :disabled='disabled' @click='$emit(\"click\")'><slot /></button>",
  },
  ElTable: {
    props: ["data"],
    template: "<div class='table-stub'><slot /></div>",
  },
  ElTableColumn: {
    template:
      "<div v-for='row in $parent.data' :key='row.personGuid'><slot :row='row' /></div>",
  },
  ElInputNumber: {
    props: ["modelValue", "valueOnClear", "disabled"],
    emits: ["update:modelValue", "change"],
    template:
      "<div><input class='tie-count' :value='modelValue' :disabled='disabled' /><button type='button' class='input-clear' :disabled='disabled' @click='onClear'>clear-input</button></div>",
    methods: {
      onClear(this: {
        valueOnClear: unknown;
        $emit: (event: string, value?: unknown) => void;
      }) {
        this.$emit("update:modelValue", this.valueOnClear);
        this.$emit("change", this.valueOnClear);
      },
    },
  },
  ElTag: { template: "<span class='tag'><slot /></span>" },
  ElSkeleton: { template: "<div />" },
  ElEmpty: { template: "<div />" },
};

describe("TieManagementPage", () => {
  beforeEach(() => {
    mockFetchTieDetails.mockReset();
    mockSaveTieCounts.mockReset();
    mockFetchResults.mockReset();
    mockConfirm.mockReset();
    mockShowSuccess.mockReset();
    mockShowError.mockReset();
    mockShowInfo.mockReset();
    electionStage.current = "ProcessingBallots";
    mockFetchTieDetails.mockResolvedValue(structuredClone(sampleTies));
    mockSaveTieCounts.mockResolvedValue({
      success: true,
      message: "ok",
      reAnalysisTriggered: true,
    });
    mockFetchResults.mockResolvedValue({});
    mockConfirm.mockResolvedValue(true);
  });

  it("sends explicit 0 when a count is cleared and refreshes results after save", async () => {
    const wrapper = mount(TieManagementPage, {
      global: { plugins: [i18n], stubs },
    });
    await flushPromises();

    const vm = wrapper.vm as unknown as {
      tieDetails: TieDetailsDto[];
      saveTieCounts: () => Promise<void>;
    };
    expect(vm.tieDetails[0]?.people[1]?.tieBreakCount).toBeNull();
    vm.tieDetails[0]!.people[1]!.tieBreakCount = 0;

    await vm.saveTieCounts();
    await flushPromises();

    expect(mockConfirm).toHaveBeenCalledWith(
      "Save tie-break counts? The tally will be re-analyzed after save.",
      "Confirmation",
      expect.objectContaining({ type: "warning" }),
    );
    expect(mockSaveTieCounts).toHaveBeenCalledWith("election-1", [
      { personGuid: "person-a", tieBreakCount: 5 },
      { personGuid: "person-b", tieBreakCount: 0 },
    ]);
    expect(mockShowSuccess).toHaveBeenCalledWith(
      "Tie-break counts saved successfully.",
    );
    expect(mockFetchResults).toHaveBeenCalledWith("election-1");
    expect(mockFetchTieDetails).toHaveBeenCalledTimes(2);
    expect(mockShowInfo).toHaveBeenCalled();
  });

  it("sends null after clearing a previous count via the input", async () => {
    const wrapper = mount(TieManagementPage, {
      global: { plugins: [i18n], stubs },
    });
    await flushPromises();

    const clearInput = wrapper.find(".input-clear");
    expect(clearInput.exists()).toBe(true);
    await clearInput.trigger("click");

    const vm = wrapper.vm as unknown as {
      tieDetails: TieDetailsDto[];
      saveTieCounts: () => Promise<void>;
    };
    expect(vm.tieDetails[0]?.people[0]?.tieBreakCount).toBeNull();

    await vm.saveTieCounts();
    await flushPromises();

    expect(mockSaveTieCounts).toHaveBeenCalledWith("election-1", [
      { personGuid: "person-a", tieBreakCount: null },
      { personGuid: "person-b", tieBreakCount: null },
    ]);
    expect(mockShowSuccess).toHaveBeenCalledWith(
      "Tie-break counts saved successfully.",
    );
  });

  it("lists a resolved group without the prompt or Tie-break Required tag", async () => {
    mockFetchTieDetails.mockResolvedValue([
      {
        tieBreakGroup: 1,
        section: "E",
        instructions: "",
        tieBreakRequired: false,
        isResolved: true,
        people: [
          {
            personGuid: "person-a",
            fullName: "Cole, Sam",
            voteCount: 2,
            tieBreakCount: 5,
          },
          {
            personGuid: "person-b",
            fullName: "Best, Pat",
            voteCount: 2,
            tieBreakCount: 0,
          },
        ],
      },
    ]);

    const wrapper = mount(TieManagementPage, {
      global: { plugins: [i18n], stubs },
    });
    await flushPromises();

    expect(wrapper.text()).toContain("Resolved");
    expect(wrapper.text()).not.toContain("Tie-break Required");
    expect(wrapper.text()).not.toContain("Enter tie-break vote counts");
    expect(wrapper.text()).not.toContain("Enter runoff counts");
    expect(wrapper.find(".tie-count").exists()).toBe(true);
    expect(wrapper.find(".tie-break-required").exists()).toBe(false);
  });

  it("does not demand counts when everyone in the elected tie is already elected", async () => {
    mockFetchTieDetails.mockResolvedValue([
      {
        tieBreakGroup: 1,
        section: "E",
        instructions: "",
        tieBreakRequired: false,
        isResolved: false,
        people: [
          {
            personGuid: "person-a",
            fullName: "Ada",
            voteCount: 1,
            tieBreakCount: null,
          },
          {
            personGuid: "person-b",
            fullName: "Bob",
            voteCount: 1,
            tieBreakCount: null,
          },
        ],
      },
    ]);

    const wrapper = mount(TieManagementPage, {
      global: { plugins: [i18n], stubs },
    });
    await flushPromises();

    expect(wrapper.text()).not.toContain(
      "Elected-section ties still have people with no tie-break count entered. An explicit 0 is allowed.",
    );
    expect(wrapper.text()).not.toContain("Tie-break Required");
    expect(wrapper.text()).not.toContain("Resolved");
  });

  it("still demands a count for a required elected-section tie", async () => {
    mockFetchTieDetails.mockResolvedValue([
      {
        tieBreakGroup: 1,
        section: "E",
        instructions: "Enter tie-break vote counts for these people",
        tieBreakRequired: true,
        isResolved: false,
        people: [
          {
            personGuid: "person-a",
            fullName: "Ada",
            voteCount: 4,
            tieBreakCount: 1,
          },
          {
            personGuid: "person-b",
            fullName: "Bob",
            voteCount: 4,
            tieBreakCount: null,
          },
        ],
      },
    ]);

    const wrapper = mount(TieManagementPage, {
      global: { plugins: [i18n], stubs },
    });
    await flushPromises();

    expect(wrapper.text()).toContain(
      "Elected-section ties still have people with no tie-break count entered. An explicit 0 is allowed.",
    );
    expect(wrapper.text()).toContain("Tie-break Required");
  });

  it("keeps tie-break counts editable before the election is Finalized", async () => {
    const wrapper = mount(TieManagementPage, {
      global: { plugins: [i18n], stubs },
    });
    await flushPromises();

    const input = wrapper.find(".tie-count");
    expect(input.attributes("disabled")).toBeUndefined();
    expect(wrapper.text()).not.toContain(
      "This election is finalized. People and ballot data cannot be changed.",
    );

    const vm = wrapper.vm as unknown as {
      tieDetails: TieDetailsDto[];
      saveTieCounts: () => Promise<void>;
    };
    vm.tieDetails[0]!.people[0]!.tieBreakCount = 8;
    await vm.saveTieCounts();
    await flushPromises();

    expect(mockSaveTieCounts).toHaveBeenCalledWith("election-1", [
      { personGuid: "person-a", tieBreakCount: 8 },
      { personGuid: "person-b", tieBreakCount: null },
    ]);
  });

  it("disables tie-break edits and refuses save when the election is Finalized", async () => {
    electionStage.current = "Finalized";
    const wrapper = mount(TieManagementPage, {
      global: { plugins: [i18n], stubs },
    });
    await flushPromises();

    expect(wrapper.text()).toContain(
      "This election is finalized. People and ballot data cannot be changed.",
    );
    const inputs = wrapper.findAll(".tie-count");
    expect(inputs.length).toBeGreaterThan(0);
    for (const input of inputs) {
      expect(input.attributes("disabled")).toBeDefined();
    }

    const clearButtons = wrapper
      .findAll("button")
      .filter((button) => button.text() === "Clear");
    expect(clearButtons.length).toBeGreaterThan(0);
    for (const button of clearButtons) {
      expect(button.attributes("disabled")).toBeDefined();
    }

    const vm = wrapper.vm as unknown as {
      tieDetails: TieDetailsDto[];
      saveTieCounts: () => Promise<void>;
    };
    vm.tieDetails[0]!.people[0]!.tieBreakCount = 99;
    await vm.saveTieCounts();
    await flushPromises();

    expect(mockConfirm).not.toHaveBeenCalled();
    expect(mockSaveTieCounts).not.toHaveBeenCalled();
    expect(mockShowError).toHaveBeenCalledWith(
      "This election is finalized. People and ballot data cannot be changed.",
    );
  });
});
