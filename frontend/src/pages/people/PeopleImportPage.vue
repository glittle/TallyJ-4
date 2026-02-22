<script setup lang="ts">
import { ref, computed, onMounted, onBeforeUnmount } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import { useI18n } from 'vue-i18n';
import { ElMessage, ElMessageBox } from 'element-plus';
import { UploadFilled, Check, Warning, InfoFilled, Clock } from '@element-plus/icons-vue';
import type { UploadFile } from 'element-plus';
import { peopleImportService } from '../../services/peopleImportService';
import { signalrService } from '../../services/signalrService';
import type { ImportFileInfo, ColumnMapping, ImportPeopleResult } from '../../types';
import { PEOPLE_TARGET_FIELDS } from '../../types';
import type {
  PeopleImportProgressEvent,
  PeopleImportCompleteEvent,
} from '../../types/SignalREvents';

const router = useRouter();
const route = useRoute();
const { t } = useI18n();

const electionGuid = route.params.id as string;
const currentStep = ref(0);
const uploading = ref(false);
const reparsing = ref<number | null>(null);
const savingMapping = ref(false);

const files = ref<ImportFileInfo[]>([]);
const selectedFile = ref<ImportFileInfo | null>(null);
const parsedResult = ref<import('../../types').ParseFileResult | null>(null);
const columnMappings = ref<ColumnMapping[]>([]);
const importing = ref(false);
const importResult = ref<ImportPeopleResult | null>(null);
const peopleCount = ref(0);
const importProgress = ref<PeopleImportProgressEvent | null>(null);
const showDeleteAllConfirm = ref(false);

const availableTargetFields = computed(() => [
  { value: null, label: t('people.import.ignore') },
  ...PEOPLE_TARGET_FIELDS
]);

const previewRows = computed(() => {
  if (!parsedResult.value?.previewRows) return [];
  return parsedResult.value.previewRows.slice(0, 5);
});

const canProceedToNext = computed(() => {
  if (currentStep.value === 0) {
    return selectedFile.value !== null;
  }
  if (currentStep.value === 1) {
    const firstNameMapped = columnMappings.value.some(m => m?.targetField === 'FirstName');
    const lastNameMapped = columnMappings.value.some(m => m?.targetField === 'LastName');
    return firstNameMapped && lastNameMapped;
  }
  return true;
});

const isMappingValid = computed(() => {
  const firstNameMapped = columnMappings.value.some(m => m.targetField === 'FirstName');
  const lastNameMapped = columnMappings.value.some(m => m?.targetField === 'LastName');
  return firstNameMapped && lastNameMapped;
});

const canDeleteAllPeople = computed(() => true);

async function loadFiles() {
  try {
    files.value = await peopleImportService.getFiles(electionGuid);
  } catch (error) {
    console.error('Failed to load import files:', error);
    ElMessage.error(t('people.import.loadFilesError'));
  }
}

async function loadPeopleCount() {
  try {
    const result = await peopleImportService.getPeopleCount(electionGuid);
    peopleCount.value = result.count;
  } catch (error) {
    console.error('Failed to load people count:', error);
  }
}

async function initializeSignalR() {
  try {
    const connection = await signalrService.connectToPeopleImportHub();

    connection.on('importProgress', (data: PeopleImportProgressEvent) => {
      importProgress.value = data;
    });

    connection.on('importError', (errorMessage: string) => {
      ElMessage.error(`Import error: ${errorMessage}`);
    });

    connection.on('importComplete', (data: PeopleImportCompleteEvent) => {
      importing.value = false;
      importResult.value = data.result;
      importProgress.value = null;
      if (data.result.success) {
        ElMessage.success(
          `Import completed: ${data.result.peopleAdded} people added, ${data.result.peopleSkipped} skipped`
        );
      } else {
        ElMessage.error('Import failed');
      }
    });
  } catch (e) {
    console.error('Failed to initialize SignalR for people import:', e);
  }
}

onMounted(async () => {
  await initializeSignalR();
  try {
    await signalrService.joinPeopleImportSession(electionGuid);
  } catch (e) {
    console.error('Failed to join people import session:', e);
  }
  await loadFiles();
  await loadPeopleCount();
});

