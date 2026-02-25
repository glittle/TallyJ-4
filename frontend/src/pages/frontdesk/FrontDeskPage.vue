<template>
  <div class="front-desk-page">
    <el-card>
      <template #header>
        <div class="card-header">
          <el-page-header @back="goBack" content="Front Desk - Voter Check-In" />
        </div>
      </template>

      <el-row :gutter="20">
        <el-col :span="16">
          <el-card shadow="never">
            <template #header>
              <div class="section-header">
                <h3>Quick Check-In</h3>
                <el-input ref="searchInputRef" v-model="searchQuery"
                  placeholder="Type name to search (↑↓ arrows, Enter to select)" style="width: 450px;" clearable
                  @input="handleSearch" @keydown="handleSearchKeydown">
                  <template #prefix>
                    <el-icon>
                      <Search />
                    </el-icon>
                  </template>
                </el-input>
              </div>
            </template>

            <!-- Registration type selection (shown after pressing Enter) -->
            <div v-if="showRegistrationButtons && selectedVoter" class="registration-buttons">
              <div class="selected-voter-info">
                <strong>Check in: {{ selectedVoter.fullName }}</strong>
                <span v-if="selectedVoter.bahaiId" class="voter-detail">ID: {{ selectedVoter.bahaiId }}</span>
                <span v-if="selectedVoter.area" class="voter-detail">Area: {{ selectedVoter.area }}</span>
              </div>
              <div class="button-group">
                <el-button v-for="(type, index) in registrationTypes" :key="type.value"
                  :type="index === selectedButtonIndex ? 'primary' : 'default'" size="large"
                  @click="confirmCheckIn(type.value)" :class="{ 'selected-button': index === selectedButtonIndex }">
                  {{ type.label }} <kbd>{{ type.key }}</kbd>
                </el-button>
              </div>
              <div class="instruction-text">
                Use ← → arrows or number keys 1-4, press Enter to confirm, Esc to cancel
              </div>
            </div>

            <!-- Not checked in voters list -->
            <div v-else>
              <el-table :data="notCheckedInVoters" :loading="loading" style="width: 100%" max-height="500px"
                :row-class-name="getRowClassName" @row-click="handleRowClick">
                <el-table-column prop="fullName" label="Name" sortable />
                <el-table-column prop="bahaiId" label="Bahá'í ID" width="120" />
                <el-table-column prop="area" label="Area" width="150" />
              </el-table>
              <div class="keyboard-hint">
                {{ notCheckedInVoters.length }} voters shown. Use keyboard to navigate and check in.
              </div>
            </div>

            <!-- Checked in voters -->
            <el-divider />
            <h4>Recently Checked In ({{ checkedInVoters.length }} total)</h4>
            <el-table :data="checkedInVoters.slice(0, 10)" style="width: 100%" max-height="300px">
              <el-table-column prop="fullName" label="Name" width="200" />
              <el-table-column prop="bahaiId" label="Bahá'í ID" width="120" />
              <el-table-column prop="envNum" label="Env #" width="80" />
              <el-table-column label="Method" width="100">
                <template #default="{ row }">
                  <el-tag v-if="row.votingMethod === 'I'" type="success" size="small">In Person</el-tag>
                  <el-tag v-else-if="row.votingMethod === 'M'" type="info" size="small">Mail</el-tag>
                  <el-tag v-else-if="row.votingMethod === 'O'" type="primary" size="small">Online</el-tag>
                  <el-tag v-else-if="row.votingMethod === 'C'" type="warning" size="small">Call-In</el-tag>
                </template>
              </el-table-column>
              <el-table-column label="Time" width="100">
                <template #default="{ row }">
                  {{ formatTimeShort(row.registrationTime) }}
                </template>
              </el-table-column>
              <el-table-column label="Actions" width="120" fixed="right">
                <template #default="{ row }">
                  <el-button type="default" size="small" :icon="Clock" @click.stop="showHistory(row)" circle
                    title="View History" />
                  <el-button type="warning" size="small" :icon="Warning" @click.stop="handleUnregister(row)" circle
                    title="Unregister" />
                </template>
              </el-table-column>
            </el-table>
          </el-card>
        </el-col>

        <el-col :span="8">
          <el-card shadow="never">
            <template #header>
              <h3>Statistics</h3>
            </template>

            <div v-if="stats" class="stats-section">
              <el-statistic :value="stats.checkedIn" title="Checked In">
                <template #prefix>
                  <el-icon>
                    <UserFilled />
                  </el-icon>
                </template>
              </el-statistic>

              <el-divider />

              <el-statistic :value="stats.notYetCheckedIn" title="Not Yet Checked In">
                <template #prefix>
                  <el-icon>
                    <User />
                  </el-icon>
                </template>
              </el-statistic>

              <el-divider />

              <el-statistic :value="stats.totalEligible" title="Total Eligible">
                <template #prefix>
                  <el-icon>
                    <Tickets />
                  </el-icon>
                </template>
              </el-statistic>

              <el-divider />

              <div class="progress-section">
                <div class="progress-label">
                  <span>Progress</span>
                  <span class="progress-percentage">{{ stats.checkInPercentage.toFixed(1) }}%</span>
                </div>
                <el-progress :percentage="stats.checkInPercentage" :color="getProgressColor(stats.checkInPercentage)"
                  :stroke-width="20" />
              </div>
            </div>
          </el-card>

          <el-card shadow="never" style="margin-top: 20px;">
            <template #header>
              <h3>Recent Check-Ins</h3>
            </template>

            <el-timeline>
              <el-timeline-item v-for="voter in recentCheckIns" :key="voter.personGuid"
                :timestamp="formatTime(voter.registrationTime)" placement="top">
                <el-card>
                  <p><strong>{{ voter.fullName }}</strong></p>
                  <p>Envelope: #{{ voter.envNum }}</p>
                </el-card>
              </el-timeline-item>
            </el-timeline>
          </el-card>
        </el-col>
      </el-row>
    </el-card>

    <!-- History dialog -->
    <el-dialog v-model="showHistoryDialog" title="Registration History" width="600px">
      <div v-if="historyVoter">
        <h4>{{ historyVoter.fullName }}</h4>
        <p v-if="historyVoter.bahaiId">Bahá'í ID: {{ historyVoter.bahaiId }}</p>
        <el-divider />

        <div v-if="historyVoter.registrationHistory && historyVoter.registrationHistory.length > 0">
          <el-timeline>
            <el-timeline-item v-for="(entry, index) in historyVoter.registrationHistory" :key="index"
              :timestamp="formatTime(entry.timestamp)" placement="top">
              <el-card>
                <p><strong>{{ entry.action }}</strong></p>
                <p v-if="entry.votingMethod">Method: {{ entry.votingMethod }}</p>
                <p v-if="entry.tellerName">Teller: {{ entry.tellerName }}</p>
                <p v-if="entry.locationName">Location: {{ entry.locationName }}</p>
                <p v-if="entry.envNum">Envelope: #{{ entry.envNum }}</p>
                <p v-if="entry.performedBy" class="performed-by">By: {{ entry.performedBy }}</p>
              </el-card>
            </el-timeline-item>
          </el-timeline>
        </div>
        <div v-else>
          <el-empty description="No history available" />
        </div>
      </div>

      <template #footer>
        <el-button @click="showHistoryDialog = false">Close</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted, watch, nextTick } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useFrontDeskStore } from '@/stores/frontDeskStore';
