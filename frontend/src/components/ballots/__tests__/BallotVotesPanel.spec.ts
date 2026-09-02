import type { VoteDto } from "@/types/Vote";
import { mount } from "@vue/test-utils";
import { ElButton, ElIcon } from "element-plus";
import { describe, expect, it } from "vitest";
import BallotVotesPanel from "../BallotVotesPanel.vue";

const t = (key: string) => {
  const translations: Record<string, string> = {
    "ballots.namesOnBallot": "Names on the ballot",
    "ballots.ballotNum": "{code}",
    "ballots.findRawName": "Find",
    "ballots.findRawNameHint": "Widen the search",
    "ballots.needsNameResolution": "Needs matching",
    "ballots.needsNameResolutionHint":
      "The voter typed this name on their online ballot.",
    "ballots.changeRawName": "Change",
    "ballots.dragToReorder": "Drag to reorder",
    "ballots.duplicateWarning": "Duplicate",
    "common.delete": "Delete",
  };
  return translations[key] || key;
};

function vote(overrides: Partial<VoteDto> = {}): VoteDto {
  return {
    rowId: 1,
    ballotGuid: "ballot-ol",
    positionOnBallot: 1,
    statusCode: "Raw",
    onlineVoteRaw:
      '{"First":"Jonathan","Last":"Smythe","OtherInfo":"Jonathan Smythe"}',
    ...overrides,
  };
}

function persist(vote: VoteDto | null | undefined) {
  return !!vote?.rowId;
}

function mountPanel(votes: (VoteDto | null)[]) {
  return mount(BallotVotesPanel, {
    props: {
      votes,
      ballotCode: "Online 3",
      canReorderVotes: false,
      reorderingVotes: false,
      dragSourceIndex: null,
      dragOverIndex: null,
      duplicatePersonGuids: [],
      targetedVoteRowId: null,
      canRemoveVotes: false,
      canSelectAnyVote: true,
      isPersistedVote: persist,
    },
    global: {
      components: { ElButton, ElIcon },
      mocks: { $t: t },
    },
  });
}

describe("BallotVotesPanel online typed-name resolution", () => {
  it("marks an unmatched online typed name and not a matched one", () => {
    const unmatched = vote({ rowId: 11 });
    const matched = vote({
      rowId: 12,
      positionOnBallot: 2,
      statusCode: "ok",
      personGuid: "p1",
      personFullName: "Smythe, Jonathan",
    });
    const paperTellerVote = vote({
      rowId: 13,
      positionOnBallot: 3,
      statusCode: "ok",
      personGuid: "p2",
      personFullName: "Ada Lovelace",
      onlineVoteRaw: undefined,
    });

    const wrapper = mountPanel([unmatched, matched, paperTellerVote]);
    const rows = wrapper.findAll(".vote-row");

    expect(rows[0].classes()).toContain("is-raw-unresolved");
    expect(rows[0].find(".needs-resolution").text()).toContain(
      "Needs matching",
    );
    expect(rows[0].find(".raw-name").text()).toBe("Jonathan Smythe");
    expect(rows[0].find(".vote-name").exists()).toBe(false);

    expect(rows[1].classes()).not.toContain("is-raw-unresolved");
    expect(rows[1].find(".needs-resolution").exists()).toBe(false);
    expect(rows[1].find(".raw-name").text()).toBe("Jonathan Smythe");
    expect(rows[1].find(".vote-name").text()).toBe("Smythe, Jonathan");

    expect(rows[2].classes()).not.toContain("is-raw-unresolved");
    expect(rows[2].find(".needs-resolution").exists()).toBe(false);
    expect(rows[2].find(".raw-name").exists()).toBe(false);
    expect(rows[2].find(".vote-name").text()).toBe("Ada Lovelace");
  });
});
