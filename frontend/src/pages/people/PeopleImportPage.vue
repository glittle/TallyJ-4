<script setup lang="ts">
import { ref, computed, onMounted, onBeforeUnmount } from "vue";
import { useRoute } from "vue-router";
import { useI18n } from "vue-i18n";
import { ElMessageBox } from "element-plus";
import { useNotifications } from "../../composables/useNotifications";
import type { UploadFile } from "element-plus";
import { peopleImportService } from "../../services/peopleImportService";
import { signalrService } from "../../services/signalrService";
import type {
  ImportFileInfo,
  ColumnMapping,
  ImportPeopleResult,
} from "../../types";
import { PEOPLE_TARGET_FIELDS } from "../../types";
import type {
  PeopleImportProgressEvent,
  PeopleImportCompleteEvent,
} from "../../types/SignalREvents";

import PeopleImportUploadStep from "@/components/people/PeopleImportUploadStep.vue";
import PeopleImportMappingStep from "@/components/people/PeopleImportMappingStep.vue";
import PeopleImportExecuteStep from "@/components/people/PeopleImportExecuteStep.vue";
import { useApiErrorHandler } from "@/composables/useApiErrorHandler";
const { handleApiError } = useApiErrorHandler();

const route = useRoute();
const { t } = useI18n();
const { showErrorMessage, showSuccessMessage } = useNotifications();

const electionGuid = route.params.id as string;
const currentStep = ref(0);
const uploading = ref(false);
const reparsing = ref<number | null>(null);
const savingMapping = ref(false);

const files = ref<ImportFileInfo[]>([]);
const selectedFile = ref<ImportFileInfo | null>(null);
const parsedResult = ref<import("../../types").ParseFileResult | null>(null);
const columnMappings = ref<ColumnMapping[]>([]);
const importing = ref(false);
const importResult = ref<ImportPeopleResult | null>(null);
const peopleCount = ref(0);
const importProgress = ref<PeopleImportProgressEvent | null>(null);
const showDeleteAllConfirm = ref(false);

const availableTargetFields = computed(() => [
  { value: null, label: t("people.import.ignore") },
  ...PEOPLE_TARGET_FIELDS,
]);

const canProceedToNext = computed(() => {
  if (currentStep.value === 0) {
    return selectedFile.value !== null;
  }
  if (currentStep.value === 1) {
    const firstNameMapped = columnMappings.value.some(
      (m) => m?.targetField === "FirstName",
    );
    const lastNameMapped = columnMappings.value.some(
      (m) => m?.targetField === "LastName",
    );
    return firstNameMapped && lastNameMapped;
  }
  return true;
});

const isMappingValid = computed(() => {
  const firstNameMapped = columnMappings.value.some(
    (m) => m.targetField === "FirstName",
  );
  const lastNameMapped = columnMappings.value.some(
    (m) => m?.targetField === "LastName",
  );
  return firstNameMapped && lastNameMapped;
});

const canDeleteAllPeople = computed(() => true);

const translatedErrors = computed(() => {
  if (!importResult.value?.errors) {
    return [];
  }
  return importResult.value.errors.map((error) => ({
    ...error,
    message: t(error.key, error.parameters),
  }));
});

const translatedWarnings = computed(() => {
  if (!importResult.value?.warnings) {
    return [];
  }
  return importResult.value.warnings.map((warning) => ({
    ...warning,
    message: t(warning.key, warning.parameters),
  }));
});

async function loadFiles() {
  try {
    files.value = await peopleImportService.getFiles(electionGuid);
  } catch (error) {
    handleApiError(error);
  }
}

async function loadPeopleCount() {
  try {
    const result = await peopleImportService.getPeopleCount(electionGuid);
    peopleCount.value = result.count;
  } catch (error) {
    handleApiError(error);
  }
}

async function initializeSignalR() {
  try {
    const connection = await signalrService.connectToPeopleImportHub();

    connection.on("importProgress", (data: PeopleImportProgressEvent) => {
      importProgress.value = data;
    });

    connection.on("importError", (msg: string) => {
      showErrorMessage(msg);
    });

    connection.on("importComplete", (data: PeopleImportCompleteEvent) => {
      importing.value = false;
      importProgress.value = null;
      if (data.success) {
        showSuccessMessage(
          `Import completed: ${data.peopleAdded} people added, ${data.peopleSkipped} skipped`,
        );
      } else {
        showErrorMessage(
          "Import failed - " + translatedErrors.value.length + " errors",
        );
      }
    });
  } catch (e) {
    handleApiError(e);
  }
}

