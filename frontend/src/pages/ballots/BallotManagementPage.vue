<script setup lang="ts">
import BallotViewFilterSelect from "@/components/ballots/BallotViewFilterSelect.vue";
import ActiveTellerSelector from "@/components/tellers/ActiveTellerSelector.vue";
import { useApiErrorHandler } from "@/composables/useApiErrorHandler";
import { useComputerCode } from "@/composables/useComputerCode";
import { useNotifications } from "@/composables/useNotifications";

import {
  getActiveTellerPayload,
  getActiveTellers,
  type ActiveTellers,
} from "@/utils/activeTellerStorage";
import { getBallotStatusLabel } from "@/utils/ballotStatusLabel";
import type { BallotSummaryDto } from "@/utils/ballotSummary";
import {
  computerFilterValue,
  defaultBallotViewFilter,
  filterBallotsByView,
} from "@/utils/ballotViewFilter";
import { Location, Plus, Refresh } from "@element-plus/icons-vue";
import { computed, onBeforeUnmount, onMounted, ref, watch } from "vue";
import { useI18n } from "vue-i18n";
import { useRoute } from "vue-router";
import BallotEntryPanel from "../../components/ballots/BallotEntryPanel.vue";
import { useBallotStore } from "../../stores/ballotStore";
import { useLocationStore } from "../../stores/locationStore";

const route = useRoute();
const { t } = useI18n();
const ballotStore = useBallotStore();
const locationStore = useLocationStore();
const { computerCode, refreshComputerCode } = useComputerCode();
const { showSuccessMessage, showErrorMessage } = useNotifications();
const { handleApiError } = useApiErrorHandler();
const electionGuid = route.params.id as string;

const showDrawer = ref(false);
const drawerBallotGuid = ref<string | null>(null);
const isNewBallot = ref(false);
const creatingBallot = ref(false);
const listLoading = ref(false);
const refreshing = ref(false);
const activeTellers = ref<ActiveTellers>(getActiveTellers());
const selectedViewFilter = ref(
  defaultBallotViewFilter("", locationStore.selectedLocationGuid),
);
const computersByLocation = ref<Record<string, never>>({});

const hasKeyboardTeller = computed(() =>
  Boolean(activeTellers.value.teller1.trim()),
);

const showLocationColumn = computed(() => locationStore.locations.length > 1);

const filteredBallots = computed(() =>
  filterBallotsByView(ballotStore.ballots, selectedViewFilter.value),
);

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

watch(computerCode, (code) => {
  selectedViewFilter.value = defaultBallotViewFilter(
    code,
    locationStore.selectedLocationGuid,
  );
});

function onTellersChanged(tellers: ActiveTellers) {
  activeTellers.value = tellers;
}

