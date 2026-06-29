<script setup lang="ts">
import { useNotifications } from "@/composables/useNotifications";
import { usePersonSearch } from "@/composables/usePersonSearch";
import { useBallotStore } from "@/stores/ballotStore";
import { usePeopleStore } from "@/stores/peopleStore";
import type { BallotDto } from "@/types/Ballot";
import type { SearchablePersonDto } from "@/types/Person";
import type { VoteDto } from "@/types/Vote";
import { getActiveTellerPayload } from "@/utils/activeTellerStorage";
import { isVoteDtoSpoiled } from "@/utils/voteDtoNormalization";
import { getVoteSpoiledLabel } from "@/utils/voteSpoiledLabel";
import { Delete, Rank, WarningFilled } from "@element-plus/icons-vue";
import { computed, nextTick, onMounted, ref, watch } from "vue";
import { useI18n } from "vue-i18n";

const MAX_BALLOT_SLOTS = 50;

const props = defineProps<{
  electionGuid: string;
  ballot: BallotDto;
  /** Minimum rows to show (election numberToElect). Extra rows appear when over-filled. */
  requiredVotes: number;
  /** Increment to discard optimistic rows after a failed save. */
  resyncKey?: number;
  /** True when the teller at keyboard is selected. */
  hasKeyboardTeller?: boolean;
}>();

const emit = defineEmits<{
  "vote-added": [vote: VoteDto];
  "vote-removed": [positionOnBallot: number];
  "votes-reordered": [voteRowIds: number[]];
  "ballot-saved": [];
}>();

const { t } = useI18n();
const ballotStore = useBallotStore();
const peopleStore = usePeopleStore();
const { showWarningMessage, showErrorMessage, showSuccessMessage } =
  useNotifications();

const votes = ref<(VoteDto | null)[]>([]);
const cacheLoading = ref(false);
const cacheError = ref(false);

const searchQuery = ref("");
const searchInputRef = ref();
const selectedSearchIndex = ref(0);
const searchResultsListRef = ref<HTMLElement | null>(null);
const reviewToggleLoading = ref(false);
const dragSourceIndex = ref<number | null>(null);
const dragOverIndex = ref<number | null>(null);
const reorderingVotes = ref(false);

const canAddVotes = computed(() => props.hasKeyboardTeller !== false);

const isNeedsReview = computed(() => props.ballot.statusCode === "Review");

const candidatesRef = computed(() => peopleStore.candidateCache);
const { searchResults } = usePersonSearch(searchQuery, candidatesRef, {
  maxResults: 20,
});

