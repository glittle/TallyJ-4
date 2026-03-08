<template>
  <div class="ballot-import-page">
    <el-card>
      <template #header>
        <div class="card-header">
          <el-page-header
            :content="$t('ballots.import.title')"
            @back="goBack"
          />
        </div>
      </template>

      <el-steps :active="currentStep" finish-status="success" align-center>
        <el-step :title="$t('ballots.import.step1')" />
        <el-step :title="$t('ballots.import.step2')" />
        <el-step :title="$t('ballots.import.step3')" />
      </el-steps>

      <div class="step-content">
        <div v-if="currentStep === 0" class="upload-step">
          <h3>{{ $t("ballots.import.uploadFile") }}</h3>
          <el-upload
            ref="uploadRef"
            :auto-upload="false"
            :limit="1"
            :on-change="handleFileChange"
            :on-remove="handleFileRemove"
            accept=".csv"
            drag
          >
            <el-icon class="el-icon--upload"><upload-filled /></el-icon>
            <div class="el-upload__text">
              {{ $t("ballots.import.dropFile") }}
              <em>{{ $t("ballots.import.clickToUpload") }}</em>
            </div>
            <template #tip>
              <div class="el-upload__tip">
                {{ $t("ballots.import.csvOnly") }}
              </div>
            </template>
          </el-upload>

          <el-form
            v-if="fileContent"
            :model="uploadConfig"
            label-width="150px"
            class="upload-config"
          >
            <el-form-item :label="$t('ballots.import.delimiter')">
              <el-select v-model="uploadConfig.delimiter">
                <el-option label="Comma (,)" value="," />
                <el-option label="Semicolon (;)" value=";" />
                <el-option label="Tab" value="\t" />
              </el-select>
            </el-form-item>
            <el-form-item :label="$t('ballots.import.hasHeaderRow')">
              <el-switch v-model="uploadConfig.hasHeaderRow" />
            </el-form-item>
            <el-form-item :label="$t('ballots.import.firstDataRow')">
              <el-input-number
                v-model="uploadConfig.firstDataRow"
                :min="1"
                :max="10"
              />
            </el-form-item>
            <el-form-item :label="$t('ballots.import.skipInvalidRows')">
              <el-switch v-model="uploadConfig.skipInvalidRows" />
            </el-form-item>
          </el-form>
        </div>

        <div v-if="currentStep === 1" class="mapping-step">
          <h3>{{ $t("ballots.import.mapFields") }}</h3>
          <p>{{ $t("ballots.import.mapFieldsDesc") }}</p>

          <div
            v-if="parsedHeaders.headers.length > 0"
            class="field-mapping-form"
          >
            <div
              v-for="targetField in targetFields"
              :key="targetField.value"
              class="mapping-row"
            >
              <span class="target-field">{{ targetField.label }}</span>
              <el-select
                v-model="fieldMappings[targetField.value]"
                :placeholder="$t('ballots.import.selectColumn')"
                clearable
              >
                <el-option
                  v-for="header in parsedHeaders.headers"
                  :key="header"
                  :label="header"
                  :value="header"
                />
              </el-select>
            </div>
          </div>

          <div v-if="parsedHeaders.previewRows.length > 0" class="preview-data">
            <h4>{{ $t("ballots.import.dataPreview") }}</h4>
            <el-table :data="previewTableData" border>
              <el-table-column
                v-for="(header, index) in parsedHeaders.headers"
                :key="index"
                :prop="`col${index}`"
                :label="header"
              />
            </el-table>
          </div>
        </div>

        <div v-if="currentStep === 2" class="review-step">
          <h3>{{ $t("ballots.import.reviewImport") }}</h3>
          <el-descriptions :column="2" border>
            <el-descriptions-item :label="$t('ballots.import.totalRows')">
              {{ parsedHeaders.totalRows }}
            </el-descriptions-item>
            <el-descriptions-item :label="$t('ballots.import.delimiter')">
              {{ getDelimiterLabel(uploadConfig.delimiter) }}
            </el-descriptions-item>
            <el-descriptions-item :label="$t('ballots.import.hasHeaderRow')">
              {{
                uploadConfig.hasHeaderRow ? $t("common.yes") : $t("common.no")
              }}
            </el-descriptions-item>
            <el-descriptions-item :label="$t('ballots.import.firstDataRow')">
              {{ uploadConfig.firstDataRow }}
            </el-descriptions-item>
          </el-descriptions>

          <h4 style="margin-top: 20px">
            {{ $t("ballots.import.fieldMappings") }}
          </h4>
          <el-table :data="mappingsTableData" border>
            <el-table-column
              prop="target"
              :label="$t('ballots.import.targetField')"
            />
            <el-table-column
              prop="source"
              :label="$t('ballots.import.sourceColumn')"
            />
          </el-table>
        </div>
      </div>

      <div class="step-actions">
        <el-button v-if="currentStep > 0" @click="currentStep--">
          {{ $t("common.previous") }}
        </el-button>
        <el-button
          v-if="currentStep < 2"
          type="primary"
          :disabled="!canProceedToNext"
          @click="handleNext"
        >
          {{ $t("common.next") }}
        </el-button>
        <el-button
          v-if="currentStep === 2"
          type="success"
          :loading="importing"
          @click="handleImport"
        >
          {{ $t("ballots.import.startImport") }}
        </el-button>
      </div>
    </el-card>

    <ImportProgressDialog v-model="showProgressDialog" />
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onBeforeUnmount } from "vue";
import { useRouter, useRoute } from "vue-router";
import { useI18n } from "vue-i18n";
import { useNotifications } from "@/composables/useNotifications";
import { UploadFilled } from "@element-plus/icons-vue";
import type { UploadFile } from "element-plus";
import { importService } from "../../services/importService";
import { useImportStore } from "../../stores/importStore";
import ImportProgressDialog from "../../components/common/ImportProgressDialog.vue";
import type {
  ParseCsvHeadersResponse,
  FieldMapping,
  ImportConfiguration,
} from "../../types";
import { IMPORT_TARGET_FIELDS } from "../../types";

