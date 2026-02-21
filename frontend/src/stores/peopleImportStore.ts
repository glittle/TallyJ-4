import { defineStore } from 'pinia';
import { ref } from 'vue';
import { peopleImportService } from '../services/peopleImportService';
import { signalrService } from '../services/signalrService';
import { ElMessage } from 'element-plus';
import type {
  ImportFileInfo,
  ParseFileResult,
  ColumnMapping,
  ImportPeopleResult
} from '../types';
import type {
  PeopleImportProgressEvent,
  PeopleImportCompleteEvent
} from '../types/SignalREvents';

export const usePeopleImportStore = defineStore('peopleImport', () => {
  // State
  const files = ref<ImportFileInfo[]>([]);
  const selectedFile = ref<ImportFileInfo | null>(null);
  const parsedResult = ref<ParseFileResult | null>(null);
  const columnMappings = ref<ColumnMapping[]>([]);
  const importing = ref(false);
  const importResult = ref<ImportPeopleResult | null>(null);
  const peopleCount = ref(0);
  const importProgress = ref<PeopleImportProgressEvent | null>(null);
  const signalrInitialized = ref(false);

  // Actions
  async function loadFiles(electionGuid: string) {
    try {
      files.value = await peopleImportService.getFiles(electionGuid);
    } catch (error) {
      console.error('Failed to load import files:', error);
      ElMessage.error('Failed to load import files');
      throw error;
    }
  }

  async function uploadFile(electionGuid: string, file: File) {
    try {
      const uploadedFile = await peopleImportService.uploadFile(electionGuid, file);
      files.value.push(uploadedFile);
      ElMessage.success('File uploaded successfully');
      return uploadedFile;
    } catch (error) {
      console.error('Failed to upload file:', error);
      ElMessage.error('Failed to upload file');
      throw error;
    }
  }

  async function selectFile(file: ImportFileInfo) {
    selectedFile.value = file;
    parsedResult.value = null;
    columnMappings.value = [];
    importResult.value = null;
    importProgress.value = null;
  }

  async function parseFile(electionGuid: string, codePage?: number, firstDataRow?: number) {
    if (!selectedFile.value) return;

    try {
      const result = await peopleImportService.parseFile(
        electionGuid,
        selectedFile.value.rowId,
        codePage,
        firstDataRow
      );
      parsedResult.value = result;
      columnMappings.value = result.autoMappings || [];
    } catch (error) {
      console.error('Failed to parse file:', error);
      ElMessage.error('Failed to parse file');
      throw error;
    }
  }

  async function saveMapping(electionGuid: string) {
    if (!selectedFile.value) return;

    try {
      const updatedFile = await peopleImportService.saveMapping(
        electionGuid,
        selectedFile.value.rowId,
        columnMappings.value
      );
      // Update the file in the files array
      const index = files.value.findIndex(f => f.rowId === selectedFile.value!.rowId);
      if (index !== -1) {
        files.value[index] = updatedFile;
      }
      selectedFile.value = updatedFile;
      ElMessage.success('Column mapping saved');
    } catch (error) {
      console.error('Failed to save mapping:', error);
      ElMessage.error('Failed to save column mapping');
      throw error;
    }
  }

  async function updateSettings(electionGuid: string, settings: { firstDataRow?: number; codePage?: number }) {
    if (!selectedFile.value) return;

    try {
      const updatedFile = await peopleImportService.updateSettings(
        electionGuid,
        selectedFile.value.rowId,
        settings
      );
      // Update the file in the files array
      const index = files.value.findIndex(f => f.rowId === selectedFile.value!.rowId);
      if (index !== -1) {
        files.value[index] = updatedFile;
      }
      selectedFile.value = updatedFile;
      // Re-parse the file with new settings
      await parseFile(electionGuid, settings.codePage, settings.firstDataRow);
    } catch (error) {
      console.error('Failed to update settings:', error);
      ElMessage.error('Failed to update file settings');
      throw error;
    }
  }

  async function executeImport(electionGuid: string) {
    if (!selectedFile.value) return;

    importing.value = true;
    importResult.value = null;
    importProgress.value = null;

    try {
      const result = await peopleImportService.executeImport(
        electionGuid,
        selectedFile.value.rowId
      );
      importResult.value = result;
      if (result.success) {
        ElMessage.success(`Import completed: ${result.peopleAdded} people added, ${result.peopleSkipped} skipped`);
        // Refresh people count
        await loadPeopleCount(electionGuid);
      } else {
        ElMessage.error('Import failed');
      }
    } catch (error) {
      console.error('Failed to execute import:', error);
      ElMessage.error('Failed to execute import');
      throw error;
    } finally {
      importing.value = false;
    }
  }

  async function deleteFile(electionGuid: string, rowId: number) {
    try {
      await peopleImportService.deleteFile(electionGuid, rowId);
      files.value = files.value.filter(f => f.rowId !== rowId);
      if (selectedFile.value?.rowId === rowId) {
        selectedFile.value = null;
        parsedResult.value = null;
        columnMappings.value = [];
      }
      ElMessage.success('File deleted');
    } catch (error) {
      console.error('Failed to delete file:', error);
      ElMessage.error('Failed to delete file');
      throw error;
    }
  }

  async function deleteAllPeople(electionGuid: string) {
    try {
      const result = await peopleImportService.deleteAllPeople(electionGuid);
      ElMessage.success(`${result.deletedCount} people deleted`);
      await loadPeopleCount(electionGuid);
    } catch (error) {
      console.error('Failed to delete all people:', error);
      ElMessage.error('Failed to delete all people');
      throw error;
    }
  }

  async function loadPeopleCount(electionGuid: string) {
    try {
      const result = await peopleImportService.getPeopleCount(electionGuid);
      peopleCount.value = result.count;
    } catch (error) {
      console.error('Failed to load people count:', error);
      // Don't show error message for count loading
    }
  }

  async function initializeSignalR() {
    if (signalrInitialized.value) return;

    try {
      const connection = await signalrService.connectToPeopleImportHub();

      connection.on('importProgress', (data: PeopleImportProgressEvent) => {
        handleImportProgress(data);
      });

      connection.on('importError', (errorMessage: string) => {
        handleImportError(errorMessage);
      });

      connection.on('importComplete', (data: PeopleImportCompleteEvent) => {
        handleImportComplete(data);
      });

      signalrInitialized.value = true;
    } catch (e) {
      console.error('Failed to initialize SignalR for people import store:', e);
    }
  }

  async function joinImportSession(electionGuid: string) {
    try {
      await signalrService.joinPeopleImportSession(electionGuid);
    } catch (e) {
      console.error('Failed to join people import session:', e);
    }
  }

  async function leaveImportSession(electionGuid: string) {
    try {
      await signalrService.leavePeopleImportSession(electionGuid);
    } catch (e) {
      console.error('Failed to leave people import session:', e);
    }
  }

  function handleImportProgress(data: PeopleImportProgressEvent) {
    importProgress.value = data;
  }

  function handleImportError(errorMessage: string) {
    ElMessage.error(`Import error: ${errorMessage}`);
  }

  function handleImportComplete(data: PeopleImportCompleteEvent) {
    importing.value = false;
    importResult.value = data.result;
    importProgress.value = null;
    if (data.result.success) {
      ElMessage.success(`Import completed: ${data.result.peopleAdded} people added, ${data.result.peopleSkipped} skipped`);
    } else {
      ElMessage.error('Import failed');
    }
  }

  return {
    // State
    files,
    selectedFile,
    parsedResult,
    columnMappings,
    importing,
    importResult,
    peopleCount,
    importProgress,

    // Actions
    loadFiles,
    uploadFile,
    selectFile,
    parseFile,
    saveMapping,
    updateSettings,
    executeImport,
    deleteFile,
    deleteAllPeople,
    loadPeopleCount,
    initializeSignalR,
    joinImportSession,
    leaveImportSession
  };
});