onMounted(async () => {
  Promise.all([initializeSignalR(), loadFiles(), loadPeopleCount()]);
  try {
    await signalrService.joinPeopleImportSession(electionGuid);
  } catch (e) {
    handleApiError(e);
  }
});

onBeforeUnmount(async () => {
  try {
    await signalrService.leavePeopleImportSession(electionGuid);
  } catch (e) {
    handleApiError(e);
  }
});

async function handleFileChange(file: UploadFile) {
  if (file.raw) {
    uploading.value = true;
    try {
      const uploadedFile = await peopleImportService.uploadFile(
        electionGuid,
        file.raw,
      );
      await loadFiles();

      // Show message if headers were detected at a non-standard row (row 2 or higher)
      if (
        uploadedFile.firstDataRow &&
        uploadedFile.firstDataRow >= 2 &&
        uploadedFile.fileType === "xlsx"
      ) {
        showSuccessMessage(
          t("people.import.headerAutoDetected", {
            row: uploadedFile.firstDataRow,
          }),
        );
      } else {
        showSuccessMessage(t("people.import.fileUploadedSuccessfully"));
      }
    } catch (error) {
      console.error("Upload failed:", error);
      showErrorMessage(t("people.import.uploadError"));
    } finally {
      uploading.value = false;
    }
  }
}

function handleUploadSuccess() {
  // Handled by handleFileChange
}

function handleUploadError() {
  uploading.value = false;
  showErrorMessage(t("people.import.uploadError"));
}

async function parseFile(codePage?: number, firstDataRow?: number) {
  if (!selectedFile.value) {
    return;
  }

  try {
    const result = await peopleImportService.parseFile(
      electionGuid,
      selectedFile.value.rowId,
      codePage,
      firstDataRow,
    );
    parsedResult.value = result;
    columnMappings.value = result.autoMappings || [];
  } catch (error) {
    handleApiError(error);
    throw error;
  }
}

async function selectFile(file: ImportFileInfo) {
  selectedFile.value = file;
  parsedResult.value = null;
  columnMappings.value = [];
  importResult.value = null;
  importProgress.value = null;

  // First try to load saved mappings
  try {
    const savedMappings = await peopleImportService.getMapping(
      electionGuid,
      file.rowId,
    );
    if (savedMappings && savedMappings.length > 0) {
      // If we have saved mappings, parse the file to get headers and preview, but use saved mappings
      await parseFile();
      columnMappings.value = savedMappings;
      return;
    }
  } catch (error) {
    console.warn(
      "Failed to load saved mappings, falling back to auto-mapping:",
      error,
    );
  }

  // Fall back to parsing file with auto-mappings
  await parseFile();
}

async function reparseFile(file: ImportFileInfo) {
  reparsing.value = file.rowId;
  try {
    selectedFile.value = file;
    parsedResult.value = null;
    columnMappings.value = [];
    importResult.value = null;
    importProgress.value = null;
    await parseFile(file.codePage || undefined, file.firstDataRow || undefined);
  } catch (error) {
    handleApiError(error);
  } finally {
    reparsing.value = null;
  }
}

async function updateFileSettings(file: ImportFileInfo) {
  try {
    const updatedFile = await peopleImportService.updateSettings(
      electionGuid,
      file.rowId,
      {
        firstDataRow: file.firstDataRow ?? undefined,
        codePage: file.codePage ?? undefined,
      },
    );
    const index = files.value.findIndex((f) => f.rowId === updatedFile.rowId);
    if (index !== -1) {
      files.value[index] = updatedFile;
    }
    if (selectedFile.value?.rowId === updatedFile.rowId) {
      selectedFile.value = updatedFile;
    }
    await parseFile(
      updatedFile.codePage || undefined,
      updatedFile.firstDataRow || undefined,
    );
  } catch (error) {
    handleApiError(error);
  }
}

async function deleteFile(file: ImportFileInfo) {
  try {
    await ElMessageBox.confirm(
      t("people.import.confirmDeleteFile"),
      t("common.warning"),
      {
        confirmButtonText: t("common.delete"),
        cancelButtonText: t("common.cancel"),
        type: "warning",
      },
    );

    await peopleImportService.deleteFile(electionGuid, file.rowId);
    files.value = files.value.filter((f) => f.rowId !== file.rowId);
    if (selectedFile.value?.rowId === file.rowId) {
      selectedFile.value = null;
      parsedResult.value = null;
      columnMappings.value = [];
    }
    showSuccessMessage("File deleted");
  } catch (error: any) {
    if (error !== "cancel") {
      showErrorMessage(error.message || t("people.import.deleteFileError"));
    }
  }
}

