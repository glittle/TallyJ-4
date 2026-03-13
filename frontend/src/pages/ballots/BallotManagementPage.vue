<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref } from "vue";
import { useRoute, useRouter } from "vue-router";
import { useI18n } from "vue-i18n";
import { ElMessageBox } from "element-plus";
import { useNotifications } from "@/composables/useNotifications";
import { Plus, Upload } from "@element-plus/icons-vue";
import { useBallotStore } from "../../stores/ballotStore";
import type { BallotDto } from "../../types";
import BallotFormDialog from "../../components/ballots/BallotFormDialog.vue";
import BallotVotesDialog from "../../components/ballots/BallotVotesDialog.vue";

const router = useRouter();
const route = useRoute();
const { t } = useI18n();
const ballotStore = useBallotStore();
const { showSuccessMessage, showErrorMessage } = useNotifications();
const electionGuid = route.params.id as string;
const showAddDialog = ref(false);
const showVotesDialog = ref(false);
const selectedBallot = ref<BallotDto | null>(null);
const loading = computed(() => ballotStore.loading);
const ballots = computed(() => ballotStore.ballots);

onMounted(async () => {
  try {
    await ballotStore.fetchBallots(electionGuid);
    console.log(1, ballotStore.ballots);
    await ballotStore.initializeSignalR();
    await ballotStore.joinElection(electionGuid);
  } catch (_error) {
    showErrorMessage(t("ballots.loadError") + ": " + (_error as Error).message);
  }
});

onBeforeUnmount(async () => {
  try {
    await ballotStore.leaveElection(electionGuid);
  } catch (error) {
    console.error("Failed to leave election group for ballot updates:", error);
  }
});

function goBack() {
  router.push(`/elections/${electionGuid}`);
}

function handleImport() {
  router.push(`/elections/${electionGuid}/ballots/import`);
}

function handleEnterVotes(ballot: BallotDto) {
  if (!ballot?.ballotGuid) return;
  router.push(`/elections/${electionGuid}/ballots/${ballot.ballotGuid}/entry`);
}

function handleViewVotes(ballot: BallotDto) {
  if (!ballot) return;
  selectedBallot.value = ballot;
  showVotesDialog.value = true;
}

async function handleDelete(ballot: BallotDto) {
  if (!ballot?.ballotGuid) return;
  try {
    await ElMessageBox.confirm(
      t("ballots.deleteConfirm"),
      t("common.warning"),
      {
        confirmButtonText: t("common.delete"),
        cancelButtonText: t("common.cancel"),
        type: "warning",
      },
    );
    await ballotStore.deleteBallot(ballot.ballotGuid);
    showSuccessMessage(t("ballots.deleteSuccess"));
  } catch (error: any) {
    if (error !== "cancel") {
      showErrorMessage((error as Error).message || t("ballots.deleteError"));
    }
  }
}

function handleFormSuccess() {
  showAddDialog.value = false;
}

function getStatusType(status: string | undefined) {
  if (!status) {
    return "info";
  }
  if (status.toLowerCase() === "ok") {
    return "success";
  }
  return "warning";
}
</script>

<template>
  <div class="ballot-management-page">
    <el-card>
      <template #header>
        <div class="card-header">
          <el-page-header :content="$t('ballots.management')" @back="goBack" />
          <div class="header-actions">
            <el-button @click="handleImport">
              <el-icon>
                <Upload />
              </el-icon>
              {{ $t("ballots.import.button") }}
            </el-button>
            <el-button type="primary" @click="showAddDialog = true">
              <el-icon>
                <Plus />
              </el-icon>
              {{ $t("ballots.addBallot") }}
            </el-button>
          </div>
        </div>
      </template>

      <div v-if="loading" class="loading-container">
        <el-skeleton :rows="5" animated />
      </div>

      <div v-else>
        <el-table :data="ballots" style="width: 100%">
          <el-table-column
            prop="ballotCode"
            :label="$t('ballots.code')"
            width="120"
          />
          <el-table-column
            prop="locationName"
            :label="$t('ballots.location')"
            min-width="150"
          />
          <el-table-column
            prop="computerCode"
            :label="$t('ballots.computer')"
            width="120"
          />
          <el-table-column
            prop="statusCode"
            :label="$t('ballots.status')"
            width="100"
          >
            <template #default="scope">
              <el-tag :type="getStatusType(scope.row?.statusCode)">
                {{ $t(`ballots.statusValue.${scope.row?.statusCode}`) || "-" }}
              </el-tag>
            </template>
          </el-table-column>
          <el-table-column
            prop="teller1"
            :label="$t('ballots.teller1')"
            width="120"
          />
          <el-table-column
            prop="teller2"
            :label="$t('ballots.teller2')"
            width="120"
          />
          <el-table-column
            prop="voteCount"
            :label="$t('ballots.voteCount')"
            width="100"
            align="center"
          />
          <el-table-column
            :label="$t('common.actions')"
            width="250"
            fixed="right"
          >
            <template #default="scope">
              <el-button-group>
                <el-button
                  size="small"
                  type="primary"
                  @click="handleEnterVotes(scope.row)"
                >
                  {{ $t("ballots.enterVotes") }}
                </el-button>
                <el-button size="small" @click="handleViewVotes(scope.row)">
                  {{ $t("ballots.viewVotes") }}
                </el-button>
                <el-button
                  size="small"
                  type="danger"
                  @click="handleDelete(scope.row)"
                >
                  {{ $t("common.delete") }}
                </el-button>
              </el-button-group>
            </template>
          </el-table-column>
        </el-table>
      </div>
    </el-card>

    <BallotFormDialog
      v-model="showAddDialog"
      :election-guid="electionGuid"
      @success="handleFormSuccess"
    />

    <BallotVotesDialog
      v-model="showVotesDialog"
      :ballot="selectedBallot"
      :election-guid="electionGuid"
    />
  </div>
</template>

<style lang="less">
.ballot-management-page {
  max-width: 1400px;
  margin: 0 auto;

  .card-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
  }

  .header-actions {
    display: flex;
    gap: 10px;
  }

  .loading-container {
    padding: 40px;
  }
}
</style>
