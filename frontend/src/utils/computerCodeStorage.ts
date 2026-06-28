const COMPUTER_CODE_KEY = "tallyj.computerCode";

export function getComputerCode(): string {
  return (localStorage.getItem(COMPUTER_CODE_KEY) ?? "").trim().toUpperCase();
}

export function setComputerCode(code: string): void {
  const normalized = code.trim().toUpperCase();
  if (normalized) {
    localStorage.setItem(COMPUTER_CODE_KEY, normalized);
  } else {
    localStorage.removeItem(COMPUTER_CODE_KEY);
  }
}

export function isValidComputerCode(code: string): boolean {
  return /^[A-Z0-9]{2}$/.test(code.trim().toUpperCase());
}