onBeforeUnmount(async () => {
  try {
    await signalrService.leavePeopleImportSession(electionGuid);
  } catch (e) {
    console.error('Failed to leave people import session:', e);
  }
});

function goBack() {
  router.push(`/elections/${electionGuid}/people`);
}

async function handleFileChange(file: UploadFile) {
  if (file.raw) {
    uploading.value = true;
    try {
      const uploadedFile = await peopleImportService.uploadFile(electionGuid, file.raw);
      await loadFiles();
      
      // Show message if headers were detected at a non-standard row (row 2 or higher)
      if (uploadedFile.firstDataRow && uploadedFile.firstDataRow >= 2 && uploadedFile.fileType === 'xlsx') {
        ElMessage.success({
          message: t('people.import.headerAutoDetected', { row: uploadedFile.firstDataRow }),
          duration: 5000
        });
      } else {
        ElMessage.success('File uploaded successfully');
      }
    } catch (error) {
      console.error('Upload failed:', error);
      ElMessage.error(t('people.import.uploadError'));
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
  ElMessage.error(t('people.import.uploadError'));
}

async function parseFile(codePage?: number, firstDataRow?: number) {
  if (!selectedFile.value) return;

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
    console.error('Failed to parse file:', error);
    ElMessage.error('Failed to parse file');
    throw error;
  }
}

async function selectFile(file: ImportFileInfo) {
  selectedFile.value = file;
  parsedResult.value = null;
  columnMappings.value = [];
  importResult.value = null;
  importProgress.value = null;
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
    console.error('Reparse failed:', error);
  } finally {
    reparsing.value = null;
  }
}

async function updateFileSettings(file: ImportFileInfo) {
  try {
    const updatedFile = await peopleImportService.updateSettings(electionGuid, file.rowId, {
      firstDataRow: file.firstDataRow ?? undefined,
      codePage: file.codePage ?? undefined,
    });
    const index = files.value.findIndex(f => f.rowId === updatedFile.rowId);
    if (index !== -1) {
      files.value[index] = updatedFile;
    }
    if (selectedFile.value?.rowId === updatedFile.rowId) {
      selectedFile.value = updatedFile;
    }
    await parseFile(updatedFile.codePage || undefined, updatedFile.firstDataRow || undefined);
  } catch (error) {
    console.error('Update settings failed:', error);
    ElMessage.error('Failed to update file settings');
  }
}

async function deleteFile(file: ImportFileInfo) {
  try {
    await ElMessageBox.confirm(
      t('people.import.confirmDeleteFile'),
      t('common.warning'),
      {
        confirmButtonText: t('common.delete'),
        cancelButtonText: t('common.cancel'),
        type: 'warning'
      }
    );

    await peopleImportService.deleteFile(electionGuid, file.rowId);
    files.value = files.value.filter(f => f.rowId !== file.rowId);
    if (selectedFile.value?.rowId === file.rowId) {
      selectedFile.value = null;
      parsedResult.value = null;
      columnMappings.value = [];
    }
    ElMessage.success('File deleted');
  } catch (error: any) {
    if (error !== 'cancel') {
      ElMessage.error(error.message || t('people.import.deleteFileError'));
    }
  }
}

