<script setup lang="ts">
import { useApiErrorHandler } from "@/composables/useApiErrorHandler";
import {
  useBallotEntryVotes,
  type VoteAddedOptions,
} from "@/composables/useBallotEntryVotes";
import { useComputerCode } from "@/composables/useComputerCode";
import { useNotifications } from "@/composables/useNotifications";
import { useBallotStore } from "@/stores/ballotStore";
import { useLocationStore } from "@/stores/locationStore";
import { usePeopleStore } from "@/stores/peopleStore";
import type { BallotDto } from "@/types/Ballot";
import type { SearchablePersonDto } from "@/types/Person";
import type { VoteDto } from "@/types/Vote";
import {
  formatBallotDisplayCode,
  isOnlineOrImportedComputerCode,
} from "@/utils/ballotDisplayCode";
import {
  getActiveTellerPayload,
  getActiveTellers,
} from "@/utils/activeTellerStorage";
import {
  BALLOT_START_BLOCK_MESSAGE_KEY,
  getBallotStartBlockReason,
  isOnlineLocationType,
  locationTypeForGuid,
  type BallotStartBlockReason,
} from "@/utils/ballotStartRequirements";
import {
  isUnresolvedRawVote,
  nextFindQuery,
  parseOnlineVoteRaw,
  type FindShortenState,
} from "@/utils/onlineVoteRaw";
import { Delete, Plus } from "@element-plus/icons-vue";
import { ElMessageBox } from "element-plus";
import { computed, onMounted, ref, toRef, watch } from "vue";
import { useI18n } from "vue-i18n";
import BallotAddPersonPanel from "./BallotAddPersonPanel.vue";
import BallotPersonSearchPanel from "./BallotPersonSearchPanel.vue";
import BallotVotesPanel from "./BallotVotesPanel.vue";

export type { VoteAddedOptions };

const props = defineProps<{
  electionGuid: string;
  ballot: BallotDto;
  requiredVotes: number;
  resyncKey?: number;
  hasKeyboardTeller?: boolean;
}>();

const emit = defineEmits<{
  "vote-added": [vote: VoteDto, options?: VoteAddedOptions];
  "vote-updated": [vote: VoteDto];
  "vote-removed": [positionOnBallot: number];
  "votes-reordered": [voteRowIds: number[]];
  "ballot-saved": [];
  "ballot-created": [ballotGuid: string];
  "ballot-deleted": [ballotGuid: string];
  "ballot-start-blocked": [reason: BallotStartBlockReason];
}>();

const { t } = useI18n();
const ballotStore = useBallotStore();
const peopleStore = usePeopleStore();
const locationStore = useLocationStore();
const { computerCode } = useComputerCode(props.electionGuid);
const { showWarningMessage, showErrorMessage, showSuccessMessage } =
  useNotifications();
const { handleApiError } = useApiErrorHandler();

const cacheLoading = ref(false);
const cacheError = ref(false);
const reviewToggleLoading = ref(false);
const creatingNewBallot = ref(false);
const deletingBallot = ref(false);
const showAddPersonDrawer = ref(false);
const searchPanelRef = ref<InstanceType<typeof BallotPersonSearchPanel> | null>(
  null,
);

const canAddVotes = computed(() => props.hasKeyboardTeller !== false);
const isOnlineLocationSelected = computed(() =>
  isOnlineLocationType(
    locationTypeForGuid(
      locationStore.locations,
      locationStore.selectedLocationGuid,
    ),
  ),
);
const isOnlineOrImported = computed(() =>
  isOnlineOrImportedComputerCode(props.ballot.computerCode),
);
const canRemoveVotes = computed(() => !isOnlineOrImported.value);
const isNeedsReview = computed(() => props.ballot.statusCode === "Review");
const people = computed(() => peopleStore.peopleCache);

const {
  votes,
  dragSourceIndex,
  dragOverIndex,
  reorderingVotes,
  canReorderVotes: canReorderSavedVotes,
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
} = useBallotEntryVotes({
  ballot: toRef(props, "ballot"),
  requiredVotes: toRef(props, "requiredVotes"),
  resyncKey: toRef(props, "resyncKey"),
  onVoteAdded: (vote, options) => emit("vote-added", vote, options),
  onVoteRemoved: (position) => emit("vote-removed", position),
  onVotesReordered: (rowIds) => emit("votes-reordered", rowIds),
  onBallotFull: () => showWarningMessage(t("ballots.ballotFull")),
});

