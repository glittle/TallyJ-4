import type { ElectionDto } from "@/types";

export function participationPct(
  election: Pick<ElectionDto, "voterCount" | "ballotCount">,
): number | null {
  if (!election.voterCount) {
    return null;
  }
  return Math.round((election.ballotCount / election.voterCount) * 100);
}

export function formatParticipationPct(
  election: Pick<ElectionDto, "voterCount" | "ballotCount">,
): string {
  const pct = participationPct(election);
  return pct !== null ? `${pct}%` : "—";
}
