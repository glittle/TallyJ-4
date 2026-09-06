import { isOnlineLocationType } from "@/utils/ballotStartRequirements";

/** Label for a location. Online-typed rows use i18n; names are not identifiers. */
export function formatLocationLabel(
  t: (key: string) => string,
  location: {
    name?: string | null;
    locationType?: string | null;
  },
): string {
  if (isOnlineLocationType(location.locationType)) {
    return t("locations.typeOnline");
  }
  return location.name?.trim() || "";
}

/** Resolve a location by guid, then apply formatLocationLabel. */
export function formatLocationLabelForGuid(
  t: (key: string) => string,
  locations: ReadonlyArray<{
    locationGuid: string;
    name?: string | null;
    locationType?: string | null;
  }>,
  locationGuid: string | null | undefined,
  fallbackName?: string | null,
): string {
  if (!locationGuid) {
    return fallbackName?.trim() || "";
  }

  const location = locations.find((item) => item.locationGuid === locationGuid);
  if (location) {
    return formatLocationLabel(t, location);
  }

  return fallbackName?.trim() || "";
}
