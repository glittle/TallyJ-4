const LEGACY_COMPUTER_CODE_KEY = "tallyj.computerCode";

function computerCodeKey(electionGuid: string): string {
  return `tallyj.computerCode.${electionGuid}`;
}

export function getComputerCode(electionGuid?: string): string {
  if (electionGuid) {
    return (localStorage.getItem(computerCodeKey(electionGuid)) ?? "")
      .trim()
      .toUpperCase();
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
}

export function isValidComputerCode(code: string): boolean {
  return /^[A-Z]{1,2}$/.test(code.trim().toUpperCase());
}
