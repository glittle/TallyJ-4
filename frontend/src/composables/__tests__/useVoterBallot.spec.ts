import { describe, expect, it } from "vitest";
import {
  buildOnlineVotes,
  createEmptyVoteSlots,
  getDuplicateVotePositions,
  getEffectiveVoteName,
  hasDuplicateVotes,
  useVoterBallotHelpers,
} from "../useVoterBallot";

describe("useVoterBallot", () => {
  it("createEmptyVoteSlots creates numbered slots", () => {
    const slots = createEmptyVoteSlots(3);
    expect(slots).toHaveLength(3);
    expect(slots[0].position).toBe(1);
    expect(slots[2].position).toBe(3);
  });

  it("getEffectiveVoteName returns person name in list mode", () => {
    const slot = createEmptyVoteSlots(1)[0];
    slot.person = {
      personGuid: "abc",
      fullName: "Jane Doe",
    };
    expect(getEffectiveVoteName(slot, "A")).toBe("Jane Doe");
  });

  it("buildOnlineVotes filters empty slots in random mode", () => {
    const slots = createEmptyVoteSlots(2);
    slots[0].freeText = "Alice";
    const votes = buildOnlineVotes(slots, "B");
    expect(votes).toHaveLength(1);
    expect(votes[0].voteName).toBe("Alice");
    expect(votes[0].positionOnBallot).toBe(1);
  });

  it("hasDuplicateVotes detects repeated free-text names in random mode", () => {
    const slots = createEmptyVoteSlots(2);
    slots[0].freeText = "Alice Smith";
    slots[1].freeText = "alice smith";
    expect(hasDuplicateVotes(slots, "B")).toBe(true);
  });

  it("getDuplicateVotePositions returns every duplicated line", () => {
    const slots = createEmptyVoteSlots(3);
    slots[0].person = { personGuid: "p1", fullName: "Alice" };
    slots[0].searchText = "Alice";
    slots[1].person = { personGuid: "p1", fullName: "Alice" };
    slots[1].searchText = "Alice";
    slots[2].searchText = "Bob";

    expect([...getDuplicateVotePositions(slots, "A")].sort()).toEqual([1, 2]);
  });

  it("hasDuplicateVotes detects repeated pool names in both mode", () => {
    const slots = createEmptyVoteSlots(2);
    slots[0].searchText = "Pool Person";
    slots[1].searchText = "Pool Person";
    expect(hasDuplicateVotes(slots, "C")).toBe(true);
  });

  it("submitPoolForm adds entry and clears form", () => {
    const { poolForm, submitPoolForm, poolEntries } = useVoterBallotHelpers(
      () => "C",
    );
    poolForm.value = {
      firstName: "New",
      lastName: "Person",
      otherInfo: "Area 1",
    };
    expect(submitPoolForm()).toBe(true);
    expect(poolEntries.value).toHaveLength(1);
    expect(poolEntries.value[0].fullName).toBe("New Person");
    expect(poolForm.value.firstName).toBe("");
  });

  it("addEntryToNextEmptyVote fills the first empty ballot line", () => {
    const { takePoolFormEntry, addEntryToNextEmptyVote, poolForm } =
      useVoterBallotHelpers(() => "C");
    const slots = createEmptyVoteSlots(2);
    slots[0].searchText = "Already filled";
    poolForm.value = {
      firstName: "New",
      lastName: "Person",
      otherInfo: "note",
    };

    const entry = takePoolFormEntry();
    expect(entry).not.toBeNull();
    expect(addEntryToNextEmptyVote(slots, entry!)).toBe(2);
    expect(slots[1].searchText).toBe("New Person");
    expect(slots[1].person?.fullName).toBe("New Person");
  });

  it("addEntryToNextEmptyVote returns null when the ballot is full", () => {
    const { addEntryToNextEmptyVote } = useVoterBallotHelpers(() => "B");
    const slots = createEmptyVoteSlots(1);
    slots[0].freeText = "Taken";

    expect(
      addEntryToNextEmptyVote(slots, {
        fullName: "Someone Else",
      }),
    ).toBeNull();
  });

  it("applyPriorVotes prefills slots from status", () => {
    const { applyPriorVotes } = useVoterBallotHelpers(() => "A");
    const slots = createEmptyVoteSlots(2);
    const votablePeople = [
      { personGuid: "p1", fullName: "Alice Smith" },
      { personGuid: "p2", fullName: "Bob Jones" },
    ];

    applyPriorVotes(
      slots,
      {
        hasVoted: true,
        priorVotes: [
          { personGuid: "p1", positionOnBallot: 1 },
          { voteName: "Free Name", positionOnBallot: 2 },
        ],
      },
      votablePeople,
    );

    expect(slots[0].person?.fullName).toBe("Alice Smith");
    expect(slots[1].searchText).toBe("Free Name");
  });
});
