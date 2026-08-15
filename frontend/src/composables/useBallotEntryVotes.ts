import type { BallotDto } from "@/types/Ballot";
import type { VoteDto } from "@/types/Vote";
import { computed, ref, watch, type Ref } from "vue";

const MAX_BALLOT_SLOTS = 50;

export type VoteAddedOptions = {
  fromNewPerson?: boolean;
};

export type UseBallotEntryVotesOptions = {
  ballot: Ref<BallotDto>;
  requiredVotes: Ref<number>;
  resyncKey?: Ref<number | undefined>;
  onVoteAdded?: (vote: VoteDto, options?: VoteAddedOptions) => void;
  onVoteRemoved?: (positionOnBallot: number) => void;
  onVotesReordered?: (voteRowIds: number[]) => void;
  onBallotFull?: () => void;
};

/**
 * Manages ballot vote slots, optimistic local state, and drag-reorder.
 * Keeps InlineBallotEntry thin so search UI / actions can live in siblings.
 */
export function useBallotEntryVotes(options: UseBallotEntryVotesOptions) {
  const votes = ref<(VoteDto | null)[]>([]);
  const dragSourceIndex = ref<number | null>(null);
  const dragOverIndex = ref<number | null>(null);
  const reorderingVotes = ref(false);

  function buildVoteMap(includeOptimistic: boolean): Map<number, VoteDto> {
    const merged = new Map<number, VoteDto>();
    for (const vote of options.ballot.value.votes) {
      merged.set(vote.positionOnBallot, vote);
    }
    if (includeOptimistic) {
      for (const localVote of votes.value) {
        if (!localVote || localVote.rowId !== 0) {
          continue;
        }
        const persistedVote = merged.get(localVote.positionOnBallot);
        if (persistedVote && persistedVote.rowId > 0) {
          continue;
        }
        merged.set(localVote.positionOnBallot, localVote);
      }
    }
    return merged;
  }

  function computeSlotCount(merged: Map<number, VoteDto>): number {
    const highestFilled = merged.size > 0 ? Math.max(...merged.keys()) : 0;
    return Math.min(
      MAX_BALLOT_SLOTS,
      Math.max(options.requiredVotes.value, highestFilled),
    );
  }

  function slotsFromMap(merged: Map<number, VoteDto>): (VoteDto | null)[] {
    const slots = computeSlotCount(merged);
    const voteArray: (VoteDto | null)[] = [];
    for (let i = 1; i <= slots; i++) {
      voteArray.push(merged.get(i) ?? null);
    }
    return voteArray;
  }

  function rebuildVoteSlots(includeOptimistic = true) {
    votes.value = slotsFromMap(buildVoteMap(includeOptimistic));
  }

  watch(
    () => options.ballot.value,
    () => {
      rebuildVoteSlots(true);
      reorderingVotes.value = false;
    },
    { immediate: true, deep: true },
  );

  if (options.resyncKey) {
    watch(
      () => options.resyncKey!.value,
      () => {
        rebuildVoteSlots(false);
        reorderingVotes.value = false;
      },
    );
  }

  const hasUnpersistedVote = computed(() =>
    votes.value.some((vote) => vote !== null && vote.rowId === 0),
  );
  const canReorderVotes = computed(() => !hasUnpersistedVote.value);

  const personGuidsOnBallot = computed(() => {
    const set = new Set<string>();
    for (const vote of votes.value) {
      if (vote?.personGuid) {
        set.add(vote.personGuid);
      }
    }
    return set;
  });

  const duplicatePersonGuids = computed(() => {
    const personGuids = votes.value
      .filter((v): v is VoteDto => v !== null && !!v.personGuid)
      .map((v) => v.personGuid!);
    const duplicates: string[] = [];
    const seen = new Set<string>();
    for (const guid of personGuids) {
      if (seen.has(guid)) {
        duplicates.push(guid);
      } else {
        seen.add(guid);
      }
    }
    return duplicates;
  });

  function findNextEmptyPosition(): number {
    for (let i = 0; i < votes.value.length; i++) {
      if (!votes.value[i]) {
        return i + 1;
      }
    }
    const merged = buildVoteMap(true);
    const highestFilled = merged.size > 0 ? Math.max(...merged.keys()) : 0;
    if (highestFilled < MAX_BALLOT_SLOTS) {
      return highestFilled + 1;
    }
    return -1;
  }

  function getPersistedVotes(): VoteDto[] {
    return votes.value.filter(isPersistedVote);
  }

  function isPersistedVote(vote: VoteDto | null | undefined): vote is VoteDto {
    return !!vote && vote.rowId > 0;
  }

  function canDropOnIndex(targetIndex: number): boolean {
    if (
      !canReorderVotes.value ||
      dragSourceIndex.value === null ||
      dragSourceIndex.value === targetIndex
    ) {
      return false;
    }
    return (
      isPersistedVote(votes.value[dragSourceIndex.value]) &&
      isPersistedVote(votes.value[targetIndex])
    );
  }

  function replaceVoteAtPosition(positionOnBallot: number, vote: VoteDto) {
    const merged = buildVoteMap(true);
    merged.set(positionOnBallot, {
      ...vote,
      positionOnBallot,
    });
    votes.value = slotsFromMap(merged);
  }

  function addVoteToBallot(
    vote: VoteDto,
    addOptions?: VoteAddedOptions,
  ): boolean {
    const emptyPos = findNextEmptyPosition();
    if (emptyPos === -1) {
      if (votes.value.length >= MAX_BALLOT_SLOTS) {
        options.onBallotFull?.();
      }
      return false;
    }
    const voteWithPosition: VoteDto = { ...vote, positionOnBallot: emptyPos };
    const merged = buildVoteMap(true);
    merged.set(emptyPos, voteWithPosition);
    votes.value = slotsFromMap(merged);
    options.onVoteAdded?.(voteWithPosition, addOptions);
    return true;
  }

  function removeVote(positionOnBallot: number) {
    const merged = buildVoteMap(true);
    merged.delete(positionOnBallot);
    votes.value = slotsFromMap(merged);
    options.onVoteRemoved?.(positionOnBallot);
  }

  function handleDragStart(index: number) {
    const vote = votes.value[index];
    if (
      !canReorderVotes.value ||
      !isPersistedVote(vote) ||
      reorderingVotes.value
    ) {
      return;
    }
    dragSourceIndex.value = index;
    dragOverIndex.value = null;
  }

  function handleDragOver(event: DragEvent, index: number) {
    if (!canDropOnIndex(index)) {
      dragOverIndex.value = null;
      return;
    }
    event.preventDefault();
    dragOverIndex.value = index;
  }

  function handleDrop(targetIndex: number) {
    if (
      dragSourceIndex.value === null ||
      dragSourceIndex.value === targetIndex
    ) {
      dragSourceIndex.value = null;
      return;
    }
    const sourceVote = votes.value[dragSourceIndex.value];
    const targetVote = votes.value[targetIndex];
    if (!isPersistedVote(sourceVote) || !isPersistedVote(targetVote)) {
      dragSourceIndex.value = null;
      return;
    }
    const persistedVotes = getPersistedVotes();
    const sourceFilledIndex = persistedVotes.findIndex(
      (vote) => vote.rowId === sourceVote.rowId,
    );
    const targetFilledIndex = persistedVotes.findIndex(
      (vote) => vote.rowId === targetVote.rowId,
    );
    if (sourceFilledIndex === -1 || targetFilledIndex === -1) {
      dragSourceIndex.value = null;
      return;
    }
    const reordered = [...persistedVotes];
    const [movedVote] = reordered.splice(sourceFilledIndex, 1);
    reordered.splice(targetFilledIndex, 0, movedVote);
    reorderingVotes.value = true;
    options.onVotesReordered?.(reordered.map((vote) => vote.rowId));
    dragSourceIndex.value = null;
    dragOverIndex.value = null;
  }

  function handleDragEnd() {
    dragSourceIndex.value = null;
    dragOverIndex.value = null;
  }

  return {
    votes,
    dragSourceIndex,
    dragOverIndex,
    reorderingVotes,
    canReorderVotes,
    personGuidsOnBallot,
    duplicatePersonGuids,
    isPersistedVote,
    addVoteToBallot,
    replaceVoteAtPosition,
    removeVote,
    handleDragStart,
    handleDragOver,
    handleDrop,
    handleDragEnd,
    rebuildVoteSlots,
  };
}
