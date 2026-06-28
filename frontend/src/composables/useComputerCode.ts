import { getComputerCode, setComputerCode } from "@/utils/computerCodeStorage";
import { ref } from "vue";

const computerCode = ref(getComputerCode());

export function useComputerCode() {
  function updateComputerCode(code: string) {
    setComputerCode(code);
    computerCode.value = getComputerCode();
  }

  function refreshComputerCode() {
    computerCode.value = getComputerCode();
  }

  return {
    computerCode,
    updateComputerCode,
    refreshComputerCode,
  };
}
