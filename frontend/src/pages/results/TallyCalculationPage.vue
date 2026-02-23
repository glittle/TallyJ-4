<template>
  <div class="tally-calculation-page">
    <el-card>
      <template #header>
        <div class="card-header">
          <el-page-header @back="goBack" :content="$t('tally.title')" />
        </div>
      </template>

      <el-alert
        :title="$t('tally.warning')"
        type="warning"
        :closable="false"
        style="margin-bottom: 20px;"
      >
        {{ $t('tally.warningMessage') }}
      </el-alert>

      <el-form label-width="150px" label-position="left">
        <el-form-item :label="$t('elections.form.type')">
          <el-radio-group v-model="electionType">
            <el-radio value="normal">{{ $t('tally.normalElection') }}</el-radio>
            <el-radio value="singlename">{{ $t('tally.singleNameElection') }}</el-radio>
          </el-radio-group>
        </el-form-item>

        <el-form-item>
          <el-button
            type="primary"
            size="large"
            :loading="calculating"
            @click="handleCalculate"
          >
            <el-icon><Operation /></el-icon>
            {{ $t('tally.calculate') }}
          </el-button>
        </el-form-item>
      </el-form>

      <div v-if="calculating && tallyProgress" class="progress-section" style="margin: 20px 0;">
        <h4>{{ $t('tally.calculatingProgress') }}</h4>
        <el-progress
          :percentage="tallyProgress.percentComplete"
          :status="tallyProgress.isComplete ? 'success' : undefined"
          :stroke-width="12"
          style="margin: 10px 0;"
        />
        <div class="progress-details">
          <p>{{ tallyProgress.message }}</p>
          <p class="progress-stats">
            {{ $t('tally.processedBallots') }}: {{ tallyProgress.processedBallots }} / {{ tallyProgress.totalBallots }}
          </p>
          <p class="progress-stats">
            {{ $t('tally.totalVotes') }}: {{ tallyProgress.totalVotes }}
          </p>
        </div>
      </div>

      <el-divider />

      <div v-if="results" class="results-preview">
        <h3>{{ $t('tally.resultsPreview') }}</h3>
        
        <el-descriptions :column="2" border style="margin-bottom: 20px;">
          <el-descriptions-item :label="$t('results.totalBallots')">
            {{ results.statistics.totalBallots }}
          </el-descriptions-item>
          <el-descriptions-item :label="$t('results.ballotsReceived')">
            {{ results.statistics.ballotsReceived }}
          </el-descriptions-item>
          <el-descriptions-item :label="$t('results.totalVotes')">
            {{ results.statistics.totalVotes }}
          </el-descriptions-item>
          <el-descriptions-item :label="$t('results.validVotes')">
            {{ results.statistics.validVotes }}
          </el-descriptions-item>
        </el-descriptions>

        <el-table :data="topResults" style="width: 100%; margin-bottom: 20px;">
          <el-table-column prop="rank" :label="$t('results.rank')" width="80" />
          <el-table-column prop="fullName" :label="$t('results.candidate')" min-width="200" />
          <el-table-column prop="voteCount" :label="$t('results.votes')" width="100" align="center" />
          <el-table-column prop="section" :label="$t('results.section')" width="100">
            <template #default="scope">
              <el-tag :type="getSectionType(scope.row.section)">
                {{ getSectionLabel(scope.row.section) }}
              </el-tag>
            </template>
          </el-table-column>
        </el-table>

        <el-button type="primary" @click="viewFullResults">
          {{ $t('tally.viewFullResults') }}
        </el-button>
      </div>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import { useI18n } from 'vue-i18n';
import { useNotifications } from '@/composables/useNotifications';
import { useApiErrorHandler } from '@/composables/useApiErrorHandler';
import { Operation } from '@element-plus/icons-vue';
import { useResultStore } from '../../stores/resultStore';

const router = useRouter();
const route = useRoute();
const { t } = useI18n();
const resultStore = useResultStore();
const { showSuccessMessage, showErrorMessage } = useNotifications();
const { handleApiError } = useApiErrorHandler();

const electionGuid = route.params.id as string;
const electionType = ref<'normal' | 'singlename'>('normal');

const calculating = computed(() => resultStore.calculating);
const results = computed(() => resultStore.results);
const tallyProgress = computed(() => resultStore.tallyProgress);

const topResults = computed(() =>
  results.value?.results.slice(0, 10) || []
);

onMounted(async () => {
  try {
    await resultStore.initializeSignalR();
    await resultStore.joinTallySession(electionGuid);
  } catch (error) {
    console.error('Failed to initialize SignalR for tally calculation:', error);
  }
});

onUnmounted(async () => {
  try {
    await resultStore.leaveTallySession(electionGuid);
  } catch (error) {
    console.error('Failed to leave tally session:', error);
  }
});

async function handleCalculate() {
  try {
    await resultStore.calculateTally(electionGuid, electionType.value);
    showSuccessMessage(t('tally.calculateSuccess'));
  } catch (error) {
    handleApiError(error);
  }
}

function viewFullResults() {
  router.push(`/elections/${electionGuid}/results`);
}

function goBack() {
  router.push(`/elections/${electionGuid}`);
}

function getSectionType(section: string) {
  const typeMap: Record<string, any> = {
    'E': 'success',
    'X': 'warning',
    'O': 'info'
  };
  return typeMap[section] || '';
}

function getSectionLabel(section: string) {
  const labelMap: Record<string, string> = {
    'E': t('results.elected'),
    'X': t('results.extra'),
    'O': t('results.other')
  };
  return labelMap[section] || section;
}
</script>

<style lang="less">
.tally-calculation-page {
  max-width: 1000px;
  margin: 0 auto;
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.results-preview h3 {
  margin-bottom: 20px;
  color: #303133;
}

.progress-section h4 {
  margin-bottom: 10px;
  color: #303133;
}

.progress-details p {
  margin: 5px 0;
  font-size: 14px;
}

.progress-stats {
  color: #909399;
  font-size: 13px;
}
</style>
