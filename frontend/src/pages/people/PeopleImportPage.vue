<script setup lang="ts">
import { ref, computed, onMounted, onBeforeUnmount, nextTick } from "vue";
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
  ParseFileResult,
} from "../../types";
import type {
  PeopleImportProgressEvent,
  PeopleImportCompleteEvent,
} from "../../types/SignalREvents";

import PeopleImportFileGuide from "@/components/people/PeopleImportFileGuide.vue";
import PeopleImportUploadStep from "@/components/people/PeopleImportUploadStep.vue";
import PeopleImportMappingStep from "@/components/people/PeopleImportMappingStep.vue";
import PeopleImportExecuteStep from "@/components/people/PeopleImportExecuteStep.vue";
import { useApiErrorHandler } from "@/composables/useApiErrorHandler";
import {
  isRequiredMappingComplete,
  mappedFieldCount,
} from "@/utils/peopleImportMapping";

const { handleApiError } = useApiErrorHandler();

const route = useRoute();
const { t } = useI18n();
const { showErrorMessage, showSuccessMessage } = useNotifications();

const electionGuid = route.params.id as string;
const uploading = ref(false);
const reparsing = ref<number | null>(null);
const savingMapping = ref(false);
const parsing = ref(false);
const uploadKey = ref(0);
const mappingSaved = ref(false);

const files = ref<ImportFileInfo[]>([]);
const selectedFile = ref<ImportFileInfo | null>(null);
const parsedResult = ref<ParseFileResult | null>(null);
const columnMappings = ref<ColumnMapping[]>([]);
const importing = ref(false);
const importResult = ref<ImportPeopleResult | null>(null);
const peopleCount = ref(0);
const importProgress = ref<PeopleImportProgressEvent | null>(null);

const isMappingValid = computed(() =>
  isRequiredMappingComplete(columnMappings.value),
);

const fileReady = computed(
  () => selectedFile.value !== null && !!parsedResult.value,
);
const mappingReady = computed(
  () => fileReady.value && mappingSaved.value && isMappingValid.value,
);

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

const pipelineFileDetail = computed(() => {
  if (!selectedFile.value) {
    return t("people.import.pipelineFileHint");
  }
  return selectedFile.value.originalFileName ?? "";
});

const pipelineMapDetail = computed(() => {
  if (!fileReady.value) {
    return t("people.import.pipelineMapHint");
  }
  if (mappingSaved.value && isMappingValid.value) {
    return t("people.import.mappedCount", {
      count: mappedFieldCount(columnMappings.value),
    });
  }
  return t("people.import.mappingNeedsConfirm");
});

