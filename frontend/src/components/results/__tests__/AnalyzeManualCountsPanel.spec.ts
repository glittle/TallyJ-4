import { flushPromises, mount } from "@vue/test-utils";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { i18n } from "@/test/setup";
import AnalyzeManualCountsPanel from "../AnalyzeManualCountsPanel.vue";

const { mockFetch, mockSave, mockShowSuccess } = vi.hoisted(() => ({
  mockFetch: vi.fn(),
  mockSave: vi.fn(),
  mockShowSuccess: vi.fn(),
}));

vi.mock("@/composables/useNotifications", () => ({
  useNotifications: () => ({
    showSuccessMessage: mockShowSuccess,
    showErrorMessage: vi.fn(),
  }),
}));

vi.mock("@/composables/useApiErrorHandler", () => ({
  useApiErrorHandler: () => ({ handleApiError: vi.fn() }),
}));

const storeState = {
  loading: false,
  analyzeCounts: {
    calculated: { numEligibleToVote: 6 },
    manual: { numEligibleToVote: 5 },
    final: { numEligibleToVote: 5 },
  },
  fetchManualCounts: mockFetch,
  saveManualCounts: mockSave,
};

vi.mock("@/stores/resultStore", () => ({
  useResultStore: () => storeState,
}));

describe("AnalyzeManualCountsPanel", () => {
  beforeEach(() => {
    mockFetch.mockReset();
    mockSave.mockReset();
    mockShowSuccess.mockReset();
    mockFetch.mockResolvedValue(storeState.analyzeCounts);
    mockSave.mockResolvedValue(storeState.analyzeCounts);
  });

  it("loads calculated and override eligible voters and saves the override", async () => {
    const wrapper = mount(AnalyzeManualCountsPanel, {
      props: { electionGuid: "election-1" },
      global: { plugins: [i18n] },
    });
    await flushPromises();

    expect(mockFetch).toHaveBeenCalledWith("election-1");
    expect(wrapper.text()).toContain("Eligible Voters");
    expect(wrapper.text()).toContain("6");

    await wrapper.get('[data-testid="save-manual-counts"]').trigger("click");
    await flushPromises();

    expect(mockSave).toHaveBeenCalledWith(
      "election-1",
      expect.objectContaining({ numEligibleToVote: 5 }),
    );
    expect(mockShowSuccess).toHaveBeenCalled();
  });
});
