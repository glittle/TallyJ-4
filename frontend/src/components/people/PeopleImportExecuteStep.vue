<script setup lang="ts">
import type { ColumnMapping, ImportPeopleResult } from "@/types";
import type { PeopleImportProgressEvent } from "@/types/SignalREvents";
import { Check, Clock, InfoFilled, Warning } from "@element-plus/icons-vue";
import { PEOPLE_TARGET_FIELDS } from "@/types";
import { computed } from "vue";

const props = defineProps<{
  parsedResult: { headers: string[]; totalDataRows?: number } | null;
  columnMappings: ColumnMapping[];
  isMappingValid: boolean;
  mappingSaved: boolean;
  selectedFile: { rowId: number; originalFileName?: string | null } | null;
  importing: boolean;
  importProgress: PeopleImportProgressEvent | null;
  importResult: ImportPeopleResult | null;
  translatedErrors: {
    key: string;
    message: string;
    parameters?: Record<string, string>;
  }[];
  translatedWarnings: {
    key: string;
    message: string;
    parameters?: Record<string, string>;
  }[];
  peopleCount: number;
}>();

const emit = defineEmits<{
  import: [];
}>();

const mappedPairs = computed(() =>
  props.columnMappings
    .filter((mapping) => mapping.targetField)
    .map((mapping) => ({
      fileColumn: mapping.fileColumn,
      targetLabel: getFieldLabel(mapping.targetField as string),
    })),
);

const canLoad = computed(
  () =>
    props.mappingSaved &&
    props.isMappingValid &&
    !!props.selectedFile &&
    !props.importing,
);

function getFieldLabel(fieldValue: string): string {
  const field = PEOPLE_TARGET_FIELDS.find((f) => f.value === fieldValue);
  return field ? field.label : fieldValue;
}

function formatTime(seconds: number): string {
  const whole = Math.max(0, Math.round(seconds));
  if (whole < 60) {
    return `${whole}s`;
  }
  const minutes = Math.floor(whole / 60);
  const remainingSeconds = whole % 60;
  return `${minutes}m ${remainingSeconds}s`;
}

function progressPercent(): number {
  if (!props.importProgress || !props.importProgress.total) {
    return 0;
  }
  return Math.round(
    (props.importProgress.processed / props.importProgress.total) * 100,
  );
}
</script>

<template>
  <div class="people-import-execute-step">
    <div v-if="!selectedFile || !parsedResult" class="stage-empty">
      {{ $t("people.import.chooseFileFirst") }}
    </div>

    <div v-else-if="!mappingSaved" class="stage-empty">
      {{ $t("people.import.confirmMappingFirst") }}
    </div>

    <template v-else>
      <el-alert
        v-if="!isMappingValid"
        :title="$t('people.import.validationWarning')"
        :description="$t('people.import.firstNameLastNameRequired')"
        type="warning"
        show-icon
        :closable="false"
        class="validation-alert"
      />

      <dl class="load-summary">
        <div>
          <dt>{{ $t("people.import.fileName") }}</dt>
          <dd>{{ selectedFile.originalFileName }}</dd>
        </div>
        <div>
          <dt>{{ $t("people.import.dataRows") }}</dt>
          <dd>{{ parsedResult.totalDataRows }}</dd>
        </div>
        <div>
          <dt>{{ $t("people.import.peopleInElection") }}</dt>
          <dd>{{ peopleCount }}</dd>
        </div>
      </dl>

      <div class="mapped-block">
        <h4>{{ $t("people.import.mappedFields") }}</h4>
        <ul class="mapped-fields-list">
          <li v-for="pair in mappedPairs" :key="pair.fileColumn">
            <span class="source">{{ pair.fileColumn }}</span>
            <span class="arrow">→</span>
            <span class="target">{{ pair.targetLabel }}</span>
          </li>
        </ul>
      </div>

      <div v-if="importing" class="import-progress">
        <h4>{{ $t("people.import.importing") }}</h4>
        <el-progress
          :percentage="progressPercent()"
          :format="(percent: number) => `${Math.round(percent)}%`"
          :text-inside="true"
          :stroke-width="20"
          status="success"
        />
        <p v-if="importProgress?.status" class="progress-message">
          {{ importProgress.status }}
        </p>
      </div>

      <div v-if="importResult && !importing" class="import-results">
        <h4>{{ $t("people.import.importResults") }}</h4>
        <div class="result-item success">
          <el-icon><Check /></el-icon>
          <span
            >{{ $t("people.import.peopleAdded") }}:
            {{ importResult.peopleAdded }}</span
          >
        </div>
        <div v-if="importResult.peopleSkipped > 0" class="result-item warning">
          <el-icon><Warning /></el-icon>
          <span
            >{{ $t("people.import.peopleSkipped") }}:
            {{ importResult.peopleSkipped }}</span
          >
        </div>
        <div v-if="translatedWarnings.length > 0" class="result-item info">
          <el-icon><InfoFilled /></el-icon>
          <span
            >{{ $t("people.import.warnings") }}:
            {{ translatedWarnings.length }}</span
          >
        </div>
        <div class="result-item time">
          <el-icon><Clock /></el-icon>
          <span
            >{{ $t("people.import.timeElapsed") }}:
            {{ formatTime(importResult.timeElapsedSeconds) }}</span
          >
        </div>
      </div>

      <div
        v-if="
          !importing &&
          (translatedErrors.length > 0 || translatedWarnings.length > 0)
        "
        class="import-details"
      >
        <h4>{{ $t("import.errorsTitle") }}</h4>
        <div
          v-for="(error, index) in translatedErrors"
          :key="`error-${error.key}-${error.parameters?.rowNumber ?? index}`"
          class="detail-item error"
        >
          <el-icon><Warning /></el-icon>
          <span>{{ error.message }}</span>
        </div>
        <div
          v-for="(warning, index) in translatedWarnings"
          :key="`warning-${warning.key}-${warning.parameters?.rowNumber ?? index}`"
          class="detail-item warning"
        >
          <el-icon><InfoFilled /></el-icon>
          <span>{{ warning.message }}</span>
        </div>
      </div>

      <div class="import-actions">
        <el-button
          type="primary"
          :loading="importing"
          :disabled="!canLoad"
          @click="emit('import')"
        >
          {{ $t("people.import.loadPeople") }}
        </el-button>
      </div>
    </template>
  </div>
