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
