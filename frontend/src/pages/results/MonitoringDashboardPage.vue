<template>
  <div class="monitoring-dashboard">
    <el-card>
      <template #header>
        <div class="card-header">
          <el-page-header @back="goBack" :content="$t('monitoring.title')" />
          <div class="header-actions">
            <el-button
              type="primary"
              :loading="loading"
              @click="refreshData"
              icon="Refresh"
            >
              {{ $t('common.refresh') }}
            </el-button>
          </div>
        </div>
      </template>

      <div v-if="loading && !monitorInfo" class="loading-container">
        <el-skeleton :rows="8" animated />
      </div>

      <div v-else-if="monitorInfo">
        <!-- Summary Cards -->
        <el-row :gutter="20" class="summary-row">
          <el-col :span="6">
            <el-card class="summary-card">
              <div class="summary-content">
                <div class="summary-icon">
                  <el-icon size="32" color="#409EFF"><DocumentChecked /></el-icon>
                </div>
                <div class="summary-text">
                  <div class="summary-value">{{ monitorInfo.totalBallots }}</div>
                  <div class="summary-label">{{ $t('monitoring.totalBallots') }}</div>
                </div>
              </div>
            </el-card>
          </el-col>
          <el-col :span="6">
            <el-card class="summary-card">
              <div class="summary-content">
                <div class="summary-icon">
                  <el-icon size="32" color="#67C23A"><Check /></el-icon>
                </div>
                <div class="summary-text">
                  <div class="summary-value">{{ monitorInfo.totalVotes }}</div>
                  <div class="summary-label">{{ $t('monitoring.totalVotes') }}</div>
                </div>
              </div>
            </el-card>
          </el-col>
          <el-col :span="6">
            <el-card class="summary-card">
              <div class="summary-content">
                <div class="summary-icon">
                  <el-icon size="32" color="#E6A23C"><Monitor /></el-icon>
                </div>
                <div class="summary-text">
                  <div class="summary-value">{{ monitorInfo.computers.length }}</div>
                  <div class="summary-label">{{ $t('monitoring.activeComputers') }}</div>
                </div>
              </div>
            </el-card>
          </el-col>
          <el-col :span="6">
            <el-card class="summary-card">
              <div class="summary-content">
                <div class="summary-icon">
                  <el-icon size="32" color="#F56C6C"><Location /></el-icon>
                </div>
                <div class="summary-text">
                  <div class="summary-value">{{ monitorInfo.locations.length }}</div>
                  <div class="summary-label">{{ $t('monitoring.locations') }}</div>
                </div>
              </div>
            </el-card>
          </el-col>
        </el-row>

        <!-- Last Updated -->
        <el-alert
          :title="$t('monitoring.lastUpdated', { time: formatDateTime(monitorInfo.lastUpdated) })"
          type="info"
          :closable="false"
          style="margin: 20px 0;"
        />

        <!-- Computers Table -->
        <el-card style="margin-bottom: 20px;">
          <template #header>
            <span>{{ $t('monitoring.computers') }}</span>
          </template>
          <el-table :data="monitorInfo.computers" stripe style="width: 100%">
            <el-table-column prop="computerCode" :label="$t('monitoring.computerCode')" width="150" />
            <el-table-column prop="locationName" :label="$t('monitoring.location')" width="200" />
            <el-table-column prop="ballotCount" :label="$t('monitoring.ballotsEntered')" width="150" align="center" />
            <el-table-column prop="lastContact" :label="$t('monitoring.lastContact')" width="180">
              <template #default="scope">
                {{ formatDateTime(scope.row.lastContact) }}
              </template>
            </el-table-column>
            <el-table-column prop="status" :label="$t('monitoring.status')" width="120">
              <template #default="scope">
                <el-tag :type="getStatusType(scope.row.status)">
                  {{ scope.row.status }}
                </el-tag>
              </template>
            </el-table-column>
          </el-table>
        </el-card>

        <!-- Locations Table -->
        <el-card style="margin-bottom: 20px;">
          <template #header>
            <span>{{ $t('monitoring.locations') }}</span>
          </template>
          <el-table :data="monitorInfo.locations" stripe style="width: 100%">
            <el-table-column prop="locationName" :label="$t('monitoring.location')" width="200" />
            <el-table-column prop="voterCount" :label="$t('monitoring.registeredVoters')" width="150" align="center" />
            <el-table-column prop="ballotCount" :label="$t('monitoring.ballotsEntered')" width="150" align="center" />
            <el-table-column prop="voteCount" :label="$t('monitoring.totalVotes')" width="120" align="center" />
            <el-table-column :label="$t('monitoring.turnout')" width="120" align="center">
              <template #default="scope">
                {{ calculateTurnout(scope.row.voterCount, scope.row.ballotCount) }}%
              </template>
            </el-table-column>
            <el-table-column prop="status" :label="$t('monitoring.status')" width="120">
              <template #default="scope">
                <el-tag :type="getStatusType(scope.row.status)">
                  {{ scope.row.status }}
                </el-tag>
              </template>
            </el-table-column>
          </el-table>
        </el-card>

        <!-- Online Voting Info -->
        <el-card>
          <template #header>
            <span>{{ $t('monitoring.onlineVoting') }}</span>
          </template>
          <el-descriptions :column="4" border>
            <el-descriptions-item :label="$t('monitoring.totalOnlineVoters')">
              {{ monitorInfo.onlineVotingInfo.totalOnlineVoters }}
            </el-descriptions-item>
            <el-descriptions-item :label="$t('monitoring.votedOnline')">
              {{ monitorInfo.onlineVotingInfo.votedOnline }}
            </el-descriptions-item>
            <el-descriptions-item :label="$t('monitoring.onlineBallotsEntered')">
              {{ monitorInfo.onlineVotingInfo.onlineBallotsEntered }}
            </el-descriptions-item>
            <el-descriptions-item :label="$t('monitoring.status')">
              <el-tag :type="getStatusType(monitorInfo.onlineVotingInfo.status)">
                {{ monitorInfo.onlineVotingInfo.status }}
              </el-tag>
            </el-descriptions-item>
          </el-descriptions>
        </el-card>
      </div>

      <el-empty v-else :description="$t('monitoring.noData')" />
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import { useI18n } from 'vue-i18n';
import { ElMessage } from 'element-plus';
import { DocumentChecked, Check, Monitor, Location } from '@element-plus/icons-vue';
import { useResultStore } from '../../stores/resultStore';
import type { MonitorInfoDto } from '../../types';

