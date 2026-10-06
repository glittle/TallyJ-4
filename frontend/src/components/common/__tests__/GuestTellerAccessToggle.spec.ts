import { describe, it, expect, vi, beforeEach } from "vitest";
import { mount, flushPromises } from "@vue/test-utils";
import { createRouter, createWebHistory } from "vue-router";
import GuestTellerAccessToggle from "../GuestTellerAccessToggle.vue";
import { pinia, i18n } from "@/test/setup";

const mockToggleTellerAccess = vi.fn();
const mockFetchElectionById = vi.fn();

const { electionState } = vi.hoisted(() => ({
  electionState: {
    electionGuid: "election-1",
    isTellerAccessOpen: false,
    electionPasscode: "secret",
    tellerLoginLockedUntil: undefined as string | undefined,
  },
}));

vi.mock("@/domain/guestTellerAccess", () => ({
  isFullTeller: () => true,
}));

vi.mock("@/stores/electionStore", () => ({
  useElectionStore: () => ({
    currentElection: electionState,
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
    electionState.tellerLoginLockedUntil = undefined;
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

    expect(wrapper.find(".guest-teller-access-box").exists()).toBe(true);
    expect(wrapper.text()).toContain("Guest tellers");
    expect(wrapper.text()).toContain("Share");
    expect(
      wrapper.find(".guest-teller-access-box").attributes("title"),
    ).toBeUndefined();
  });

  it("shows the owner when guest teller login is locked", async () => {
    electionState.tellerLoginLockedUntil = new Date(
      Date.now() + 15 * 60 * 1000,
    ).toISOString();
    await router.push("/elections/election-1");
    await router.isReady();

    const wrapper = mount(GuestTellerAccessToggle, {
      global: {
        plugins: [pinia, router, i18n],
      },
    });

    await flushPromises();

    const title = wrapper.find(".guest-teller-access-box").attributes("title");
    expect(title).toContain("Guest teller login is locked until");
  });
});