import { useLocationStore } from '@/stores/locationStore';
import { ElMessageBox } from 'element-plus';
import { useNotifications } from '@/composables/useNotifications';
import { Search, UserFilled, User, Tickets, Clock, Warning } from '@element-plus/icons-vue';
import type { FrontDeskVoterDto } from '@/types/FrontDesk';

const route = useRoute();
const router = useRouter();
const frontDeskStore = useFrontDeskStore();
const locationStore = useLocationStore();
const { showSuccessMessage, showErrorMessage } = useNotifications();

const electionGuid = ref(route.params.electionId as string);
const searchQuery = ref('');
const loading = computed(() => frontDeskStore.loading);
const stats = computed(() => frontDeskStore.stats);
const locations = computed(() => locationStore.locations);

// Keyboard navigation
const searchInputRef = ref();
const selectedIndex = ref(0);
const selectedVoter = ref<FrontDeskVoterDto | null>(null);
const showRegistrationButtons = ref(false);
const selectedButtonIndex = ref(0);

// History dialog
const showHistoryDialog = ref(false);
const historyVoter = ref<FrontDeskVoterDto | null>(null);

const notCheckedInVoters = computed(() => frontDeskStore.notCheckedInVoters);
const checkedInVoters = computed(() => frontDeskStore.checkedInVoters);

