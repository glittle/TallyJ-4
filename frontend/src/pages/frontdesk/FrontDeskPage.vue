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
                <h3>Eligible Voters</h3>
                <el-input
                  v-model="searchQuery"
                  placeholder="Search by name, Bahai ID, or area"
                  style="width: 300px;"
                  clearable
                  @input="handleSearch"
                >
                  <template #prefix>
                    <el-icon><Search /></el-icon>
                  </template>
                </el-input>
              </div>
            </template>

            <el-tabs v-model="activeTab">
              <el-tab-pane label="Not Checked In" name="not-checked-in">
                <el-table
                  :data="notCheckedInVoters"
                  :loading="loading"
                  style="width: 100%"
                  max-height="500px"
                  @row-click="handleRowClick"
                >
                  <el-table-column prop="fullName" label="Name" sortable />
                  <el-table-column prop="bahaiId" label="Bahai ID" width="120" />
                  <el-table-column prop="area" label="Area" width="150" />
                  <el-table-column label="Actions" width="120" fixed="right">
                    <template #default="{ row }">
                      <el-button
                        type="primary"
                        size="small"
                        @click.stop="handleCheckIn(row)"
                      >
                        Check In
                      </el-button>
                    </template>
                  </el-table-column>
                </el-table>
              </el-tab-pane>

              <el-tab-pane label="Checked In" name="checked-in">
                <el-table
                  :data="checkedInVoters"
                  :loading="loading"
                  style="width: 100%"
                  max-height="500px"
                >
                  <el-table-column prop="fullName" label="Name" sortable />
                  <el-table-column prop="bahaiId" label="Bahai ID" width="120" />
                  <el-table-column prop="envNum" label="Envelope #" width="100" />
                  <el-table-column prop="votingMethod" label="Method" width="100" />
                  <el-table-column label="Time" width="150">
                    <template #default="{ row }">
                      {{ formatTime(row.registrationTime) }}
                    </template>
                  </el-table-column>
                </el-table>
              </el-tab-pane>
            </el-tabs>
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
                  <el-icon><UserFilled /></el-icon>
                </template>
              </el-statistic>

              <el-divider />

              <el-statistic :value="stats.notYetCheckedIn" title="Not Yet Checked In">
                <template #prefix>
                  <el-icon><User /></el-icon>
                </template>
              </el-statistic>

              <el-divider />

              <el-statistic :value="stats.totalEligible" title="Total Eligible">
                <template #prefix>
                  <el-icon><Tickets /></el-icon>
                </template>
              </el-statistic>

              <el-divider />

              <div class="progress-section">
                <div class="progress-label">
                  <span>Progress</span>
                  <span class="progress-percentage">{{ stats.checkInPercentage.toFixed(1) }}%</span>
                </div>
                <el-progress
                  :percentage="stats.checkInPercentage"
                  :color="getProgressColor(stats.checkInPercentage)"
                  :stroke-width="20"
                />
              </div>
            </div>
          </el-card>

          <el-card shadow="never" style="margin-top: 20px;">
            <template #header>
              <h3>Recent Check-Ins</h3>
            </template>

            <el-timeline>
              <el-timeline-item
                v-for="voter in recentCheckIns"
                :key="voter.personGuid"
                :timestamp="formatTime(voter.registrationTime)"
                placement="top"
              >
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

    <el-dialog
      v-model="showCheckInDialog"
      title="Check In Voter"
      width="500px"
    >
      <el-form
        ref="checkInFormRef"
        :model="checkInForm"
        :rules="checkInRules"
        label-width="140px"
      >
        <el-form-item label="Voter Name">
          <el-input :model-value="selectedVoter?.fullName" disabled />
        </el-form-item>

        <el-form-item label="Voting Method" prop="votingMethod">
          <el-select v-model="checkInForm.votingMethod" placeholder="Select method">
            <el-option label="In Person" value="I" />
            <el-option label="Mail" value="M" />
            <el-option label="Online" value="O" />
            <el-option label="Call-In" value="C" />
          </el-select>
        </el-form-item>

        <el-form-item label="Teller Name">
          <el-input v-model="checkInForm.tellerName" placeholder="Optional" />
        </el-form-item>

        <el-form-item label="Location">
          <el-select v-model="checkInForm.votingLocationGuid" placeholder="Optional">
            <el-option
              v-for="location in locations"
              :key="location.locationGuid"
              :label="location.name"
              :value="location.locationGuid"
            />
          </el-select>
        </el-form-item>
      </el-form>

      <template #footer>
        <el-button @click="showCheckInDialog = false">Cancel</el-button>
        <el-button type="primary" @click="confirmCheckIn" :loading="loading">
          Check In
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useFrontDeskStore } from '@/stores/frontDeskStore';
import { useLocationStore } from '@/stores/locationStore';
import { ElMessageBox, type FormInstance, type FormRules } from 'element-plus';
import { useNotifications } from '@/composables/useNotifications';
import { Search, UserFilled, User, Tickets } from '@element-plus/icons-vue';
import type { FrontDeskVoterDto } from '@/types/FrontDesk';

