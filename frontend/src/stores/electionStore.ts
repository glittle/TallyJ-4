import { defineStore } from "pinia";
import { computed, ref } from "vue";
import { electionService } from "../services/electionService";
import { signalrService } from "../services/signalrService";

import { ElMessage } from "element-plus";
import type {
  CreateElectionDto,
  ElectionDto,
  UpdateElectionDto,
} from "../types";
import type { ElectionUpdateEvent } from "../types/SignalREvents";
import { extractApiErrorMessage } from "../utils/errorHandler";
import { type ElectionStage } from "../domain/electionStages";

export const useElectionStore = defineStore("election", () => {
  const elections = ref<ElectionDto[]>([]);
  const currentElection = ref<ElectionDto | null>(null);
  const loading = ref(false);
  const error = ref<string | null>(null);
  const signalrInitialized = ref(false);

  const activeElections = computed(() =>
    elections.value.filter((e) => e.electionStage !== "ProcessingBallots"),
  );

  const finalizedElections = computed(() =>
    elections.value.filter((e) => e.electionStage === "ProcessingBallots"),
  );

  const currentStage = computed<ElectionStage>(
    () => currentElection.value?.electionStage ?? "SettingUp",
  );

  async function fetchElections() {
    loading.value = true;
    error.value = null;
    try {
      elections.value = await electionService.getAll();
    } catch (e: any) {
      error.value = extractApiErrorMessage(e);
      throw e;
    } finally {
      loading.value = false;
    }
  }

  async function fetchElectionById(electionGuid: string) {
    loading.value = true;
    error.value = null;
    try {
      const election = await electionService.getById(electionGuid);
      currentElection.value = election;

      const index = elections.value.findIndex(
        (e) => e.electionGuid === electionGuid,
      );
      if (index !== -1) {
        elections.value[index] = election;
      } else {
        elections.value.push(election);
      }

      return election;
    } catch (e: any) {
      error.value = extractApiErrorMessage(e);
      throw e;
    } finally {
      loading.value = false;
    }
  }

  async function createElection(dto: CreateElectionDto) {
    loading.value = true;
    error.value = null;
    try {
      const election = await electionService.create(dto);
      elections.value.push(election);
      currentElection.value = election;
      return election;
    } catch (e: any) {
      error.value = extractApiErrorMessage(e);
      throw e;
    } finally {
      loading.value = false;
    }
  }

  async function updateElection(electionGuid: string, dto: UpdateElectionDto) {
    loading.value = true;
    error.value = null;
    try {
      const election = await electionService.update(electionGuid, dto);

      const index = elections.value.findIndex(
        (e) => e.electionGuid === electionGuid,
      );
      if (index !== -1) {
        elections.value[index] = election;
      }

      if (currentElection.value?.electionGuid === electionGuid) {
        currentElection.value = election;
      }

      return election;
    } catch (e: any) {
      error.value = extractApiErrorMessage(e);
      throw e;
    } finally {
      loading.value = false;
    }
  }

  async function deleteElection(electionGuid: string) {
    loading.value = true;
    error.value = null;
    try {
      await electionService.delete(electionGuid);

      elections.value = elections.value.filter(
        (e) => e.electionGuid !== electionGuid,
      );

      if (currentElection.value?.electionGuid === electionGuid) {
        currentElection.value = null;
      }
    } catch (e: any) {
      error.value = extractApiErrorMessage(e);
      throw e;
    } finally {
      loading.value = false;
    }
  }

  function setCurrentElection(election: ElectionDto | null) {
    currentElection.value = election;
  }

  function clearError() {
    error.value = null;
  }

  async function initializeSignalR() {
    if (signalrInitialized.value) {
      return;
    }

    try {
      const connection = await signalrService.connectToMainHub();

      connection.on("statusChanged", (data: any) => {
        if (data && typeof data === "object") {
          const updateEvent: ElectionUpdateEvent = {
            electionGuid: data.electionGuid || "",
            name: data.name,
            electionStage: data.electionStage,
            updatedAt: data.updatedAt || new Date().toISOString(),
          };
          handleElectionUpdate(updateEvent);
        }
      });

      connection.on("electionClosed", (electionGuid: string) => {
        const updateEvent: ElectionUpdateEvent = {
          electionGuid,
          updatedAt: new Date().toISOString(),
        };
        handleElectionUpdate(updateEvent);
      });

      signalrInitialized.value = true;
    } catch (e) {
      console.error("Failed to initialize SignalR for election store:", e);
    }
  }

  function handleElectionUpdate(data: ElectionUpdateEvent) {
    const index = elections.value.findIndex(
      (e) => e.electionGuid === data.electionGuid,
    );
    if (index !== -1) {
      const existingElection = elections.value[index]!;
      const oldStage = existingElection.electionStage;

      elections.value[index] = {
        ...existingElection,
        name: data.name ?? existingElection.name,
        electionStage: data.electionStage ?? existingElection.electionStage,
      } as ElectionDto;

      if (data.electionStage && data.electionStage !== oldStage) {
        showElectionStageNotification(
          data.name || "Election",
          data.electionStage,
        );
      }
    }

    if (currentElection.value?.electionGuid === data.electionGuid) {
      const existingCurrentElection = currentElection.value!;
      const oldStage = existingCurrentElection.electionStage;

      currentElection.value = {
        ...existingCurrentElection,
        name: data.name ?? existingCurrentElection.name,
        electionStage:
          data.electionStage ?? existingCurrentElection.electionStage,
      } as ElectionDto;

      if (data.electionStage && data.electionStage !== oldStage) {
        showElectionStageNotification(
          data.name || "Election",
          data.electionStage,
        );
      }
    }
  }

  function showElectionStageNotification(
    electionName: string,
    newStage: string,
  ) {
    const message = `${electionName} stage changed to: ${newStage}`;
    ElMessage({
      message,
      type: "info",
      duration: 5000,
    });
  }

  async function joinElection(electionGuid: string) {
    try {
      await signalrService.joinElection(electionGuid);
    } catch (e) {
      console.error("Failed to join election group:", e);
    }
  }

  async function leaveElection(electionGuid: string) {
    try {
      await signalrService.leaveElection(electionGuid);
    } catch (e) {
      console.error("Failed to leave election group:", e);
    }
  }

  async function setStage(electionGuid: string, stage: ElectionStage) {
    loading.value = true;
    error.value = null;
    try {
      const election = await electionService.changeStage(electionGuid, stage);

      const index = elections.value.findIndex(
        (e) => e.electionGuid === electionGuid,
      );
      if (index !== -1) {
        elections.value[index] = election;
      }

      if (currentElection.value?.electionGuid === electionGuid) {
        currentElection.value = election;
      }

      return election;
    } catch (e: any) {
      error.value = extractApiErrorMessage(e);
      throw e;
    } finally {
      loading.value = false;
    }
  }

  async function toggleTellerAccess(electionGuid: string, isOpen: boolean) {
    loading.value = true;
    error.value = null;
    try {
      const election = await electionService.toggleTellerAccess(
        electionGuid,
        isOpen,
      );

      const index = elections.value.findIndex(
        (e) => e.electionGuid === electionGuid,
      );
      if (index !== -1) {
        elections.value[index] = election;
      }

      if (currentElection.value?.electionGuid === electionGuid) {
        currentElection.value = election;
      }

      return election;
    } catch (e: any) {
      error.value = extractApiErrorMessage(e);
      throw e;
    } finally {
      loading.value = false;
    }
  }

  return {
    elections,
    currentElection,
    loading,
    error,
    activeElections,
    finalizedElections,
    currentStage,
    fetchElections,
    fetchElectionById,
    createElection,
    updateElection,
    deleteElection,
    setCurrentElection,
    clearError,
    initializeSignalR,
    joinElection,
    leaveElection,
    setStage,
    toggleTellerAccess,
  };
});
