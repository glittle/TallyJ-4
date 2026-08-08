<script setup lang="ts">
import { computed, onMounted, ref } from "vue";
import { useI18n } from "vue-i18n";
import { useRoute, useRouter } from "vue-router";
import ReportingReportBody from "@/components/results/ReportingReportBody.vue";
import { reportService } from "../../services/reportService";
import type { ReportListItem } from "../../types";

const route = useRoute();
const router = useRouter();
const { t } = useI18n();

const electionGuid = computed(() => route.params.id as string);
const availableReports = ref<ReportListItem[]>([]);
const selectedReport = ref<string>("");
const reportData = ref<unknown>(null);
const loading = ref(false);
const loadingList = ref(true);
const error = ref("");

const ballotReports = computed(() =>
  availableReports.value.filter((r) => r.category === "Ballot Reports"),
);
const voterReports = computed(() =>
  availableReports.value.filter((r) => r.category === "Voter Reports"),
);
const selectedReportName = computed(
  () =>
    availableReports.value.find((r) => r.code === selectedReport.value)?.name ??
    "",
);

onMounted(async () => {
  try {
    availableReports.value = await reportService.getAvailableReports(
      electionGuid.value,
    );
  } catch {
    error.value = t("reporting.error");
  } finally {
    loadingList.value = false;
  }
});

async function selectReport(code: string) {
  if (selectedReport.value === code && reportData.value) {
    return;
  }
  selectedReport.value = code;
  reportData.value = null;
  loading.value = true;
  error.value = "";
  try {
    reportData.value = await reportService.getReport(electionGuid.value, code);
  } catch {
    error.value = t("reporting.error");
  } finally {
    loading.value = false;
  }
}

function goBack() {
  router.back();
}

function printPage() {
  globalThis.print();
}
</script>

<template>
  <div class="reports-page">
    <div class="reports-chooser no-print">
      <div class="chooser-inner">
        <div v-if="ballotReports.length">
          <h3>{{ $t("reporting.ballotReports") }}</h3>
          <ul>
            <li
              v-for="r in ballotReports"
              :key="r.code"
              :class="{ active: selectedReport === r.code }"
            >
              <a href="#" @click.prevent="selectReport(r.code)">{{ r.name }}</a>
            </li>
          </ul>
        </div>
        <div v-if="voterReports.length">
          <h3>{{ $t("reporting.voterReports") }}</h3>
          <ul>
            <li
              v-for="r in voterReports"
              :key="r.code"
              :class="{ active: selectedReport === r.code }"
            >
              <a href="#" @click.prevent="selectReport(r.code)">{{ r.name }}</a>
            </li>
          </ul>
        </div>
        <div class="chooser-actions">
          <el-button type="primary" @click="printPage">
            {{ $t("reporting.print") }}
          </el-button>
          <p class="print-hint">{{ $t("reporting.printHint") }}</p>
        </div>
        <div class="chooser-actions">
          <el-button @click="goBack">{{ $t("common.back") }}</el-button>
        </div>
      </div>
    </div>

    <div class="reports-panel">
      <div v-if="!selectedReport" class="placeholder">
        {{ $t("reporting.selectReport") }}
      </div>
      <div v-else-if="loading" class="placeholder">
        <el-skeleton :rows="8" animated />
      </div>
      <div v-else-if="error" class="placeholder error-text">
        {{ error }}
      </div>
      <ReportingReportBody
        v-else-if="reportData"
        :selected-report="selectedReport"
        :selected-report-name="selectedReportName"
        :report-data="reportData"
      />
    </div>
  </div>
</template>

<style lang="less">
.reports-page {
  display: flex;
  gap: 20px;
  min-height: calc(100vh - 120px);

  .reports-chooser {
    width: 220px;
    flex-shrink: 0;

    .chooser-inner {
      position: sticky;
      top: 70px;
    }

    h3 {
      margin: 16px 0 6px;
      font-size: 14px;
      color: var(--el-text-color-secondary);
      text-transform: uppercase;
      letter-spacing: 0.04em;
    }

    ul {
      list-style: none;
      margin: 0;
      padding: 0;
    }

    li {
      margin: 2px 0;
      a {
        display: block;
        padding: 4px 8px;
        border-radius: 4px;
        color: var(--el-text-color-regular);
        text-decoration: none;
        font-size: 13px;
        &:hover {
          background: var(--el-fill-color-light);
        }
      }
      &.active a {
        background: var(--el-color-primary-light-9);
        color: var(--el-color-primary);
        font-weight: 600;
      }
    }

    .chooser-actions {
      margin-top: 16px;
    }

    .print-hint {
      font-size: 11px;
      color: var(--el-text-color-secondary);
      margin: 6px 0 0;
    }
  }

  .reports-panel {
    flex: 1;
    min-width: 0;
    background: var(--el-bg-color);
    padding: 16px 20px;
    border-radius: 8px;
    border: 1px solid var(--el-border-color-lighter);
  }

  .placeholder {
    color: var(--el-text-color-secondary);
    padding: 40px 16px;
    text-align: center;
  }

  .error-text {
    color: var(--el-color-danger);
  }
}

@media print {
  .reports-page {
    display: block;
    .no-print {
      display: none !important;
    }
    .reports-panel {
      border: none;
      padding: 0;
    }
  }
}
</style>
