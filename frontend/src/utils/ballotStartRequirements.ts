export type BallotStartBlockReason = "computerCode" | "location" | "teller";

export const BALLOT_START_BLOCK_MESSAGE_KEY: Record<
  BallotStartBlockReason,
  string
> = {
  computerCode: "ballots.computerCodeRequired",
  location: "ballots.locationRequired",
  teller: "ballots.tellerRequired",
};

/**
 * A new paper ballot must not start without a computer code, location, and
 * main teller (teller 1). Teller 2 is optional.
 */
export function getBallotStartBlockReason(input: {
  computerCode?: string | null;
  locationGuid?: string | null;
  teller1?: string | null;
}): BallotStartBlockReason | null {
  if (!input.computerCode?.trim()) {
    return "computerCode";
  }
  if (!input.locationGuid?.trim()) {
    return "location";
  }
  if (!input.teller1?.trim()) {
    return "teller";
  }
  return null;
}
