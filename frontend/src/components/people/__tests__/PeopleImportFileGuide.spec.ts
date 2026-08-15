import { describe, it, expect, vi, beforeEach } from "vitest";
import { mount, flushPromises } from "@vue/test-utils";
import ElementPlus from "element-plus";
import PeopleImportFileGuide from "../PeopleImportFileGuide.vue";
import { pinia, i18n } from "@/test/setup";

const mockFetchReasons = vi.fn().mockResolvedValue(undefined);
const writeText = vi.fn().mockResolvedValue(undefined);
const showSuccessMessage = vi.fn();

vi.mock("@/stores/eligibilityStore", () => ({
  useEligibilityStore: () => ({
    groupedReasons: {
      X: [
        {
          reasonGuid: "guid-x01",
          code: "X01",
          description: "Deceased",
          canVote: false,
          canReceiveVotes: false,
          internalOnly: false,
        },
      ],
      V: [],
      R: [],
    },
    fetchReasons: mockFetchReasons,
  }),
}));

vi.mock("@/composables/useNotifications", () => ({
  useNotifications: () => ({
    showSuccessMessage,
    showErrorMessage: vi.fn(),
  }),
}));

describe("PeopleImportFileGuide", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    Object.defineProperty(navigator, "clipboard", {
      configurable: true,
      value: { writeText },
    });
  });

  it("opens the eligibility statuses dialog with codes and descriptions", async () => {
    const wrapper = mount(PeopleImportFileGuide, {
      global: {
        plugins: [pinia, i18n, ElementPlus],
      },
    });

    await flushPromises();

    expect(mockFetchReasons).toHaveBeenCalled();
    expect(wrapper.text()).not.toContain("Under Age");
    expect(wrapper.text()).not.toContain("Duplicate");

    await wrapper.get("button").trigger("click");
    await flushPromises();

    const dialogText = document.body.textContent ?? "";
    expect(dialogText).toContain("X01");
    expect(dialogText).toContain("Deceased");
    expect(dialogText).toContain("Eligible");
    expect(dialogText).not.toContain("Unidentifiable");
  });

  it("copies a code when the copy icon is clicked", async () => {
    const wrapper = mount(PeopleImportFileGuide, {
      global: {
        plugins: [pinia, i18n, ElementPlus],
      },
    });

    await wrapper.get("button").trigger("click");
    await flushPromises();

    const copyButtons = document.body.querySelectorAll(
      'button[aria-label="Copy code"]',
    );
    expect(copyButtons.length).toBeGreaterThan(0);
    (copyButtons[0] as HTMLButtonElement).click();
    await flushPromises();

    expect(writeText).toHaveBeenCalledWith("X01");
    expect(showSuccessMessage).toHaveBeenCalled();
  });
});
