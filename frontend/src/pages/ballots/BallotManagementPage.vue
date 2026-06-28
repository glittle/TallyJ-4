<script setup lang="ts">
import ActiveTellerSelector from "@/components/tellers/ActiveTellerSelector.vue";
import { useApiErrorHandler } from "@/composables/useApiErrorHandler";
import { useNotifications } from "@/composables/useNotifications";
import { getActiveTellerPayload } from "@/utils/activeTellerStorage";
import { Plus } from "@element-plus/icons-vue";
import { computed, onBeforeUnmount, onMounted, ref } from "vue";
import { useI18n } from "vue-i18n";
import { useRoute } from "vue-router";
import BallotEntryPanel from "../../components/ballots/BallotEntryPanel.vue";
import { useBallotStore } from "../../stores/ballotStore";
import type { BallotDto } from "../../types";
import { getBallotStatusLabel } from "@/utils/ballotStatusLabel";

const route = useRoute();
const { t } = useI18n();
const ballotStore = useBallotStore();
const { showSuccessMessage, showErrorMessage } = useNotifications();
const { handleApiError } = useApiErrorHandler();
const electionGuid = route.params.id as string;

const showDrawer = ref(false);
const drawerBallotGuid = ref<string | null>(null);
const isNewBallot = ref(false);
const creatingBallot = ref(false);
const listLoading = ref(false);

const ballots = computed(() => ballotStore.ballots);

const drawerBallot = computed(() => {
  if (!drawerBallotGuid.value) {
    return null;
  }

  if (ballotStore.currentBallot?.ballotGuid === drawerBallotGuid.value) {
    return ballotStore.currentBallot;
  }

  return (
    ballotStore.ballots.find((b) => b.ballotGuid === drawerBallotGuid.value) ??
    null
  );
});

const drawerTitle = computed(() => {
  const ballot = drawerBallot.value;
  if (!ballot?.ballotCode) {
    return t("ballots.entryPage");
  }

  let statusLabel = getBallotStatusLabel(t, ballot.statusCode, {
    isNewSession: isNewBallot.value,
  });
  if (ballot.statusCode === "TooFew" || ballot.statusCode === "TooMany") {
    statusLabel = `${statusLabel} (${ballot.voteCount})`;
  }

  return t("ballots.entryDrawerTitle", {
    code: ballot.ballotCode,
    status: statusLabel,
  });
});

onMounted(async () => {
  listLoading.value = true;
  try {
    await ballotStore.fetchBallots(electionGuid);
    await ballotStore.initializeSignalR();
    await ballotStore.joinElection(electionGuid);
  } catch (error) {
    showErrorMessage(t("ballots.loadError") + ": " + (error as Error).message);
  } finally {
    listLoading.value = false;
  }
});

onBeforeUnmount(async () => {
  try {
    await ballotStore.leaveElection(electionGuid);
  } catch (error) {
    console.error("Failed to leave election group for ballot updates:", error);
  }
});

function openBallot(ballot: BallotDto) {
  if (!ballot?.ballotGuid) {
    return;
  }
  isNewBallot.value = false;
  drawerBallotGuid.value = ballot.ballotGuid;
  showDrawer.value = true;
}

function handleDrawerClosed() {
  isNewBallot.value = false;
}

async function handleAddBallot() {
  creatingBallot.value = true;
  try {
    const ballot = await ballotStore.createBallot({
      electionGuid,
      computerCode: "A",
      ...getActiveTellerPayload(),
    });
    showSuccessMessage(t("ballots.createSuccess"));
    isNewBallot.value = true;
    drawerBallotGuid.value = ballot.ballotGuid;
    showDrawer.value = true;
  } catch (error) {
    handleApiError(error);
  } finally {
    creatingBallot.value = false;
  }
}

function getStatusType(status: string | undefined) {
  if (!status) {
    return "info";
  }
  switch (status.toLowerCase()) {
    case "ok":
      return "success";
    case "review":
      return "danger";
    case "duplicate":
      return "info";
    default:
      return "warning";
  }
}
</script>

<template>
  <div class="enter-ballots-page">
    <el-card>
      <template #header>
        <div class="card-header">
          <div class="header-actions">
            <el-button
              type="primary"
              :loading="creatingBallot"
              @click="handleAddBallot"
            >
              <el-icon>
                <Plus />
              </el-icon>
              {{ $t("ballots.addBallot") }}
            </el-button>
          </div>
        </div>
        <ActiveTellerSelector
          :election-guid="electionGuid"
          class="header-tellers"
        />
      </template>

      <div v-if="listLoading" class="loading-container">
        <el-skeleton :rows="5" animated />
      </div>

      <div v-else>
        <el-table :data="ballots" row-key="ballotGuid" style="width: 100%">
          <el-table-column
            prop="ballotCode"
            :label="$t('ballots.code')"
            width="120"
          >
            <template #default="scope">
              <el-button
                link
                type="primary"
                class="ballot-code-link"
                @click="openBallot(scope.row)"
              >
                {{ scope.row.ballotCode }}
              </el-button>
            </template>
          </el-table-column>
          <el-table-column prop="statusCode" :label="$t('ballots.status')">
            <template #default="scope">
              <el-tag :type="getStatusType(scope.row?.statusCode)">
                {{ getBallotStatusLabel($t, scope.row?.statusCode) || "-" }}
                <span
                  v-if="
                    scope.row?.statusCode === 'TooFew' ||
                    scope.row?.statusCode === 'TooMany'
                  "
                >
                  ({{ scope.row.voteCount }})</span
                >
              </el-tag>
            </template>
          </el-table-column>
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
            prop="teller1"
            :label="$t('ballots.teller1')"
            width="120"
          />
          <el-table-column
            prop="teller2"
            :label="$t('ballots.teller2')"
            width="120"
          />
        </el-table>
      </div>
    </el-card>

    <el-drawer
      v-model="showDrawer"
      :title="drawerTitle"
      direction="rtl"
      size="70%"
      :lock-scroll="false"
      modal-class="ballot-entry-drawer"
      @closed="handleDrawerClosed"
    >
      <BallotEntryPanel
        v-if="drawerBallotGuid"
        v-show="showDrawer"
        :key="drawerBallotGuid"
        :election-guid="electionGuid"
        :ballot-guid="drawerBallotGuid"
        :show-metadata="!isNewBallot"
        :manage-ballot-signal-r="false"
      />
    </el-drawer>
  </div>
</template>

<style lang="less">
.enter-ballots-page {
  max-width: 1400px;
  margin: 0 auto;
  .el-drawer__title {
    font-size: 18px;
    font-weight: bold;
  }
  .card-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
  }

  .header-actions {
    display: flex;
    gap: 10px;
  }

  .header-tellers {
    margin-top: 12px;
  }

  .loading-container {
    padding: 40px;
  }

  .ballot-code-link {
    padding: 2px 5px;
  }
}

// Element Plus applies `transition: all` on .el-drawer AND on the
// el-drawer-fade enter/leave wrapper. On re-open the two fight and
// produce a bounce. Only the wrapper transition should animate.
.ballot-entry-drawer {
  .el-drawer {
    transition: none;
  }
}
</style>