const pipelineLoadDetail = computed(() => {
  if (importResult.value?.success) {
    return t("people.import.importComplete", {
      added: importResult.value.peopleAdded,
      skipped: importResult.value.peopleSkipped,
    });
  }
  if (!mappingReady.value) {
    return t("people.import.pipelineLoadHint");
  }
  return t("people.import.readyToLoad", {
    count: parsedResult.value?.totalDataRows ?? 0,
  });
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

    connection.on("importComplete", (_data: PeopleImportCompleteEvent) => {
      importing.value = false;
      importProgress.value = null;
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

function replaceFileInList(updatedFile: ImportFileInfo) {
  const index = files.value.findIndex((f) => f.rowId === updatedFile.rowId);
  if (index !== -1) {
    files.value[index] = updatedFile;
  }
  if (selectedFile.value?.rowId === updatedFile.rowId) {
    selectedFile.value = updatedFile;
  }
}

function resetWorkingState() {
  parsedResult.value = null;
  columnMappings.value = [];
  importResult.value = null;
  importProgress.value = null;
  mappingSaved.value = false;
}

async function parseFile(codePage?: number, firstDataRow?: number) {
  if (!selectedFile.value) {
    return;
  }

  parsing.value = true;
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
  } finally {
    parsing.value = false;
  }
}

async function selectFile(file: ImportFileInfo, scrollToMapping = true) {
  if (selectedFile.value?.rowId === file.rowId && parsedResult.value) {
    return;
  }

  selectedFile.value = file;
  resetWorkingState();

  try {
    const savedMappings = await peopleImportService.getMapping(
      electionGuid,
      file.rowId,
    );
    if (savedMappings && savedMappings.length > 0) {
      await parseFile();
      columnMappings.value = savedMappings;
      mappingSaved.value = isRequiredMappingComplete(savedMappings);
      if (scrollToMapping) {
        await scrollToStage(mappingSaved.value ? "load" : "map");
      }
      return;
    }
  } catch (error) {
    console.warn(
      "Failed to load saved mappings, falling back to auto-mapping:",
      error,
    );
  }

  await parseFile();
  if (scrollToMapping) {
    await scrollToStage("map");
  }
}

async function scrollToStage(stage: "map" | "load") {
  await nextTick();
  document
    .getElementById(`import-stage-${stage}`)
    ?.scrollIntoView({ behavior: "smooth", block: "start" });
}

async function handleFileChange(file: UploadFile) {
  if (!file.raw) {
    return;
  }

  uploading.value = true;
  try {
    const uploadedFile = await peopleImportService.uploadFile(
      electionGuid,
      file.raw,
    );
    await loadFiles();
    uploadKey.value += 1;

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

    const fromList =
      files.value.find((item) => item.rowId === uploadedFile.rowId) ??
      uploadedFile;
    await selectFile(fromList, true);
  } catch (error) {
    console.error("Upload failed:", error);
    showErrorMessage(t("people.import.uploadError"));
  } finally {
    uploading.value = false;
  }
}

async function reparseFile(file: ImportFileInfo) {
  reparsing.value = file.rowId;
  try {
    selectedFile.value = file;
    resetWorkingState();
    await parseFile(file.codePage || undefined, file.firstDataRow || undefined);
    await scrollToStage("map");
  } catch (error) {
    handleApiError(error);
  } finally {
    reparsing.value = null;
  }
}

async function updateFileSettings(file: ImportFileInfo) {
  try {
    const responseFile = await peopleImportService.updateSettings(
      electionGuid,
      file.rowId,
      {
        firstDataRow: file.firstDataRow ?? undefined,
        codePage: file.codePage ?? undefined,
      },
    );
    const updatedFile = responseFile?.rowId ? responseFile : file;
    replaceFileInList(updatedFile);
    mappingSaved.value = false;
    importResult.value = null;
    await parseFile(
      updatedFile.codePage || undefined,
      updatedFile.firstDataRow ?? undefined,
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
      resetWorkingState();
    }
    showSuccessMessage(t("people.import.fileDeleted"));
  } catch (error: unknown) {
    if (error !== "cancel") {
      const message =
        error instanceof Error
          ? error.message
          : t("people.import.deleteFileError");
      showErrorMessage(message);
    }
  }
}

function onMappingsChanged(next: ColumnMapping[]) {
  columnMappings.value = next;
  mappingSaved.value = false;
  importResult.value = null;
}

async function saveMapping() {
  if (!selectedFile.value) {
    return;
  }

  savingMapping.value = true;
  try {
    const updatedFile = await peopleImportService.saveMapping(
      electionGuid,
      selectedFile.value.rowId,
      columnMappings.value,
    );
    replaceFileInList(updatedFile);
    mappingSaved.value = true;
    showSuccessMessage(t("people.import.mappingSaved"));
    await scrollToStage("load");
  } catch (error) {
    handleApiError(error);
  } finally {
    savingMapping.value = false;
  }
}

async function executeImport() {
  if (!selectedFile.value || !mappingSaved.value) {
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
        t("people.import.importComplete", {
          added: result.peopleAdded,
          skipped: result.peopleSkipped,
        }),
      );
      await loadPeopleCount();
      await loadFiles();
      const refreshed = files.value.find(
        (item) => item.rowId === selectedFile.value?.rowId,
      );
      if (refreshed) {
        selectedFile.value = refreshed;
      }
    } else {
      showErrorMessage(
        t("people.import.importFailed", { count: result.errors.length }),
      );
    }
  } catch (error) {
    console.error("Import failed:", error);
    showErrorMessage(t("people.import.importFailedGeneric"));
  } finally {
    importing.value = false;
  }
}
</script>

<template>
  <div class="people-import-page">
    <ol class="import-pipeline" :aria-label="$t('people.import.title')">
      <li :class="{ done: fileReady, current: !fileReady }">
        <span class="pipeline-index">1</span>
        <div class="pipeline-copy">
          <strong>{{ $t("people.import.step1") }}</strong>
          <span>{{ pipelineFileDetail }}</span>
        </div>
      </li>
      <li :class="{ done: mappingReady, current: fileReady && !mappingReady }">
        <span class="pipeline-index">2</span>
        <div class="pipeline-copy">
          <strong>{{ $t("people.import.step2") }}</strong>
          <span>{{ pipelineMapDetail }}</span>
        </div>
      </li>
      <li
        :class="{
          done: !!importResult?.success,
          current: mappingReady && !importResult?.success,
        }"
      >
        <span class="pipeline-index">3</span>
        <div class="pipeline-copy">
          <strong>{{ $t("people.import.step3") }}</strong>
          <span>{{ pipelineLoadDetail }}</span>
        </div>
      </li>
    </ol>

    <el-card class="import-stage import-guide" shadow="never">
      <template #header>
        <h3>{{ $t("people.import.fileGuideTitle") }}</h3>
      </template>
      <PeopleImportFileGuide />
    </el-card>

    <el-card id="import-stage-file" class="import-stage" shadow="never">
      <template #header>
        <div class="stage-header">
          <span class="stage-number">1</span>
          <div>
            <h3>{{ $t("people.import.step1") }}</h3>
            <p>{{ $t("people.import.chooseFileDesc") }}</p>
          </div>
        </div>
      </template>
      <PeopleImportUploadStep
        :files="files"
        :selected-file="selectedFile"
        :uploading="uploading"
        :reparsing="reparsing"
        :upload-key="uploadKey"
        @change="handleFileChange"
        @select="(file) => selectFile(file)"
        @reparse="reparseFile"
        @delete="deleteFile"
        @update-settings="updateFileSettings"
      />
    </el-card>

    <el-card
      id="import-stage-map"
      class="import-stage"
      :class="{ 'is-inactive': !fileReady }"
      shadow="never"
    >
      <template #header>
        <div class="stage-header">
          <span class="stage-number">2</span>
          <div>
            <h3>{{ $t("people.import.step2") }}</h3>
            <p>{{ $t("people.import.mapColumnsDesc") }}</p>
          </div>
        </div>
      </template>
      <el-skeleton v-if="parsing" :rows="5" animated />
      <PeopleImportMappingStep
        v-else
        :parsed-result="parsedResult"
        :column-mappings="columnMappings"
        :saving-mapping="savingMapping"
        :mapping-saved="mappingSaved"
        :is-mapping-valid="isMappingValid"
        @update:column-mappings="onMappingsChanged"
        @save="saveMapping"
      />
    </el-card>

    <el-card
      id="import-stage-load"
      class="import-stage"
      :class="{ 'is-inactive': !mappingReady }"
      shadow="never"
    >
      <template #header>
        <div class="stage-header">
          <span class="stage-number">3</span>
          <div>
            <h3>{{ $t("people.import.step3") }}</h3>
            <p>{{ $t("people.import.loadPeopleDesc") }}</p>
          </div>
        </div>
      </template>
      <PeopleImportExecuteStep
        :parsed-result="parsedResult"
        :column-mappings="columnMappings"
        :is-mapping-valid="isMappingValid"
        :mapping-saved="mappingSaved"
        :selected-file="selectedFile"
        :importing="importing"
        :import-progress="importProgress"
        :import-result="importResult"
        :translated-errors="translatedErrors"
        :translated-warnings="translatedWarnings"
        :people-count="peopleCount"
        @import="executeImport"
      />
    </el-card>
  </div>
