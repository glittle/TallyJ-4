<script setup lang="ts">
import { useNotifications } from "@/composables/useNotifications";
import { computed, onMounted, onUnmounted } from "vue";
import { useI18n } from "vue-i18n";
import { useRoute, useRouter } from "vue-router";
import InlineBallotEntry from "../../components/ballots/InlineBallotEntry.vue";
import { useBallotStore } from "../../stores/ballotStore";
import { useElectionStore } from "../../stores/electionStore";
import { usePeopleStore } from "../../stores/peopleStore";
import type {
  CreateVoteDto,
  VoteDto,
  VoteWithBallotStatusDto,
} from "../../types";

const router = useRouter();
const route = useRoute();
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

const electionGuid = route.params.id as string;
const ballotGuid = route.params.ballotId as string;

const loading = computed(() => ballotStore.loading || electionStore.loading);
const ballot = computed(() => ballotStore.currentBallot);
const election = computed(() => electionStore.currentElection);

onMounted(async () => {
  try {
    await Promise.all([
      ballotStore.initializeSignalR(),
      peopleStore.initializeSignalR(),
    ]);

    await Promise.all([
      ballotStore.joinElection(electionGuid),
      peopleStore.joinElection(electionGuid),
    ]);

    await Promise.all([
      ballotStore.fetchBallotById(ballotGuid),
      electionStore.fetchElectionById(electionGuid),
    ]);

    setupPersonUpdateHandler();
  } catch (_error) {
    showErrorMessage(t("ballots.loadError"));
  }
});

onUnmounted(async () => {
  try {
    await Promise.all([
      ballotStore.leaveElection(electionGuid),
      peopleStore.leaveElection(electionGuid),
    ]);
  } catch (error) {
    console.error("Failed to leave election group for ballot entry:", error);
  }
});

function setupPersonUpdateHandler() {
  const originalHandler = peopleStore.handlePersonUpdated.bind(peopleStore);

  peopleStore.handlePersonUpdated = async (data) => {
    await originalHandler(data);

    if (data.firstName || data.lastName) {
      const fullName = [data.firstName, data.lastName]
        .filter(Boolean)
        .join(" ");
      showInfoMessage(t("ballots.personUpdated", { name: fullName }));
    }
  };
}

function goBack() {
  router.push(`/elections/${electionGuid}/ballots`);
}

async function handleVoteAdded(vote: VoteDto) {
  try {
    const createDto: CreateVoteDto = {
      ballotGuid: vote.ballotGuid,
      positionOnBallot: vote.positionOnBallot,
      personGuid: vote.personGuid || undefined,
    };

    const result: VoteWithBallotStatusDto =
      await ballotStore.createVote(createDto);
    const isSpoiled = result.vote.statusCode && result.vote.statusCode !== "ok";
    if (isSpoiled) {
      showWarningMessage(
        t("ballots.voteSpoiledSuccess", { code: result.vote.statusCode }),
      );
    } else {
      showSuccessMessage(t("ballots.voteAddedSuccess"));
    }
  } catch (error: any) {
    showErrorMessage(error.message || t("ballots.voteAddedError"));
  }
}

async function handleVoteRemoved(positionOnBallot: number) {
  try {
    await ballotStore.deleteVote(ballotGuid, positionOnBallot);
    showSuccessMessage(t("ballots.voteRemovedSuccess"));
  } catch (error: any) {
    showErrorMessage(error.message || t("ballots.voteRemovedError"));
  }
}

function getStatusType(status: string) {
  const typeMap: Record<string, any> = {
    ok: "success",
    review: "warning",
    spoiled: "danger",
  };
  return typeMap[status] || "info";
}
</script>
<template>
  <div class="ballot-entry-page">
    <el-card>
      <template #header>
        <div class="card-header">
          <el-page-header
            :content="$t('ballots.entry', { code: ballot?.ballotCode })"
            @back="goBack"
          />
        </div>
      </template>

      <div v-if="loading" class="loading-container">
        <el-skeleton :rows="5" animated />
      </div>

      <div v-else-if="ballot && election">
        <div class="ballot-info">
          <el-descriptions :column="2" border>
            <el-descriptions-item :label="$t('ballots.location')">
              {{ ballot.locationName }}
            </el-descriptions-item>
            <el-descriptions-item :label="$t('ballots.status')">
              <el-tag :type="getStatusType(ballot.statusCode)">
                {{ ballot.statusCode }}
              </el-tag>
            </el-descriptions-item>
            <el-descriptions-item :label="$t('ballots.teller1')">
              {{ ballot.teller1 || "-" }}
            </el-descriptions-item>
            <el-descriptions-item :label="$t('ballots.teller2')">
              {{ ballot.teller2 || "-" }}
            </el-descriptions-item>
            <el-descriptions-item :label="$t('ballots.voteCount')">
              {{ ballot.voteCount }}
            </el-descriptions-item>
          </el-descriptions>
        </div>

        <div class="votes-section">
          <InlineBallotEntry
            :election-guid="electionGuid"
            :ballot="ballot"
            :max-votes="election.numberToElect || 9"
            @vote-added="handleVoteAdded"
            @vote-removed="handleVoteRemoved"
          />
        </div>
      </div>
    </el-card>
  </div>
</template>

<style lang="less">
.ballot-entry-page {
  max-width: var(--normal-max-width);
  margin: 0 auto;

  .card-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
  }

  .ballot-info {
    margin-bottom: 30px;
  }

  .votes-section {
    margin-top: 20px;
  }

  .section-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 20px;
  }

  .section-header h3 {
    margin: 0;
    font-size: 18px;
    color: #303133;
  }

  .loading-container {
    padding: 40px;
  }
}
</style>