function buildVoteMap(includeOptimistic: boolean): Map<number, VoteDto> {
  const merged = new Map<number, VoteDto>();
  for (const vote of props.ballot.votes) {
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
  // Show requiredVotes rows by default; extra rows appear only when over-filled.
  return Math.min(
    MAX_BALLOT_SLOTS,
    Math.max(props.requiredVotes, highestFilled),
  );
}

function rebuildVoteSlots(includeOptimistic = true) {
  const merged = buildVoteMap(includeOptimistic);
  const slots = computeSlotCount(merged);
  const voteArray: (VoteDto | null)[] = [];

  for (let i = 1; i <= slots; i++) {
    voteArray.push(merged.get(i) ?? null);
  }

  votes.value = voteArray;
}

watch(
  () => props.ballot,
  () => {
    rebuildVoteSlots(true);
    reorderingVotes.value = false;
  },
  { immediate: true, deep: true },
);

watch(
  () => props.resyncKey,
  () => {
    rebuildVoteSlots(false);
    reorderingVotes.value = false;
  },
);

const hasUnpersistedVote = computed(() =>
  votes.value.some((vote) => vote !== null && vote.rowId === 0),
);

const canReorderVotes = computed(() => !hasUnpersistedVote.value);

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

async function handlePersonSelected(person: SearchablePersonDto) {
  if (!canAddVotes.value) {
    showWarningMessage(t("ballots.keyboardTellerRequired"));
    return;
  }

  const emptyPos = findNextEmptyPosition();
  if (emptyPos === -1) {
    if (votes.value.length >= MAX_BALLOT_SLOTS) {
      showWarningMessage(t("ballots.ballotFull"));
    }
    return;
  }

  const isSpoiled = person.canReceiveVotes === false;

  const vote: VoteDto = {
    rowId: 0,
    ballotGuid: props.ballot.ballotGuid,
    positionOnBallot: emptyPos,
    personGuid: person.personGuid,
    personFullName: person.fullName,
    statusCode: isSpoiled ? "Spoiled" : "ok",
    ineligibleReasonCode: isSpoiled
      ? person.ineligibleReasonCode || "X01"
      : undefined,
  };

  const merged = buildVoteMap(true);
  merged.set(emptyPos, vote);
  const slots = computeSlotCount(merged);
  const voteArray: (VoteDto | null)[] = [];
  for (let i = 1; i <= slots; i++) {
    voteArray.push(merged.get(i) ?? null);
  }
  votes.value = voteArray;

  emit("vote-added", vote);

  searchQuery.value = "";
  selectedSearchIndex.value = 0;

  await nextTick();
  searchInputRef.value?.focus();
}

function handleKeyDown(e: KeyboardEvent) {
  if (e.key === "ArrowDown") {
    e.preventDefault();
    if (selectedSearchIndex.value < searchResults.value.length - 1) {
      selectedSearchIndex.value++;
      scrollToSelected();
    }
  } else if (e.key === "ArrowUp") {
    e.preventDefault();
    if (selectedSearchIndex.value > 0) {
      selectedSearchIndex.value--;
      scrollToSelected();
    }
  } else if (e.key === "Enter") {
    e.preventDefault();
    if (
      searchResults.value.length > 0 &&
      selectedSearchIndex.value >= 0 &&
      selectedSearchIndex.value < searchResults.value.length
    ) {
      handlePersonSelected(searchResults.value[selectedSearchIndex.value]);
    }
  } else if (e.key === "Escape") {
    e.preventDefault();
    searchQuery.value = "";
    selectedSearchIndex.value = 0;
  }
}

function scrollToSelected() {
  nextTick(() => {
    const list = searchResultsListRef.value;
    if (list) {
      const selected = list.querySelector(".is-selected") as HTMLElement;
      if (selected) {
        selected.scrollIntoView({ block: "nearest" });
      }
    }
  });
}

watch(searchResults, () => {
  selectedSearchIndex.value = 0;
});

function handleVoteRemoved(positionOnBallot: number) {
  const merged = buildVoteMap(true);
  merged.delete(positionOnBallot);
  const slots = computeSlotCount(merged);
  const voteArray: (VoteDto | null)[] = [];
  for (let i = 1; i <= slots; i++) {
    voteArray.push(merged.get(i) ?? null);
  }
  votes.value = voteArray;

  emit("vote-removed", positionOnBallot);
  searchInputRef.value?.focus();
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
  if (dragSourceIndex.value === null || dragSourceIndex.value === targetIndex) {
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

  const voteRowIds = reordered.map((vote) => vote.rowId);

  reorderingVotes.value = true;
  emit("votes-reordered", voteRowIds);
  dragSourceIndex.value = null;
  dragOverIndex.value = null;
}

function handleDragEnd() {
  dragSourceIndex.value = null;
  dragOverIndex.value = null;
}

defineExpose({
  reorderingVotes,
});

async function toggleNeedsReview() {
  reviewToggleLoading.value = true;
  try {
    if (isNeedsReview.value) {
      await ballotStore.updateBallot(props.ballot.ballotGuid, {
        ...getActiveTellerPayload(),
        statusCode: "Review",
        clearNeedsReview: true,
      });
    } else {
      await ballotStore.updateBallot(props.ballot.ballotGuid, {
        ...getActiveTellerPayload(),
        statusCode: "Review",
      });
    }
    showSuccessMessage(t("ballots.needsReviewUpdated"));
  } catch (error: any) {
    showErrorMessage(error.message || t("ballots.needsReviewError"));
  } finally {
    reviewToggleLoading.value = false;
  }
}

onMounted(async () => {
  cacheLoading.value = true;
  cacheError.value = false;

  peopleStore
    .initializeCandidateCache(props.electionGuid)
    .then(() => {
      cacheLoading.value = false;
      nextTick(() => {
        searchInputRef.value?.focus();
      });
    })
    .catch((e) => {
      console.error("Failed to initialize candidate cache:", e);
      cacheError.value = true;
      cacheLoading.value = false;
      showErrorMessage(t("ballots.cacheLoadError"));
    });
});
</script>

<template>
  <div class="inline-ballot-entry">
    <el-alert
      v-if="!canAddVotes"
      type="warning"
      :title="$t('ballots.keyboardTellerRequired')"
      :closable="false"
      class="keyboard-teller-alert"
    />

    <div v-if="cacheError" class="inline-ballot-entry__error">
      <el-alert
        type="danger"
        :title="$t('ballots.cacheLoadError')"
        :closable="false"
      />
    </div>

    <div v-else class="inline-ballot-entry__content ballot-entry-layout">
      <!-- Left Panel: Search -->
      <div>
        <div class="search-panel">
          <div class="search-panel-header">
            <h4>{{ $t("ballots.searchPerson") }}</h4>
          </div>
          <div class="search-input-wrapper">
            <el-input
              ref="searchInputRef"
              v-model="searchQuery"
              :placeholder="$t('ballots.searchPlaceholder')"
              :disabled="!canAddVotes"
              clearable
              class="search-input"
              @keydown="handleKeyDown"
            />
          </div>

          <div class="search-help">
            <small>{{ $t("ballots.searchHelp") }}</small>
          </div>

          <div ref="searchResultsListRef" class="search-results">
            <div
              v-if="searchQuery && searchResults.length === 0"
              class="no-results"
            >
              {{ $t("ballots.noMatchesFound") }}
            </div>
            <div
              v-for="(person, index) in searchResults"
              :key="person.personGuid"
              class="search-result-item"
              :class="{
                'is-selected': index === selectedSearchIndex,
                'is-ineligible': person.canReceiveVotes === false,
              }"
              @click="handlePersonSelected(person)"
              @mouseover="selectedSearchIndex = index"
            >
              <div class="person-info">
                <span class="person-name">{{ person.fullName }}</span>
                <span
                  v-if="person.canReceiveVotes === false"
                  class="ineligible-badge"
                  :title="$t('ballots.ineligible')"
                >
                  {{ person.ineligibleReasonCode }}
                </span>
              </div>
              <span v-if="person.voteCount > 0" class="vote-count-badge">
                {{ person.voteCount }}
              </span>
            </div>
          </div>
        </div>
        <div class="needs-review-toggle">
          <el-button
            :type="isNeedsReview ? 'danger' : 'default'"
            :plain="!isNeedsReview"
            :loading="reviewToggleLoading"
            @click="toggleNeedsReview"
          >
            {{
              isNeedsReview
                ? $t("ballots.clearNeedsReview")
                : $t("ballots.markNeedsReview")
            }}
          </el-button>
        </div>
      </div>

      <!-- Right Panel: Votes -->
      <div class="votes-panel">
        <div class="votes-panel-header">
          <h4>{{ $t("ballots.namesOnBallot") }}</h4>
          <span class="ballot-id">{{
            $t("ballots.ballotNum", { code: ballot.ballotCode })
          }}</span>
        </div>

        <div class="votes-list">
          <div
            v-for="(vote, index) in votes"
            :key="vote?.rowId ? `vote-${vote.rowId}` : `slot-${index}`"
            class="vote-row"
            :class="{
              'has-vote': !!vote,
              'is-duplicate':
                vote && duplicatePersonGuids.includes(vote.personGuid!),
              'is-dragging': dragSourceIndex === index,
              'is-drop-target-top':
                dragOverIndex === index &&
                dragSourceIndex !== null &&
                index < dragSourceIndex,
              'is-drop-target-bottom':
                dragOverIndex === index &&
                dragSourceIndex !== null &&
                index > dragSourceIndex,
              'is-draggable': canReorderVotes && isPersistedVote(vote),
            }"
            :draggable="
              canReorderVotes && isPersistedVote(vote) && !reorderingVotes
            "
            @dragstart="handleDragStart(index)"
            @dragover="handleDragOver($event, index)"
            @drop="handleDrop(index)"
            @dragend="handleDragEnd"
          >
            <div class="vote-position">{{ index + 1 }}</div>
            <div class="vote-content">
              <template v-if="vote">
                <span
                  v-if="canReorderVotes && isPersistedVote(vote)"
                  class="drag-handle"
                  :title="$t('ballots.dragToReorder')"
                >
                  <el-icon><Rank /></el-icon>
                </span>
                <div class="vote-name-block">
                  <span
                    class="vote-name"
                    :class="{
                      'is-spoiled': isVoteDtoSpoiled(vote),
                    }"
                  >
                    {{ vote.personFullName }}
                  </span>
                  <span
                    v-if="isVoteDtoSpoiled(vote)"
                    class="vote-ineligible-reason"
                  >
                    {{ getVoteSpoiledLabel($t, vote) }}
                  </span>
                </div>

                <div class="vote-actions">
                  <span
                    v-if="duplicatePersonGuids.includes(vote.personGuid!)"
                    class="status-badge warning"
                    :title="$t('ballots.duplicateWarning')"
                  >
                    <el-icon><WarningFilled /></el-icon>
                  </span>
                  <el-button
                    type="danger"
                    :icon="Delete"
                    circle
                    plain
                    size="small"
                    :aria-label="$t('common.delete')"
                    @click="handleVoteRemoved(index + 1)"
                  />
                </div>
              </template>
              <template v-else>
                <div class="empty-slot"></div>
              </template>
            </div>
          </div>

          <p v-if="canReorderVotes" class="votes-drag-hint">
            {{ $t("ballots.dragToReorder") }}
          </p>
        </div>
      </div>
    </div>
  </div>