const router = useRouter();
const route = useRoute();
const { t } = useI18n();
const resultStore = useResultStore();

const electionGuid = route.params.id as string;
const monitorInfo = ref<MonitorInfoDto | null>(null);
const loading = ref(false);
const refreshInterval = ref<NodeJS.Timeout | null>(null);

const loadingComputed = computed(() => resultStore.loading || loading.value);

onMounted(async () => {
  await loadData();
  startAutoRefresh();
});

onUnmounted(() => {
  stopAutoRefresh();
});

async function loadData() {
  try {
    loading.value = true;
    const data = await resultStore.fetchMonitorInfo(electionGuid);
    monitorInfo.value = data;
  } catch (error) {
    ElMessage.error(t('monitoring.loadError'));
  } finally {
    loading.value = false;
  }
}

async function refreshData() {
  await loadData();
}

function startAutoRefresh() {
  // Refresh every 30 seconds
  refreshInterval.value = setInterval(() => {
    loadData();
  }, 30000);
}

function stopAutoRefresh() {
  if (refreshInterval.value) {
    clearInterval(refreshInterval.value);
    refreshInterval.value = null;
  }
}

function goBack() {
  router.push(`/elections/${electionGuid}`);
}

function formatDateTime(date: string) {
  if (!date) return '-';
  return new Date(date).toLocaleString();
}

function getStatusType(status: string) {
  const statusMap: Record<string, string> = {
    'Online': 'success',
    'Offline': 'danger',
    'Active': 'success',
    'Inactive': 'warning',
    'Closed': 'info'
  };
  return statusMap[status] || 'info';
}

function calculateTurnout(registered: number, ballots: number) {
  if (registered === 0) return 0;
  return Math.round((ballots / registered) * 100);
}
</script>

<style scoped>
.monitoring-dashboard {
  max-width: 1400px;
  margin: 0 auto;
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

.summary-row {
  margin-bottom: 20px;
}

.summary-card {
  height: 100px;
}

.summary-content {
  display: flex;
  align-items: center;
  gap: 15px;
  height: 100%;
}

.summary-icon {
  flex-shrink: 0;
}

.summary-text {
  flex: 1;
}

.summary-value {
  font-size: 24px;
  font-weight: bold;
  color: #303133;
  line-height: 1.2;
}

.summary-label {
  font-size: 14px;
  color: #909399;
  margin-top: 4px;
}

.loading-container {
  padding: 40px;
}
</style>