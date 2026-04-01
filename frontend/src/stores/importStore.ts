import { defineStore } from "pinia";
import { ref } from "vue";
import { signalrService } from "../services/signalrService";

import type { ImportProgressEvent } from "../types/SignalREvents";

export const useImportStore = defineStore("import", () => {
  const importProgress = ref<ImportProgressEvent | null>(null);
  const importErrors = ref<string[]>([]);
  const importComplete = ref(false);
  const signalrInitialized = ref(false);

  async function initializeSignalR() {
    if (signalrInitialized.value) {
      return;
    }

    try {
      const connection = await signalrService.connectToBallotImportHub();

      connection.on("importProgress", (data: ImportProgressEvent) => {
        handleImportProgress(data);
      });

      connection.on(
        "importError",
        (errorMessage: string, rowNumber: number) => {
          handleImportError(errorMessage, rowNumber);
        },
      );

      connection.on("importComplete", (summary: any) => {
        handleImportComplete(summary);
      });

      signalrInitialized.value = true;
    } catch (e) {
      console.error("Failed to initialize SignalR for import store:", e);
    }
  }

  function handleImportProgress(data: ImportProgressEvent) {
    importProgress.value = data;
    importComplete.value = false;
  }

  function handleImportError(errorMessage: string, rowNumber: number) {
    const error = `Row ${rowNumber}: ${errorMessage}`;
    importErrors.value.push(error);
  }

  function handleImportComplete(_summary: any) {
    importComplete.value = true;
    importProgress.value = null;
  }

  async function joinImportSession(electionGuid: string) {
    try {
      await signalrService.joinImportSession(electionGuid);
    } catch (e) {
      console.error("Failed to join import session:", e);
    }
  }

  async function leaveImportSession(electionGuid: string) {
    try {
      await signalrService.leaveImportSession(electionGuid);
    } catch (e) {
      console.error("Failed to leave import session:", e);
    }
  }

  function clearImportState() {
    importProgress.value = null;
    importErrors.value = [];
    importComplete.value = false;
  }

  return {
    importProgress,
    importErrors,
    importComplete,
    initializeSignalR,
    joinImportSession,
    leaveImportSession,
    clearImportState,
  };
});
