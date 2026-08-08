import { isGuestTeller } from "@/domain/guestTellerAccess";
import { defineStore } from "pinia";
import { computed, ref } from "vue";
import { electionService } from "../services/electionService";
import { signalrService } from "../services/signalrService";
import { useAuthStore } from "./authStore";

import { ElMessage } from "element-plus";
import type {
  CreateElectionDto,
  ElectionDto,
  UpdateElectionDto,
} from "../types";
import type { ElectionUpdateEvent } from "../types/SignalREvents";
import { setComputerCode } from "../utils/computerCodeStorage";
import { extractApiErrorMessage } from "../utils/errorHandler";
import {
  getActiveElectionHubGuid,
  setActiveElectionHubGuid,
} from "../utils/activeElectionHubStorage";
import { STAGE_META, type ElectionStage } from "../domain/electionStages";
import { i18n } from "../locales";

/** How long to suppress remote stage toasts after a local setStage (covers SignalR racing HTTP). */
const LOCAL_STAGE_NOTIFY_SUPPRESS_MS = 5000;

export const useElectionStore = defineStore("election", () => {
  const elections = ref<ElectionDto[]>([]);
  const currentElection = ref<ElectionDto | null>(null);
  const loading = ref(false);
  const error = ref<string | null>(null);
  const signalrInitialized = ref(false);
  /** electionGuid → suppress remote stage toast until this timestamp (ms). */
  const suppressRemoteStageNotifyUntil = new Map<string, number>();

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

      connection.on("electionClosed", () => {
        if (isGuestTeller()) {
          void handleGuestTellerClosedOut();
          return;
        }

        const electionGuid = currentElection.value?.electionGuid ?? "";
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

  async function handleGuestTellerClosedOut() {
    const authStore = useAuthStore();
    await authStore.logout("/teller-join?electionClosed=1");
  }

  function isRemoteStageNotifySuppressed(electionGuid: string): boolean {
    const until = suppressRemoteStageNotifyUntil.get(electionGuid);
    if (until === undefined) {
      return false;
    }
    if (Date.now() >= until) {
      suppressRemoteStageNotifyUntil.delete(electionGuid);
      return false;
    }
    return true;
  }

  function handleElectionUpdate(data: ElectionUpdateEvent) {
    const index = elections.value.findIndex(
      (e) => e.electionGuid === data.electionGuid,
    );
    const listMatch = index !== -1 ? elections.value[index] : undefined;
    const currentMatch =
      currentElection.value?.electionGuid === data.electionGuid
        ? currentElection.value
        : undefined;

    // Prefer list stage when both exist (they should match); fall back to current.
    const previousStage =
      listMatch?.electionStage ?? currentMatch?.electionStage;

    if (listMatch && index !== -1) {
      elections.value[index] = {
        ...listMatch,
        name: data.name ?? listMatch.name,
        electionStage: data.electionStage ?? listMatch.electionStage,
      } as ElectionDto;
    }

    if (currentMatch) {
      currentElection.value = {
        ...currentMatch,
        name: data.name ?? currentMatch.name,
        electionStage: data.electionStage ?? currentMatch.electionStage,
      } as ElectionDto;
    }

    const stageChanged =
      !!data.electionStage &&
      previousStage !== undefined &&
      data.electionStage !== previousStage;

    if (
      stageChanged &&
      data.electionStage &&
      !isRemoteStageNotifySuppressed(data.electionGuid)
    ) {
      showElectionStageNotification(data.electionStage);
    }
  }

  function showElectionStageNotification(newStage: string) {
    // Prefer `in` over Object.hasOwn so TS can narrow against STAGE_META keys.
    const meta =
      newStage in STAGE_META
        ? STAGE_META[newStage as ElectionStage]
        : undefined;
    const stageKey = meta ? meta.i18nKey : `elections.stage.${newStage}`;
    const stageLabel = i18n.global.t(stageKey);
    const message = i18n.global.t("elections.stageAdvanced", {
      stage: stageLabel,
    });
    ElMessage({
      message,
      type: "info",
      duration: 5000,
    });
  }

  async function joinElection(electionGuid: string) {
    try {
      const assignedCode = await signalrService.joinElection(electionGuid);
      if (assignedCode) {
        setComputerCode(electionGuid, assignedCode);
      }
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

  async function setActiveElectionHub(electionGuid: string) {
    const previousGuid = getActiveElectionHubGuid();
    if (previousGuid && previousGuid !== electionGuid) {
      await leaveElection(previousGuid);
    }

    setActiveElectionHubGuid(electionGuid);
    await initializeSignalR();
    await joinElection(electionGuid);
  }

  async function ensureActiveElectionHubConnection() {
    const electionGuid = getActiveElectionHubGuid();
    if (!electionGuid) {
      return;
    }

    await initializeSignalR();
    await joinElection(electionGuid);
  }

  async function clearActiveElectionHubConnection() {
    const electionGuid = getActiveElectionHubGuid();
    if (!electionGuid) {
      return;
    }

    setActiveElectionHubGuid(null);
    await leaveElection(electionGuid);
  }

  async function setStage(electionGuid: string, stage: ElectionStage) {
    loading.value = true;
    error.value = null;
    // StageControl shows the success toast; suppress the echo from statusChanged
    // (SignalR often arrives before the HTTP response updates local state).
    suppressRemoteStageNotifyUntil.set(
      electionGuid,
      Date.now() + LOCAL_STAGE_NOTIFY_SUPPRESS_MS,
    );
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
      suppressRemoteStageNotifyUntil.delete(electionGuid);
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
    setActiveElectionHub,
    ensureActiveElectionHubConnection,
    clearActiveElectionHubConnection,
    setStage,
    toggleTellerAccess,
  };
});
