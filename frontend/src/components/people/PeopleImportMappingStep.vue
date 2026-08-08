<script setup lang="ts">
import type { ColumnMapping } from "@/types";
import { PEOPLE_TARGET_FIELDS } from "@/types";
import { useI18n } from "vue-i18n";
import { computed } from "vue";

const props = defineProps<{
  parsedResult: {
    headers: string[];
    previewRows?: string[][];
    totalDataRows?: number;
  } | null;
  columnMappings: ColumnMapping[];
  savingMapping: boolean;
  availableTargetFields: { value: string | null; label: string }[];
}>();

const emit = defineEmits<{
  save: [];
}>();

const { t } = useI18n();

const previewRows = computed(() => {
  if (!props.parsedResult?.previewRows) {
    return [];
  }
  return props.parsedResult.previewRows.slice(0, 5);
});

function getFieldDescription(fieldValue: string): string {
  const descriptions: Record<string, string> = {
    FirstName: t("people.import.firstNameDesc"),
    LastName: t("people.import.lastNameDesc"),
    BahaiId: t("people.import.bahaiIdDesc"),
    IneligibleReasonDescription: t("people.import.eligibilityDesc"),
    Area: t("people.import.areaDesc"),
    Email: t("people.import.emailDesc"),
    Phone: t("people.import.phoneDesc"),
    OtherNames: t("people.import.otherNamesDesc"),
    OtherLastNames: t("people.import.otherLastNamesDesc"),
    OtherInfo: t("people.import.otherInfoDesc"),
  };
  return descriptions[fieldValue] || "";
}
</script>

<template>
  <div class="people-import-mapping-step">
    <h3>{{ $t("people.import.mapColumns") }}</h3>
    <p>{{ $t("people.import.mapColumnsDesc") }}</p>

    <div
      v-if="parsedResult && parsedResult.headers.length > 0"
      class="column-mapping"
    >
      <div class="mapping-table-container">
        <table class="mapping-table">
          <thead>
            <tr>
              <th class="target-header">
                {{ $t("people.import.tallyJField") }}
              </th>
              <th
                v-for="header in parsedResult.headers"
                :key="header"
                class="file-header"
              >
                {{ header }}
              </th>
            </tr>
          </thead>
          <tbody>
            <!-- Mapping row -->
            <tr class="mapping-row">
              <td class="target-cell">{{ $t("people.import.mapTo") }}</td>
              <td
                v-for="(header, index) in parsedResult.headers"
                :key="`mapping-${index}`"
                class="mapping-cell"
              >
                <el-select
                  v-if="columnMappings[index]"
                  v-model="columnMappings[index]!.targetField"
                  size="small"
                  clearable
                  :placeholder="$t('people.import.ignore')"
                >
                  <el-option
                    v-for="field in availableTargetFields"
                    :key="field.value"
                    :label="field.label"
                    :value="field.value"
                  />
                </el-select>
              </td>
            </tr>
            <!-- Preview rows -->
            <tr
              v-for="(row, rowIndex) in previewRows"
              :key="`preview-${rowIndex}`"
              class="preview-row"
            >
              <td class="target-cell preview-label">
                {{ $t("people.import.preview") }} {{ rowIndex + 1 }}
              </td>
              <td
                v-for="(cell, cellIndex) in row"
                :key="`cell-${rowIndex}-${cellIndex}`"
                class="preview-cell"
              >
                {{ cell }}
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <div class="mapping-actions">
        <el-button
          type="primary"
          :loading="savingMapping"
          @click="emit('save')"
        >
          {{ $t("people.import.saveMapping") }}
        </el-button>
      </div>
    </div>

    <!-- Reference sections -->
    <el-collapse class="reference-sections">
      <el-collapse-item :title="$t('people.import.tallyJFields')" name="fields">
        <div class="field-reference">
          <div
            v-for="field in PEOPLE_TARGET_FIELDS"
            :key="field.value"
            class="field-item"
          >
            <strong>{{ field.label }}</strong>
            <span v-if="field.required" class="required-mark">*</span>
            <span class="field-desc">{{
              getFieldDescription(field.value)
            }}</span>
          </div>
        </div>
      </el-collapse-item>
      <el-collapse-item
        :title="$t('people.import.eligibilityValues')"
        name="eligibility"
      >
        <div class="eligibility-reference">
          <p>{{ $t("people.import.eligibilityDesc") }}</p>
          <ul>
            <li>
              <strong>Eligible</strong> -
              {{ $t("people.import.eligibleDesc") }}
            </li>
            <li>
              <strong>Ineligible</strong> -
              {{ $t("people.import.ineligibleDesc") }}
            </li>
            <li>
              <strong>Under Age</strong> -
              {{ $t("people.import.underAgeDesc") }}
            </li>
            <li>
              <strong>Duplicate</strong> -
              {{ $t("people.import.duplicateDesc") }}
            </li>
          </ul>
        </div>
      </el-collapse-item>
    </el-collapse>
  </div>
</template>

<style lang="less">
.people-import-mapping-step {
  h3 {
    margin-bottom: 10px;
  }
  p {
    margin-bottom: 20px;
    color: var(--el-text-color-secondary);
  }
  .column-mapping {
    margin-bottom: 30px;
  }
  .mapping-table-container {
    overflow-x: auto;
    margin-bottom: 20px;
  }
  .mapping-table {
    width: 100%;
    border-collapse: collapse;
    border: 1px solid #ebeef5;
    th,
    td {
      border: 1px solid #ebeef5;
      padding: 8px 12px;
      text-align: left;
    }
    .target-header,
    .file-header {
      background-color: var(--el-color-primary);
      color: var(--color-text-inverse);
      font-weight: bold;
      min-width: 120px;
    }
    .target-header {
      min-width: 150px;
    }
    .target-cell {
      background-color: var(--el-fill-color-light);
      font-weight: bold;
    }
    .mapping-row .target-cell {
      background-color: var(--el-color-primary);
      color: var(--color-text-inverse);
    }
    .mapping-cell {
      background-color: var(--el-bg-color);
    }
    .preview-row {
      background-color: var(--el-fill-color-lighter);
    }
    .preview-cell {
      font-size: 12px;
      color: var(--el-text-color-secondary);
    }
    .preview-label {
      font-size: 12px;
      color: var(--el-text-color-placeholder);
    }
    .mapping-row .el-select {
      width: 100%;
    }
  }
  .mapping-actions {
    text-align: center;
    margin-top: 20px;
  }
  .reference-sections {
    margin-top: 30px;
  }
  .field-reference .field-item {
    margin-bottom: 10px;
    padding: 8px;
    background-color: var(--el-fill-color-light);
    border-radius: 4px;
    strong {
      display: block;
      color: var(--el-text-color-primary);
    }
    .required-mark {
      color: var(--el-color-error);
      margin-left: 4px;
    }
    .field-desc {
      display: block;
      font-size: 12px;
      color: var(--el-text-color-secondary);
      margin-top: 4px;
    }
  }
  .eligibility-reference ul {
    padding-left: 20px;
    li {
      margin-bottom: 8px;
    }
  }
}
</style>
