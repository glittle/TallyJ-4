<script setup lang="ts">
import type { ImportFileInfo } from "@/types";
import type { UploadFile } from "element-plus";
import { InfoFilled, UploadFilled } from "@element-plus/icons-vue";

const props = defineProps<{
  files: ImportFileInfo[];
  selectedFile: ImportFileInfo | null;
  uploading: boolean;
  reparsing: number | null;
  uploadKey: number;
}>();

const emit = defineEmits<{
  change: [file: UploadFile];
  select: [file: ImportFileInfo];
  reparse: [file: ImportFileInfo];
  delete: [file: ImportFileInfo];
  "update-settings": [file: ImportFileInfo];
}>();

function getStatusType(status: string | null): string {
  switch (status) {
    case "Imported":
      return "success";
    case "Mapped":
      return "primary";
    case "Processing":
      return "warning";
    case "Failed":
      return "danger";
    default:
      return "info";
  }
}

function formatFileSize(bytes: number): string {
  if (bytes === 0) {
    return "0 B";
  }
  const k = 1024;
  const sizes = ["B", "KB", "MB", "GB"];
  const i = Math.floor(Math.log(bytes) / Math.log(k));
  return (
    Number.parseFloat((bytes / Math.pow(k, i)).toFixed(1)) + " " + sizes[i]
  );
}

function isSelected(file: ImportFileInfo): boolean {
  return props.selectedFile?.rowId === file.rowId;
}

function rowClassName({ row }: { row: ImportFileInfo }): string {
  return isSelected(row) ? "is-selected-file" : "is-selectable-file";
}

function updateFirstDataRow(value: number | undefined) {
  if (!props.selectedFile) {
    return;
  }
  emit("update-settings", {
    ...props.selectedFile,
    firstDataRow: value ?? null,
  });
}

function updateCodePage(value: number | undefined) {
  if (!props.selectedFile) {
    return;
  }
  emit("update-settings", {
    ...props.selectedFile,
    codePage: value ?? null,
  });
}
</script>

