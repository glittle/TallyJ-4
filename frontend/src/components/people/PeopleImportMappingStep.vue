<script setup lang="ts">
import type { ColumnMapping } from "@/types";
import { PEOPLE_TARGET_FIELDS } from "@/types";
import { computed } from "vue";
import {
  assignTargetToFileColumn,
  previewValuesForColumn,
  requiredFieldStatus,
  targetForFileColumn,
} from "@/utils/peopleImportMapping";

const props = defineProps<{
  parsedResult: {
    headers: string[];
    previewRows?: string[][];
    totalDataRows?: number;
  } | null;
  columnMappings: ColumnMapping[];
  savingMapping: boolean;
  mappingSaved: boolean;
  isMappingValid: boolean;
}>();

const emit = defineEmits<{
  "update:columnMappings": [mappings: ColumnMapping[]];
  save: [];
}>();

const headers = computed(() => props.parsedResult?.headers ?? []);

const requiredFields = computed(() =>
  requiredFieldStatus(props.columnMappings),
);

function targetFor(fileColumn: string): string | null {
  return targetForFileColumn(props.columnMappings, fileColumn);
}

function isTargetUsedByOther(targetField: string, fileColumn: string): boolean {
  return props.columnMappings.some(
    (mapping) =>
      mapping.targetField === targetField && mapping.fileColumn !== fileColumn,
  );
}

function previewFor(fileColumn: string): string {
  return previewValuesForColumn(
    headers.value,
    props.parsedResult?.previewRows,
    fileColumn,
  ).join(" · ");
}

function onTargetChange(fileColumn: string, targetField: string | null) {
  if (!props.parsedResult) {
    return;
  }
  emit(
    "update:columnMappings",
    assignTargetToFileColumn(
      props.columnMappings,
      props.parsedResult.headers,
      fileColumn,
      targetField,
    ),
  );
}
</script>

<template>
  <div class="people-import-mapping-step">
    <div v-if="!parsedResult" class="stage-empty">
      {{ $t("people.import.chooseFileFirst") }}
    </div>

    <template v-else>
      <div class="required-checklist" aria-live="polite">
        <h4>{{ $t("people.import.requiredToConfirm") }}</h4>
        <ul>
          <li
            v-for="field in requiredFields"
            :key="field.value"
            :class="{ 'is-ready': field.mapped, 'is-missing': !field.mapped }"
          >
            <span class="required-mark">{{ field.mapped ? "✓" : "•" }}</span>
            <span>{{ field.label }}</span>
            <span class="required-state">
              {{
                field.mapped
                  ? $t("people.import.requiredMapped")
                  : $t("people.import.requiredMissing")
              }}
            </span>
          </li>
        </ul>
      </div>

      <p class="unused-note">{{ $t("people.import.unusedColumns") }}</p>

      <table class="source-table">
        <thead>
          <tr>
            <th>{{ $t("people.import.fileColumn") }}</th>
            <th>{{ $t("people.import.sourcePreview") }}</th>
            <th>{{ $t("people.import.mapToField") }}</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="header in headers" :key="header">
            <td class="file-cell">
              <span class="field-name">{{ header }}</span>
            </td>
            <td class="preview-cell">{{ previewFor(header) }}</td>
            <td class="target-cell">
              <el-select
                :model-value="targetFor(header)"
                clearable
                filterable
                size="default"
                style="width: 240px"
                :placeholder="$t('people.import.ignore')"
                @update:model-value="
                  (value: string | null) => onTargetChange(header, value)
                "
              >
                <el-option
                  v-for="field in PEOPLE_TARGET_FIELDS"
                  :key="`${header}-${field.value}`"
                  :label="field.label"
                  :value="field.value"
                  :disabled="isTargetUsedByOther(field.value, header)"
                />
              </el-select>
            </td>
          </tr>
        </tbody>
      </table>

      <div class="mapping-actions">
        <el-button
          type="primary"
          :loading="savingMapping"
          :disabled="!isMappingValid"
          @click="emit('save')"
        >
          {{ $t("people.import.confirmMapping") }}
        </el-button>
        <el-tag v-if="mappingSaved" type="success" effect="light">
          {{ $t("people.import.mappingConfirmed") }}
        </el-tag>
        <span v-else-if="isMappingValid" class="needs-confirm">
          {{ $t("people.import.mappingNeedsConfirm") }}
        </span>
      </div>
    </template>
  </div>
</template>

<style lang="less">
.people-import-mapping-step {
  .required-checklist {
    margin: 0 0 16px;
    padding: 12px 14px;
    border: 1px solid var(--el-border-color-lighter);
    border-radius: 8px;
    background: var(--el-fill-color-blank);

    h4 {
      margin: 0 0 8px;
      font-size: var(--font-size-sm);
      color: var(--el-text-color-secondary);
      font-weight: 600;
    }

    ul {
      margin: 0;
      padding: 0;
      list-style: none;
    }

    li {
      display: flex;
      align-items: baseline;
      gap: 8px;
      margin-bottom: 4px;
      font-size: var(--font-size-sm);
    }

    .required-mark {
      width: 1em;
      font-weight: 600;
    }

    .is-ready {
      color: var(--color-success-600);

      .required-state {
        color: var(--color-success-600);
      }
    }

    .is-missing {
      color: var(--el-text-color-primary);

      .required-state {
        color: var(--color-warning-600);
      }
    }
  }

  .unused-note {
    margin: 0 0 16px;
    font-size: var(--font-size-sm);
    color: var(--el-text-color-secondary);
  }

  .stage-empty {
    padding: 24px 8px;
    color: var(--el-text-color-secondary);
  }

  .source-table {
    width: 100%;
    border-collapse: collapse;
    margin-bottom: 16px;

    th,
    td {
      border-bottom: 1px solid var(--el-border-color-lighter);
      padding: 10px 12px;
      text-align: left;
      vertical-align: middle;
    }

    th {
      font-size: var(--font-size-sm);
      color: var(--el-text-color-secondary);
      font-weight: 600;
      background: var(--el-fill-color-light);
    }
  }

  .field-name {
    font-weight: 600;
    color: var(--el-text-color-primary);
  }

  .target-cell {
    width: 240px;
    min-width: 240px;
  }

  .target-cell .el-select,
  .target-cell .el-select__wrapper {
    width: 240px;
  }

  .preview-cell {
    font-size: var(--font-size-sm);
    color: var(--el-text-color-regular);
    min-width: 140px;
  }

  .mapping-actions {
    display: flex;
    align-items: center;
    gap: 12px;
    flex-wrap: wrap;
    margin-top: 16px;
  }

  .needs-confirm {
    font-size: var(--font-size-sm);
    color: var(--color-warning-600);
  }
}
</style>
