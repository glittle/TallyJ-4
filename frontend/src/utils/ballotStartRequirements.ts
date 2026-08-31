export type BallotStartBlockReason =
  | "computerCode"
  | "location"
  | "onlineLocation"
  | "teller";

export const BALLOT_START_BLOCK_MESSAGE_KEY: Record<
  BallotStartBlockReason,
  string
> = {
  computerCode: "ballots.computerCodeRequired",
  location: "ballots.locationRequired",
  onlineLocation: "ballots.onlineLocationNotAllowed",
  teller: "ballots.tellerRequired",
};

/**
 * A new paper/teller ballot must not start without a computer code, a
 * non-Online location, and main teller (teller 1). Teller 2 is optional.
 * The Online location is reserved for voter-initiated ballots.
 */
export function getBallotStartBlockReason(input: {
  computerCode?: string | null;
  locationGuid?: string | null;
  locationType?: string | null;
  teller1?: string | null;
}): BallotStartBlockReason | null {
  if (!input.computerCode?.trim()) {
    return "computerCode";
  }
  if (!input.locationGuid?.trim()) {
    return "location";
  }
  if (isOnlineLocationType(input.locationType)) {
    return "onlineLocation";
  }
  if (!input.teller1?.trim()) {
    return "teller";
  }
  return null;
}

export function isOnlineLocationType(
  locationType?: string | null,
): boolean {
  return locationType?.trim().toLowerCase() === "online";
}

export function locationTypeForGuid(
  locations: ReadonlyArray<{
    locationGuid: string;
    locationType?: string | null;
  }>,
  locationGuid: string | null | undefined,
): string | null {
  if (!locationGuid) {
    return null;
  }
  return (
    locations.find((location) => location.locationGuid === locationGuid)
      ?.locationType ?? null
  );
}