async function saveMapping() {
  if (!selectedFile.value) return;

  savingMapping.value = true;
  try {
    await peopleImportService.saveMapping(electionGuid, selectedFile.value.rowId, columnMappings.value);
    ElMessage.success(t('people.import.mappingSaved'));
  } catch (error) {
    console.error('Save mapping failed:', error);
    ElMessage.error('Failed to save column mapping');
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

function getStatusType(status: string | null): string {
  switch (status) {
    case 'Imported': return 'success';
    case 'Processing': return 'warning';
    case 'Failed': return 'danger';
    default: return 'info';
  }
}

function formatFileSize(bytes: number): string {
  if (bytes === 0) return '0 B';
  const k = 1024;
  const sizes = ['B', 'KB', 'MB', 'GB'];
  const i = Math.floor(Math.log(bytes) / Math.log(k));
  return parseFloat((bytes / Math.pow(k, i)).toFixed(1)) + ' ' + sizes[i];
}

function getFieldDescription(fieldValue: string): string {
  const descriptions: Record<string, string> = {
    'FirstName': t('people.import.firstNameDesc'),
    'LastName': t('people.import.lastNameDesc'),
    'BahaiId': t('people.import.bahaiIdDesc'),
    'IneligibleReasonDescription': t('people.import.eligibilityDesc'),
    'Area': t('people.import.areaDesc'),
    'Email': t('people.import.emailDesc'),
    'Phone': t('people.import.phoneDesc'),
    'OtherNames': t('people.import.otherNamesDesc'),
    'OtherLastNames': t('people.import.otherLastNamesDesc'),
    'OtherInfo': t('people.import.otherInfoDesc')
  };
  return descriptions[fieldValue] || '';
}

function getFieldLabel(fieldValue: string): string {
  const field = PEOPLE_TARGET_FIELDS.find(f => f.value === fieldValue);
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

async function executeImport() {
  if (!selectedFile.value) return;

  importing.value = true;
  importResult.value = null;
  importProgress.value = null;

  try {
    const result = await peopleImportService.executeImport(electionGuid, selectedFile.value.rowId);
    importResult.value = result;
    if (result.success) {
      ElMessage.success(
        `Import completed: ${result.peopleAdded} people added, ${result.peopleSkipped} skipped`
      );
      await loadPeopleCount();
    } else {
      ElMessage.error('Import failed');
    }
  } catch (error) {
    console.error('Import failed:', error);
    ElMessage.error('Failed to execute import');
  } finally {
    importing.value = false;
  }
}

async function confirmDeleteAllPeople() {
  try {
    await ElMessageBox.confirm(
      t('people.import.confirmDeleteAllPeople'),
      t('common.warning'),
      {
        confirmButtonText: t('common.delete'),
        cancelButtonText: t('common.cancel'),
        type: 'warning'
      }
    );

    const result = await peopleImportService.deleteAllPeople(electionGuid);
    ElMessage.success(`${result.deletedCount} people deleted`);
    await loadPeopleCount();
    showDeleteAllConfirm.value = false;
  } catch (error: any) {
    if (error !== 'cancel') {
      ElMessage.error(error.message || t('people.import.deleteAllPeopleError'));
    }
  }
}
</script>

<template>
  <div class="people-import-page">
    <el-card>
      <template #header>
        <div class="card-header">
          <el-page-header @back="goBack" :content="$t('people.import.title')" />
        </div>
      </template>

      <el-steps :active="currentStep" finish-status="success" align-center>
        <el-step :title="$t('people.import.step1')" />
        <el-step :title="$t('people.import.step2')" />
        <el-step :title="$t('people.import.step3')" />
      </el-steps>

      <div class="step-content">
        <!-- Step 1: Upload -->
        <div v-if="currentStep === 0" class="upload-step">
          <h3>{{ $t('people.import.uploadFile') }}</h3>
          <el-upload ref="uploadRef" :auto-upload="false" :limit="1" :on-change="handleFileChange"
            :on-success="handleUploadSuccess" :on-error="handleUploadError" accept=".csv,.tsv,.tab,.txt,.xlsx" drag
            :disabled="uploading">
            <el-icon class="el-icon--upload"><upload-filled /></el-icon>
            <div class="el-upload__text">
              {{ $t('people.import.dropFile') }} <em>{{ $t('people.import.clickToUpload') }}</em>
            </div>
            <template #tip>
              <div class="el-upload__tip">{{ $t('people.import.supportedFormats') }}</div>
            </template>
          </el-upload>

          <div v-if="files.length > 0" class="files-section">
            <h4>{{ $t('people.import.filesOnServer') }}</h4>
            <el-table :data="files" stripe style="width: 100%">
              <el-table-column prop="originalFileName" :label="$t('people.import.fileName')" width="200" />
              <el-table-column prop="processingStatus" :label="$t('people.import.status')" width="120">
                <template #default="scope">
                  <el-tag :type="getStatusType(scope.row.processingStatus)">
                    {{ scope.row.processingStatus || 'Uploaded' }}
                  </el-tag>
                </template>
              </el-table-column>
              <el-table-column prop="uploadTime" :label="$t('people.import.uploadTime')" width="180">
                <template #default="scope">
                  {{ scope.row.uploadTime ? new Date(scope.row.uploadTime).toLocaleString() : '-' }}
                </template>
              </el-table-column>
              <el-table-column prop="firstDataRow" width="140">
                <template #header>
                  <el-tooltip :content="$t('people.import.headersOnLineTooltip')" placement="top">
                    <span>
                      {{ $t('people.import.headersOnLine') }}
                      <el-icon style="margin-left: 4px; vertical-align: middle;"><InfoFilled /></el-icon>
                    </span>
                  </el-tooltip>
                </template>
                <template #default="scope">
                  <el-input-number v-model="scope.row.firstDataRow" :min="1" :max="10" size="small"
                    :disabled="scope.row.processingStatus === 'Imported'" @change="updateFileSettings(scope.row)" />
                </template>
              </el-table-column>
              <el-table-column prop="codePage" :label="$t('people.import.contentEncoding')" width="150">
                <template #default="scope">
                  <el-select v-if="scope.row.fileType !== 'xlsx'" v-model="scope.row.codePage" size="small"
                    :disabled="scope.row.processingStatus === 'Imported'" @change="updateFileSettings(scope.row)">
                    <el-option label="UTF-8" :value="65001" />
                    <el-option label="Windows-1252" :value="1252" />
                    <el-option label="ISO-8859-1" :value="28591" />
                  </el-select>
                  <span v-else class="encoding-text">UTF-8 (Excel)</span>
                </template>
              </el-table-column>
              <el-table-column prop="fileSize" :label="$t('people.import.size')" width="100">
                <template #default="scope">
                  {{ scope.row.fileSize ? formatFileSize(scope.row.fileSize) : '-' }}
                </template>
              </el-table-column>
              <el-table-column :label="$t('people.import.action')" width="120">
                <template #default="scope">
                  <el-button v-if="selectedFile?.rowId !== scope.row.rowId" type="primary" size="small"
                    @click="selectFile(scope.row)">
                    {{ $t('people.import.select') }}
                  </el-button>
                  <el-tag v-else type="success">{{ $t('people.import.selected') }}</el-tag>
                </template>
              </el-table-column>
              <el-table-column :label="$t('people.import.otherActions')" width="150">
                <template #default="scope">
                  <el-space>
                    <el-button type="default" size="small" @click="reparseFile(scope.row)"
                      :loading="reparsing === scope.row.rowId">
                      {{ $t('people.import.reparse') }}
                    </el-button>
                    <el-button type="danger" size="small" @click="deleteFile(scope.row)">
                      {{ $t('common.delete') }}
                    </el-button>
                  </el-space>
                </template>
              </el-table-column>
            </el-table>
          </div>
        </div>

        <!-- Step 2: Map Columns -->
        <div v-if="currentStep === 1" class="mapping-step">
          <h3>{{ $t('people.import.mapColumns') }}</h3>
          <p>{{ $t('people.import.mapColumnsDesc') }}</p>

          <div v-if="parsedResult && parsedResult.headers.length > 0" class="column-mapping">
            <div class="mapping-table-container">
              <table class="mapping-table">
                <thead>
                  <tr>
                    <th class="target-header">{{ $t('people.import.tallyJField') }}</th>
                    <th v-for="header in parsedResult.headers" :key="header" class="file-header">
                      {{ header }}
                    </th>
                  </tr>
                </thead>
                <tbody>
                  <!-- Mapping row -->
                  <tr class="mapping-row">
                    <td class="target-cell">{{ $t('people.import.mapTo') }}</td>
                    <td v-for="(header, index) in parsedResult.headers" :key="`mapping-${index}`" class="mapping-cell">
                      <el-select v-model="columnMappings[index].targetField" size="small" clearable
                        :placeholder="$t('people.import.ignore')">
                        <el-option v-for="field in availableTargetFields" :key="field.value" :label="field.label"
                          :value="field.value" />
                      </el-select>
                    </td>
                  </tr>
                  <!-- Preview rows -->
                  <tr v-for="(row, rowIndex) in previewRows" :key="`preview-${rowIndex}`" class="preview-row">
                    <td class="target-cell preview-label">{{ $t('people.import.preview') }} {{ rowIndex + 1 }}</td>
                    <td v-for="(cell, cellIndex) in row" :key="`cell-${rowIndex}-${cellIndex}`" class="preview-cell">
                      {{ cell }}
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>

            <div class="mapping-actions">
              <el-button type="primary" @click="saveMapping" :loading="savingMapping">
                {{ $t('people.import.saveMapping') }}
              </el-button>
            </div>
          </div>

          <!-- Reference sections -->
          <el-collapse class="reference-sections">
            <el-collapse-item :title="$t('people.import.tallyJFields')" name="fields">
              <div class="field-reference">
                <div v-for="field in PEOPLE_TARGET_FIELDS" :key="field.value" class="field-item">
                  <strong>{{ field.label }}</strong>
                  <span v-if="field.required" class="required-mark">*</span>
                  <span class="field-desc">{{ getFieldDescription(field.value) }}</span>
                </div>
              </div>
            </el-collapse-item>
            <el-collapse-item :title="$t('people.import.eligibilityValues')" name="eligibility">
              <div class="eligibility-reference">
                <p>{{ $t('people.import.eligibilityDesc') }}</p>
                <ul>
                  <li><strong>Eligible</strong> - {{ $t('people.import.eligibleDesc') }}</li>
                  <li><strong>Ineligible</strong> - {{ $t('people.import.ineligibleDesc') }}</li>
                  <li><strong>Under Age</strong> - {{ $t('people.import.underAgeDesc') }}</li>
                  <li><strong>Duplicate</strong> - {{ $t('people.import.duplicateDesc') }}</li>
                </ul>
              </div>
            </el-collapse-item>
          </el-collapse>
        </div>

        <!-- Step 3: Import -->
        <div v-if="currentStep === 2" class="import-step">
          <h3>{{ $t('people.import.reviewImport') }}</h3>
          <p>{{ $t('people.import.importStepDesc') }}</p>

          <!-- Import Summary -->
          <div v-if="parsedResult" class="import-summary">
            <el-card class="summary-card">
              <template #header>
                <h4>{{ $t('people.import.importSummary') }}</h4>
              </template>
              <div class="summary-content">
                <div class="summary-item">
                  <strong>{{ $t('people.import.dataRows') }}:</strong> {{ parsedResult.totalDataRows }}
                </div>
                <div class="summary-item">
                  <strong>{{ $t('people.import.mappedFields') }}:</strong>
                  <ul class="mapped-fields-list">
                    <li v-for="mapping in columnMappings" :key="mapping.fileColumn" v-if="columnMappings.length > 0">
                      {{ mapping.fileColumn }} → {{ mapping.targetField ? getFieldLabel(mapping.targetField) : '?' }}
                    </li>
                  </ul>
                </div>
              </div>
            </el-card>
          </div>

          <!-- Validation Warnings -->
          <el-alert v-if="!isMappingValid" :title="$t('people.import.validationWarning')"
            :description="$t('people.import.firstNameLastNameRequired')" type="warning" show-icon
            class="validation-alert" />

          <!-- Import Progress -->
          <div v-if="importing" class="import-progress">
            <h4>{{ $t('people.import.importing') }}</h4>
            <el-progress v-if="importProgress"
              :percentage="(importProgress.processed / importProgress.total) * 100 || 0" :text-inside="true"
              :stroke-width="20" status="success" />
            <p v-if="importProgress?.status" class="progress-message">
              {{ importProgress.status }}
            </p>
          </div>

          <!-- Import Results -->
          <div v-if="importResult && !importing" class="import-results">
            <el-card class="results-card">
              <template #header>
                <h4>{{ $t('people.import.importResults') }}</h4>
              </template>
              <div class="results-content">
                <div class="result-item success">
                  <el-icon>
                    <Check />
                  </el-icon>
                  <span>{{ $t('people.import.peopleAdded') }}: {{ importResult.peopleAdded }}</span>
                </div>
                <div class="result-item warning" v-if="importResult.peopleSkipped > 0">
                  <el-icon>
                    <Warning />
                  </el-icon>
                  <span>{{ $t('people.import.peopleSkipped') }}: {{ importResult.peopleSkipped }}</span>
                </div>
                <div class="result-item info" v-if="importResult.warnings && importResult.warnings.length > 0">
                  <el-icon>
                    <InfoFilled />
                  </el-icon>
                  <span>{{ $t('people.import.warnings') }}: {{ importResult.warnings.length }}</span>
                </div>
                <div class="result-item time">
                  <el-icon>
                    <Clock />
                  </el-icon>
                  <span>{{ $t('people.import.timeElapsed') }}: {{ formatTime(importResult.timeElapsedSeconds) }}</span>
                </div>
              </div>
            </el-card>
          </div>

          <!-- Import Actions -->
          <div class="import-actions">
            <el-space>
              <el-button type="primary" @click="executeImport" :loading="importing"
                :disabled="!isMappingValid || !selectedFile">
                {{ $t('people.import.importNow') }}
              </el-button>
              <el-button type="danger" @click="showDeleteAllConfirm = true" :disabled="canDeleteAllPeople === false">
                {{ $t('people.import.deleteAllPeople') }}
              </el-button>
            </el-space>
          </div>

          <!-- People Count -->
          <div class="people-count">
            <el-statistic :title="$t('people.import.currentPeopleCount')" :value="peopleCount" :loading="false" />
          </div>
        </div>
      </div>

      <div class="step-actions">
        <el-button v-if="currentStep > 0" @click="currentStep--">
          {{ $t('common.previous') }}
        </el-button>
        <el-button v-if="currentStep < 2" type="primary" :disabled="!canProceedToNext" @click="handleNext">
          {{ $t('common.next') }}
        </el-button>
      </div>
    </el-card>

    <!-- Delete All People Confirmation -->
    <el-dialog v-model="showDeleteAllConfirm" :title="$t('people.import.confirmDeleteAllPeople')" width="500px">
      <p>{{ $t('people.import.deleteAllPeopleMessage') }}</p>
      <p class="warning-text">{{ $t('common.actionIrreversible') }}</p>

      <template #footer>
        <el-button @click="showDeleteAllConfirm = false">
          {{ $t('common.cancel') }}
        </el-button>
        <el-button type="danger" @click="confirmDeleteAllPeople" :loading="false">
          {{ $t('common.delete') }}
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<style lang="less">
.people-import-page {
  max-width: 1400px;
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

  .mapping-step {
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

      .target-header {
        background-color: var(--el-color-primary);
        color: var(--color-text-inverse);
        font-weight: bold;
        min-width: 150px;
      }

      .file-header {
        background-color: var(--el-color-primary);
        color: var(--color-text-inverse);
        font-weight: bold;
        min-width: 120px;
      }

      .target-cell {
        background-color: var(--el-fill-color-light);
        font-weight: bold;
      }

      .mapping-row {
        .target-cell {
          background-color: var(--el-color-primary);
          color: var(--color-text-inverse);
          font-weight: bold;
        }
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

    .field-reference {
      .field-item {
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
    }

    .eligibility-reference {
      ul {
        padding-left: 20px;

        li {
          margin-bottom: 8px;
        }
      }
    }
  }

  .import-step {
    h3 {
      margin-bottom: 20px;
    }

    p {
      color: var(--el-text-color-secondary);
      margin-bottom: 20px;
    }

    .import-summary {
      margin-bottom: 20px;

      .summary-card {
        .summary-content {
          .summary-item {
            margin-bottom: 15px;

            strong {
              display: block;
              margin-bottom: 5px;
              color: var(--el-text-color-primary);
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
      }
    }

    .validation-alert {
      margin-bottom: 20px;
    }

    .import-progress {
      margin-bottom: 20px;

      h4 {
        margin-bottom: 15px;
        color: #303133;
      }

      .progress-message {
        margin-top: 10px;
        color: #606266;
        font-style: italic;
      }
    }

    .import-results {
      margin-bottom: 20px;

      .results-card {
        .results-content {
          .result-item {
            display: flex;
            align-items: center;
            margin-bottom: 10px;
            padding: 8px;
            border-radius: 4px;

            &.success {
              background-color: #f0f9ff;
              color: #67c23a;
            }

            &.warning {
              background-color: #fdf6ec;
              color: #e6a23c;
            }

            &.info {
              background-color: #f4f4f5;
              color: #909399;
            }

            &.time {
              background-color: #f5f7fa;
              color: #606266;
            }

            .el-icon {
              margin-right: 8px;
            }

            span {
              font-weight: 500;
            }
          }
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

  .step-actions {
    text-align: center;
    margin-top: 30px;
  }
}
</style>
