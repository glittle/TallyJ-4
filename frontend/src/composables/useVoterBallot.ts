import { ref } from "vue";
import type {
  OnlinePerson,
  OnlinePoolEntry,
  OnlineVote,
  OnlineVoteStatus,
} from "../types";

export interface VoteSlot {
  position: number;
  person: OnlinePerson | null;
  freeText: string;
  searchText: string;
}

export function createEmptyVoteSlots(count: number): VoteSlot[] {
  return Array.from({ length: count }, (_, i) => ({
    position: i + 1,
    person: null,
    freeText: "",
    searchText: "",
  }));
}

export function getEffectiveVoteName(
  slot: VoteSlot,
  selectionMode: string,
): string {
  if (slot.person) {
    return slot.person.fullName;
  }
  if (selectionMode === "B") {
    return slot.freeText;
  }
  if (selectionMode === "C") {
    return slot.searchText || slot.freeText;
  }
  return slot.searchText;
}

/** Returns true when the same person or name appears on multiple slots. */
export function hasDuplicateVotes(
  votes: VoteSlot[],
  selectionMode: string,
): boolean {
  const seen = new Set<string>();

  for (const slot of votes) {
    const guid = slot.person?.personGuid;
    if (guid && !String(guid).startsWith("pool-")) {
      const key = `guid:${guid}`;
      if (seen.has(key)) {
        return true;
      }
      seen.add(key);
    }

    const name = getEffectiveVoteName(slot, selectionMode).trim().toLowerCase();
    if (name) {
      const key = `name:${name}`;
      if (seen.has(key)) {
        return true;
      }
      seen.add(key);
    }
  }

  return false;
}

export function buildOnlineVotes(
  votes: VoteSlot[],
  selectionMode: string,
): OnlineVote[] {
  return votes
    .filter((v) => {
      if (v.person) {
        return true;
      }
      if (selectionMode === "B") {
        return v.freeText.trim().length > 0;
      }
      if (selectionMode === "C") {
        return v.searchText.trim().length > 0 || v.freeText.trim().length > 0;
      }
      return v.searchText.trim().length > 0;
    })
    .map((v) => ({
      personGuid: v.person?.personGuid,
      voteName: getEffectiveVoteName(v, selectionMode) || undefined,
      positionOnBallot: v.position,
    }));
}

export function useVoterBallotHelpers(selectionMode: () => string) {
  const poolEntries = ref<OnlinePoolEntry[]>([]);
  const notifyWhenProcessed = ref(false);
  const isEditing = ref(false);

  const duplicateVotes = (votes: VoteSlot[]) =>
    hasDuplicateVotes(votes, selectionMode());

  const hasAnyVote = (votes: VoteSlot[]) =>
    votes.some((v) => {
      const mode = selectionMode();
      return (
        v.person !== null ||
        (mode === "B" && v.freeText.trim().length > 0) ||
        (mode === "C" &&
          (v.searchText.trim().length > 0 || v.freeText.trim().length > 0)) ||
        (mode === "A" && v.searchText.trim().length > 0)
      );
    });

  const canSubmit = (votes: VoteSlot[]) =>
    hasAnyVote(votes) && !duplicateVotes(votes);

  function addPoolEntry(entry: OnlinePoolEntry) {
    const exists = poolEntries.value.some(
      (p) => p.fullName.toLowerCase() === entry.fullName.toLowerCase(),
    );
    if (!exists) {
      poolEntries.value.push(entry);
    }
  }

  function poolAsVotablePeople(): OnlinePerson[] {
    return poolEntries.value.map((p, index) => ({
      personGuid: `pool-${index}`,
      fullName: p.fullName,
      otherInfo: p.otherInfo,
    }));
  }

  function applyPriorVotes(
    votes: VoteSlot[],
    status: OnlineVoteStatus,
    votablePeople: OnlinePerson[],
  ) {
    isEditing.value = status.hasVoted;
    notifyWhenProcessed.value = status.notifyWhenProcessed ?? false;
    poolEntries.value = status.listPool ?? [];

    for (const prior of status.priorVotes ?? []) {
      const slot = votes.find((v) => v.position === prior.positionOnBallot);
      if (!slot) {
        continue;
      }

      const matched = votablePeople.find(
        (p) => p.personGuid === prior.personGuid,
      );
      if (matched) {
        slot.person = matched;
        slot.searchText = matched.fullName;
      } else if (prior.voteName) {
        slot.freeText = prior.voteName;
        slot.searchText = prior.voteName;
      }
    }
  }

  const poolForm = ref({
    firstName: "",
    lastName: "",
    otherInfo: "",
  });

  function submitPoolForm() {
    const first = poolForm.value.firstName.trim();
    const last = poolForm.value.lastName.trim();
    if (!first && !last) {
      return false;
    }
    const fullName = [first, last].filter(Boolean).join(" ");
    addPoolEntry({
      fullName,
      firstName: first || undefined,
      lastName: last || undefined,
      otherInfo: poolForm.value.otherInfo.trim() || undefined,
    });
    poolForm.value = { firstName: "", lastName: "", otherInfo: "" };
    return true;
  }

  return {
    poolEntries,
    poolForm,
    notifyWhenProcessed,
    isEditing,
    duplicateVotes,
    hasAnyVote,
    canSubmit,
    addPoolEntry,
    submitPoolForm,
    poolAsVotablePeople,
    applyPriorVotes,
  };
}