<template>
  <div class="people-import-upload-step">
    <el-upload
      :key="uploadKey"
      :auto-upload="false"
      :show-file-list="false"
      :on-change="(f: UploadFile) => emit('change', f)"
      accept=".csv,.tsv,.tab,.txt,.xlsx"
      drag
      :disabled="uploading"
    >
      <el-icon class="el-icon--upload"><UploadFilled /></el-icon>
      <div class="el-upload__text">
        {{ $t("people.import.dropFile") }}
        <em>{{ $t("people.import.clickToUpload") }}</em>
      </div>
      <template #tip>
        <div class="el-upload__tip">
          {{ $t("people.import.supportedFormats") }}
        </div>
      </template>
    </el-upload>

    <div v-if="files.length > 0" class="files-section">
      <div class="files-heading">
        <h4>{{ $t("people.import.filesOnServer") }}</h4>
        <span class="files-hint">{{
          $t("people.import.rowClickToSelect")
        }}</span>
      </div>
      <el-table
        :data="files"
        stripe
        row-key="rowId"
        highlight-current-row
        :current-row-key="selectedFile?.rowId"
        :row-class-name="rowClassName"
        style="width: 100%"
        @row-click="(row: ImportFileInfo) => emit('select', row)"
      >
        <el-table-column
          prop="originalFileName"
          :label="$t('people.import.fileName')"
          min-width="180"
        />
        <el-table-column
          prop="processingStatus"
          :label="$t('people.import.status')"
          width="120"
        >
          <template #default="scope">
            <el-tag
              :type="getStatusType(scope.row.processingStatus)"
              size="small"
            >
              {{
                scope.row.processingStatus || $t("people.import.statusUploaded")
              }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column
          prop="uploadTime"
          :label="$t('people.import.uploadTime')"
          width="180"
        >
          <template #default="scope">
            {{
              scope.row.uploadTime
                ? new Date(scope.row.uploadTime).toLocaleString()
                : "-"
            }}
          </template>
        </el-table-column>
        <el-table-column
          prop="fileSize"
          :label="$t('people.import.size')"
          width="90"
        >
          <template #default="scope">
            {{ scope.row.fileSize ? formatFileSize(scope.row.fileSize) : "-" }}
          </template>
        </el-table-column>
        <el-table-column :label="$t('common.actions')" width="90" align="right">
          <template #default="scope">
            <el-button
              type="danger"
              link
              @click.stop="emit('delete', scope.row)"
            >
              {{ $t("common.delete") }}
            </el-button>
          </template>
        </el-table-column>
      </el-table>

      <div v-if="selectedFile" class="file-settings">
        <div class="file-settings-title">
          {{ $t("people.import.selectedFile") }}:
          <strong>{{ selectedFile.originalFileName }}</strong>
        </div>
        <div class="file-settings-controls">
          <div class="setting">
            <el-tooltip
              :content="$t('people.import.headersOnLineTooltip')"
              placement="top"
            >
              <span class="setting-label">
                {{ $t("people.import.headersOnLine") }}
                <el-icon><InfoFilled /></el-icon>
              </span>
            </el-tooltip>
            <el-input-number
              :model-value="selectedFile.firstDataRow"
              :min="1"
              :max="10"
              size="small"
              :disabled="selectedFile.processingStatus === 'Imported'"
              @update:model-value="updateFirstDataRow"
            />
          </div>
          <div class="setting">
            <span class="setting-label">{{
              $t("people.import.contentEncoding")
            }}</span>
            <el-select
              v-if="selectedFile.fileType !== 'xlsx'"
              :model-value="selectedFile.codePage"
              size="small"
              :disabled="selectedFile.processingStatus === 'Imported'"
              style="width: 160px"
              @update:model-value="updateCodePage"
            >
              <el-option label="UTF-8" :value="65001" />
              <el-option label="Windows-1252" :value="1252" />
              <el-option label="ISO-8859-1" :value="28591" />
            </el-select>
            <span v-else class="encoding-text">UTF-8 (Excel)</span>
          </div>
          <el-button
            size="small"
            :loading="reparsing === selectedFile.rowId"
            @click="emit('reparse', selectedFile)"
          >
            {{ $t("people.import.reparse") }}
          </el-button>
        </div>
      </div>
    </div>
  </div>
</template>

<style lang="less">
.people-import-upload-step {
  .el-upload {
    width: 100%;
  }

  .el-upload-dragger {
    padding: 24px 16px;
    width: 100%;
  }

  .files-section {
    margin-top: 28px;
  }

  .files-heading {
    display: flex;
    align-items: baseline;
    gap: 12px;
    margin-bottom: 12px;

    h4 {
      margin: 0;
      color: var(--el-text-color-primary);
    }

    .files-hint {
      font-size: var(--font-size-sm);
      color: var(--el-text-color-secondary);
    }
  }

  .el-table .is-selectable-file {
    cursor: pointer;
  }

  .el-table .is-selected-file > td.el-table__cell {
    background-color: var(--el-color-primary-light-9);
  }

  .file-settings {
    margin-top: 16px;
    padding: 14px 16px;
    border: 1px solid var(--el-border-color-lighter);
    border-radius: 8px;
    background: var(--el-fill-color-blank);
  }

  .file-settings-title {
    margin-bottom: 12px;
    color: var(--el-text-color-regular);
  }

  .file-settings-controls {
    display: flex;
    flex-wrap: wrap;
    align-items: center;
    gap: 16px 24px;
  }

  .setting {
    display: flex;
    align-items: center;
    gap: 8px;
  }

  .setting-label {
    display: inline-flex;
    align-items: center;
    gap: 4px;
    font-size: var(--font-size-sm);
    color: var(--el-text-color-regular);
  }

  .encoding-text {
    font-size: var(--font-size-sm);
    color: var(--el-text-color-placeholder);
  }
}
</style>