const router = useRouter();
const route = useRoute();
const { t } = useI18n();
const importStore = useImportStore();
const { showSuccessMessage, showErrorMessage } = useNotifications();

const electionGuid = route.params.id as string;
const currentStep = ref(0);
const fileContent = ref<string>("");
const parsedHeaders = ref<ParseCsvHeadersResponse>({
  headers: [],
  previewRows: [],
  totalRows: 0,
});
const uploadConfig = ref({
  delimiter: ",",
  hasHeaderRow: true,
  firstDataRow: 2,
  skipInvalidRows: true,
});
const fieldMappings = ref<Record<string, string>>({});
const importing = ref(false);
const showProgressDialog = ref(false);

const targetFields = IMPORT_TARGET_FIELDS;

const canProceedToNext = computed(() => {
  if (currentStep.value === 0) {
    return fileContent.value !== "";
  }
  if (currentStep.value === 1) {
    return fieldMappings.value["BallotCode"] && fieldMappings.value["Votes"];
  }
  return true;
});

const previewTableData = computed(() => {
  return parsedHeaders.value.previewRows.map((row) => {
    const obj: Record<string, string> = {};
    row.forEach((cell, index) => {
      obj[`col${index}`] = cell;
    });
    return obj;
  });
});

const mappingsTableData = computed(() => {
  return Object.entries(fieldMappings.value)
    .filter(([_, source]) => source)
    .map(([target, source]) => ({
      target: targetFields.find((f) => f.value === target)?.label || target,
      source,
    }));
});

onMounted(async () => {
  await importStore.initializeSignalR();
  await importStore.joinImportSession(electionGuid);
});

onBeforeUnmount(async () => {
  await importStore.leaveImportSession(electionGuid);
});

function goBack() {
  router.push(`/elections/${electionGuid}/ballots`);
}

async function handleFileChange(file: UploadFile) {
  if (file.raw) {
    const reader = new FileReader();
    reader.onload = async (e) => {
      fileContent.value = e.target?.result as string;
      await parseHeaders();
    };
    reader.readAsText(file.raw);
  }
}