const canReorderVotes = computed(
  () => canRemoveVotes.value && canReorderSavedVotes.value,
);

const targetedVoteRowId = ref<number | null>(null);
const findShortenState = ref<FindShortenState | null>(null);
const targetClearedByUser = ref(false);

const targetedVote = computed(() =>
  votes.value.find(
    (vote) =>
      vote !== null &&
      targetedVoteRowId.value !== null &&
      vote.rowId === targetedVoteRowId.value,
  ),
);

const showAddNameForSelectedVote = computed(() => {
  const target = targetedVote.value;
  return (
    isOnlineOrImported.value &&
    !!target &&
    isPersistedVote(target) &&
    findShortenState.value?.voteId === target.rowId
  );
});

function firstUnresolvedRawVote(): VoteDto | undefined {
  return votes.value.find((vote) => isUnresolvedRawVote(vote)) ?? undefined;
}

function selectTarget(vote: VoteDto) {
  targetedVoteRowId.value = vote.rowId;
  targetClearedByUser.value = false;
}

function clearTarget() {
  targetedVoteRowId.value = null;
  targetClearedByUser.value = true;
}

watch(
  () => props.ballot.ballotGuid,
  () => {
    searchPanelRef.value?.clearSearch();
    findShortenState.value = null;
    targetClearedByUser.value = false;
    targetedVoteRowId.value = firstUnresolvedRawVote()?.rowId ?? null;
    void focusSearchInput();
  },
);

watch(
  votes,
  () => {
    if (targetClearedByUser.value) {
      return;
    }
    if (
      targetedVoteRowId.value &&
      votes.value.some((vote) => vote?.rowId === targetedVoteRowId.value)
    ) {
      return;
    }
    targetedVoteRowId.value = firstUnresolvedRawVote()?.rowId ?? null;
  },
  { immediate: true },
);

async function focusSearchInput() {
  await searchPanelRef.value?.focus();
}

function applyVoteToTarget(incoming: VoteDto): VoteDto | null {
  const target = targetedVote.value;
  if (!target || !isPersistedVote(target)) {
    return null;
  }
  if (!isOnlineOrImported.value && !hasDisplayableRawOn(target)) {
    return null;
  }
  return {
    ...target,
    ...incoming,
    rowId: target.rowId,
    positionOnBallot: target.positionOnBallot,
    onlineVoteRaw: target.onlineVoteRaw,
    personGuid: incoming.personGuid,
    personFullName: incoming.personFullName,
  };
}

async function finishReplace(updated: VoteDto) {
  replaceVoteAtPosition(updated.positionOnBallot, updated);
  emit("vote-updated", updated);
  searchPanelRef.value?.clearSearch();
  findShortenState.value = null;
  targetedVoteRowId.value = firstUnresolvedRawVote()?.rowId ?? null;
  const next = targetedVote.value;
  if (next) {
    await handleFindRawName(next);
  } else {
    await focusSearchInput();
  }
}

function buildSelectedVote(person: SearchablePersonDto): VoteDto {
  const isSpoiled = person.canReceiveVotes === false;
  return {
    rowId: 0,
    ballotGuid: props.ballot.ballotGuid,
    positionOnBallot: 0,
    personGuid: person.personGuid,
    personFullName: person.fullName,
    statusCode: isSpoiled ? "Spoiled" : "ok",
    ineligibleReasonCode: isSpoiled
      ? person.ineligibleReasonCode || "X01"
      : undefined,
  };
}

async function handlePersonSelected(person: SearchablePersonDto) {
  if (!canAddVotes.value) {
    showWarningMessage(t("ballots.keyboardTellerRequired"));
    return;
  }
  const vote = buildSelectedVote(person);
  const updated = applyVoteToTarget(vote);
  if (updated) {
    await finishReplace(updated);
    return;
  }
  if (isOnlineOrImported.value) {
    return;
  }
  if (!addVoteToBallot(vote)) {
    return;
  }
  searchPanelRef.value?.clearSearch();
  await focusSearchInput();
}

