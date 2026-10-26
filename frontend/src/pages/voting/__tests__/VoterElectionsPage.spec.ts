import { flushPromises, mount } from "@vue/test-utils";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { reactive } from "vue";
import { i18n } from "@/test/setup";
import type { AvailableElection } from "@/types";
import VoterElectionsPage from "../VoterElectionsPage.vue";

const storeState = reactive({
  loginElsewhereNotice: false,
  voterId: "voter@example.com",
  isKioskSession: false,
  loading: false,
  availableElections: [] as AvailableElection[],
  restoreSession: vi.fn().mockResolvedValue(true),
  loadAvailableElections: vi.fn().mockResolvedValue(undefined),
  ensureVoterHubsConnected: vi.fn().mockResolvedValue(undefined),
  logout: vi.fn(),
  dismissLoginElsewhereNotice: vi.fn(),
});

vi.mock("vue-router", () => ({
  useRouter: () => ({ push: vi.fn() }),
}));

vi.mock("@/composables/useNotifications", () => ({
  useNotifications: () => ({
    showErrorMessage: vi.fn(),
  }),
}));

vi.mock("@/stores/onlineVotingStore", () => ({
  useOnlineVotingStore: () => storeState,
}));

const stubs = {
  ElAlert: { template: "<div><slot /></div>" },
  ElButton: {
    template: "<button v-bind='$attrs'><slot /></button>",
  },
  ElEmpty: { template: "<div />" },
  ElTag: { template: "<span><slot /></span>" },
};

function votedElection(
  overrides: Partial<AvailableElection>,
): AvailableElection {
  return {
    electionGuid: "election-1",
    name: "Open election",
    isOpen: true,
    hasOnlineVoting: true,
    hasVoted: true,
    onlineWhenClose: new Date(Date.now() + 60 * 60 * 1000),
    onlineCloseIsEstimate: false,
    ...overrides,
  };
}

describe("VoterElectionsPage Edit visibility", () => {
  beforeEach(() => {
    storeState.availableElections = [];
    storeState.isKioskSession = false;
    storeState.voterId = "voter@example.com";
    storeState.restoreSession.mockResolvedValue(true);
    storeState.loadAvailableElections.mockResolvedValue(undefined);
    storeState.ensureVoterHubsConnected.mockResolvedValue(undefined);
  });

  async function mountPage(election: AvailableElection) {
    storeState.availableElections = [election];
    const wrapper = mount(VoterElectionsPage, {
      global: {
        plugins: [i18n],
        stubs,
      },
    });
    await flushPromises();
    return wrapper;
  }

  it("does not show Edit when status is Processing", async () => {
    const wrapper = await mountPage(
      votedElection({
        ballotStatus: "Processing",
        canChangeVote: false,
      }),
    );

    expect(wrapper.find("[data-testid='edit-ballot']").exists()).toBe(false);
    expect(wrapper.find("[data-testid='cannot-change-vote']").exists()).toBe(
      true,
    );
  });

  it("does not show Edit when status is Processed", async () => {
    const wrapper = await mountPage(
      votedElection({
        ballotStatus: "Processed",
        canChangeVote: false,
      }),
    );

    expect(wrapper.find("[data-testid='edit-ballot']").exists()).toBe(false);
    expect(wrapper.find("[data-testid='cannot-change-vote']").exists()).toBe(
      true,
    );
  });

  it("shows Edit when Submitted without BallotGuid (canChangeVote)", async () => {
    const wrapper = await mountPage(
      votedElection({
        ballotStatus: "Submitted",
        canChangeVote: true,
      }),
    );

    expect(wrapper.find("[data-testid='edit-ballot']").exists()).toBe(true);
    expect(wrapper.find("[data-testid='cannot-change-vote']").exists()).toBe(
      false,
    );
  });

  it("does not show Edit for legacy Submitted when canChangeVote is false", async () => {
    const wrapper = await mountPage(
      votedElection({
        ballotStatus: "Submitted",
        canChangeVote: false,
      }),
    );

    expect(wrapper.find("[data-testid='edit-ballot']").exists()).toBe(false);
    expect(wrapper.find("[data-testid='cannot-change-vote']").exists()).toBe(
      true,
    );
  });

  it("does not show the kiosk code or election guid in the welcome", async () => {
    storeState.isKioskSession = true;
    storeState.voterId = "AB.11111111-1111-1111-1111-111111111111";
    const wrapper = await mountPage(votedElection({}));

    expect(wrapper.text()).toContain("shared kiosk");
    expect(wrapper.text()).not.toContain(storeState.voterId);
    expect(wrapper.text()).not.toContain("11111111");
  });
});
