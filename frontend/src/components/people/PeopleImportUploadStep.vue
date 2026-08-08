<script setup lang="ts">
import type { ImportFileInfo } from "@/types";
import type { UploadFile } from "element-plus";
import { InfoFilled, UploadFilled } from "@element-plus/icons-vue";

defineProps<{
  files: ImportFileInfo[];
  selectedFile: ImportFileInfo | null;
  uploading: boolean;
  reparsing: number | null;
}>();

const emit = defineEmits<{
  change: [file: UploadFile];
  success: [];
  error: [];
  select: [file: ImportFileInfo];
  reparse: [file: ImportFileInfo];
  delete: [file: ImportFileInfo];
  "update-settings": [file: ImportFileInfo];
}>();

function getStatusType(status: string | null): string {
  switch (status) {
    case "Imported":
      return "success";
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
</script>

<template>
  <div class="people-import-upload-step">
    <h3>{{ $t("people.import.uploadFile") }}</h3>
    <el-upload
      :auto-upload="false"
      :limit="1"
      :on-change="(f: UploadFile) => emit('change', f)"
      :on-success="() => emit('success')"
      :on-error="() => emit('error')"
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
      <h4>{{ $t("people.import.filesOnServer") }}</h4>
      <el-table :data="files" stripe style="width: 100%">
        <el-table-column
          prop="originalFileName"
          :label="$t('people.import.fileName')"
          width="200"
        />
        <el-table-column
          prop="processingStatus"
          :label="$t('people.import.status')"
          width="120"
        >
          <template #default="scope">
            <el-tag :type="getStatusType(scope.row.processingStatus)">
              {{ scope.row.processingStatus || "Uploaded" }}
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
        <el-table-column prop="firstDataRow" width="140">
          <template #header>
            <el-tooltip
              :content="$t('people.import.headersOnLineTooltip')"
              placement="top"
            >
              <span>
                {{ $t("people.import.headersOnLine") }}
                <el-icon style="margin-left: 4px; vertical-align: middle">
                  <InfoFilled />
                </el-icon>
              </span>
            </el-tooltip>
          </template>
          <template #default="scope">
            <el-input-number
              v-model="scope.row.firstDataRow"
              :min="1"
              :max="10"
              size="small"
              :disabled="scope.row.processingStatus === 'Imported'"
              @change="emit('update-settings', scope.row)"
            />
          </template>
        </el-table-column>
        <el-table-column
          prop="codePage"
          :label="$t('people.import.contentEncoding')"
          width="150"
        >
          <template #default="scope">
            <el-select
              v-if="scope.row.fileType !== 'xlsx'"
              v-model="scope.row.codePage"
              size="small"
              :disabled="scope.row.processingStatus === 'Imported'"
              @change="emit('update-settings', scope.row)"
            >
              <el-option label="UTF-8" :value="65001" />
              <el-option label="Windows-1252" :value="1252" />
              <el-option label="ISO-8859-1" :value="28591" />
            </el-select>
            <span v-else class="encoding-text">UTF-8 (Excel)</span>
          </template>
        </el-table-column>
        <el-table-column
          prop="fileSize"
          :label="$t('people.import.size')"
          width="100"
        >
          <template #default="scope">
            {{ scope.row.fileSize ? formatFileSize(scope.row.fileSize) : "-" }}
          </template>
        </el-table-column>
        <el-table-column :label="$t('people.import.action')" width="120">
          <template #default="scope">
            <el-button
              v-if="selectedFile?.rowId !== scope.row.rowId"
              type="primary"
              size="small"
              @click="emit('select', scope.row)"
            >
              {{ $t("people.import.select") }}
            </el-button>
            <el-tag v-else type="success">{{
              $t("people.import.selected")
            }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column :label="$t('people.import.otherActions')" width="150">
          <template #default="scope">
            <el-space>
              <el-button
                type="default"
                size="small"
                :loading="reparsing === scope.row.rowId"
                @click="emit('reparse', scope.row)"
              >
                {{ $t("people.import.reparse") }}
              </el-button>
              <el-button
                type="danger"
                size="small"
                @click="emit('delete', scope.row)"
              >
                {{ $t("common.delete") }}
              </el-button>
            </el-space>
          </template>
        </el-table-column>
      </el-table>
    </div>
  </div>
</template>

<style lang="less">
.people-import-upload-step {
  h3 {
    margin-bottom: 20px;
    text-align: center;
  }

  .files-section {
    margin-top: 40px;

    h4 {
      margin-bottom: 15px;
      color: var(--el-text-color-secondary);
    }
  }

  .encoding-text {
    font-size: 12px;
    color: var(--el-text-color-placeholder);
  }
}
</style>
