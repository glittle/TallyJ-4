<template>
  <div class="dashboard-page">
    <el-card class="welcome-card">
      <h1>{{ $t('dashboard.welcome') }}</h1>
      <p>{{ $t('dashboard.subtitle') }}</p>
    </el-card>

    <el-row :gutter="20" class="stats-row">
      <el-col :xs="24" :sm="12" :md="6">
        <el-card class="stat-card">
          <div class="stat-icon elections">
            <el-icon><Document /></el-icon>
          </div>
          <div class="stat-content">
            <div class="stat-value">{{ statistics.totalElections }}</div>
            <div class="stat-label">{{ $t('dashboard.totalElections') }}</div>
          </div>
        </el-card>
      </el-col>
      <el-col :xs="24" :sm="12" :md="6">
        <el-card class="stat-card">
          <div class="stat-icon active">
            <el-icon><CircleCheck /></el-icon>
          </div>
          <div class="stat-content">
            <div class="stat-value">{{ statistics.activeElections }}</div>
            <div class="stat-label">{{ $t('dashboard.activeElections') }}</div>
          </div>
        </el-card>
      </el-col>
      <el-col :xs="24" :sm="12" :md="6">
        <el-card class="stat-card">
          <div class="stat-icon voters">
            <el-icon><UserFilled /></el-icon>
          </div>
          <div class="stat-content">
            <div class="stat-value">{{ statistics.totalVoters }}</div>
            <div class="stat-label">{{ $t('dashboard.totalVoters') }}</div>
          </div>
        </el-card>
      </el-col>
      <el-col :xs="24" :sm="12" :md="6">
        <el-card class="stat-card">
          <div class="stat-icon ballots">
            <el-icon><Tickets /></el-icon>
          </div>
          <div class="stat-content">
            <div class="stat-value">{{ statistics.totalBallots }}</div>
            <div class="stat-label">{{ $t('dashboard.totalBallots') }}</div>
          </div>
        </el-card>
      </el-col>
    </el-row>

    <el-card class="recent-elections-card">
      <template #header>
        <div class="card-header">
          <span>{{ $t('dashboard.recentElections') }}</span>
          <el-button type="primary" @click="createElection">
            <el-icon><Plus /></el-icon>
            {{ $t('elections.createNew') }}
          </el-button>
        </div>
      </template>
      <div v-if="loading" class="loading-container">
        <el-skeleton :rows="3" animated />
      </div>
      <div v-else-if="elections.length === 0" class="empty-state">
        <el-empty :description="$t('dashboard.noElections')">
          <el-button type="primary" @click="createElection">
            {{ $t('elections.createFirst') }}
          </el-button>
        </el-empty>
      </div>
      <el-table v-else :data="elections" style="width: 100%">
        <el-table-column prop="name" :label="$t('elections.name')" min-width="200" />
        <el-table-column prop="electionType" :label="$t('elections.type')" width="120" />
        <el-table-column prop="dateOfElection" :label="$t('elections.date')" width="140">
          <template #default="scope">
            {{ formatDate(scope.row.dateOfElection) }}
          </template>
        </el-table-column>
        <el-table-column prop="tallyStatus" :label="$t('elections.status')" width="120">
          <template #default="scope">
            <el-tag :type="getStatusType(scope.row.tallyStatus)">
              {{ scope.row.tallyStatus || 'Draft' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column :label="$t('common.actions')" width="150" fixed="right">
          <template #default="scope">
            <el-button size="small" @click="viewElection(scope.row.electionGuid)">
              {{ $t('common.view') }}
            </el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { Document, CircleCheck, UserFilled, Tickets, Plus } from '@element-plus/icons-vue';
import { useElectionStore } from '../stores/electionStore';

const router = useRouter();
const electionStore = useElectionStore();

const elections = computed(() => electionStore.elections.slice(0, 5));
const loading = computed(() => electionStore.loading);

const statistics = computed(() => ({
  totalElections: electionStore.elections.length,
  activeElections: electionStore.activeElections.length,
  totalVoters: electionStore.elections.reduce((sum, e) => sum + e.voterCount, 0),
  totalBallots: electionStore.elections.reduce((sum, e) => sum + e.ballotCount, 0)
}));

onMounted(async () => {
  await loadDashboardData();
});

async function loadDashboardData() {
  try {
    await electionStore.fetchElections();
  } catch (error) {
    console.error('Failed to load dashboard data:', error);
  }
}

function createElection() {
  router.push('/elections/create');
}

function viewElection(guid: string) {
  router.push(`/elections/${guid}`);
}

function formatDate(date: string) {
  if (!date) return '-';
  return new Date(date).toLocaleDateString();
}

function getStatusType(status: string) {
  const typeMap: Record<string, any> = {
    'Draft': '',
    'Voting': 'success',
    'Tallying': 'warning',
    'Finalized': 'info'
  };
  return typeMap[status] || '';
}
</script>

<style scoped>
.dashboard-page {
  max-width: 1400px;
  margin: 0 auto;
}

.welcome-card {
  margin-bottom: 20px;
  text-align: center;
}

.welcome-card h1 {
  margin: 0 0 10px 0;
  color: #303133;
}

.welcome-card p {
  margin: 0;
  color: #606266;
}

.stats-row {
  margin-bottom: 20px;
}

.stat-card {
  display: flex;
  align-items: center;
  padding: 20px;
  margin-bottom: 20px;
}

:deep(.stat-card .el-card__body) {
  display: flex;
  align-items: center;
  width: 100%;
  padding: 0;
}

.stat-icon {
  width: 60px;
  height: 60px;
  border-radius: 8px;
  display: flex;
  align-items: center;
  justify-content: center;
  margin-right: 15px;
}

.stat-icon.elections {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
}

.stat-icon.active {
  background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%);
}

.stat-icon.voters {
  background: linear-gradient(135deg, #4facfe 0%, #00f2fe 100%);
}

.stat-icon.ballots {
  background: linear-gradient(135deg, #43e97b 0%, #38f9d7 100%);
}

.stat-icon :deep(.el-icon) {
  font-size: 28px;
  color: white;
}

.stat-content {
  flex: 1;
}

.stat-value {
  font-size: 28px;
  font-weight: 600;
  color: #303133;
  line-height: 1;
}

.stat-label {
  font-size: 14px;
  color: #909399;
  margin-top: 5px;
}

.recent-elections-card {
  margin-bottom: 20px;
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.loading-container {
  padding: 20px;
}

.empty-state {
  padding: 40px 20px;
}
</style>
