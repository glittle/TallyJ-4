export function formatReportDate(d?: string) {
  if (!d) {
    return "";
  }
  return new Date(d).toLocaleDateString();
}

export function formatReportDateTime(d?: string) {
  if (!d) {
    return "";
  }
  return new Date(d).toLocaleString();
}

export function formatReportPercent(v: number) {
  return (v / 100).toLocaleString(undefined, {
    style: "percent",
    minimumFractionDigits: 0,
  });
}

export const BALLOT_REPORT_CODES = [
  "Main",
  "VotesByNum",
  "VotesByName",
  "Ballots",
  "BallotsOnline",
  "BallotsImported",
  "BallotsTied",
  "SpoiledVotes",
  "BallotAlignment",
  "BallotsSame",
  "BallotsSummary",
] as const;

export function isBallotReportCode(code: string): boolean {
  return (BALLOT_REPORT_CODES as readonly string[]).includes(code);
}
