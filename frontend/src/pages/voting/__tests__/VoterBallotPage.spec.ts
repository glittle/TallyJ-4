import { flushPromises, mount } from "@vue/test-utils";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { reactive } from "vue";
import { i18n } from "@/test/setup";
import type {
  AvailableElection,
  OnlineElectionInfo,
  OnlineVoteStatus,
} from "@/types";
import { forgetRememberedVoterElection } from "@/utils/voterElectionContext";
import VoterBallotPage from "../VoterBallotPage.vue";

const submitBallot = vi.fn().mockResolvedValue({});
const logout = vi.fn().mockResolvedValue(undefined);
const push = vi.fn();
const replace = vi.fn().mockResolvedValue(undefined);

const routeState = reactive({
  params: { electionId: "election-1" } as Record<string, string | undefined>,
});

const storeState = reactive({
  voterId: "voter@example.com" as string | null,
  electionInfo: null as OnlineElectionInfo | null,
  votablePeople: [] as { personGuid: string; fullName: string }[],
  voteStatus: null as OnlineVoteStatus | null,
  loginElsewhereNotice: false,
  isKioskSession: false,
  restoreSession: vi.fn().mockResolvedValue(true),
  ensureVoterHubsConnected: vi.fn().mockResolvedValue(undefined),
  joinElectionBallotPresence: vi.fn().mockResolvedValue(undefined),
  leaveElectionBallotPresence: vi.fn().mockResolvedValue(undefined),
  loadElectionInfo: vi.fn(),
  loadAvailableElections: vi.fn().mockResolvedValue([]),
  checkVoteStatus: vi.fn(),
  loadVotablePeople: vi.fn().mockResolvedValue(undefined),
  submitBallot,
  logout,
  dismissLoginElsewhereNotice: vi.fn(),
});

vi.mock("vue-router", () => ({
  useRouter: () => ({ push, replace }),
  useRoute: () => routeState,
}));

vi.mock("@/composables/useNotifications", () => ({
  useNotifications: () => ({
    showSuccessMessage: vi.fn(),
    showErrorMessage: vi.fn(),
  }),
}));

vi.mock("@/stores/onlineVotingStore", () => ({
  useOnlineVotingStore: () => storeState,
}));

const stubs = {
  ElAlert: { template: "<div><slot /></div>" },
  ElAutocomplete: { template: "<input />" },
  ElButton: {
    template: "<button v-bind='$attrs'><slot /></button>",
  },
  ElCard: { template: "<div><slot name='header' /><slot /></div>" },
  ElCheckbox: { template: "<input type='checkbox' />" },
  ElDivider: { template: "<hr />" },
  ElEmpty: { template: "<div class='election-empty' />" },
  ElForm: { template: "<form><slot /></form>" },
  ElFormItem: { template: "<div><slot /></div>" },
  ElInput: { template: "<input />" },
  ElTag: { template: "<span><slot /></span>" },
};

const openElection: OnlineElectionInfo = {
  electionGuid: "election-1",
  name: "Open election",
  isOpen: true,
  numberToElect: 2,
  onlineSelectionProcess: "B",
};

function submittedStatus(): OnlineVoteStatus {
  return {
    hasVoted: true,
    canChangeVote: true,
    whenSubmitted: new Date("2026-09-11T00:00:00Z"),
    priorVotes: [{ voteName: "Alice Smith", positionOnBallot: 1 }],
  };
}

function availableElection(
  electionGuid: string,
  isOpen: boolean,
): AvailableElection {
  return { electionGuid, name: electionGuid, isOpen };
}

function draftStatus(): OnlineVoteStatus {
  return {
    hasVoted: true,
    canChangeVote: true,
    whenSubmitted: null,
    priorVotes: [{ voteName: "Draft Name", positionOnBallot: 1 }],
  };
}

