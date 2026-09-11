import { shallowRef, type ShallowRef } from "vue";

const LEGACY_COMPUTER_CODE_KEY = "tallyj.computerCode";

/** In-memory codes so UI updates when SignalR join/reconnect writes a code. */
const codesByElection: ShallowRef<Record<string, string>> = shallowRef({});

function computerCodeKey(electionGuid: string): string {
  return `tallyj.computerCode.${electionGuid}`;
}

function readFromStorage(electionGuid: string): string {
  return (localStorage.getItem(computerCodeKey(electionGuid)) ?? "")
    .trim()
    .toUpperCase();
}

/**
 * Reactive map of election GUID → computer code.
 * Subscribe via this ref so setComputerCode updates the badge and ballot UI.
 */
export function getComputerCodesState(): ShallowRef<Record<string, string>> {
  return codesByElection;
}

export function getComputerCode(electionGuid?: string): string {
  if (electionGuid) {
    if (Object.hasOwn(codesByElection.value, electionGuid)) {
      return codesByElection.value[electionGuid] ?? "";
    }
    return readFromStorage(electionGuid);
  }

  return (localStorage.getItem(LEGACY_COMPUTER_CODE_KEY) ?? "")
    .trim()
    .toUpperCase();
}

export function setComputerCode(electionGuid: string, code: string): void {
  const normalized = code.trim().toUpperCase();
  if (normalized) {
    localStorage.setItem(computerCodeKey(electionGuid), normalized);
    localStorage.removeItem(LEGACY_COMPUTER_CODE_KEY);
  } else {
    localStorage.removeItem(computerCodeKey(electionGuid));
  }

  codesByElection.value = {
    ...codesByElection.value,
    [electionGuid]: normalized,
  };
}

/** Re-read localStorage into the reactive map for one election. */
export function refreshComputerCodeFromStorage(electionGuid: string): void {
  codesByElection.value = {
    ...codesByElection.value,
    [electionGuid]: readFromStorage(electionGuid),
  };
}

/** Clears the in-memory map (tests). Does not touch localStorage. */
export function resetComputerCodeCache(): void {
  codesByElection.value = {};
}

export function isValidComputerCode(code: string): boolean {
  return /^[A-Z]{1,2}$/.test(code.trim().toUpperCase());
}