</template>

<style lang="less">
.inline-ballot-entry {
  width: 100%;

  .keyboard-teller-alert {
    margin-bottom: var(--spacing-3, 12px);
  }

  .ballot-entry-layout {
    display: flex;
    gap: var(--spacing-6, 24px);
    align-items: flex-start;

    @media (max-width: 768px) {
      flex-direction: column;
    }
  }

  .search-panel {
    flex: 1;
    min-width: 200px;
    max-width: 300px;
    background: var(--el-bg-color);
    border: 1px solid var(--el-border-color);
    border-radius: var(--el-border-radius-base);
    display: flex;
    flex-direction: column;

    .search-panel-header {
      padding: var(--spacing-3, 12px) var(--spacing-4, 16px);
      background: var(--el-fill-color-light);
      border-bottom: 1px solid var(--el-border-color-lighter);

      h4 {
        margin: 0;
        font-size: var(--el-font-size-base);
        color: var(--el-text-color-regular);
      }
    }

    .search-input-wrapper {
      padding: var(--spacing-3, 12px);
    }

    .search-help {
      padding: 0 var(--spacing-3, 12px) var(--spacing-2, 8px);
      color: var(--el-text-color-secondary);
      font-size: var(--el-font-size-small);
    }

    .search-results {
      flex: 1;
      overflow-y: auto;
      max-height: 400px;
      border-top: 1px solid var(--el-border-color-lighter);

      .no-results {
        padding: var(--spacing-4, 16px);
        text-align: center;
        color: var(--el-text-color-secondary);
      }

      .search-result-item {
        display: flex;
        justify-content: space-between;
        align-items: center;
        padding: var(--spacing-2, 8px) var(--spacing-3, 12px);
        cursor: pointer;
        border-bottom: 1px solid var(--el-border-color-lighter);

        &:last-child {
          border-bottom: none;
        }

        &.is-selected {
          background-color: var(--el-color-primary-light-9);
        }

        &.is-ineligible {
          .person-name {
            color: var(--el-text-color-secondary);
            text-decoration: line-through;
          }
        }

        .person-info {
          display: flex;
          align-items: center;
          gap: var(--spacing-2, 8px);
          overflow: hidden;

          .person-name {
            white-space: nowrap;
            overflow: hidden;
            text-overflow: ellipsis;
          }

          .ineligible-badge {
            background: var(--el-color-danger-light-9);
            color: var(--el-color-danger);
            font-size: 11px;
            padding: 2px 6px;
            border-radius: 4px;
            border: 1px solid var(--el-color-danger-light-5);
            font-family: monospace;
          }
        }

        .vote-count-badge {
          background: var(--el-color-success-light-9);
          color: var(--el-color-success);
          font-size: 12px;
          padding: 2px 8px;
          border-radius: 12px;
          font-weight: bold;
        }
      }
    }
  }

  .needs-review-toggle {
    margin: 1em 0;
    padding: 0 var(--spacing-3, 12px) var(--spacing-3, 12px);

    //.el-button {
    //  width: 100%;
    //}
  }

  .votes-panel {
    flex: 1.5;
    max-width: 500px;
    background: var(--el-bg-color);
    border: 1px solid var(--el-border-color);
    border-radius: var(--el-border-radius-base);

    .votes-panel-header {
      padding: var(--spacing-3, 12px) var(--spacing-4, 16px);
      background: var(--el-fill-color-light);
      border-bottom: 1px solid var(--el-border-color-lighter);
      display: flex;
      justify-content: space-between;
      align-items: center;

      h4 {
        margin: 0;
        font-size: var(--el-font-size-base);
        color: var(--el-text-color-regular);
      }

      .ballot-id {
        font-weight: bold;
      }
    }

    .votes-drag-hint {
      margin: 0;
      padding: var(--spacing-2, 8px) var(--spacing-4, 16px);
      color: var(--el-text-color-secondary);
      font-size: var(--el-font-size-small);
    }

    .votes-list {
      padding: var(--spacing-2, 8px);
    }

    .vote-row {
      display: flex;
      align-items: center;
      gap: var(--spacing-3, 12px);
      padding: var(--spacing-1, 4px) var(--spacing-2, 8px);
      margin-bottom: var(--spacing-1, 4px);
      border-radius: var(--el-border-radius-base);

      &.has-vote {
        background-color: var(--el-color-success-light-9);
        border: 1px solid var(--el-color-success-light-5);
      }

      &.is-draggable {
        cursor: grab;
      }

      &.is-dragging {
        opacity: 0.55;
      }

      &.is-drop-target-top .vote-content {
        border-top: 2px dashed var(--el-color-primary);
      }

      &.is-drop-target-bottom .vote-content {
        border-bottom: 2px dashed var(--el-color-primary);
      }

      &.is-duplicate {
        background-color: var(--el-color-warning-light-9);
        border: 1px solid var(--el-color-warning-light-5);
      }

      .vote-position {
        width: 24px;
        text-align: right;
        color: var(--el-text-color-secondary);
        font-size: var(--el-font-size-small);
      }

      .vote-content {
        flex: 1;
        display: flex;
        justify-content: space-between;
        align-items: center;
        min-height: 32px;

        .empty-slot {
          flex: 1;
          height: 1px;
          background-color: var(--el-border-color-lighter);
          margin: auto 0;
        }

        .drag-handle {
          display: inline-flex;
          align-items: center;
          color: var(--el-text-color-secondary);
          margin-right: var(--spacing-1, 4px);
        }

        .vote-name-block {
          display: flex;
          flex-direction: column;
          gap: 2px;
          margin-right: auto;
          margin-left: 10px;
          min-width: 0;
        }

        .vote-name {
          font-weight: 500;

          &.is-spoiled {
            color: var(--el-color-danger);
            text-decoration: line-through;
          }
        }

        .vote-ineligible-reason {
          color: var(--el-color-danger);
          font-size: var(--el-font-size-small);
          line-height: 1.2;
        }

        .vote-actions {
          display: flex;
          align-items: center;
          gap: var(--spacing-2, 8px);

          .status-badge {
            display: inline-flex;
            align-items: center;
            gap: 4px;
            font-size: 11px;
            padding: 2px 6px;
            border-radius: 4px;
            font-weight: bold;

            &.error {
              background: var(--el-color-danger-light-9);
              color: var(--el-color-danger);
              border: 1px solid var(--el-color-danger-light-5);
            }

            &.warning {
              color: var(--el-color-warning);
            }
          }
        }
      }
    }
  }
}
</style>
