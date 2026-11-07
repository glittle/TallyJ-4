import { authService } from "@/services/authService";
import { useElectionStore } from "@/stores/electionStore";
import { i18n } from "@/test/setup";
import type { ElectionDto } from "@/types";
import { createTestingPinia } from "@pinia/testing";
import { flushPromises, mount } from "@vue/test-utils";
import ElementPlus from "element-plus";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { createRouter, createWebHistory } from "vue-router";
import OwnerRestrictionBanner from "./OwnerRestrictionBanner.vue";

vi.mock("@/services/authService", () => ({
  authService: {
    getOwnerRestriction: vi.fn(),
    requestOwnerReview: vi.fn(),
    sendOwnerTestCode: vi.fn(),
  },
}));

const electionGuid = "11111111-1111-1111-1111-111111111111";

describe("OwnerRestrictionBanner", () => {
  beforeEach(() => {
    vi.mocked(authService.getOwnerRestriction).mockReset();
  });

  it("drops the banner and reloads the open election after approval", async () => {
    vi.mocked(authService.getOwnerRestriction)
      .mockResolvedValueOnce({
        status: "ReviewRequested",
        blocksSends: true,
        canRequestReview: false,
      })
      .mockResolvedValueOnce({
        status: "Approved",
        blocksSends: false,
      });

    const pinia = createTestingPinia({ stubActions: true });
    const electionStore = useElectionStore(pinia);
    electionStore.currentElection = {
      electionGuid,
      name: "Test Election",
      paidChannelBlockReason: "owner-restricted",
    } as ElectionDto;

    const router = createRouter({
      history: createWebHistory(),
      routes: [
        { path: "/", component: { template: "<div />" } },
        {
          path: "/elections/:id/edit",
          component: { template: "<div />" },
        },
      ],
    });
    await router.push("/");
    await router.isReady();

    const wrapper = mount(OwnerRestrictionBanner, {
      global: {
        plugins: [pinia, router, i18n, ElementPlus],
      },
    });
    await flushPromises();

    expect(
      wrapper.get('[data-testid="owner-restriction-banner"]').text(),
    ).toContain("separate from verifying your email");
    expect(electionStore.fetchElectionById).not.toHaveBeenCalled();

    await router.push(`/elections/${electionGuid}/edit`);
    await flushPromises();

    expect(
      wrapper.find('[data-testid="owner-restriction-banner"]').exists(),
    ).toBe(false);
    expect(electionStore.fetchElectionById).toHaveBeenCalledWith(electionGuid);
  });
});
