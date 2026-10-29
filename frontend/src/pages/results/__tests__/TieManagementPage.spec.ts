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
} = vi.hoisted(() => ({
  mockFetchTieDetails: vi.fn(),
  mockSaveTieCounts: vi.fn(),
  mockFetchResults: vi.fn(),
  mockConfirm: vi.fn(),
  mockShowSuccess: vi.fn(),
  mockShowError: vi.fn(),
  mockShowInfo: vi.fn(),
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
    props: ["modelValue", "valueOnClear"],
    emits: ["update:modelValue", "change"],
    template:
      "<div><input class='tie-count' :value='modelValue' /><button type='button' class='input-clear' @click='onClear'>clear-input</button></div>",
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
});
