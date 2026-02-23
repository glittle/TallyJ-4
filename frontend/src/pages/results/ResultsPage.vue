<template>
  <div class="results-page">
    <el-card>
      <template #header>
        <div class="card-header">
          <el-page-header @back="goBack" :content="$t('results.title')" />
        </div>
      </template>

      <div v-if="loading" class="loading-container">
        <el-skeleton :rows="5" animated />
      </div>

      <div v-else-if="results">
        <el-alert
          v-if="results.calculatedAt"
          :title="$t('results.calculatedAt', { date: formatDateTime(results.calculatedAt) })"
          type="info"
          :closable="false"
          style="margin-bottom: 20px;"
        />

        <el-alert
          v-if="calculating || tallyProgress"
          :title="calculating ? $t('results.calculating') : $t('results.tallyProgress')"
          type="warning"
          :closable="false"
          style="margin-bottom: 20px;"
        >
          <div v-if="tallyProgress" style="margin-top: 10px;">
            <el-progress
              :percentage="tallyProgress.percentComplete"
              :text-inside="true"
              :stroke-width="20"
              status="warning"
            />
            <div style="margin-top: 10px; font-size: 14px;">
              {{ tallyProgress.message }}
              <br>
              {{ $t('results.processedBallots', { processed: tallyProgress.processedBallots, total: tallyProgress.totalBallots }) }}
            </div>
          </div>
        </el-alert>

        <el-descriptions :column="2" border style="margin-bottom: 20px;">
          <el-descriptions-item :label="$t('results.totalBallots')">
            {{ results.statistics.totalBallots }}
          </el-descriptions-item>
          <el-descriptions-item :label="$t('results.ballotsReceived')">
            {{ results.statistics.ballotsReceived }}
          </el-descriptions-item>
          <el-descriptions-item :label="$t('results.spoiledBallots')">
            {{ results.statistics.spoiledBallots }}
          </el-descriptions-item>
          <el-descriptions-item :label="$t('results.totalVotes')">
            {{ results.statistics.totalVotes }}
          </el-descriptions-item>
          <el-descriptions-item :label="$t('results.validVotes')">
            {{ results.statistics.validVotes }}
          </el-descriptions-item>
          <el-descriptions-item :label="$t('results.numberToElect')">
            {{ results.statistics.numberToElect }}
          </el-descriptions-item>
        </el-descriptions>

        <el-tabs v-model="activeTab">
          <el-tab-pane :label="$t('results.allResults')" name="all">
            <ResultsTable :results="results.results" />
          </el-tab-pane>
          <el-tab-pane :label="$t('results.elected')" name="elected">
            <ResultsTable :results="electedCandidates" />
          </el-tab-pane>
          <el-tab-pane :label="$t('results.extra')" name="extra">
            <ResultsTable :results="extraCandidates" />
          </el-tab-pane>
          <el-tab-pane :label="$t('results.ties')" name="ties" :badge="results.ties.length || undefined">
            <TiesDisplay :ties="results.ties" />
          </el-tab-pane>
        </el-tabs>
      </div>

      <el-empty v-else :description="$t('results.noResults')" />
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onBeforeUnmount } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import { useI18n } from 'vue-i18n';
import { useNotifications } from '@/composables/useNotifications';
import { useResultStore } from '../../stores/resultStore';
import ResultsTable from '../../components/results/ResultsTable.vue';
import TiesDisplay from '../../components/results/TiesDisplay.vue';

const router = useRouter();
const route = useRoute();
const { t } = useI18n();
const resultStore = useResultStore();
const { showErrorMessage } = useNotifications();

const electionGuid = route.params.id as string;
const activeTab = ref('all');

const loading = computed(() => resultStore.loading);
const results = computed(() => resultStore.results);
const calculating = computed(() => resultStore.calculating);
const tallyProgress = computed(() => resultStore.tallyProgress);

const electedCandidates = computed(() => 
  results.value?.results.filter(r => r.section === 'E') || []
);

const extraCandidates = computed(() => 
  results.value?.results.filter(r => r.section === 'X') || []
);

onMounted(async () => {
  try {
    await resultStore.fetchResults(electionGuid);
    await resultStore.initializeSignalR();
    await resultStore.joinTallySession(electionGuid);
  } catch (error) {
    showErrorMessage(t('results.loadError'));
  }
});

onBeforeUnmount(async () => {
  try {
    await resultStore.leaveTallySession(electionGuid);
  } catch (error) {
    console.error('Failed to leave tally session:', error);
  }
});

function goBack() {
  router.push(`/elections/${electionGuid}`);
}

function formatDateTime(date: string) {
  if (!date) return '-';
  return new Date(date).toLocaleString();
}
</script>

<style lang="less">
.results-page {
  max-width: 1400px;
  margin: 0 auto;

  .card-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
  }

  .loading-container {
    padding: 40px;
  }
}
</style>
