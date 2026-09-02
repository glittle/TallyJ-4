<script setup lang="ts">
import type { CountReconciliationReportDto } from "@/types";
import { getVotingMethodLabel } from "@/utils/votingMethodLabels";
import { computed } from "vue";
import { useI18n } from "vue-i18n";

const props = defineProps<{
  report: CountReconciliationReportDto | null;
  loading?: boolean;
}>();

const { t } = useI18n();

const rows = computed(() => props.report?.mismatches ?? []);

function kindLabel(kind: string): string {
  const key = `tally.reconciliation.kind.${kind}`;
  const translated = t(key);
  return translated === key ? kind : translated;
}

function subject(
  row: CountReconciliationReportDto["mismatches"][number],
): string {
  if (row.personName) {
    return row.personName;
  }
  if (row.ballotCode) {
    return row.ballotCode;
  }
  if (row.kind === "FrontDeskVsBallots") {
    return t("tally.reconciliation.frontDeskVsBallotsSubject", {
      frontDesk: row.frontDeskCount ?? 0,
      ballots: row.ballotCount ?? 0,
    });
  }
  return t("frontDesk.common.dash");
}

function detail(
  row: CountReconciliationReportDto["mismatches"][number],
): string {
  const parts: string[] = [];
  if (row.votingMethod) {
    parts.push(getVotingMethodLabel(row.votingMethod, t));
  }
  if (typeof row.envNum === "number") {
    parts.push(t("tally.reconciliation.envelope", { n: row.envNum }));
  }
  if (row.onlineStatus) {
    parts.push(
      t("tally.reconciliation.onlineStatus", { status: row.onlineStatus }),
    );
  }
  return parts.join(" · ");
}
</script>

<template>
  <div class="reconciliation-report-panel">
    <h3>{{ $t("tally.reconciliation.title") }}</h3>
    <p class="reconciliation-report-panel__intro">
      {{ $t("tally.reconciliation.intro") }}
    </p>

    <el-skeleton v-if="loading" :rows="3" animated />

    <template v-else-if="report">
      <el-alert
        :title="
          report.isReconciled
            ? $t('tally.reconciliation.ready')
            : $t('tally.reconciliation.blocked', {
                count: report.mismatches.length,
              })
        "
        :type="report.isReconciled ? 'success' : 'error'"
        :closable="false"
        show-icon
      />

      <el-descriptions
        :column="2"
        border
        class="reconciliation-report-panel__counts"
      >
        <el-descriptions-item
          :label="$t('tally.reconciliation.frontDeskCount')"
        >
          {{ report.frontDeskCount }}
        </el-descriptions-item>
        <el-descriptions-item :label="$t('tally.reconciliation.ballotCount')">
          {{ report.ballotCount }}
        </el-descriptions-item>
        <el-descriptions-item
          :label="$t('tally.reconciliation.pendingOnlineCount')"
        >
          {{ report.pendingOnlineCount }}
        </el-descriptions-item>
        <el-descriptions-item
          :label="$t('tally.reconciliation.spoiledBallotCount')"
        >
          {{ report.spoiledBallotCount }}
        </el-descriptions-item>
      </el-descriptions>

      <el-table
        v-if="rows.length"
        :data="rows"
        class="reconciliation-report-panel__table"
      >
        <el-table-column
          :label="$t('tally.reconciliation.kindColumn')"
          min-width="160"
        >
          <template #default="scope">
            {{ kindLabel(scope.row.kind) }}
          </template>
        </el-table-column>
        <el-table-column
          :label="$t('tally.reconciliation.subjectColumn')"
          min-width="200"
        >
          <template #default="scope">
            {{ subject(scope.row) }}
          </template>
        </el-table-column>
        <el-table-column
          :label="$t('tally.reconciliation.detailColumn')"
          min-width="180"
        >
          <template #default="scope">
            {{ detail(scope.row) }}
          </template>
        </el-table-column>
      </el-table>
    </template>
  </div>
</template>

<style lang="less">
.reconciliation-report-panel {
  margin-bottom: 20px;

  h3 {
    margin: 0 0 8px;
  }

  &__intro {
    margin: 0 0 12px;
    color: var(--el-text-color-regular);
  }

  &__counts {
    margin: 16px 0;
  }

  &__table {
    width: 100%;
  }
}
</style>