onMounted(async () => {
  refreshComputerCode();
  selectedViewFilter.value = defaultBallotViewFilter(
    computerCode.value,
    locationStore.selectedLocationGuid,
  );

  listLoading.value = true;
  try {
    await ballotStore.initializeSignalR();
    await locationStore.fetchLocations(electionGuid);
    await Promise.all([
      ballotStore.fetchBallots(electionGuid),
      ballotStore.joinElection(electionGuid),
    ]);
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

function openBallot(ballot: BallotSummaryDto) {
  if (!ballot?.ballotGuid) {
    return;
  }
  isNewBallot.value = false;
  drawerBallotGuid.value = ballot.ballotGuid;
  showDrawer.value = true;
}

function handleDrawerClosed() {
  isNewBallot.value = false;
  ballotStore.clearCurrentBallot();
}

async function handleRefresh() {
  refreshing.value = true;
  try {
    await ballotStore.fetchBallots(electionGuid);
  } catch (error) {
    showErrorMessage(t("ballots.loadError") + ": " + (error as Error).message);
  } finally {
    refreshing.value = false;
  }
}

async function handleAddBallot() {
  if (!computerCode.value) {
    showErrorMessage(t("ballots.computerCodeRequired"));
    return;
  }

  if (!locationStore.selectedLocationGuid) {
    showErrorMessage(t("ballots.locationRequired"));
    return;
  }

  creatingBallot.value = true;
  try {
    const ballot = await ballotStore.createBallot({
      electionGuid,
      computerCode: computerCode.value,
      locationGuid: locationStore.selectedLocationGuid!,
      ...getActiveTellerPayload(),
    });
    showSuccessMessage(t("ballots.createSuccess"));
    isNewBallot.value = true;
    drawerBallotGuid.value = ballot.ballotGuid;
    showDrawer.value = true;
    selectedViewFilter.value = computerFilterValue(
      locationStore.selectedLocationGuid,
      computerCode.value,
    );
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

function handleLocationChange(locationGuid: string | null) {
  locationStore.selectLocation(locationGuid);
  if (computerCode.value) {
    selectedViewFilter.value = defaultBallotViewFilter(
      computerCode.value,
      locationGuid,
    );
  }
  showSuccessMessage(t("locations.locationSelected"));
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

          <ActiveTellerSelector
            :election-guid="electionGuid"
            class="header-tellers"
            @tellers-changed="onTellersChanged"
          />

          <div
            v-if="locationStore.locations?.length > 0"
            class="location-selector"
          >
            <el-icon class="location-icon">
              <Location />
            </el-icon>
            <el-select
              :model-value="locationStore.selectedLocationGuid"
              :placeholder="$t('locations.selectLocation')"
              clearable
              :aria-label="$t('locations.currentLocation')"
              class="location-select"
              @update:model-value="handleLocationChange"
            >
              <el-option
                v-for="location in locationStore.sortedLocations"
                :key="location.locationGuid"
                :label="location.name"
                :value="location.locationGuid"
              />
            </el-select>
          </div>
        </div>
      </template>

      <div v-if="listLoading" class="loading-container">
        <el-skeleton :rows="5" animated />
      </div>

      <div v-else class="ballot-list-section">
        <div class="ballot-list-toolbar">
          <BallotViewFilterSelect
            v-model="selectedViewFilter"
            :locations="locationStore.sortedLocations"
            :ballots="ballotStore.ballots"
            :computers-by-location="computersByLocation"
          />

          <el-button
            text
            class="refresh-button"
            :loading="refreshing"
            :aria-label="$t('common.refresh')"
            @click="handleRefresh"
          >
            <el-icon>
              <Refresh />
            </el-icon>
          </el-button>
        </div>

        <el-table
          :data="filteredBallots"
          row-key="ballotGuid"
          class="ballot-list-table"
        >
          <el-table-column prop="ballotCode" :label="$t('ballots.code')">
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
          <el-table-column
            v-if="showLocationColumn"
            prop="locationName"
            :label="$t('ballots.location')"
            min-width="140"
          />
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
        </el-table>
      </div>
    </el-card>

    <el-drawer
      v-model="showDrawer"
      :title="drawerTitle"
      direction="rtl"
      size="1200px"
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
        :has-keyboard-teller="hasKeyboardTeller"
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

  .loading-container {
    padding: 40px;
  }

  .ballot-list-section {
    display: flex;
    flex-direction: column;
    gap: 8px;
  }

  .ballot-list-toolbar {
    display: flex;
    align-items: center;
    gap: 4px;
    max-width: 420px;
  }

  .ballot-list-table {
    width: 100%;
    max-width: 520px;
  }

  .refresh-button {
    color: var(--el-text-color-secondary);
    padding: 4px;
    flex-shrink: 0;

    &:hover {
      color: var(--el-text-color-regular);
    }
  }

  .ballot-code-link {
    padding: 2px 5px;
  }

  .location-selector {
    display: flex;
    align-items: center;
    gap: 8px;
    margin-left: 16px;
    padding-left: 16px;
    border-left: 1px solid var(--el-border-color-lighter);

    .location-icon {
      color: var(--el-color-primary);
      font-size: 16px;
    }

    .location-select {
      min-width: 180px;
      max-width: 250px;
    }
  }
}

.ballot-entry-drawer {
  .el-drawer {
    transition: none;
  }
}
</style>
