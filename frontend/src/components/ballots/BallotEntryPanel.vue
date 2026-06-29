<script setup lang="ts">
import { useNotifications } from "@/composables/useNotifications";
import { getActiveTellerPayload } from "@/utils/activeTellerStorage";
import { computed, onMounted, onUnmounted, ref, watch } from "vue";
import { useI18n } from "vue-i18n";
import InlineBallotEntry from "./InlineBallotEntry.vue";
import { useBallotStore } from "../../stores/ballotStore";
import { useElectionStore } from "../../stores/electionStore";
import { usePeopleStore } from "../../stores/peopleStore";
import type {
  CreateVoteDto,
  VoteDto,
  VoteWithBallotStatusDto,
} from "../../types";
import { isVoteDtoSpoiled } from "@/utils/voteDtoNormalization";

const emit = defineEmits<{
  "ballot-created": [ballotGuid: string];
  "ballot-deleted": [ballotGuid: string];
}>();

const props = withDefaults(
  defineProps<{
    electionGuid: string;
    ballotGuid: string;
    showMetadata?: boolean;
    manageBallotSignalR?: boolean;
    managePeopleSignalR?: boolean;
    hasKeyboardTeller?: boolean;
  }>(),
  {
    showMetadata: true,
    manageBallotSignalR: true,
    managePeopleSignalR: true,
    hasKeyboardTeller: true,
  },
);

const { t } = useI18n();
const ballotStore = useBallotStore();
const electionStore = useElectionStore();
const peopleStore = usePeopleStore();
const {
  showSuccessMessage,
  showErrorMessage,
  showWarningMessage,
  showInfoMessage,
} = useNotifications();

const panelLoading = ref(false);
const voteResyncKey = ref(0);
const ballot = computed(() => ballotStore.currentBallot);
const election = computed(() => electionStore.currentElection);

function hasCachedPanelData(): boolean {
  return (
    ballotStore.currentBallot?.ballotGuid === props.ballotGuid &&
    electionStore.currentElection?.electionGuid === props.electionGuid
  );
}

async function loadBallotData() {
  const showLoading = !hasCachedPanelData();
  if (showLoading) {
    panelLoading.value = true;
  }
  try {
    await Promise.all([
      ballotStore.fetchBallotById(props.ballotGuid),
      electionStore.fetchElectionById(props.electionGuid),
    ]);

    const loadedBallot = ballotStore.currentBallot;
    if (loadedBallot) {
      await ballotStore.updateBallot(props.ballotGuid, {
        ...getActiveTellerPayload(),
        statusCode: loadedBallot.statusCode,
      });
    }
  } catch (_error) {
    showErrorMessage(t("ballots.loadError"));
  } finally {
    panelLoading.value = false;
  }
}

let unsubscribePersonUpdated: (() => void) | undefined;

function subscribeToPersonUpdates() {
  unsubscribePersonUpdated = peopleStore.onPersonUpdated((data) => {
    if (data.firstName || data.lastName) {
      const fullName = [data.firstName, data.lastName]
        .filter(Boolean)
        .join(" ");
      showInfoMessage(t("ballots.personUpdated", { name: fullName }));
    }
  });
}

onMounted(async () => {
  try {
    const initTasks = [];
    if (props.manageBallotSignalR) {
      initTasks.push(ballotStore.initializeSignalR());
    }
    if (props.managePeopleSignalR) {
      initTasks.push(peopleStore.initializeSignalR());
    }
    await Promise.all(initTasks);

    const joinTasks = [];
    if (props.manageBallotSignalR) {
      joinTasks.push(ballotStore.joinElection(props.electionGuid));
    }
    if (props.managePeopleSignalR) {
      joinTasks.push(peopleStore.joinElection(props.electionGuid));
    }
    await Promise.all(joinTasks);

    await loadBallotData();
    subscribeToPersonUpdates();
  } catch (_error) {
    showErrorMessage(t("ballots.loadError"));
  }
});

onUnmounted(async () => {
  unsubscribePersonUpdated?.();
  unsubscribePersonUpdated = undefined;

  try {
    const leaveTasks = [];
    if (props.manageBallotSignalR) {
      leaveTasks.push(ballotStore.leaveElection(props.electionGuid));
    }
    if (props.managePeopleSignalR) {
      leaveTasks.push(peopleStore.leaveElection(props.electionGuid));
    }
    await Promise.all(leaveTasks);
  } catch (error) {
    console.error("Failed to leave election group for ballot entry:", error);
  }
});

