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
import { getActiveTellerPayload } from "@/utils/activeTellerStorage";
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
  "vote-removed": [positionOnBallot: number];
  "votes-reordered": [voteRowIds: number[]];
  "ballot-saved": [];
  "ballot-created": [ballotGuid: string];
  "ballot-deleted": [ballotGuid: string];
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
const isNeedsReview = computed(() => props.ballot.statusCode === "Review");
const people = computed(() => peopleStore.peopleCache);

const {
  votes,
  dragSourceIndex,
  dragOverIndex,
  reorderingVotes,
  canReorderVotes,
  personGuidsOnBallot,
  duplicatePersonGuids,
  isPersistedVote,
  addVoteToBallot,
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

watch(
  () => props.ballot.ballotGuid,
  () => {
    searchPanelRef.value?.clearSearch();
    void focusSearchInput();
  },
);

async function focusSearchInput() {
  await searchPanelRef.value?.focus();
}

async function handlePersonSelected(person: SearchablePersonDto) {
  if (!canAddVotes.value) {
    showWarningMessage(t("ballots.keyboardTellerRequired"));
    return;
  }
  const isSpoiled = person.canReceiveVotes === false;
  const vote: VoteDto = {
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
  if (!addVoteToBallot(vote)) {
    return;
  }
  searchPanelRef.value?.clearSearch();
  await focusSearchInput();
}

async function handleNewPersonAdded(vote: VoteDto) {
  if (!canAddVotes.value) {
    showWarningMessage(t("ballots.keyboardTellerRequired"));
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
      t("ballots.deleteConfirm", { code: props.ballot.ballotCode }),
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
  if (!computerCode.value) {
    showErrorMessage(t("ballots.computerCodeRequired"));
    return;
  }
  if (!locationStore.selectedLocationGuid) {
    showErrorMessage(t("ballots.locationRequired"));
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
          @select="handlePersonSelected"
        />

        <div class="new-ballot-action">
          <el-button
            type="primary"
            :loading="creatingNewBallot"
            @click="handleNewBallot"
          >
            <el-icon><Plus /></el-icon>
            {{ $t("ballots.addNextBallot") }}
          </el-button>
        </div>

        <div class="add-name-action">
          <el-button
            type="default"
            :disabled="!canAddVotes"
            @click="showAddPersonDrawer = true"
          >
            <el-icon><Plus /></el-icon>
            {{ $t("ballots.addName") }}
          </el-button>
        </div>

        <div class="needs-review-toggle">
          <el-button
            :type="isNeedsReview ? 'danger' : 'warning'"
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

        <div class="delete-ballot-action">
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
        :ballot-code="ballot.ballotCode"
        :can-reorder-votes="canReorderVotes"
        :reordering-votes="reorderingVotes"
        :drag-source-index="dragSourceIndex"
        :drag-over-index="dragOverIndex"
        :duplicate-person-guids="duplicatePersonGuids"
        :is-persisted-vote="isPersistedVote"
        @remove="handleVoteRemoved"
        @drag-start="handleDragStart"
        @drag-over="handleDragOver"
        @drop="handleDrop"
        @drag-end="handleDragEnd"
      />
    </div>

    <el-drawer
      v-model="showAddPersonDrawer"
      :title="$t('ballots.addNameDrawerTitle')"
      direction="rtl"
      size="700px"
      :lock-scroll="false"
      modal-class="ballot-add-person-drawer"
    >
      <BallotAddPersonPanel
        v-if="showAddPersonDrawer"
        :election-guid="electionGuid"
        :ballot-guid="ballot.ballotGuid"
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
}
</style>