function hasDisplayableRawOn(vote: VoteDto): boolean {
  return !!parseOnlineVoteRaw(vote.onlineVoteRaw);
}

async function handleFindRawName(vote: VoteDto) {
  selectTarget(vote);
  const raw = parseOnlineVoteRaw(vote.onlineVoteRaw);
  if (!raw) {
    return;
  }
  const { query, state } = nextFindQuery(
    raw,
    "FL",
    vote.rowId,
    findShortenState.value,
  );
  findShortenState.value = state;
  await searchPanelRef.value?.setQuery(query);
}

async function handleNewPersonAdded(vote: VoteDto) {
  if (!canAddVotes.value) {
    showWarningMessage(t("ballots.keyboardTellerRequired"));
    return;
  }
  const updated = applyVoteToTarget(vote);
  if (updated) {
    showAddPersonDrawer.value = false;
    await finishReplace(updated);
    return;
  }
  if (isOnlineOrImported.value) {
    showAddPersonDrawer.value = false;
    return;
  }
  if (addVoteToBallot(vote, { fromNewPerson: !!vote.personGuid })) {
    showAddPersonDrawer.value = false;
    await focusSearchInput();
  }
}

function handleVoteRemoved(positionOnBallot: number) {
  removeVote(positionOnBallot);
  void focusSearchInput();
}

async function handleDeleteBallot() {
  try {
    await ElMessageBox.confirm(
      t("ballots.deleteConfirm", {
        code: formatBallotDisplayCode(t, props.ballot),
      }),
      t("common.warning"),
      {
        confirmButtonText: t("common.delete"),
        cancelButtonText: t("common.cancel"),
        type: "warning",
      },
    );
  } catch (error) {
    if (error !== "cancel") {
      handleApiError(error);
    }
    return;
  }
  deletingBallot.value = true;
  try {
    await ballotStore.deleteBallot(props.ballot.ballotGuid);
    showSuccessMessage(t("ballots.deleteSuccess"));
    emit("ballot-deleted", props.ballot.ballotGuid);
  } catch (error) {
    handleApiError(error);
  } finally {
    deletingBallot.value = false;
  }
}

async function handleNewBallot() {
  const reason = getBallotStartBlockReason({
    computerCode: computerCode.value,
    locationGuid: locationStore.selectedLocationGuid,
    locationType: locationTypeForGuid(
      locationStore.locations,
      locationStore.selectedLocationGuid,
    ),
    teller1: getActiveTellers().teller1,
  });
  if (reason) {
    showErrorMessage(t(BALLOT_START_BLOCK_MESSAGE_KEY[reason]));
    emit("ballot-start-blocked", reason);
    return;
  }
  creatingNewBallot.value = true;
  try {
    const ballot = await ballotStore.createBallot({
      electionGuid: props.electionGuid,
      computerCode: computerCode.value,
      locationGuid: locationStore.selectedLocationGuid!,
      ...getActiveTellerPayload(),
    });
    showSuccessMessage(t("ballots.createSuccess"));
    emit("ballot-created", ballot.ballotGuid);
  } catch (error) {
    handleApiError(error);
  } finally {
    creatingNewBallot.value = false;
  }
}

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

defineExpose({ reorderingVotes, focusSearchInput });

