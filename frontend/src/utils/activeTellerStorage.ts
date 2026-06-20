const ACTIVE_TELLER1_KEY = "tallyj.activeTeller1";
const ACTIVE_TELLER2_KEY = "tallyj.activeTeller2";

export interface ActiveTellers {
  teller1: string;
  teller2: string;
}

export function getActiveTellers(): ActiveTellers {
  return {
    teller1: localStorage.getItem(ACTIVE_TELLER1_KEY) ?? "",
    teller2: localStorage.getItem(ACTIVE_TELLER2_KEY) ?? "",
  };
}

export function setActiveTeller1(name: string): void {
  if (name) {
    localStorage.setItem(ACTIVE_TELLER1_KEY, name);
  } else {
    localStorage.removeItem(ACTIVE_TELLER1_KEY);
  }
}

export function setActiveTeller2(name: string): void {
  if (name) {
    localStorage.setItem(ACTIVE_TELLER2_KEY, name);
  } else {
    localStorage.removeItem(ACTIVE_TELLER2_KEY);
  }
}

export function clearActiveTellers(): void {
  localStorage.removeItem(ACTIVE_TELLER1_KEY);
  localStorage.removeItem(ACTIVE_TELLER2_KEY);
}

export function getActiveTellerPayload(): {
  teller1?: string;
  teller2?: string;
} {
  const { teller1, teller2 } = getActiveTellers();
  return {
    teller1: teller1.trim() || undefined,
    teller2: teller2.trim() || undefined,
  };
}