watch(
  () => props.ballotGuid,
  async (newGuid, oldGuid) => {
    if (newGuid && newGuid !== oldGuid) {
      await loadBallotData();
    }
  },
);

async function handleVoteAdded(vote: VoteDto) {
  try {
    const createDto: CreateVoteDto = {
      ballotGuid: vote.ballotGuid,
      positionOnBallot: vote.positionOnBallot,
      personGuid: vote.personGuid || undefined,
      ineligibleReasonCode: vote.personGuid
        ? undefined
        : vote.ineligibleReasonCode,
    };

    const result: VoteWithBallotStatusDto =
      await ballotStore.createVote(createDto);
    const isSpoiled = result.vote && isVoteDtoSpoiled(result.vote);
    if (isSpoiled) {
      const reasonCode =
        result.vote.ineligibleReasonCode || result.vote.statusCode;
      showWarningMessage(t("ballots.voteSpoiledSuccess", { code: reasonCode }));
    } else {
      showSuccessMessage(t("ballots.voteAddedSuccess"));
    }
  } catch (error: any) {
    voteResyncKey.value++;
    showErrorMessage(error.message || t("ballots.voteAddedError"));
  }
}

async function handleVoteRemoved(positionOnBallot: number) {
  try {
    await ballotStore.deleteVote(props.ballotGuid, positionOnBallot);
    voteResyncKey.value++;
    showSuccessMessage(t("ballots.voteRemovedSuccess"));
  } catch (error: any) {
    voteResyncKey.value++;
    showErrorMessage(error.message || t("ballots.voteRemovedError"));
  }
}

async function handleVotesReordered(voteRowIds: number[]) {
  try {
    await ballotStore.reorderVotes({
      ballotGuid: props.ballotGuid,
      voteRowIds,
    });
    showSuccessMessage(t("ballots.votesReorderedSuccess"));
  } catch (error: any) {
    voteResyncKey.value++;
    showErrorMessage(error.message || t("ballots.votesReorderedError"));
  }
}

async function handleDeleteBallot(ballotGuid: string) {
  try {
    await ballotStore.deleteBallot(ballotGuid);
    showSuccessMessage(t("ballots.deleteSuccess"));
    emit("ballot-deleted", ballotGuid);
  } catch (error: any) {
    showErrorMessage(error.message || t("ballots.deleteError"));
  }
}
</script>

<template>
  <div class="ballot-entry-panel">
    <div v-if="panelLoading" class="loading-container">
      <el-skeleton :rows="5" animated />
    </div>

    <div v-else-if="ballot && election">
      <div v-if="showMetadata" class="ballot-info">
        <el-descriptions :column="2" border>
          <el-descriptions-item :label="$t('ballots.location')">
            {{ ballot.locationName }}
          </el-descriptions-item>
          <el-descriptions-item :label="$t('ballots.computer')">
            {{ ballot.computerCode }}
          </el-descriptions-item>
          <el-descriptions-item :label="$t('ballots.teller1')">
            {{ ballot.teller1 || "-" }}
          </el-descriptions-item>
          <el-descriptions-item :label="$t('ballots.teller2')">
            {{ ballot.teller2 || "-" }}
          </el-descriptions-item>
        </el-descriptions>
      </div>

      <div class="votes-section">
        <InlineBallotEntry
          :election-guid="electionGuid"
          :ballot="ballot"
          :required-votes="election.numberToElect || 9"
          :resync-key="voteResyncKey"
          :has-keyboard-teller="hasKeyboardTeller"
          @vote-added="handleVoteAdded"
          @vote-removed="handleVoteRemoved"
          @votes-reordered="handleVotesReordered"
          @ballot-created="emit('ballot-created', $event)"
          @delete-ballot="handleDeleteBallot"
        />
      </div>
    </div>
  </div>
</template>

<style lang="less">
.ballot-entry-panel {
  .ballot-info {
    margin-bottom: 24px;
  }

  .votes-section {
    margin-top: 8px;
  }

  .loading-container {
    padding: 24px;
  }
}
</style>
