import {
  getComputerCode,
  getComputerCodesState,
  refreshComputerCodeFromStorage,
  setComputerCode,
} from "@/utils/computerCodeStorage";
import { computed } from "vue";
import { useRoute } from "vue-router";

export function useComputerCode(electionGuid?: string) {
  const route = useRoute();
  const codesByElection = getComputerCodesState();

  const resolvedElectionGuid = computed(
    () => electionGuid ?? (route.params.id as string | undefined) ?? "",
  );

  const computerCode = computed(() => {
    const guid = resolvedElectionGuid.value;
    if (!guid) {
      return "";
    }

    // Track the reactive map so SignalR setComputerCode updates subscribers.
    const map = codesByElection.value;
    if (Object.hasOwn(map, guid)) {
      return map[guid] ?? "";
    }

    return getComputerCode(guid);
  });

  function applyAssignedCode(code: string) {
    const guid = resolvedElectionGuid.value;
    if (!guid) {
      return;
    }

    setComputerCode(guid, code);
  }

  function refreshComputerCode() {
    const guid = resolvedElectionGuid.value;
    if (!guid) {
      return;
    }

    refreshComputerCodeFromStorage(guid);
  }

  return {
    computerCode,
    resolvedElectionGuid,
    applyAssignedCode,
    refreshComputerCode,
  };
}