</template>

<style lang="less">
.people-import-execute-step {
  .stage-empty {
    padding: 24px 8px;
    color: var(--el-text-color-secondary);
  }

  .validation-alert {
    margin-bottom: 16px;
  }

  .load-summary {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(160px, 1fr));
    gap: 12px 24px;
    margin: 0 0 20px;

    div {
      margin: 0;
    }

    dt {
      font-size: var(--font-size-sm);
      color: var(--el-text-color-secondary);
      margin-bottom: 4px;
    }

    dd {
      margin: 0;
      font-weight: 600;
      color: var(--el-text-color-primary);
    }
  }

  .mapped-block {
    margin-bottom: 20px;

    h4 {
      margin: 0 0 8px;
      font-size: var(--font-size-sm);
      color: var(--el-text-color-secondary);
    }
  }

  .mapped-fields-list {
    margin: 0;
    padding: 0;
    list-style: none;

    li {
      display: flex;
      align-items: baseline;
      gap: 8px;
      margin-bottom: 4px;
    }

    .source {
      font-weight: 600;
    }

    .arrow {
      color: var(--el-text-color-placeholder);
    }

    .target {
      color: var(--el-text-color-regular);
    }
  }

  .import-progress {
    margin-bottom: 20px;

    h4 {
      margin: 0 0 12px;
    }

    .progress-message {
      margin-top: 10px;
      color: var(--color-text-secondary);
      font-style: italic;
    }
  }

  .import-results,
  .import-details {
    margin-bottom: 20px;

    h4 {
      margin: 0 0 10px;
    }
  }

  .result-item,
  .detail-item {
    display: flex;
    align-items: flex-start;
    margin-bottom: 8px;
    padding: 8px;
    border-radius: 4px;

    .el-icon {
      margin-right: 8px;
      margin-top: 2px;
      flex-shrink: 0;
    }
  }

  .result-item {
    &.success {
      background-color: var(--color-success-50);
      color: var(--color-success-600);
    }
    &.warning {
      background-color: var(--color-warning-50);
      color: var(--color-warning-600);
    }
    &.info,
    &.time {
      background-color: var(--color-gray-100);
      color: var(--color-text-secondary);
    }
  }

  .detail-item {
    font-size: 14px;

    &.error {
      background-color: var(--color-error-50);
      color: var(--color-error-600);
      border: 1px solid var(--color-error-500);
    }

    &.warning {
      background-color: var(--color-warning-50);
      color: var(--color-warning-600);
      border: 1px solid var(--color-warning-500);
    }
  }

  .import-actions {
    margin-top: 8px;
  }
}
</style>
