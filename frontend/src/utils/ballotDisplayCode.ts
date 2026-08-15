const ONLINE_CODES = new Set(["OL", "WW"]);
const IMPORTED_CODES = new Set(["IM", "IMPORT"]);

export function isOnlineComputerCode(code?: string | null): boolean {
  return !!code && ONLINE_CODES.has(code.trim().toUpperCase());
}

export function isImportedComputerCode(code?: string | null): boolean {
  return !!code && IMPORTED_CODES.has(code.trim().toUpperCase());
}

export function isOnlineOrImportedComputerCode(code?: string | null): boolean {
  return isOnlineComputerCode(code) || isImportedComputerCode(code);
}

function trailingNumber(value?: string | null): number | null {
  if (!value) {
    return null;
  }
  const match = value.match(/(\d+)\s*$/);
  return match ? Number(match[1]) : null;
}

export function formatBallotDisplayCode(
  t: (key: string, values?: Record<string, unknown>) => string,
  ballot: {
    computerCode?: string | null;
    ballotNumAtComputer?: number | null;
    ballotCode?: string | null;
    isOnline?: boolean;
    isImported?: boolean;
  },
): string {
  const number =
    ballot.ballotNumAtComputer && ballot.ballotNumAtComputer > 0
      ? ballot.ballotNumAtComputer
      : trailingNumber(ballot.ballotCode);

  if (ballot.isOnline || isOnlineComputerCode(ballot.computerCode)) {
    return t("ballots.onlineCode", { n: number ?? ballot.ballotCode ?? "" });
  }

  if (ballot.isImported || isImportedComputerCode(ballot.computerCode)) {
    return t("ballots.importedCode", { n: number ?? ballot.ballotCode ?? "" });
  }

  return ballot.ballotCode || `${ballot.computerCode ?? ""}${number ?? ""}`;
}

export function formatComputerCodeLabel(
  t: (key: string, values?: Record<string, unknown>) => string,
  computerCode?: string | null,
): string {
  if (isOnlineComputerCode(computerCode)) {
    return t("ballots.onlineComputer");
  }
  if (isImportedComputerCode(computerCode)) {
    return t("ballots.importedComputer");
  }
  return computerCode ?? "";
}