// Registration type options
const registrationTypes = [
  { value: 'I', label: 'In Person', key: '1' },
  { value: 'M', label: 'Mail', key: '2' },
  { value: 'O', label: 'Online', key: '3' },
  { value: 'C', label: 'Call-In', key: '4' }
];

const recentCheckIns = computed(() => {
  return checkedInVoters.value
    .slice()
    .sort((a, b) => {
      const aTime = a.registrationTime ? new Date(a.registrationTime).getTime() : 0;
      const bTime = b.registrationTime ? new Date(b.registrationTime).getTime() : 0;
      return bTime - aTime;
    })
    .slice(0, 5);
});

onMounted(async () => {
  await loadData();
  await frontDeskStore.initializeSignalR();
  await frontDeskStore.joinElection(electionGuid.value);

  // Focus search input
  nextTick(() => {
    searchInputRef.value?.focus();
  });
});

onUnmounted(async () => {
  await frontDeskStore.leaveElection(electionGuid.value);
});

// Watch search query and update selection
watch(searchQuery, () => {
  selectedIndex.value = 0;
  updateSelectedVoter();
  showRegistrationButtons.value = false;
});

// Watch filtered voters and update selection
watch(notCheckedInVoters, () => {
  updateSelectedVoter();
});

async function loadData() {
  try {
    await frontDeskStore.fetchEligibleVoters(electionGuid.value);
    await locationStore.fetchLocations(electionGuid.value);
  } catch (error: any) {
    ElMessage.error(error.message || 'Failed to load data');
  }
}

function updateSelectedVoter() {
  const voters = notCheckedInVoters.value;
  if (voters.length > 0 && selectedIndex.value >= 0 && selectedIndex.value < voters.length) {
    selectedVoter.value = voters[selectedIndex.value];
  } else {
    selectedVoter.value = null;
  }
}

function handleSearch() {
  frontDeskStore.setSearchQuery(searchQuery.value);
}

function handleSearchKeydown(event: KeyboardEvent) {
  const voters = notCheckedInVoters.value;

  if (showRegistrationButtons.value) {
    // Handle button navigation
    if (event.key === 'ArrowLeft') {
      event.preventDefault();
      selectedButtonIndex.value = Math.max(0, selectedButtonIndex.value - 1);
    } else if (event.key === 'ArrowRight') {
      event.preventDefault();
      selectedButtonIndex.value = Math.min(registrationTypes.length - 1, selectedButtonIndex.value + 1);
    } else if (event.key === 'Enter') {
      event.preventDefault();
      const selectedType = registrationTypes[selectedButtonIndex.value];
      if (selectedType) {
        confirmCheckIn(selectedType.value);
      }
    } else if (event.key === 'Escape') {
      event.preventDefault();
      showRegistrationButtons.value = false;
      selectedButtonIndex.value = 0;
    } else if (event.key >= '1' && event.key <= '4') {
      event.preventDefault();
      const index = parseInt(event.key) - 1;
      if (index >= 0 && index < registrationTypes.length) {
        confirmCheckIn(registrationTypes[index].value);
      }
    }
  } else {
    // Handle list navigation
    if (event.key === 'ArrowDown') {
      event.preventDefault();
      selectedIndex.value = Math.min(voters.length - 1, selectedIndex.value + 1);
      updateSelectedVoter();
    } else if (event.key === 'ArrowUp') {
      event.preventDefault();
      selectedIndex.value = Math.max(0, selectedIndex.value - 1);
      updateSelectedVoter();
    } else if (event.key === 'Enter') {
      event.preventDefault();
      if (selectedVoter.value) {
        showRegistrationButtons.value = true;
        selectedButtonIndex.value = 0;
      }
    }
  }
}

function handleRowClick(row: FrontDeskVoterDto) {
  selectedVoter.value = row;
  selectedIndex.value = notCheckedInVoters.value.findIndex(v => v.personGuid === row.personGuid);
  showRegistrationButtons.value = true;
  selectedButtonIndex.value = 0;
}

