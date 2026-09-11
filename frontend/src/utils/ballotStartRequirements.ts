export type BallotStartBlockReason =
  | "computerCode"
  | "location"
  | "onlineLocation"
  | "importedLocation"
  | "teller";

export const BALLOT_START_BLOCK_MESSAGE_KEY: Record<
  BallotStartBlockReason,
  string
> = {
  computerCode: "ballots.computerCodeRequired",
  location: "ballots.locationRequired",
  onlineLocation: "ballots.onlineLocationNotAllowed",
  importedLocation: "ballots.importedLocationNotAllowed",
  teller: "ballots.tellerRequired",
};

/**
 * A new paper/teller ballot must not start without a computer code, a
 * non-reserved location (not Online / Imported), and main teller (teller 1).
 * Teller 2 is optional. Reserved rows are identified by LocationType, not by
 * display name (names are translated; English labels are not stable keys).
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
  if (isImportedLocationType(input.locationType)) {
    return "importedLocation";
  }
  if (!input.teller1?.trim()) {
    return "teller";
  }
  return null;
}

/** True when LocationType is Online (API enum), not when the location is named "Online". */
export function isOnlineLocationType(
  locationType?: string | null,
): boolean {
  return locationType?.trim().toLowerCase() === "online";
}

/** True when LocationType is Imported (API enum), not when named "Imported". */
export function isImportedLocationType(
  locationType?: string | null,
): boolean {
  return locationType?.trim().toLowerCase() === "imported";
}

/** Online and Imported are system-managed: i18n label, sort-only edit, no delete. */
export function isReservedLocationType(
  locationType?: string | null,
): boolean {
  return (
    isOnlineLocationType(locationType) || isImportedLocationType(locationType)
  );
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
