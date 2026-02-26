<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted, watch, nextTick } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useFrontDeskStore } from '@/stores/frontDeskStore';
import { useLocationStore } from '@/stores/locationStore';
import { ElMessageBox } from 'element-plus';
import { useNotifications } from '@/composables/useNotifications';
import { Search, UserFilled, Clock, Warning } from '@element-plus/icons-vue';
import type { FrontDeskVoterDto } from '@/types/FrontDesk';

const route = useRoute();
const router = useRouter();
const frontDeskStore = useFrontDeskStore();
const locationStore = useLocationStore();
const { showSuccessMessage, showErrorMessage } = useNotifications();

const electionGuid = ref(route.params.id as string);
const searchQuery = computed({
  get: () => frontDeskStore.searchQuery,
  set: (value) => frontDeskStore.setSearchQuery(value)
});
const loading = computed(() => frontDeskStore.loading);
const stats = computed(() => frontDeskStore.stats);
const locations = computed(() => locationStore.locations);

// Keyboard navigation
const searchInputRef = ref();
const voterTableRef = ref();
const selectedIndex = ref(0);
const selectedVoter = ref<FrontDeskVoterDto | null>(null);
const showRegistrationButtons = ref(false);
const selectedButtonIndex = ref(0);

// History dialog
const showHistoryDialog = ref(false);
const historyVoter = ref<FrontDeskVoterDto | null>(null);

const notCheckedInVoters = computed(() => frontDeskStore.notCheckedInVoters);
const checkedInVoters = computed(() => frontDeskStore.checkedInVoters);

// Merge not-checked-in and checked-in voters into one list
const allVoters = computed(() => {
  // Sort by registration status (not checked in first) then by name
  return [...notCheckedInVoters.value, ...checkedInVoters.value].sort((a, b) => {
    if (a.isCheckedIn === b.isCheckedIn) {
      return a.fullName.localeCompare(b.fullName);
    }
    return a.isCheckedIn ? 1 : -1;
  });
});

// Registration type options
const registrationTypes = [
  { value: 'I', label: 'In Person', key: '1' },
  { value: 'M', label: 'Mail', key: '2' },
  { value: 'O', label: 'Online', key: '3' },
  { value: 'C', label: 'Call-In', key: '4' }
];

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
watch(allVoters, () => {
  updateSelectedVoter();
});

async function loadData() {
  try {
    await frontDeskStore.fetchEligibleVoters(electionGuid.value);
    await locationStore.fetchLocations(electionGuid.value);
    // Select first voter after loading
    nextTick(() => {
      updateSelectedVoter();
    });
  } catch (error: any) {
    showErrorMessage(error.message || 'Failed to load data');
  }
}

function updateSelectedVoter() {
  const voters = allVoters.value;
  if (voters.length > 0 && selectedIndex.value >= 0 && selectedIndex.value < voters.length) {
    selectedVoter.value = voters[selectedIndex.value]!;
  } else {
    selectedVoter.value = null;
  }
  scrollToSelectedRow();
}

function scrollToSelectedRow() {
  nextTick(() => {
    if (voterTableRef.value && selectedIndex.value >= 0) {
      const tableWrapper = voterTableRef.value.$el.querySelector('.el-table__body-wrapper');
      if (tableWrapper) {
        const rows = tableWrapper.querySelectorAll('.el-table__row');
        const selectedRow = rows[selectedIndex.value];
        if (selectedRow) {
          selectedRow.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
        }
      }
    }
  });
}



function handleSearchKeydown(event: KeyboardEvent) {
  console.log('handleSearchKeydown called with key:', event.key);
  console.log('Key pressed:', event.key, 'showRegistrationButtons:', showRegistrationButtons.value);
  const voters = allVoters.value;
  console.log('Voters count:', voters.length);

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
        const regType = registrationTypes[index];
        if (!regType) {
          showErrorMessage(`Invalid registration type selected: ${index}`);
          return;
        }
        confirmCheckIn(regType.value);
      }
    }
  } else {
    // Handle list navigation
    if (event.key === 'ArrowDown') {
      event.preventDefault();
      console.log('ArrowDown pressed, current index:', selectedIndex.value, 'voters length:', voters.length);
      selectedIndex.value = Math.min(voters.length - 1, selectedIndex.value + 1);
      console.log('New index:', selectedIndex.value);
      updateSelectedVoter();
    } else if (event.key === 'ArrowUp') {
      event.preventDefault();
      console.log('ArrowUp pressed, current index:', selectedIndex.value);
      selectedIndex.value = Math.max(0, selectedIndex.value - 1);
      console.log('New index:', selectedIndex.value);
      updateSelectedVoter();
    } else if (event.key === 'Enter') {
      event.preventDefault();
      console.log('Enter pressed, selectedVoter:', selectedVoter.value);
      if (selectedVoter.value) {
        showRegistrationButtons.value = true;
        selectedButtonIndex.value = 0;
      }
    }
  }
}

