import {
  clearActiveTellers,
  getActiveTellers,
  setActiveTeller1,
  setActiveTeller2,
  type ActiveTellers,
} from "@/utils/activeTellerStorage";
import { ref } from "vue";

const sessionTellers = ref<ActiveTellers>(getActiveTellers());

/**
 * Browser-session Teller 1/2 (localStorage). Shared so the ballot listing
 * and an open ballot edit the same values.
 */
export function useActiveTellers() {
  function refreshActiveTellers() {
    sessionTellers.value = getActiveTellers();
  }

  function setTeller1(name: string) {
    setActiveTeller1(name);
    sessionTellers.value = {
      ...sessionTellers.value,
      teller1: name,
    };
  }

  function setTeller2(name: string) {
    setActiveTeller2(name);
    sessionTellers.value = {
      ...sessionTellers.value,
      teller2: name,
    };
  }

  function clearTellers() {
    clearActiveTellers();
    sessionTellers.value = { teller1: "", teller2: "" };
  }

  return {
    tellers: sessionTellers,
    setTeller1,
    setTeller2,
    clearTellers,
    refreshActiveTellers,
  };
}
