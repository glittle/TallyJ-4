import { defineStore } from "pinia";
import { ref } from "vue";
import { tellerService } from "@/services/tellerService";
import { signalrService } from "@/services/signalrService";
import type { Teller, CreateTellerDto, UpdateTellerDto } from "@/types/teller";

export interface TellerUpdateEvent {
  electionGuid: string;
  rowId: number;
  name: string;
  action: string;
}

export const useTellerStore = defineStore("teller", () => {
  const tellers = ref<Teller[]>([]);
  const currentTeller = ref<Teller | null>(null);
  const loading = ref(false);
  const error = ref<string | null>(null);
  const totalCount = ref(0);
  const currentPage = ref(1);
  const pageSize = ref(50);
  const listedElectionGuid = ref<string | null>(null);
  const signalrInitialized = ref(false);

  function sortTellers() {
    tellers.value.sort((a, b) =>
      a.name.localeCompare(b.name, undefined, { sensitivity: "base" }),
    );
  }

  function upsertTeller(teller: Teller) {
    const byId = tellers.value.findIndex((t) => t.rowId === teller.rowId);
    if (byId !== -1) {
      tellers.value[byId] = teller;
    } else {
      const byName = tellers.value.findIndex(
        (t) => t.name.toLowerCase() === teller.name.toLowerCase(),
      );
      if (byName !== -1) {
        tellers.value[byName] = teller;
      } else {
        tellers.value.push(teller);
        totalCount.value++;
      }
    }
    sortTellers();
  }

  function normalizeTellerUpdate(data: unknown): TellerUpdateEvent | null {
    if (!data || typeof data !== "object") {
      return null;
    }
    const raw = data as Record<string, unknown>;
    const electionGuid = String(raw.electionGuid ?? raw.ElectionGuid ?? "");
    const rowId = Number(raw.rowId ?? raw.RowId);
    const name = String(raw.name ?? raw.Name ?? "");
    const action = String(raw.action ?? raw.Action ?? "").toLowerCase();
    if (!electionGuid || !Number.isFinite(rowId)) {
      return null;
    }
    return { electionGuid, rowId, name, action };
  }

  function applyTellerUpdate(update: TellerUpdateEvent) {
    if (
      !listedElectionGuid.value ||
      update.electionGuid.toLowerCase() !==
        listedElectionGuid.value.toLowerCase()
    ) {
      return;
    }

    if (update.action === "deleted") {
      const existed = tellers.value.some((t) => t.rowId === update.rowId);
      tellers.value = tellers.value.filter((t) => t.rowId !== update.rowId);
      if (existed) {
        totalCount.value = Math.max(0, totalCount.value - 1);
      }
      if (currentTeller.value?.rowId === update.rowId) {
        currentTeller.value = null;
      }
      return;
    }

    upsertTeller({
      rowId: update.rowId,
      electionGuid: update.electionGuid,
      name: update.name,
    });
  }

  async function initializeSignalR() {
    if (signalrInitialized.value) {
      return;
    }

    try {
      const connection = await signalrService.connectToMainHub();
      connection.on("tellersChanged", (data: unknown) => {
        const update = normalizeTellerUpdate(data);
        if (update) {
          applyTellerUpdate(update);
        }
      });
      signalrInitialized.value = true;
    } catch (e) {
      console.error("Failed to initialize SignalR for teller store:", e);
    }
  }

  async function fetchTellers(electionGuid: string, page = 1, size = 50) {
    loading.value = true;
    error.value = null;
    listedElectionGuid.value = electionGuid;
    try {
      const response = await tellerService.getTellersByElection(
        electionGuid,
        page,
        size,
      );
      tellers.value = [...response.items];
      sortTellers();
      totalCount.value = response.totalCount;
      currentPage.value = response.pageNumber;
      pageSize.value = response.pageSize;
      await initializeSignalR();
    } catch (err: any) {
      error.value = err.message || "Failed to fetch tellers";
      throw err;
    } finally {
      loading.value = false;
    }
  }

  async function fetchTellerById(electionGuid: string, rowId: number) {
    loading.value = true;
    error.value = null;
    try {
      const teller = await tellerService.getTellerById(electionGuid, rowId);
      currentTeller.value = teller;
      return teller;
    } catch (err: any) {
      error.value = err.message || "Failed to fetch teller";
      throw err;
    } finally {
      loading.value = false;
    }
  }

  async function createTeller(
    electionGuid: string,
    tellerData: CreateTellerDto,
  ) {
    loading.value = true;
    error.value = null;
    listedElectionGuid.value = electionGuid;
    try {
      const newTeller = await tellerService.createTeller(
        electionGuid,
        tellerData,
      );
      upsertTeller(newTeller);
      await initializeSignalR();
      return newTeller;
    } catch (err: any) {
      error.value = err.message || "Failed to create teller";
      throw err;
    } finally {
      loading.value = false;
    }
  }

  async function updateTeller(
    electionGuid: string,
    rowId: number,
    tellerData: UpdateTellerDto,
  ) {
    loading.value = true;
    error.value = null;
    try {
      const updatedTeller = await tellerService.updateTeller(
        electionGuid,
        rowId,
        tellerData,
      );
      upsertTeller(updatedTeller);
      if (currentTeller.value?.rowId === rowId) {
        currentTeller.value = updatedTeller;
      }
      return updatedTeller;
    } catch (err: any) {
      error.value = err.message || "Failed to update teller";
      throw err;
    } finally {
      loading.value = false;
    }
  }

  async function deleteTeller(electionGuid: string, rowId: number) {
    loading.value = true;
    error.value = null;
    try {
      await tellerService.deleteTeller(electionGuid, rowId);
      const existed = tellers.value.some((t) => t.rowId === rowId);
      tellers.value = tellers.value.filter((t) => t.rowId !== rowId);
      if (existed) {
        totalCount.value = Math.max(0, totalCount.value - 1);
      }
      if (currentTeller.value?.rowId === rowId) {
        currentTeller.value = null;
      }
    } catch (err: any) {
      error.value = err.message || "Failed to delete teller";
      throw err;
    } finally {
      loading.value = false;
    }
  }

  function clearError() {
    error.value = null;
  }

  function $reset() {
    tellers.value = [];
    currentTeller.value = null;
    loading.value = false;
    error.value = null;
    totalCount.value = 0;
    currentPage.value = 1;
    pageSize.value = 50;
    listedElectionGuid.value = null;
  }

  return {
    tellers,
    currentTeller,
    loading,
    error,
    totalCount,
    currentPage,
    pageSize,
    listedElectionGuid,
    fetchTellers,
    fetchTellerById,
    createTeller,
    updateTeller,
    deleteTeller,
    applyTellerUpdate,
    initializeSignalR,
    clearError,
    $reset,
  };
});