function handleFileRemove() {
  fileContent.value = "";
  parsedHeaders.value = { headers: [], previewRows: [], totalRows: 0 };
  fieldMappings.value = {};
}

async function parseHeaders() {
  try {
    const result = await importService.parseCsvHeaders({
      csvContent: fileContent.value,
      delimiter: uploadConfig.value.delimiter,
    });
    parsedHeaders.value = result;

    if (result.headers.length > 0) {
      fieldMappings.value["BallotCode"] =
        result.headers.find(
          (h) =>
            h.toLowerCase().includes("ballot") ||
            h.toLowerCase().includes("code"),
        ) || "";
      fieldMappings.value["Votes"] =
        result.headers.find(
          (h) =>
            h.toLowerCase().includes("vote") ||
            h.toLowerCase().includes("name"),
        ) || "";
      fieldMappings.value["Teller1"] =
        result.headers.find(
          (h) => h.toLowerCase().includes("teller") && h.includes("1"),
        ) || "";
      fieldMappings.value["Teller2"] =
        result.headers.find(
          (h) => h.toLowerCase().includes("teller") && h.includes("2"),
        ) || "";
    }
  } catch (_error: any) {
    showErrorMessage(t("ballots.import.parseError"));
  }
}

async function handleNext() {
  if (currentStep.value === 0) {
    await parseHeaders();
  }
  currentStep.value++;
}

async function handleImport() {
  try {
    importing.value = true;
    showProgressDialog.value = true;

    const mappings: FieldMapping[] = Object.entries(fieldMappings.value)
      .filter(([_, source]) => source)
      .map(([target, source]) => ({
        targetField: target,
        sourceColumn: source,
      }));

    const config: ImportConfiguration = {
      firstDataRow: uploadConfig.value.firstDataRow,
      hasHeaderRow: uploadConfig.value.hasHeaderRow,
      delimiter: uploadConfig.value.delimiter,
      fieldMappings: mappings,
      skipInvalidRows: uploadConfig.value.skipInvalidRows,
    };

    const result = await importService.importBallots({
      csvContent: fileContent.value,
      electionGuid,
      configuration: config,
    });

    if (result.success) {
      showSuccessMessage(t("ballots.import.success"));
      setTimeout(() => {
        router.push(`/elections/${electionGuid}/ballots`);
      }, 2000);
    } else {
      showErrorMessage(t("ballots.import.failed"));
    }
  } catch (error: any) {
    showErrorMessage(error.message || t("ballots.import.failed"));
  } finally {
    importing.value = false;
  }
}

function getDelimiterLabel(delimiter: string): string {
  const labels: Record<string, string> = {
    ",": "Comma (,)",
    ";": "Semicolon (;)",
    "\t": "Tab",
  };
  return labels[delimiter] || delimiter;
}
</script>

<style lang="less">
.ballot-import-page {
  max-width: 1200px;
  margin: 0 auto;

  .card-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
  }

  .el-steps {
    margin: 20px 0;
  }

  .step-content {
    min-height: 400px;
    margin: 30px 0;
  }

  .upload-step {
    max-width: 600px;
    margin: 0 auto;

    h3 {
      margin-bottom: 20px;
      text-align: center;
    }

    .upload-config {
      margin-top: 30px;
    }
  }

  .mapping-step {
    h3 {
      margin-bottom: 10px;
    }

    p {
      margin-bottom: 20px;
      color: var(--el-text-color-secondary);
    }

    .field-mapping-form {
      margin-bottom: 30px;
    }

    .mapping-row {
      display: flex;
      align-items: center;
      margin-bottom: 15px;
      gap: 20px;

      .target-field {
        width: 200px;
        font-weight: bold;
      }

      .el-select {
        flex: 1;
      }
    }

    .preview-data {
      margin-top: 30px;

      h4 {
        margin-bottom: 10px;
      }
    }
  }

  .review-step {
    h3 {
      margin-bottom: 20px;
    }
  }

  .step-actions {
    display: flex;
    justify-content: center;
    gap: 10px;
    margin-top: 30px;
    padding-top: 20px;
    border-top: 1px solid #eee;
  }
}
</style>