function handleRowClick(row: FrontDeskVoterDto) {
  selectedVoter.value = row;
  selectedIndex.value = allVoters.value.findIndex(v => v.personGuid === row.personGuid);
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
    const voters = allVoters.value;
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

    showSuccessMessage('Voter unregistered successfully');
  } catch (error: any) {
    if (error !== 'cancel') {
      showErrorMessage(error.message || 'Failed to unregister voter');
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

function formatTimeline(entry: any): string {
  const items = [];
  if (entry.action === 'CheckedIn') {
    items.push(`Checked in - ${entry.votingMethod === 'I' ? 'In Person' : entry.votingMethod === 'M' ? 'Mail' : entry.votingMethod === 'O' ? 'Online' : entry.votingMethod === 'C' ? 'Call-In' : entry.votingMethod}`);
  }
  //               <span v-if="entry.votingMethod">Method: {{ entry.votingMethod }}</span>
  // <span v -if= "entry.tellerName" > Teller: { { entry.tellerName } } </span>
  //   < span v -if= "entry.locationName" > Location: { { entry.locationName } } </>
  //     < span v -if= "entry.envNum" > Envelope: #{ { entry.envNum } } </>
  //       < span v -if= "entry.performedBy" class="performed-by" > By: { { entry.performedBy } } </>
  if (entry.tellerName) {
    items.push(`Teller: ${entry.tellerName}`);
  }
  if (entry.locationName) {
    items.push(`Location: ${entry.locationName}`);
  }
  if (entry.envNum) {
    items.push(`Envelope: #${entry.envNum}`);
  }
  if (entry.performedBy) {
    items.push(entry.performedBy);
  }
  return items.join(', ');
}

function goBack() {
  router.push(`/elections/${electionGuid.value}`);
}
</script>
<template>
  <div class="front-desk-page">
    <el-card>
      <template #header>
        <div class="card-header">
          <el-page-header @back="goBack" content="Front Desk - Voter Check-In" />
          <div v-if="stats" class="header-stats">
            <el-statistic :value="stats.checkedIn" title="Checked In">
              <template #prefix>
                <el-icon><UserFilled /></el-icon>
              </template>
            </el-statistic>
          </div>
        </div>
      </template>

      <el-row :gutter="20">
        <el-col :span="24">
          <el-card shadow="never">
            <template #header>
              <div class="section-header">
                <h3>Quick Check-In</h3>
                <el-input ref="searchInputRef" v-model="searchQuery"
                  placeholder="Type name to search (↑↓ arrows, Enter to select)" style="width: 450px;" clearable
                  @keydown="handleSearchKeydown">
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

            <!-- All voters list (merged not-checked-in and checked-in) -->
            <div v-else>
              <el-table ref="voterTableRef" :data="allVoters" :loading="loading" style="width: 100%"
                max-height="600px" :row-class-name="getRowClassName" @row-click="handleRowClick">
                <el-table-column prop="fullName" label="Name" sortable width="200" />
                <el-table-column prop="bahaiId" label="Bahá'í ID" width="120" />
                <el-table-column prop="area" label="Area" width="120" />
                <el-table-column prop="envNum" label="Env #" width="80" />
                <el-table-column label="Method" width="110">
                  <template #default="{ row }">
                    <el-tag v-if="row.votingMethod === 'I'" type="success" size="small">In Person</el-tag>
                    <el-tag v-else-if="row.votingMethod === 'M'" type="info" size="small">Mail</el-tag>
                    <el-tag v-else-if="row.votingMethod === 'O'" type="primary" size="small">Online</el-tag>
                    <el-tag v-else-if="row.votingMethod === 'C'" type="warning" size="small">Call-In</el-tag>
                    <span v-else>-</span>
                  </template>
                </el-table-column>
                <el-table-column label="Time" width="100">
                  <template #default="{ row }">
                    <span v-if="row.registrationTime">{{ formatTimeShort(row.registrationTime) }}</span>
                    <span v-else>-</span>
                  </template>
                </el-table-column>
                <el-table-column label="Actions" width="120" fixed="right">
                  <template #default="{ row }">
                    <template v-if="row.isCheckedIn">
                      <el-button type="default" size="small" :icon="Clock" @click.stop="showHistory(row)" circle
                        title="View History" />
                      <el-button type="warning" size="small" :icon="Warning" @click.stop="handleUnregister(row)" circle
                        title="Unregister" />
                    </template>
                  </template>
                </el-table-column>
              </el-table>
              <div class="keyboard-hint">
                {{ notCheckedInVoters.length }} not checked in, {{ checkedInVoters.length }} checked in. Use keyboard to navigate and check in.
              </div>
            </div>
          </el-card>
        </el-col>
      </el-row>
    </el-card>

    <!-- History dialog -->
    <el-dialog v-model="showHistoryDialog" title="Registration History" width="760px" class="history-detail">
      <div v-if="historyVoter">
        <h4>{{ historyVoter.fullName }} <span v-if="historyVoter.bahaiId">Bahá'í ID: {{ historyVoter.bahaiId }}</span>
        </h4>
        <el-divider />

        <div v-if="historyVoter.registrationHistory && historyVoter.registrationHistory.length > 0">
          <el-timeline>
            <el-timeline-item v-for="(entry, index) in historyVoter.registrationHistory" :key="index"
              :timestamp="formatTimeline(entry)">
              {{ formatTime(entry.timestamp) }}
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

<style lang="less">
.front-desk-page {
  padding: 20px;

  .card-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
  }

  .header-stats {
    display: flex;
    align-items: center;
    gap: 20px;
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
    background: var(--el-color-primary-light-1);
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

  .selected-row {
    background-color: var(--el-color-primary-light-9) !important;
    font-weight: bold;
  }

  .performed-by {
    color: var(--el-text-color-secondary);
    font-size: 12px;
    font-style: italic;
  }

  .history-detail {
    h4 {
      display: flex;
      align-items: center;
      gap: 20px;
      justify-content: space-between;
    }
  }
}
</style>