async function confirmCheckIn(votingMethod: string) {
  if (!selectedVoter.value) return;

  try {
    await frontDeskStore.checkInVoter(electionGuid.value, {
      personGuid: selectedVoter.value.personGuid,
      votingMethod,
      tellerName: undefined,
      votingLocationGuid: undefined
    });

    showSuccessMessage(`${selectedVoter.value.fullName} checked in successfully`);
    showRegistrationButtons.value = false;

    // Move to next voter or reset
    const voters = notCheckedInVoters.value;
    if (selectedIndex.value >= voters.length) {
      selectedIndex.value = Math.max(0, voters.length - 1);
    }
    updateSelectedVoter();

    // Refocus search input
    nextTick(() => {
      searchInputRef.value?.focus();
    });
  } catch (error: any) {
    showErrorMessage(error.message || 'Failed to check in voter');
  }
}

async function handleUnregister(voter: FrontDeskVoterDto) {
  try {
    await ElMessageBox.confirm(
      `Are you sure you want to unregister ${voter.fullName}? This will clear their check-in status.`,
      'Confirm Unregister',
      {
        confirmButtonText: 'Unregister',
        cancelButtonText: 'Cancel',
        type: 'warning'
      }
    );

    await frontDeskStore.unregisterVoter(electionGuid.value, {
      personGuid: voter.personGuid,
      reason: 'Unregistered by front desk'
    });

    ElMessage.success('Voter unregistered successfully');
  } catch (error: any) {
    if (error !== 'cancel') {
      ElMessage.error(error.message || 'Failed to unregister voter');
    }
  }
}

function showHistory(voter: FrontDeskVoterDto) {
  historyVoter.value = voter;
  showHistoryDialog.value = true;
}

function formatTime(time?: string): string {
  if (!time) return '';
  const date = new Date(time);
  return date.toLocaleString();
}

function formatTimeShort(time?: string): string {
  if (!time) return '';
  const date = new Date(time);
  return date.toLocaleTimeString();
}

function getProgressColor(percentage: number): string {
  if (percentage < 30) return '#f56c6c';
  if (percentage < 70) return '#e6a23c';
  return '#67c23a';
}

function getRowClassName({ row, rowIndex }: { row: FrontDeskVoterDto, rowIndex: number }) {
  if (rowIndex === selectedIndex.value) {
    return 'selected-row';
  }
  return '';
}

function goBack() {
  router.push(`/elections/${electionGuid.value}`);
}
</script>

<style scoped>
.front-desk-page {
  padding: 20px;
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.section-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.section-header h3 {
  margin: 0;
}

.registration-buttons {
  padding: 20px;
  background: var(--el-color-primary-light-9);
  border-radius: 8px;
  margin-bottom: 20px;
}

.selected-voter-info {
  display: flex;
  align-items: center;
  gap: 20px;
  margin-bottom: 20px;
  font-size: 18px;
}

.voter-detail {
  color: var(--el-text-color-secondary);
  font-size: 14px;
}

.button-group {
  display: flex;
  gap: 10px;
  margin-bottom: 10px;
}

.button-group .el-button {
  flex: 1;
  position: relative;
}

.button-group kbd {
  position: absolute;
  top: 5px;
  right: 5px;
  font-size: 10px;
  padding: 2px 4px;
  background: rgba(0, 0, 0, 0.1);
  border-radius: 3px;
}

.selected-button {
  transform: scale(1.05);
  box-shadow: 0 0 10px var(--el-color-primary);
}

.instruction-text {
  text-align: center;
  color: var(--el-text-color-secondary);
  font-size: 12px;
  margin-top: 10px;
}

.keyboard-hint {
  margin-top: 10px;
  text-align: center;
  color: var(--el-text-color-secondary);
  font-size: 12px;
}

.stats-section {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.progress-section {
  margin-top: 16px;
}

.progress-label {
  display: flex;
  justify-content: space-between;
  margin-bottom: 8px;
  font-weight: bold;
}

.progress-percentage {
  color: var(--el-color-primary);
}

:deep(.selected-row) {
  background-color: var(--el-color-primary-light-9) !important;
  font-weight: bold;
}

.performed-by {
  color: var(--el-text-color-secondary);
  font-size: 12px;
  font-style: italic;
}
</style>