async function saveMapping() {
  if (!selectedFile.value) {
    return;
  }

  savingMapping.value = true;
  try {
    await peopleImportService.saveMapping(
      electionGuid,
      selectedFile.value.rowId,
      columnMappings.value,
    );
    showSuccessMessage(t("people.import.mappingSaved"));
  } catch (error) {
    handleApiError(error);
  } finally {
    savingMapping.value = false;
  }
}

async function handleNext() {
  if (currentStep.value === 0 && selectedFile.value) {
    await parseFile();
  }
  currentStep.value++;
}

async function executeImport() {
  if (!selectedFile.value) {
    return;
  }

  importing.value = true;
  importResult.value = null;
  importProgress.value = null;

  try {
    const result = await peopleImportService.executeImport(
      electionGuid,
      selectedFile.value.rowId,
    );
    importResult.value = result;
    if (result.success) {
      showSuccessMessage(
        `Import completed: ${result.peopleAdded} people added, ${result.peopleSkipped} skipped`,
      );
      await loadPeopleCount();
    } else {
      showErrorMessage("Import failed: " + result.errors.length);
    }
  } catch (error) {
    console.error("Import failed:", error);
    showErrorMessage("Failed to execute import");
  } finally {
    importing.value = false;
  }
}

async function confirmDeleteAllPeople() {
  try {
    await ElMessageBox.confirm(
      t("people.import.confirmDeleteAllPeople"),
      t("common.warning"),
      {
        confirmButtonText: t("common.delete"),
        cancelButtonText: t("common.cancel"),
        type: "warning",
      },
    );

    const result = await peopleImportService.deleteAllPeople(electionGuid);
    showSuccessMessage(`${result.deletedCount} people deleted`);
    await loadPeopleCount();
    showDeleteAllConfirm.value = false;
  } catch (error) {
    if (error !== "cancel") {
      handleApiError(error);
    }
    showDeleteAllConfirm.value = false;
  }
}
</script>

<template>
  <div class="people-import-page">
    <el-card>
      <el-steps :active="currentStep" finish-status="success" align-center>
        <el-step :title="$t('people.import.step1')" />
        <el-step :title="$t('people.import.step2')" />
        <el-step :title="$t('people.import.step3')" />
      </el-steps>

      <div class="step-content">
        <PeopleImportUploadStep
          v-if="currentStep === 0"
          :files="files"
          :selected-file="selectedFile"
          :uploading="uploading"
          :reparsing="reparsing"
          @change="handleFileChange"
          @success="handleUploadSuccess"
          @error="handleUploadError"
          @select="selectFile"
          @reparse="reparseFile"
          @delete="deleteFile"
          @update-settings="updateFileSettings"
        />

        <PeopleImportMappingStep
          v-if="currentStep === 1"
          :parsed-result="parsedResult"
          :column-mappings="columnMappings"
          :saving-mapping="savingMapping"
          :available-target-fields="availableTargetFields"
          @save="saveMapping"
        />

        <PeopleImportExecuteStep
          v-if="currentStep === 2"
          :parsed-result="parsedResult"
          :column-mappings="columnMappings"
          :is-mapping-valid="isMappingValid"
          :selected-file="selectedFile"
          :importing="importing"
          :import-progress="importProgress"
          :import-result="importResult"
          :translated-errors="translatedErrors"
          :translated-warnings="translatedWarnings"
          :people-count="peopleCount"
          :can-delete-all-people="canDeleteAllPeople"
          @import="executeImport"
          @delete-all="showDeleteAllConfirm = true"
        />
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
      </div>
    </el-card>

    <el-dialog
      v-model="showDeleteAllConfirm"
      :title="$t('people.import.confirmDeleteAllPeople')"
      width="500px"
    >
      <p>{{ $t("people.import.deleteAllPeopleMessage") }}</p>
      <p class="warning-text">{{ $t("common.actionIrreversible") }}</p>
      <template #footer>
        <el-button @click="showDeleteAllConfirm = false">
          {{ $t("common.cancel") }}
        </el-button>
        <el-button type="danger" @click="confirmDeleteAllPeople">
          {{ $t("common.delete") }}
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<style lang="less">
.people-import-page {
  max-width: 1400px;
  margin: 0 auto;

  .el-steps {
    margin: 20px 0;
  }

  .step-content {
    min-height: 400px;
    margin: 30px 0;
  }

  .step-actions {
    text-align: center;
    margin-top: 30px;
  }

  .warning-text {
    color: var(--el-color-danger);
    font-weight: 500;
  }
}
</style>