const route = useRoute();
const router = useRouter();
const frontDeskStore = useFrontDeskStore();
const locationStore = useLocationStore();
const { showSuccessMessage, showErrorMessage } = useNotifications();

const electionGuid = ref(route.params.electionId as string);
const searchQuery = ref('');
const activeTab = ref('not-checked-in');
const loading = computed(() => frontDeskStore.loading);
const stats = computed(() => frontDeskStore.stats);

const filteredVoters = computed(() => frontDeskStore.filteredVoters);
const notCheckedInVoters = computed(() => frontDeskStore.notCheckedInVoters);
const checkedInVoters = computed(() => frontDeskStore.checkedInVoters);
const locations = computed(() => locationStore.locations);

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

const showCheckInDialog = ref(false);
const selectedVoter = ref<FrontDeskVoterDto | null>(null);
const checkInFormRef = ref<FormInstance>();
const checkInForm = ref({
  votingMethod: 'I',
  tellerName: '',
  votingLocationGuid: ''
});

const checkInRules: FormRules = {
  votingMethod: [
    { required: true, message: 'Please select a voting method', trigger: 'change' }
  ]
};

onMounted(async () => {
  await loadData();
  await frontDeskStore.initializeSignalR();
  await frontDeskStore.joinElection(electionGuid.value);
});

onUnmounted(async () => {
  await frontDeskStore.leaveElection(electionGuid.value);
});

async function loadData() {
  try {
    await frontDeskStore.fetchEligibleVoters(electionGuid.value);
    await locationStore.fetchLocations(electionGuid.value);
  } catch (error: any) {
    showErrorMessage(error.message || 'Failed to load data');
  }
}

function handleSearch() {
  frontDeskStore.setSearchQuery(searchQuery.value);
}

function handleRowClick(row: FrontDeskVoterDto) {
  if (!row.isCheckedIn) {
    handleCheckIn(row);
  }
}

function handleCheckIn(voter: FrontDeskVoterDto) {
  selectedVoter.value = voter;
  checkInForm.value = {
    votingMethod: 'I',
    tellerName: '',
    votingLocationGuid: ''
  };
  showCheckInDialog.value = true;
}

async function confirmCheckIn() {
  if (!checkInFormRef.value || !selectedVoter.value) return;

  await checkInFormRef.value.validate(async (valid) => {
    if (!valid) return;

    try {
      await frontDeskStore.checkInVoter(electionGuid.value, {
        personGuid: selectedVoter.value!.personGuid,
        votingMethod: checkInForm.value.votingMethod,
        tellerName: checkInForm.value.tellerName || undefined,
        votingLocationGuid: checkInForm.value.votingLocationGuid || undefined
      });

      showSuccessMessage('Voter checked in successfully');
      showCheckInDialog.value = false;
      selectedVoter.value = null;
    } catch (error: any) {
      showErrorMessage(error.message || 'Failed to check in voter');
    }
  });
}

function formatTime(time?: string): string {
  if (!time) return '';
  return new Date(time).toLocaleTimeString();
}

function getProgressColor(percentage: number): string {
  if (percentage < 30) return '#f56c6c';
  if (percentage < 70) return '#e6a23c';
  return '#67c23a';
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
</style>
