import { describe, it, expect, vi, beforeEach } from "vitest";
import { mount, flushPromises } from "@vue/test-utils";
import { createRouter, createWebHistory } from "vue-router";
import GuestTellerAccessToggle from "../GuestTellerAccessToggle.vue";
import { pinia, i18n } from "@/test/setup";

const mockToggleTellerAccess = vi.fn();
const mockFetchElectionById = vi.fn();

vi.mock("@/domain/guestTellerAccess", () => ({
  isFullTeller: () => true,
}));

vi.mock("@/stores/electionStore", () => ({
  useElectionStore: () => ({
    currentElection: {
      electionGuid: "election-1",
      isTellerAccessOpen: false,
    },
    fetchElectionById: mockFetchElectionById,
    toggleTellerAccess: mockToggleTellerAccess,
  }),
}));

vi.mock("@/composables/useNotifications", () => ({
  useNotifications: () => ({
    showSuccessMessage: vi.fn(),
    showErrorMessage: vi.fn(),
  }),
}));

describe("GuestTellerAccessToggle", () => {
  let router: ReturnType<typeof createRouter>;

  beforeEach(() => {
    vi.clearAllMocks();
    router = createRouter({
      history: createWebHistory(),
      routes: [
        {
          path: "/elections/:id",
          name: "Election",
          component: { template: "<div />" },
        },
      ],
    });
  });

  it("renders switch for full teller on election route", async () => {
    await router.push("/elections/election-1");
    await router.isReady();

    const wrapper = mount(GuestTellerAccessToggle, {
      global: {
        plugins: [pinia, router, i18n],
      },
    });

    await flushPromises();

    expect(wrapper.find(".guest-teller-access-toggle").exists()).toBe(true);
    expect(wrapper.text()).toContain("Guest tellers");
  });
});