<script setup lang="ts">
import type { ColumnMapping, ImportPeopleResult } from "@/types";
import type { PeopleImportProgressEvent } from "@/types/SignalREvents";
import { Check, Clock, InfoFilled, Warning } from "@element-plus/icons-vue";
import { PEOPLE_TARGET_FIELDS } from "@/types";

defineProps<{
  parsedResult: { headers: string[]; totalDataRows?: number } | null;
  columnMappings: ColumnMapping[];
  isMappingValid: boolean;
  selectedFile: { rowId: number } | null;
  importing: boolean;
  importProgress: PeopleImportProgressEvent | null;
  importResult: ImportPeopleResult | null;
  translatedErrors: { key: string; message: string }[];
  translatedWarnings: { key: string; message: string }[];
  peopleCount: number;
  canDeleteAllPeople: boolean;
}>();

const emit = defineEmits<{
  import: [];
  "delete-all": [];
}>();

function getFieldLabel(fieldValue: string): string {
  const field = PEOPLE_TARGET_FIELDS.find((f) => f.value === fieldValue);
  return field ? field.label : fieldValue;
}

function formatTime(seconds: number): string {
  if (seconds < 60) {
    return `${seconds}s`;
  }
  const minutes = Math.floor(seconds / 60);
  const remainingSeconds = seconds % 60;
  return `${minutes}m ${remainingSeconds}s`;
}
</script>

<template>
  <div class="people-import-execute-step">
    <h3>{{ $t("people.import.reviewImport") }}</h3>
    <p>{{ $t("people.import.importStepDesc") }}</p>

    <!-- Import Summary -->
    <div v-if="parsedResult" class="import-summary">
      <el-card class="summary-card">
        <template #header>
          <h4>{{ $t("people.import.importSummary") }}</h4>
        </template>
        <div class="summary-content">
          <div class="summary-item">
            <strong>{{ $t("people.import.dataRows") }}:</strong>
            {{ parsedResult.totalDataRows }}
          </div>
          <div class="summary-item">
            <strong>{{ $t("people.import.mappedFields") }}:</strong>
            <ul class="mapped-fields-list">
              <template v-if="columnMappings.length > 0">
                <li v-for="mapping in columnMappings" :key="mapping.fileColumn">
                  {{ mapping.fileColumn }} →
                  {{
                    mapping.targetField
                      ? getFieldLabel(mapping.targetField)
                      : "?"
                  }}
                </li>
              </template>
            </ul>
          </div>
        </div>
      </el-card>
    </div>

    <!-- Validation Warnings -->
    <el-alert
      v-if="!isMappingValid"
      :title="$t('people.import.validationWarning')"
      :description="$t('people.import.firstNameLastNameRequired')"
      type="warning"
      show-icon
      class="validation-alert"
    />

    <!-- Import Progress -->
    <div v-if="importing" class="import-progress">
      <h4>{{ $t("people.import.importing") }}</h4>
      <el-progress
        v-if="importProgress"
        :percentage="
          (importProgress.processed / importProgress.total) * 100 || 0
        "
        :text-inside="true"
        :stroke-width="20"
        status="success"
      />
      <p v-if="importProgress?.status" class="progress-message">
        {{ importProgress.status }}
      </p>
    </div>

    <!-- Import Results -->
    <div v-if="importResult && !importing" class="import-results">
      <el-card class="results-card">
        <template #header>
          <h4>{{ $t("people.import.importResults") }}</h4>
        </template>
        <div class="results-content">
          <div class="result-item success">
            <el-icon>
              <Check />
            </el-icon>
            <span
              >{{ $t("people.import.peopleAdded") }}:
              {{ importResult.peopleAdded }}</span
            >
          </div>
          <div
            v-if="importResult.peopleSkipped > 0"
            class="result-item warning"
          >
            <el-icon>
              <Warning />
            </el-icon>
            <span
              >{{ $t("people.import.peopleSkipped") }}:
              {{ importResult.peopleSkipped }}</span
            >
          </div>
          <div v-if="translatedWarnings.length > 0" class="result-item info">
            <el-icon>
              <InfoFilled />
            </el-icon>
            <span
              >{{ $t("people.import.warnings") }}:
              {{ translatedWarnings.length }}</span
            >
          </div>
          <div class="result-item time">
            <el-icon>
              <Clock />
            </el-icon>
            <span
              >{{ $t("people.import.timeElapsed") }}:
              {{ formatTime(importResult.timeElapsedSeconds) }}</span
            >
          </div>
        </div>
      </el-card>
    </div>

    <!-- Detailed Errors and Warnings -->
    <div
      v-if="translatedErrors.length > 0 || translatedWarnings.length > 0"
      class="import-details"
    >
      <el-card class="details-card">
        <template #header>
          <h4>{{ $t("import.errorsTitle") }}</h4>
        </template>
        <div class="details-content">
          <div
            v-for="error in translatedErrors"
            :key="`error-${error.key}`"
            class="detail-item error"
          >
            <el-icon>
              <Warning />
            </el-icon>
            <span>{{ error.message }}</span>
          </div>
          <div
            v-for="warning in translatedWarnings"
            :key="`warning-${warning.key}`"
            class="detail-item warning"
          >
            <el-icon>
              <InfoFilled />
            </el-icon>
            <span>{{ warning.message }}</span>
          </div>
        </div>
      </el-card>
    </div>

    <!-- Import Actions -->
    <div class="import-actions">
      <el-space>
        <el-button
          type="primary"
          :loading="importing"
          :disabled="!isMappingValid || !selectedFile"
          @click="emit('import')"
        >
          {{ $t("people.import.importNow") }}
        </el-button>
        <el-button
          type="danger"
          :disabled="canDeleteAllPeople === false"
          @click="emit('delete-all')"
        >
          {{ $t("people.import.deleteAllPeople") }}
        </el-button>
      </el-space>
    </div>

    <!-- People Count -->
    <div class="people-count">
      <el-statistic
        :title="$t('people.import.currentPeopleCount')"
        :value="peopleCount"
        :loading="false"
      />
    </div>
  </div>
