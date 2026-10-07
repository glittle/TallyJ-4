import { describe, it, expect, vi, beforeEach } from "vitest";
import { flushPromises, mount } from "@vue/test-utils";
import SuperAdminPaidSendsPage from "./SuperAdminPaidSendsPage.vue";
import { pinia, router, i18n } from "@/test/setup";
import ElementPlus from "element-plus";
import { superAdminService } from "@/services/superAdminService";

vi.mock("@/services/superAdminService", () => ({
  superAdminService: {
    getPaidSends: vi.fn(),
    approvePaidSends: vi.fn(),
    clearElectionFlag: vi.fn(),
  },
}));

vi.mock("@/composables/useNotifications", () => ({
  useNotifications: () => ({
    showSuccessMessage: vi.fn(),
    showErrorMessage: vi.fn(),
  }),
}));

const mockedService = vi.mocked(superAdminService);

const ownerId = "11111111-1111-1111-1111-111111111111";
const electionId = "22222222-2222-2222-2222-222222222222";

describe("SuperAdminPaidSendsPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockedService.getPaidSends.mockResolvedValue({
      pendingOwners: [
        {
          userId: ownerId,
          email: "owner@example.com",
          displayName: "Owner",
          electionCount: 1,
        },
      ],
      capHits: [],
      frozenElections: [],
      frozenOwners: [],
      flaggedElections: [
        {
          electionGuid: electionId,
          name: "Flagged election",
          rows: [
            {
              rowNumber: 4,
              maskedValue: "+1*****71",
              reason: "consecutive-run",
            },
          ],
        },
      ],
    });
    mockedService.approvePaidSends.mockResolvedValue();
    mockedService.clearElectionFlag.mockResolvedValue();
  });

  it("approves a pending owner and clears a flagged election", async () => {
    const wrapper = mount(SuperAdminPaidSendsPage, {
      global: {
        plugins: [pinia, router, i18n, ElementPlus],
      },
    });
    await flushPromises();

    expect(wrapper.text()).toContain("owner@example.com");
    expect(wrapper.text()).toContain("Flagged election");
    expect(wrapper.text()).toContain("Consecutive numbers");

    const approve = wrapper
      .findAll("button")
      .find((button) => button.text().includes("Approve paid sends"));
    expect(approve).toBeTruthy();
    await approve!.trigger("click");
    await flushPromises();
    expect(mockedService.approvePaidSends).toHaveBeenCalledWith(ownerId);

    const clear = wrapper
      .findAll("button")
      .find((button) => button.text().includes("Clear flag"));
    expect(clear).toBeTruthy();
    await clear!.trigger("click");
    await flushPromises();
    expect(mockedService.clearElectionFlag).toHaveBeenCalledWith(electionId);
  });
});
