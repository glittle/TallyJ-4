import { flushPromises, mount } from "@vue/test-utils";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { reactive } from "vue";
import { i18n } from "@/test/setup";
import type { OnlineElectionInfo, OnlineVoteStatus } from "@/types";
import VoterBallotPage from "../VoterBallotPage.vue";

const submitBallot = vi.fn().mockResolvedValue({});

const storeState = reactive({
  voterId: "voter@example.com",
  electionInfo: null as OnlineElectionInfo | null,
  votablePeople: [] as { personGuid: string; fullName: string }[],
  voteStatus: null as OnlineVoteStatus | null,
  loginElsewhereNotice: false,
  restoreSession: vi.fn().mockResolvedValue(true),
  ensureVoterHubsConnected: vi.fn().mockResolvedValue(undefined),
  joinElectionBallotPresence: vi.fn().mockResolvedValue(undefined),
  leaveElectionBallotPresence: vi.fn().mockResolvedValue(undefined),
  loadElectionInfo: vi.fn(),
  checkVoteStatus: vi.fn(),
  loadVotablePeople: vi.fn().mockResolvedValue(undefined),
  submitBallot,
  dismissLoginElsewhereNotice: vi.fn(),
});

vi.mock("vue-router", () => ({
  useRouter: () => ({ push: vi.fn() }),
  useRoute: () => ({ params: { electionId: "election-1" } }),
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
  ElEmpty: { template: "<div />" },
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
    submitBallot.mockClear();
    storeState.electionInfo = openElection;
    storeState.voteStatus = null;
    storeState.restoreSession.mockResolvedValue(true);
    storeState.loadElectionInfo.mockResolvedValue(openElection);
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
});