</template>

<style lang="less">
.people-import-execute-step {
  h3 {
    margin-bottom: 20px;
  }
  p {
    color: var(--el-text-color-secondary);
    margin-bottom: 20px;
  }
  .import-summary {
    margin-bottom: 20px;
    .summary-item {
      margin-bottom: 15px;
      strong {
        display: block;
        margin-bottom: 5px;
      }
      .mapped-fields-list {
        margin: 0;
        padding-left: 20px;
        li {
          margin-bottom: 5px;
          color: #606266;
        }
      }
    }
  }
  .validation-alert {
    margin-bottom: 20px;
  }
  .import-progress {
    margin-bottom: 20px;
    h4 {
      margin-bottom: 15px;
    }
    .progress-message {
      margin-top: 10px;
      color: var(--color-text-secondary);
      font-style: italic;
    }
  }
  .import-results {
    margin-bottom: 20px;
    .result-item {
      display: flex;
      align-items: center;
      margin-bottom: 10px;
      padding: 8px;
      border-radius: 4px;
      &.success {
        background-color: var(--color-success-50);
        color: var(--color-success-600);
      }
      &.warning {
        background-color: var(--color-warning-50);
        color: var(--color-warning-600);
      }
      &.info {
        background-color: var(--color-gray-100);
        color: var(--color-gray-400);
      }
      &.time {
        background-color: var(--color-gray-100);
        color: var(--color-text-secondary);
      }
      .el-icon {
        margin-right: 8px;
      }
      span {
        font-weight: 500;
      }
    }
  }
  .import-details {
    margin-bottom: 20px;
    .detail-item {
      display: flex;
      align-items: flex-start;
      margin-bottom: 8px;
      padding: 8px;
      border-radius: 4px;
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
      .el-icon {
        margin-right: 8px;
        margin-top: 2px;
        flex-shrink: 0;
      }
    }
  }
  .import-actions {
    text-align: center;
    margin: 30px 0;
  }
  .people-count {
    text-align: center;
    margin-top: 20px;
  }
}
</style>