</template>

<style lang="less">
.people-import-page {
  max-width: 1100px;
  margin: 0 auto;
  display: flex;
  flex-direction: column;
  gap: 20px;
  padding-bottom: 40px;

  .import-pipeline {
    display: grid;
    grid-template-columns: repeat(3, minmax(0, 1fr));
    gap: 12px;
    list-style: none;
    margin: 0;
    padding: 0;

    li {
      display: flex;
      gap: 12px;
      align-items: flex-start;
      padding: 14px 16px;
      border: 1px solid var(--el-border-color-lighter);
      border-radius: 10px;
      background: var(--el-bg-color);
      min-width: 0;
    }

    .pipeline-index {
      flex-shrink: 0;
      width: 28px;
      height: 28px;
      border-radius: 50%;
      display: inline-flex;
      align-items: center;
      justify-content: center;
      font-weight: 600;
      font-size: var(--font-size-sm);
      background: var(--el-fill-color);
      color: var(--el-text-color-regular);
    }

    .pipeline-copy {
      min-width: 0;

      strong {
        display: block;
        margin-bottom: 2px;
      }

      span {
        display: block;
        font-size: var(--font-size-sm);
        color: var(--el-text-color-secondary);
        overflow: hidden;
        text-overflow: ellipsis;
        white-space: nowrap;
      }
    }

    li.current {
      border-color: var(--el-color-primary-light-5);
      box-shadow: 0 0 0 1px var(--el-color-primary-light-5);

      .pipeline-index {
        background: var(--el-color-primary);
        color: var(--color-text-inverse);
      }
    }

    li.done {
      .pipeline-index {
        background: var(--el-color-success);
        color: var(--color-text-inverse);
      }
    }
  }

  .import-stage {
    .el-card__header {
      padding: 16px 20px 12px;
      border-bottom: 1px solid var(--el-border-color-extra-light);
    }

    .el-card__body {
      padding: 16px 20px 24px;
    }

    &.import-guide {
      h3 {
        margin: 0;
        font-size: var(--font-size-lg);
      }
    }

    &.is-inactive {
      opacity: 0.62;
    }
  }

  .stage-header {
    display: flex;
    align-items: flex-start;
    gap: 12px;

    h3 {
      margin: 0 0 2px;
      font-size: var(--font-size-lg);
    }

    p {
      margin: 0;
      font-size: var(--font-size-sm);
      color: var(--el-text-color-secondary);
      font-weight: 400;
    }
  }

  .stage-number {
    width: 28px;
    height: 28px;
    border-radius: 50%;
    display: inline-flex;
    align-items: center;
    justify-content: center;
    background: var(--el-color-primary);
    color: var(--color-text-inverse);
    font-weight: 600;
    flex-shrink: 0;
  }

  @media (max-width: 800px) {
    .import-pipeline {
      grid-template-columns: 1fr;
    }
  }
}
</style>
