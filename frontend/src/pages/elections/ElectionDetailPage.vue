<template>
  <div class="election-detail-page">
    <div v-if="loading" class="loading-container">
      <el-skeleton :rows="5" animated />
    </div>
    
    <div v-else-if="election">
      <el-page-header @back="goBack" :content="election.name">
        <template #extra>
          <el-button type="primary" @click="editElection">
            <el-icon><Edit /></el-icon>
            {{ $t('common.edit') }}
          </el-button>
        </template>
      </el-page-header>

      <el-row :gutter="20" style="margin-top: 20px;">
        <el-col :span="16">
          <el-card class="info-card">
            <template #header>
              <span>{{ $t('elections.details') }}</span>
            </template>
            <el-descriptions :column="2" border>
              <el-descriptions-item :label="$t('elections.form.name')">
                {{ election.name }}
              </el-descriptions-item>
              <el-descriptions-item :label="$t('elections.form.type')">
                {{ election.electionType || '-' }}
              </el-descriptions-item>
              <el-descriptions-item :label="$t('elections.form.date')">
                {{ formatDate(election.dateOfElection) }}
              </el-descriptions-item>
              <el-descriptions-item :label="$t('elections.status')">
                <el-tag :type="getStatusType(election.tallyStatus)">
                  {{ election.tallyStatus || 'Draft' }}
                </el-tag>
              </el-descriptions-item>
              <el-descriptions-item :label="$t('elections.form.numberToElect')">
                {{ election.numberToElect }}
              </el-descriptions-item>
              <el-descriptions-item :label="$t('elections.form.numberExtra')">
                {{ election.numberExtra }}
              </el-descriptions-item>
              <el-descriptions-item :label="$t('elections.form.convenor')">
                {{ election.convenor || '-' }}
              </el-descriptions-item>
              <el-descriptions-item :label="$t('elections.form.electionMode')">
                {{ election.electionMode || '-' }}
              </el-descriptions-item>
            </el-descriptions>
          </el-card>

          <el-card class="actions-card" style="margin-top: 20px;">
            <template #header>
              <span>{{ $t('elections.quickActions') }}</span>
            </template>
            <el-space wrap :size="15">
              <el-button @click="managePeople">
                <el-icon><UserFilled /></el-icon>
                {{ $t('elections.managePeople') }}
              </el-button>
              <el-button @click="manageBallots">
                <el-icon><Tickets /></el-icon>
                {{ $t('elections.manageBallots') }}
              </el-button>
              <el-button @click="viewResults">
                <el-icon><DataAnalysis /></el-icon>
                {{ $t('elections.viewResults') }}
              </el-button>
              <el-button @click="calculateTally" type="warning">
                <el-icon><Operation /></el-icon>
                {{ $t('elections.calculateTally') }}
              </el-button>
            </el-space>
          </el-card>
        </el-col>

        <el-col :span="8">
          <el-card class="stats-card">
            <template #header>
              <span>{{ $t('elections.statistics') }}</span>
            </template>
            <div class="stat-item">
              <div class="stat-label">{{ $t('dashboard.totalVoters') }}</div>
              <div class="stat-value">{{ election.voterCount }}</div>
            </div>
            <el-divider />
            <div class="stat-item">
              <div class="stat-label">{{ $t('dashboard.totalBallots') }}</div>
              <div class="stat-value">{{ election.ballotCount }}</div>
            </div>
            <el-divider />
            <div class="stat-item">
              <div class="stat-label">{{ $t('elections.locations') }}</div>
              <div class="stat-value">{{ election.locationCount }}</div>
            </div>
          </el-card>

          <el-card class="danger-zone" style="margin-top: 20px;">
            <template #header>
              <span style="color: #f56c6c;">{{ $t('common.dangerZone') }}</span>
            </template>
            <el-button type="danger" plain @click="confirmDelete" style="width: 100%;">
              <el-icon><Delete /></el-icon>
              {{ $t('common.delete') }}
            </el-button>
          </el-card>
        </el-col>
      </el-row>
    </div>

    <el-empty v-else :description="$t('elections.notFound')" />
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, onUnmounted } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import { useI18n } from 'vue-i18n';
import { ElMessage, ElMessageBox } from 'element-plus';
import { Edit, UserFilled, Tickets, DataAnalysis, Operation, Delete } from '@element-plus/icons-vue';
import { useElectionStore } from '../../stores/electionStore';

const router = useRouter();
const route = useRoute();
const { t } = useI18n();
const electionStore = useElectionStore();

const electionGuid = route.params.id as string;
const loading = computed(() => electionStore.loading);
const election = computed(() => electionStore.currentElection);

onMounted(async () => {
  try {
    await electionStore.initializeSignalR();
    await electionStore.fetchElectionById(electionGuid);
    await electionStore.joinElection(electionGuid);
  } catch (error) {
    ElMessage.error(t('elections.loadError'));
  }
});

onUnmounted(async () => {
  try {
    await electionStore.leaveElection(electionGuid);
  } catch (error) {
    console.error('Failed to leave election:', error);
  }
});

function goBack() {
  router.push('/elections');
}

function editElection() {
  router.push(`/elections/${electionGuid}/edit`);
}

function managePeople() {
  router.push(`/elections/${electionGuid}/people`);
}

function manageBallots() {
  router.push(`/elections/${electionGuid}/ballots`);
}

function viewResults() {
  router.push(`/elections/${electionGuid}/results`);
}

function calculateTally() {
  router.push(`/elections/${electionGuid}/tally`);
}

async function confirmDelete() {
  try {
    await ElMessageBox.confirm(
      t('elections.deleteConfirm'),
      t('common.warning'),
      {
        confirmButtonText: t('common.delete'),
        cancelButtonText: t('common.cancel'),
        type: 'warning',
        confirmButtonClass: 'el-button--danger'
      }
    );
    
    await electionStore.deleteElection(electionGuid);
    ElMessage.success(t('elections.deleteSuccess'));
    router.push('/elections');
  } catch (error: any) {
    if (error !== 'cancel') {
      ElMessage.error(error.message || t('elections.deleteError'));
    }
  }
}

function formatDate(date?: string) {
  if (!date) return '-';
  return new Date(date).toLocaleDateString();
}

function getStatusType(status?: string) {
  const typeMap: Record<string, any> = {
    'Draft': '',
    'Voting': 'success',
    'Tallying': 'warning',
    'Finalized': 'info'
  };
  return typeMap[status || ''] || '';
}
</script>

<style scoped>
.election-detail-page {
  max-width: 1400px;
  margin: 0 auto;
}

.loading-container {
  padding: 40px;
}

.info-card,
.actions-card,
.stats-card,
.danger-zone {
  box-shadow: 0 2px 12px 0 rgba(0, 0, 0, 0.1);
}

.stat-item {
  text-align: center;
  padding: 10px 0;
}

.stat-label {
  font-size: 14px;
  color: #909399;
  margin-bottom: 8px;
}

.stat-value {
  font-size: 28px;
  font-weight: 600;
  color: #303133;
}

.el-divider {
  margin: 12px 0;
}
</style>
