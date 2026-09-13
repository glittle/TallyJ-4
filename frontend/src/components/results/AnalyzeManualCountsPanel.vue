<script setup lang="ts">
import { computed, onMounted, reactive, ref } from "vue";
import { useI18n } from "vue-i18n";
import { useApiErrorHandler } from "@/composables/useApiErrorHandler";
import { useNotifications } from "@/composables/useNotifications";
import { useResultStore } from "../../stores/resultStore";
import type { AnalyzeCountRowDto } from "../../types";

const props = defineProps<{
  electionGuid: string;
  finalized?: boolean;
  showCalledIn?: boolean;
  custom1Name?: string;
  custom2Name?: string;
  custom3Name?: string;
}>();

const { t } = useI18n();
const resultStore = useResultStore();
const { showSuccessMessage } = useNotifications();
const { handleApiError } = useApiErrorHandler();

const saving = ref(false);
const summaries = computed(() => resultStore.analyzeCounts);

const draft = reactive<AnalyzeCountRowDto>({});

type CountKey = keyof AnalyzeCountRowDto;

const rows = computed(() => {
  const list: { key: CountKey; label: string }[] = [
    { key: "numEligibleToVote", label: t("tally.manualCounts.eligibleVoters") },
    { key: "inPersonBallots", label: t("tally.manualCounts.inPerson") },
    { key: "droppedOffBallots", label: t("tally.manualCounts.droppedOff") },
    { key: "mailedInBallots", label: t("tally.manualCounts.mailedIn") },
  ];
  if (props.showCalledIn) {
    list.push({
      key: "calledInBallots",
      label: t("tally.manualCounts.calledIn"),
    });
  }
  if (props.custom1Name) {
    list.push({ key: "custom1Ballots", label: props.custom1Name });
  }
  if (props.custom2Name) {
    list.push({ key: "custom2Ballots", label: props.custom2Name });
  }
  if (props.custom3Name) {
    list.push({ key: "custom3Ballots", label: props.custom3Name });
  }
  list.push({
    key: "spoiledManualBallots",
    label: t("tally.manualCounts.spoiledManual"),
  });
  return list;
});

function copyManualIntoDraft(manual?: AnalyzeCountRowDto | null) {
  const source = manual ?? {};
  for (const row of rows.value) {
    const value = source[row.key];
    draft[row.key] = value ?? null;
  }
}

function displayCount(value: number | null | undefined): string {
  return value === null || value === undefined ? "—" : String(value);
}

async function loadCounts() {
  try {
    const data = await resultStore.fetchManualCounts(props.electionGuid);
    copyManualIntoDraft(data.manual);
  } catch (error) {
    handleApiError(error);
  }
}

async function saveCounts() {
  saving.value = true;
  try {
    const data = await resultStore.saveManualCounts(props.electionGuid, {
      ...draft,
    });
    copyManualIntoDraft(data.manual);
    showSuccessMessage(t("tally.manualCounts.saveSuccess"));
  } catch (error) {
    handleApiError(error);
  } finally {
    saving.value = false;
  }
}

onMounted(() => {
  void loadCounts();
});
</script>

<template>
  <div class="analyze-manual-counts-panel">
    <h3>{{ $t("tally.manualCounts.title") }}</h3>
    <p class="analyze-manual-counts-panel__hint">
      {{ $t("tally.manualCounts.hint") }}
    </p>

    <table class="analyze-manual-counts-panel__table">
      <thead>
        <tr>
          <th>{{ $t("tally.manualCounts.counts") }}</th>
          <th>{{ $t("tally.manualCounts.calculated") }}</th>
          <th>{{ $t("tally.manualCounts.override") }}</th>
          <th>{{ $t("tally.manualCounts.final") }}</th>
        </tr>
      </thead>
      <tbody>
        <tr>
          <td></td>
          <td></td>
          <td>
            <el-button
              data-testid="save-manual-counts"
              size="small"
              type="primary"
              :loading="saving"
              :disabled="finalized"
              @click="saveCounts"
            >
              {{ $t("tally.manualCounts.saveValues") }}
            </el-button>
          </td>
          <td></td>
        </tr>
        <tr v-for="row in rows" :key="row.key">
          <td>{{ row.label }}</td>
          <td>{{ displayCount(summaries?.calculated[row.key]) }}</td>
          <td>
            <el-input-number
              v-model="draft[row.key]"
              :min="0"
              :controls="false"
              :disabled="finalized"
              :placeholder="$t('tally.manualCounts.useCalculated')"
            />
          </td>
          <td>
            <strong>{{ displayCount(summaries?.final[row.key]) }}</strong>
          </td>
        </tr>
      </tbody>
    </table>
  </div>
</template>

<style lang="less">
.analyze-manual-counts-panel {
  margin: 0 0 24px;

  h3 {
    margin: 0 0 8px;
    color: #303133;
  }

  .analyze-manual-counts-panel__hint {
    margin: 0 0 12px;
    color: #909399;
    font-size: 13px;
  }

  .analyze-manual-counts-panel__table {
    width: 100%;
    border-collapse: collapse;
    margin-bottom: 8px;

    th,
    td {
      border: 1px solid var(--el-border-color-lighter);
      padding: 8px 10px;
      text-align: left;
      vertical-align: middle;
    }

    th {
      background: var(--el-fill-color-light);
      font-weight: 600;
    }

    .el-input-number {
      width: 120px;
    }
  }
}
</style>
