<script setup lang="ts">
import { computed } from "vue";
import ReportingBallotReports from "./ReportingBallotReports.vue";
import ReportingVoterReports from "./ReportingVoterReports.vue";
import { isBallotReportCode } from "@/utils/reportFormatters";

const props = defineProps<{
  selectedReport: string;
  selectedReportName: string;
  reportData: unknown;
}>();

const showBallotReports = computed(() =>
  isBallotReportCode(props.selectedReport),
);
</script>

<template>
  <div class="report-content">
    <ReportingBallotReports
      v-if="showBallotReports"
      :selected-report="selectedReport"
      :selected-report-name="selectedReportName"
      :report-data="reportData"
    />
    <ReportingVoterReports
      v-else
      :selected-report="selectedReport"
      :selected-report-name="selectedReportName"
      :report-data="reportData"
    />
  </div>
</template>

<style lang="less">
.report-content {
  h2 {
    margin: 0 0 8px;
    font-size: 20px;
  }
  .report-meta {
    margin-bottom: 16px;
    color: var(--el-text-color-secondary);
    font-size: 13px;
  }
  .info-table,
  .data-table,
  .ballots-table {
    width: 100%;
    border-collapse: collapse;
    margin-bottom: 16px;
    font-size: 13px;
    td,
    th {
      padding: 4px 8px;
      border-bottom: 1px solid var(--el-border-color-lighter);
      text-align: left;
    }
    th {
      font-weight: 600;
      background: var(--el-fill-color-light);
    }
    .num {
      text-align: right;
      font-variant-numeric: tabular-nums;
    }
  }
  .info-table {
    max-width: 480px;
    .sub-row td {
      padding-left: 24px;
      color: var(--el-text-color-secondary);
    }
    .divider td div {
      border-top: 1px solid var(--el-border-color);
      margin: 4px 0;
    }
    .spacer td {
      height: 8px;
      border: none;
    }
    .warn-row {
      color: var(--el-color-warning);
    }
  }
  .page-break {
    break-before: page;
    margin-top: 24px;
  }
  .votes-list {
    .section-break {
      height: 12px;
      border-top: 1px dashed var(--el-border-color);
      margin: 8px 0;
    }
    .vote-person {
      padding: 2px 0;
      &.elected {
        font-weight: 600;
      }
    }
  }
  .ballots-table {
    .ballot-id {
      width: 120px;
      vertical-align: top;
    }
    .ballot-code {
      font-weight: 600;
    }
    .ballot-loc,
    .ballot-status {
      font-size: 11px;
      color: var(--el-text-color-secondary);
    }
    .vote-entry {
      display: inline-block;
      margin-right: 8px;
      &.vote-spoiled {
        color: var(--el-color-danger);
        text-decoration: line-through;
      }
    }
    .invalid-reason {
      font-size: 11px;
      color: var(--el-color-danger);
    }
    .sne-count {
      font-weight: 600;
      margin-right: 4px;
    }
    tr.spoiled {
      background: var(--el-color-danger-light-9);
    }
  }
  .empty-msg {
    color: var(--el-text-color-secondary);
    font-style: italic;
  }
  .name-columns {
    column-count: 3;
    column-gap: 16px;
    .name-entry {
      break-inside: avoid;
      padding: 2px 0;
    }
  }
  .flag-cell {
    text-align: center;
  }
  .total-row {
    font-weight: 600;
  }
  .dup-group {
    margin-bottom: 20px;
  }
  .loc-area-group {
    margin-bottom: 16px;
  }
  .data-table.compact {
    max-width: 400px;
  }
  .section-E {
    font-weight: 600;
  }
}
@media print {
  .report-content {
    .page-break {
      break-before: page;
    }
  }
}
</style>
