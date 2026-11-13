<script setup lang="ts">
import { ref, computed, onMounted, onBeforeUnmount } from "vue";
import { useI18n } from "vue-i18n";
import { useRoute } from "vue-router";
import { useApiErrorHandler } from "@/composables/useApiErrorHandler";
import { translateAnalysisBlocker } from "@/utils/electionStageErrorMessages";
import { translateTallyProgressMessage } from "@/utils/tallyProgressMessages";
import { useResultStore } from "../../stores/resultStore";
import ReconciliationReportPanel from "../../components/results/ReconciliationReportPanel.vue";
import ResultsTable from "../../components/results/ResultsTable.vue";
import TiesDisplay from "../../components/results/TiesDisplay.vue";

const COUNTS_DO_NOT_RECONCILE =
  "elections.stageChangeError.countsDoNotReconcile";

const route = useRoute();
const { t } = useI18n();
const resultStore = useResultStore();
const { handleApiError } = useApiErrorHandler();

const electionGuid = route.params.id as string;
const activeTab = ref("all");
const initialLoad = ref(true);

const loading = computed(() => initialLoad.value || resultStore.loading);
const results = computed(() => resultStore.results);
const reconciliation = computed(() => resultStore.reconciliation);
const calculating = computed(() => resultStore.calculating);
const tallyProgress = computed(() => resultStore.tallyProgress);

const hasCalculation = computed(() => Boolean(results.value?.calculatedAt));

const countBlocked = computed(
  () => reconciliation.value?.isReconciled === false,
);

const blockerAlert = computed(() => {
  const key = results.value?.analysisBlocker;
  if (!key) {
    return "";
  }

  const isCountBlock =
    key === COUNTS_DO_NOT_RECONCILE ||
    key.startsWith(`${COUNTS_DO_NOT_RECONCILE}|`);
  if (isCountBlock && countBlocked.value) {
    return "";
  }

  return translateAnalysisBlocker(key, t);
});

const tallyBlocked = computed(
  () => countBlocked.value || Boolean(results.value?.analysisBlocker),
);

const tallyProgressMessage = computed(() => {
  if (!tallyProgress.value) {
    return "";
  }

  return translateTallyProgressMessage(
    tallyProgress.value.message,
    tallyProgress.value,
    t,
  );
});

const electedPeople = computed(
  () => results.value?.results.filter((r) => r.section === "E") || [],
);

const extraPeople = computed(
  () => results.value?.results.filter((r) => r.section === "X") || [],
);

onMounted(async () => {
  try {
    try {
      await resultStore.fetchResults(electionGuid);
    } catch (error) {
      handleApiError(error);
    }

    try {
      await resultStore.fetchReconciliation(electionGuid);
    } catch (error) {
      handleApiError(error);
    }
  } finally {
    initialLoad.value = false;
  }

  try {
    await resultStore.initializeSignalR();
    await resultStore.joinTallySession(electionGuid);
  } catch (error) {
    console.error("Failed to join tally session:", error);
  }
});

onBeforeUnmount(async () => {
  try {
    await resultStore.leaveTallySession(electionGuid);
  } catch (error) {
    console.error("Failed to leave tally session:", error);
  }
});

function formatDateTime(date: string | Date) {
  if (!date) {
    return "-";
  }
  return new Date(date).toLocaleString();
}
</script>

<template>
  <div class="results-page">
    <el-card>
      <div v-if="loading" class="loading-container">
        <el-skeleton :rows="5" animated />
      </div>

      <div v-else-if="results">
        <ReconciliationReportPanel
          v-if="countBlocked && reconciliation"
          :report="reconciliation"
        />

        <el-alert
          v-if="blockerAlert"
          class="results-blocked"
          data-testid="results-blocker"
          :title="blockerAlert"
          type="error"
          :closable="false"
          show-icon
        />

        <template v-if="hasCalculation">
          <el-alert
            v-if="results.calculatedAt"
            data-testid="results-calculated"
            :title="
              $t('results.calculatedAt', {
                date: formatDateTime(results.calculatedAt),
              })
            "
            type="info"
            :closable="false"
            style="margin-bottom: 20px"
          />

          <el-alert
            v-if="calculating || tallyProgress"
            :title="
              calculating
                ? $t('results.calculating')
                : $t('results.tallyProgress')
            "
            type="warning"
            :closable="false"
            style="margin-bottom: 20px"
          >
            <div v-if="tallyProgress" style="margin-top: 10px">
              <el-progress
                :percentage="tallyProgress.percentComplete"
                :text-inside="true"
                :stroke-width="20"
                status="warning"
              />
              <div style="margin-top: 10px; font-size: 14px">
                {{ tallyProgressMessage }}
                <br />
                {{
                  $t("results.processedBallots", {
                    processed: tallyProgress.processedBallots,
                    total: tallyProgress.totalBallots,
                  })
                }}
              </div>
            </div>
          </el-alert>

          <el-descriptions :column="2" border style="margin-bottom: 20px">
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
              <ResultsTable :results="electedPeople" />
            </el-tab-pane>
            <el-tab-pane :label="$t('results.extra')" name="extra">
              <ResultsTable :results="extraPeople" />
            </el-tab-pane>
            <el-tab-pane
              :label="$t('results.ties')"
              name="ties"
              :badge="results.ties.length || undefined"
            >
              <TiesDisplay :ties="results.ties" />
            </el-tab-pane>
          </el-tabs>
        </template>

        <el-empty
          v-else-if="!tallyBlocked"
          :description="$t('results.noResults')"
        />
      </div>

      <el-empty v-else :description="$t('results.noResults')" />
    </el-card>
  </div>
</template>

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

  .results-blocked {
    margin-bottom: 20px;
  }
}
</style>
