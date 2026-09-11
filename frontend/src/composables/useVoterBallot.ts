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

export function isVoteSlotFilled(
  slot: VoteSlot,
  selectionMode: string,
): boolean {
  if (slot.person) {
    return true;
  }
  if (selectionMode === "B") {
    return slot.freeText.trim().length > 0;
  }
  if (selectionMode === "C") {
    return slot.searchText.trim().length > 0 || slot.freeText.trim().length > 0;
  }
  return slot.searchText.trim().length > 0;
}

/** Positions that share a person guid or visible name with another filled slot. */
export function getDuplicateVotePositions(
  votes: VoteSlot[],
  selectionMode: string,
): Set<number> {
  const guidToPositions = new Map<string, number[]>();
  const nameToPositions = new Map<string, number[]>();

  for (const slot of votes) {
    const guid = slot.person?.personGuid;
    if (guid && !String(guid).startsWith("pool-")) {
      const key = guid.toLowerCase();
      const list = guidToPositions.get(key) ?? [];
      list.push(slot.position);
      guidToPositions.set(key, list);
    }

    const name = getEffectiveVoteName(slot, selectionMode).trim().toLowerCase();
    if (name) {
      const list = nameToPositions.get(name) ?? [];
      list.push(slot.position);
      nameToPositions.set(name, list);
    }
  }

  const duplicates = new Set<number>();
  for (const positions of guidToPositions.values()) {
    if (positions.length > 1) {
      positions.forEach((p) => duplicates.add(p));
    }
  }
  for (const positions of nameToPositions.values()) {
    if (positions.length > 1) {
      positions.forEach((p) => duplicates.add(p));
    }
  }
  return duplicates;
}

/** Returns true when the same person or name appears on multiple slots. */
export function hasDuplicateVotes(
  votes: VoteSlot[],
  selectionMode: string,
): boolean {
  return getDuplicateVotePositions(votes, selectionMode).size > 0;
}

/**
 * Autosave is Draft only before the first Submitted write. After Submit, or
 * when status already has whenSubmitted, keep isDraft false so the client
 * does not ask the server to demote Accept-all pending.
 */
export function autosaveAsDraft(alreadySubmitted: boolean): boolean {
  return !alreadySubmitted;
}

/** Draft restores have hasVoted but no whenSubmitted. */
export function isSubmittedOnlineVoteStatus(
  status: Pick<OnlineVoteStatus, "whenSubmitted"> | null | undefined,
): boolean {
  return status?.whenSubmitted !== undefined && status?.whenSubmitted !== null;
}

/**
 * Silent autosave writes when the ballot has names, or when a saved payload
 * already exists (including after the voter clears the last name). A first
 * visit with no names does not create a Draft.
 */
export function shouldWriteAutosave(
  hasVotes: boolean,
  hasPersistedPayload: boolean,
): boolean {
  return hasVotes || hasPersistedPayload;
}

export function buildOnlineVotes(
  votes: VoteSlot[],
  selectionMode: string,
): OnlineVote[] {
  return votes
    .filter((v) => isVoteSlotFilled(v, selectionMode))
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

  const duplicatePositions = (votes: VoteSlot[]) =>
    getDuplicateVotePositions(votes, selectionMode());

  const hasAnyVote = (votes: VoteSlot[]) =>
    votes.some((v) => isVoteSlotFilled(v, selectionMode()));

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
    isEditing.value = isSubmittedOnlineVoteStatus(status);
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

  function clearPoolForm() {
    poolForm.value = { firstName: "", lastName: "", otherInfo: "" };
  }

  /**
   * Read a pool entry from the form; returns null if name is empty.
   * Does not clear the form — call {@link clearPoolForm} after a successful
   * placement so a full-ballot refusal keeps the entered name.
   */
  function takePoolFormEntry(): OnlinePoolEntry | null {
    const first = poolForm.value.firstName.trim();
    const last = poolForm.value.lastName.trim();
    if (!first && !last) {
      return null;
    }
    const fullName = [first, last].filter(Boolean).join(" ");
    return {
      fullName,
      firstName: first || undefined,
      lastName: last || undefined,
      otherInfo: poolForm.value.otherInfo.trim() || undefined,
    };
  }

  /**
   * Place the entry on the first empty ballot line. Returns the position, or
   * null when the ballot is full.
   */
  function addEntryToNextEmptyVote(
    votes: VoteSlot[],
    entry: OnlinePoolEntry,
  ): number | null {
    const mode = selectionMode();
    const empty = votes.find((slot) => !isVoteSlotFilled(slot, mode));
    if (!empty) {
      return null;
    }

    addPoolEntry(entry);
    const poolPerson = poolAsVotablePeople().find(
      (p) => p.fullName.toLowerCase() === entry.fullName.toLowerCase(),
    );

    empty.person = poolPerson ?? {
      personGuid: `pool-${entry.fullName}`,
      fullName: entry.fullName,
      otherInfo: entry.otherInfo,
    };
    empty.searchText = entry.fullName;
    empty.freeText = entry.fullName;
    return empty.position;
  }

  function submitPoolForm() {
    const entry = takePoolFormEntry();
    if (!entry) {
      return false;
    }
    addPoolEntry(entry);
    clearPoolForm();
    return true;
  }

  /**
   * Read the pool form and place it on the first empty line. Clears the form
   * only after a successful placement. A full ballot leaves the form intact.
   */
  function placePoolFormOnBallot(
    votes: VoteSlot[],
  ): number | "empty-name" | "full" {
    const entry = takePoolFormEntry();
    if (!entry) {
      return "empty-name";
    }
    const position = addEntryToNextEmptyVote(votes, entry);
    if (position === null) {
      return "full";
    }
    clearPoolForm();
    return position;
  }

  return {
    poolEntries,
    poolForm,
    notifyWhenProcessed,
    isEditing,
    duplicateVotes,
    duplicatePositions,
    hasAnyVote,
    canSubmit,
    addPoolEntry,
    takePoolFormEntry,
    clearPoolForm,
    addEntryToNextEmptyVote,
    placePoolFormOnBallot,
    submitPoolForm,
    poolAsVotablePeople,
    applyPriorVotes,
  };
}