onMounted(async () => {
  cacheLoading.value = true;
  cacheError.value = false;
  peopleStore
    .initializePeopleCache(props.electionGuid)
    .then(() => {
      cacheLoading.value = false;
      void focusSearchInput();
    })
    .catch((e) => {
      console.error("Failed to initialize cache:", e);
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
      <div>
        <BallotPersonSearchPanel
          ref="searchPanelRef"
          :can-add-votes="canAddVotes"
          :people="people"
          :person-guids-on-ballot="personGuidsOnBallot"
          :resolving-raw="!!findShortenState"
          @select="handlePersonSelected"
        />

        <div v-if="showAddNameForSelectedVote" class="add-name-action">
          <el-button
            type="default"
            :disabled="!canAddVotes"
            @click="showAddPersonDrawer = true"
          >
            {{ $t("ballots.setSpoiledOrNewName") }}
          </el-button>
        </div>

        <div v-if="isNeedsReview" class="needs-review-toggle">
          <el-button
            type="danger"
            :loading="reviewToggleLoading"
            @click="toggleNeedsReview"
          >
            {{ $t("ballots.clearNeedsReview") }}
          </el-button>
        </div>

        <div class="new-ballot-action">
          <el-button
            type="primary"
            plain
            :loading="creatingNewBallot"
            :disabled="isOnlineLocationSelected"
            :title="
              isOnlineLocationSelected
                ? $t('ballots.onlineLocationNotAllowed')
                : undefined
            "
            @click="handleNewBallot"
          >
            <el-icon><Plus /></el-icon>
            {{ $t("ballots.addNextBallot") }}
          </el-button>
        </div>

        <div v-if="!isOnlineOrImported" class="add-name-action">
          <el-button
            type="default"
            :disabled="!canAddVotes"
            @click="showAddPersonDrawer = true"
          >
            <el-icon><Plus /></el-icon>
            {{ $t("ballots.addName") }}
          </el-button>
        </div>

        <div v-if="!isNeedsReview" class="needs-review-toggle">
          <el-button
            type="warning"
            plain
            :loading="reviewToggleLoading"
            @click="toggleNeedsReview"
          >
            {{ $t("ballots.markNeedsReview") }}
          </el-button>
        </div>

        <div v-if="canRemoveVotes" class="delete-ballot-action">
          <el-button
            type="danger"
            plain
            :loading="deletingBallot"
            @click="handleDeleteBallot"
          >
            <el-icon><Delete /></el-icon>
            {{ $t("ballots.deleteBallot") }}
          </el-button>
        </div>
      </div>

      <BallotVotesPanel
        :votes="votes"
        :ballot-code="formatBallotDisplayCode(t, ballot)"
        :can-reorder-votes="canReorderVotes"
        :reordering-votes="reorderingVotes"
        :drag-source-index="dragSourceIndex"
        :drag-over-index="dragOverIndex"
        :duplicate-person-guids="duplicatePersonGuids"
        :targeted-vote-row-id="targetedVoteRowId"
        :can-remove-votes="canRemoveVotes"
        :can-select-any-vote="isOnlineOrImported"
        :is-persisted-vote="isPersistedVote"
        @remove="handleVoteRemoved"
        @drag-start="handleDragStart"
        @drag-over="handleDragOver"
        @drop="handleDrop"
        @drag-end="handleDragEnd"
        @select-target="selectTarget"
        @clear-target="clearTarget"
        @find="handleFindRawName"
      />
    </div>

    <el-drawer
      v-model="showAddPersonDrawer"
      :title="
        isOnlineOrImported
          ? $t('ballots.setSpoiledOrNewName')
          : $t('ballots.addNameDrawerTitle')
      "
      direction="rtl"
      size="700px"
      :lock-scroll="false"
      modal-class="ballot-add-person-drawer"
    >
      <BallotAddPersonPanel
        v-if="showAddPersonDrawer"
        :election-guid="electionGuid"
        :ballot-guid="ballot.ballotGuid"
        :raw-vote="
          targetedVote ? parseOnlineVoteRaw(targetedVote.onlineVoteRaw) : null
        "
        @person-added="handleNewPersonAdded"
        @cancel="showAddPersonDrawer = false"
      />
    </el-drawer>
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

  .new-ballot-action,
  .needs-review-toggle,
  .delete-ballot-action,
  .add-name-action {
    margin: 1em 0 0;
    padding: 0 var(--spacing-3, 12px) var(--spacing-3, 12px);
  }

  .new-ballot-action .el-button--primary {
    background: var(--el-color-primary-light-9);
    border: 1px solid var(--el-color-primary-light-3);
    color: var(--el-color-primary);

    &:not(:disabled):hover,
    &:not(:disabled):focus-visible {
      background: var(--el-color-primary);
      border-color: var(--el-color-primary);
      color: var(--el-color-white);
    }
  }
}
</style>