describe("VoterBallotPage silent autosave isDraft", () => {
  beforeEach(() => {
    vi.useFakeTimers();
    sessionStorage.clear();
    submitBallot.mockClear();
    logout.mockClear();
    push.mockClear();
    replace.mockClear();
    routeState.params.electionId = "election-1";
    storeState.voterId = "voter@example.com";
    storeState.isKioskSession = false;
    storeState.electionInfo = openElection;
    storeState.voteStatus = null;
    storeState.restoreSession.mockReset();
    storeState.restoreSession.mockResolvedValue(true);
    storeState.ensureVoterHubsConnected.mockReset();
    storeState.ensureVoterHubsConnected.mockResolvedValue(undefined);
    storeState.loadElectionInfo.mockReset();
    storeState.loadElectionInfo.mockResolvedValue(openElection);
    storeState.loadAvailableElections.mockReset();
    storeState.loadAvailableElections.mockResolvedValue([]);
    storeState.joinElectionBallotPresence.mockReset();
    storeState.joinElectionBallotPresence.mockResolvedValue(undefined);
    storeState.leaveElectionBallotPresence.mockClear();
    storeState.checkVoteStatus.mockResolvedValue(draftStatus());
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  async function mountAndFlushAutosave() {
    const wrapper = mount(VoterBallotPage, {
      global: {
        plugins: [i18n],
        stubs,
      },
    });
    await flushPromises();
    await vi.advanceTimersByTimeAsync(800);
    await flushPromises();
    return wrapper;
  }

  it("keeps isDraft false after loading a Submitted ballot", async () => {
    storeState.checkVoteStatus.mockResolvedValue(submittedStatus());
    storeState.voteStatus = submittedStatus();

    await mountAndFlushAutosave();

    expect(submitBallot).toHaveBeenCalled();
    for (const [, payload] of submitBallot.mock.calls) {
      expect(payload.isDraft).toBe(false);
    }
  });

  it("sends isDraft true when restoring a Draft ballot", async () => {
    storeState.checkVoteStatus.mockResolvedValue(draftStatus());
    storeState.voteStatus = draftStatus();

    await mountAndFlushAutosave();

    expect(submitBallot).toHaveBeenCalled();
    expect(submitBallot.mock.calls[0][1].isDraft).toBe(true);
  });

  it("does not autosave an empty first visit", async () => {
    storeState.checkVoteStatus.mockResolvedValue({
      hasVoted: false,
      canChangeVote: true,
      priorVotes: [],
    });
    storeState.voteStatus = {
      hasVoted: false,
      canChangeVote: true,
      priorVotes: [],
    };

    await mountAndFlushAutosave();

    expect(submitBallot).not.toHaveBeenCalled();
  });

  it("overwrites a saved Draft when the last vote is cleared", async () => {
    storeState.checkVoteStatus.mockResolvedValue(draftStatus());
    storeState.voteStatus = draftStatus();

    const wrapper = await mountAndFlushAutosave();
    expect(submitBallot).toHaveBeenCalled();
    expect(submitBallot.mock.calls[0][1].votes).toEqual(
      expect.arrayContaining([
        expect.objectContaining({ voteName: "Draft Name" }),
      ]),
    );
    submitBallot.mockClear();

    await wrapper.get(".clear-btn").trigger("click");
    await vi.advanceTimersByTimeAsync(800);
    await flushPromises();

    expect(submitBallot).toHaveBeenCalled();
    const cleared = submitBallot.mock.calls.at(-1)?.[1];
    expect(cleared.votes).toEqual([]);
    expect(cleared.isDraft).toBe(true);

    wrapper.unmount();
    submitBallot.mockClear();
    storeState.checkVoteStatus.mockResolvedValue({
      hasVoted: true,
      canChangeVote: true,
      whenSubmitted: null,
      priorVotes: cleared.votes,
    });
    storeState.voteStatus = {
      hasVoted: true,
      canChangeVote: true,
      whenSubmitted: null,
      priorVotes: cleared.votes,
    };

    await mountAndFlushAutosave();

    expect(submitBallot).toHaveBeenCalled();
    expect(submitBallot.mock.calls[0][1].votes).toEqual([]);
    expect(submitBallot.mock.calls[0][1].isDraft).toBe(true);
  });

  it("overwrites a Submitted payload with isDraft false when cleared", async () => {
    storeState.checkVoteStatus.mockResolvedValue(submittedStatus());
    storeState.voteStatus = submittedStatus();

    const wrapper = await mountAndFlushAutosave();
    submitBallot.mockClear();

    await wrapper.get(".clear-btn").trigger("click");
    await vi.advanceTimersByTimeAsync(800);
    await flushPromises();

    const cleared = submitBallot.mock.calls.at(-1)?.[1];
    expect(cleared.votes).toEqual([]);
    expect(cleared.isDraft).toBe(false);
  });

  it("keeps isDraft false on later autosave after explicit Submit", async () => {
    storeState.checkVoteStatus.mockResolvedValue(draftStatus());
    storeState.voteStatus = draftStatus();

    const wrapper = await mountAndFlushAutosave();
    submitBallot.mockClear();

    await wrapper.get("form").trigger("submit");
    await flushPromises();
    await vi.advanceTimersByTimeAsync(800);
    await flushPromises();

    expect(submitBallot).toHaveBeenCalled();
    const explicit = submitBallot.mock.calls[0];
    expect(explicit[1].isDraft).toBe(false);
    for (const [, payload] of submitBallot.mock.calls.slice(1)) {
      expect(payload.isDraft).toBe(false);
    }
  });

  it("shows already-voted-another-way when the vote is locked that way", async () => {
    storeState.checkVoteStatus.mockResolvedValue({
      hasVoted: true,
      canChangeVote: false,
      message: "voting.status.alreadyVotedAnotherWay",
      priorVotes: [],
    });
    storeState.voteStatus = {
      hasVoted: true,
      canChangeVote: false,
      message: "voting.status.alreadyVotedAnotherWay",
      priorVotes: [],
    };

    const wrapper = await mountAndFlushAutosave();
    const alert = wrapper.get("[data-testid='ballot-processed-alert']");
    expect(alert.text()).toContain(
      i18n.global.t("voting.status.alreadyVotedAnotherWay"),
    );
    expect(submitBallot).not.toHaveBeenCalled();
  });

  it("loads the route election without waiting for voter hubs", async () => {
    let releaseHubs: (() => void) | undefined;
    storeState.ensureVoterHubsConnected.mockImplementation(
      () =>
        new Promise<void>((resolve) => {
          releaseHubs = resolve;
        }),
    );

    const wrapper = mount(VoterBallotPage, {
      global: { plugins: [i18n], stubs },
    });
    await flushPromises();

    expect(storeState.loadElectionInfo).toHaveBeenCalledWith("election-1");
    expect(storeState.checkVoteStatus).toHaveBeenCalledWith(
      "election-1",
      "voter@example.com",
    );
    expect(wrapper.find(".election-empty").exists()).toBe(false);
    expect(wrapper.text()).toContain("Open election");

    releaseHubs?.();
    await flushPromises();
    storeState.ensureVoterHubsConnected.mockResolvedValue(undefined);
  });

  it("does not show election not found while the voter session is restoring", async () => {
    let releaseRestore: ((ok: boolean) => void) | undefined;
    storeState.restoreSession.mockImplementation(
      () =>
        new Promise<boolean>((resolve) => {
          releaseRestore = resolve;
        }),
    );
    storeState.electionInfo = null;

    const wrapper = mount(VoterBallotPage, {
      global: { plugins: [i18n], stubs },
    });
    await flushPromises();

    expect(wrapper.find(".election-empty").exists()).toBe(false);
    expect(wrapper.text()).toContain("Loading election information...");

    releaseRestore?.(true);
    await flushPromises();
    storeState.restoreSession.mockResolvedValue(true);
  });

  it("restores a kiosk ballot when the reload URL has no election id", async () => {
    const guid = "f3f1ef0b-1d76-4117-82f4-b72b99a28fb1";
    routeState.params.electionId = undefined;
    storeState.isKioskSession = true;
    storeState.voterId = "SMART.f3f1ef0b1d76411782f4b72b99a28fb1";
    storeState.electionInfo = null;
    storeState.loadElectionInfo.mockImplementation(async (id: string) => {
      storeState.electionInfo = { ...openElection, electionGuid: id };
      return storeState.electionInfo;
    });

    const wrapper = mount(VoterBallotPage, {
      global: { plugins: [i18n], stubs },
    });
    await flushPromises();

    expect(storeState.loadElectionInfo).toHaveBeenCalledWith(guid);
    expect(storeState.checkVoteStatus).toHaveBeenCalledWith(
      guid,
      storeState.voterId,
    );
    expect(replace).toHaveBeenCalledWith({
      name: "voter-ballot",
      params: { electionId: guid },
    });
    expect(wrapper.find(".election-empty").exists()).toBe(false);
    expect(wrapper.text()).toContain("Open election");
    expect(sessionStorage.getItem("tallyj.voterElectionGuid")).toBe(guid);
  });

  it("does not use a remembered election after logout", async () => {
    sessionStorage.setItem("tallyj.voterElectionGuid", "previous-election");
    forgetRememberedVoterElection();
    routeState.params.electionId = undefined;
    storeState.electionInfo = null;
    storeState.loadAvailableElections.mockResolvedValue([
      availableElection("previous-election", true),
      availableElection("second-open", true),
    ]);

    const wrapper = mount(VoterBallotPage, {
      global: { plugins: [i18n], stubs },
    });
    await flushPromises();

    expect(storeState.loadElectionInfo).not.toHaveBeenCalled();
    expect(replace).not.toHaveBeenCalled();
    expect(wrapper.find(".election-empty").exists()).toBe(true);
  });

  it("ignores a remembered election that is not in this voter's list", async () => {
    sessionStorage.setItem("tallyj.voterElectionGuid", "previous-election");
    routeState.params.electionId = undefined;
    storeState.electionInfo = null;
    storeState.loadElectionInfo.mockImplementation(async (id: string) => {
      storeState.electionInfo = {
        ...openElection,
        electionGuid: id,
        name: "Only open",
      };
      return storeState.electionInfo;
    });
    storeState.loadAvailableElections.mockResolvedValue([
      availableElection("only-open", true),
    ]);

    const wrapper = mount(VoterBallotPage, {
      global: { plugins: [i18n], stubs },
    });
    await flushPromises();

    expect(storeState.loadElectionInfo).toHaveBeenCalledWith("only-open");
    expect(storeState.loadElectionInfo).not.toHaveBeenCalledWith(
      "previous-election",
    );
    expect(replace).toHaveBeenCalledWith({
      name: "voter-ballot",
      params: { electionId: "only-open" },
    });
    expect(wrapper.text()).toContain("Only open");
  });

  it("puts the only open election on the address when nothing else identifies one", async () => {
    routeState.params.electionId = undefined;
    storeState.electionInfo = null;
    storeState.loadElectionInfo.mockImplementation(async (id: string) => {
      storeState.electionInfo = {
        ...openElection,
        electionGuid: id,
        name: "Sole election",
      };
      return storeState.electionInfo;
    });
    storeState.loadAvailableElections.mockResolvedValue([
      availableElection("sole-election", false),
      availableElection("sole-open", true),
    ]);

    const wrapper = mount(VoterBallotPage, {
      global: { plugins: [i18n], stubs },
    });
    await flushPromises();

    expect(storeState.loadElectionInfo).toHaveBeenCalledWith("sole-open");
    expect(replace).toHaveBeenCalledWith({
      name: "voter-ballot",
      params: { electionId: "sole-open" },
    });
    expect(wrapper.find(".election-empty").exists()).toBe(false);
    expect(wrapper.text()).toContain("Sole election");
  });

  it("shows election not found when two elections are open and none is remembered", async () => {
    routeState.params.electionId = undefined;
    storeState.electionInfo = null;
    storeState.loadAvailableElections.mockResolvedValue([
      availableElection("open-a", true),
      availableElection("open-b", true),
    ]);

    const wrapper = mount(VoterBallotPage, {
      global: { plugins: [i18n], stubs },
    });
    await flushPromises();

    expect(storeState.loadElectionInfo).not.toHaveBeenCalled();
    expect(replace).not.toHaveBeenCalled();
    expect(wrapper.find(".election-empty").exists()).toBe(true);
  });

  it("does not show another election when the requested one fails to load", async () => {
    storeState.electionInfo = {
      ...openElection,
      electionGuid: "other-election",
      name: "Other election",
    };
    storeState.loadElectionInfo.mockRejectedValue(new Error("missing"));

    const wrapper = mount(VoterBallotPage, {
      global: { plugins: [i18n], stubs },
    });
    await flushPromises();

    expect(wrapper.find(".election-empty").exists()).toBe(true);
    expect(wrapper.text()).not.toContain("Other election");
  });

  it("does not join ballot presence after the page has unmounted", async () => {
    let releaseHubs: (() => void) | undefined;
    storeState.ensureVoterHubsConnected.mockImplementation(
      () =>
        new Promise<void>((resolve) => {
          releaseHubs = resolve;
        }),
    );

    const wrapper = mount(VoterBallotPage, {
      global: { plugins: [i18n], stubs },
    });
    await flushPromises();
    wrapper.unmount();
    releaseHubs?.();
    await flushPromises();

    expect(storeState.joinElectionBallotPresence).not.toHaveBeenCalled();
    expect(storeState.leaveElectionBallotPresence).toHaveBeenCalled();
    storeState.ensureVoterHubsConnected.mockResolvedValue(undefined);
  });

  it("logs out after kiosk submit so the next voter starts clean", async () => {
    storeState.isKioskSession = true;
    storeState.checkVoteStatus.mockResolvedValue(draftStatus());
    storeState.voteStatus = draftStatus();

    const wrapper = await mountAndFlushAutosave();
    submitBallot.mockClear();

    await wrapper.get("form").trigger("submit");
    await flushPromises();

    expect(submitBallot).toHaveBeenCalled();
    expect(logout).toHaveBeenCalled();
    expect(push).toHaveBeenCalledWith({
      name: "voter-confirmation",
      query: { kiosk: "1" },
    });
  });
});
