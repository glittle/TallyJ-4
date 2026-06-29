import {
  getComputerCode,
  setComputerCode,
} from "@/utils/computerCodeStorage";
import { computed, ref, watch } from "vue";
import { useRoute } from "vue-router";

const assignedCodes = ref<Record<string, string>>({});

export function useComputerCode(electionGuid?: string) {
  const route = useRoute();
  const resolvedElectionGuid = computed(
    () => electionGuid ?? (route.params.id as string | undefined) ?? "",
  );

  const computerCode = computed(() => {
    const guid = resolvedElectionGuid.value;
    if (!guid) {
      return "";
    }

    return assignedCodes.value[guid] ?? getComputerCode(guid);
  });

  watch(
    resolvedElectionGuid,
    (guid) => {
      if (guid && assignedCodes.value[guid] === undefined) {
        assignedCodes.value[guid] = getComputerCode(guid);
      }
    },
    { immediate: true },
  );

  function applyAssignedCode(code: string) {
    const guid = resolvedElectionGuid.value;
    if (!guid) {
      return;
    }

    setComputerCode(guid, code);
    assignedCodes.value[guid] = code;
  }

  function refreshComputerCode() {
    const guid = resolvedElectionGuid.value;
    if (!guid) {
      return;
    }

    assignedCodes.value[guid] = getComputerCode(guid);
  }

  return {
    computerCode,
    resolvedElectionGuid,
    applyAssignedCode,
    refreshComputerCode,
  };
}