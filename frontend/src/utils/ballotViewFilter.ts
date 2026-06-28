import type { BallotSummaryDto } from "@/utils/ballotSummary";
import type { ComputerDto } from "@/types/Computer";
import type { LocationDto } from "@/types/Location";

export const ALL_BALLOTS_FILTER = "all";

export type BallotViewFilter =
  | { type: "all" }
  | { type: "location"; locationGuid: string }
  | { type: "computer"; locationGuid: string | null; computerCode: string };

export interface BallotViewFilterGroup {
  locationGuid: string;
  locationName: string;
  computerCodes: string[];
}

export function locationFilterValue(locationGuid: string): string {
  return `location|${locationGuid}`;
}

export function computerFilterValue(
  locationGuid: string | null,
  computerCode: string,
): string {
  return `computer|${locationGuid ?? "*"}|${computerCode}`;
}

export function parseBallotViewFilter(value: string): BallotViewFilter {
  if (value === ALL_BALLOTS_FILTER) {
    return { type: "all" };
  }

  if (value.startsWith("location|")) {
    return {
      type: "location",
      locationGuid: value.slice("location|".length),
    };
  }

  if (value.startsWith("computer|")) {
    const [, locationGuid, computerCode] = value.split("|");
    return {
      type: "computer",
      locationGuid: locationGuid === "*" ? null : locationGuid,
      computerCode,
    };
  }

  return { type: "all" };
}

export function matchesBallotViewFilter(
  ballot: BallotSummaryDto,
  filter: BallotViewFilter,
): boolean {
  switch (filter.type) {
    case "all":
      return true;
    case "location":
      return ballot.locationGuid === filter.locationGuid;
    case "computer":
      if (ballot.computerCode !== filter.computerCode) {
        return false;
      }
      if (!filter.locationGuid) {
        return true;
      }
      return ballot.locationGuid === filter.locationGuid;
  }
}

export function filterBallotsByView(
  ballots: BallotSummaryDto[],
  filterValue: string,
): BallotSummaryDto[] {
  const filter = parseBallotViewFilter(filterValue);
  if (filter.type === "all") {
    return ballots;
  }

  return ballots.filter((ballot) => matchesBallotViewFilter(ballot, filter));
}

export function buildBallotViewFilterGroups(
  locations: LocationDto[],
  ballots: BallotSummaryDto[],
  computersByLocation: Record<string, ComputerDto[]>,
): BallotViewFilterGroup[] {
  return [...locations]
    .sort((a, b) => {
      if (a.sortOrder !== b.sortOrder) {
        return (a.sortOrder ?? 0) - (b.sortOrder ?? 0);
      }
      return a.name.localeCompare(b.name);
    })
    .map((location) => {
      const codes = new Set<string>();

      for (const computer of computersByLocation[location.locationGuid] ?? []) {
        if (computer.computerCode) {
          codes.add(computer.computerCode);
        }
      }

      for (const ballot of ballots) {
        if (
          ballot.locationGuid === location.locationGuid &&
          ballot.computerCode
        ) {
          codes.add(ballot.computerCode);
        }
      }

      return {
        locationGuid: location.locationGuid,
        locationName: location.name,
        computerCodes: [...codes].sort((a, b) => a.localeCompare(b)),
      };
    })
    .filter(
      (group) =>
        group.computerCodes.length > 0 ||
        ballots.some((ballot) => ballot.locationGuid === group.locationGuid),
    );
}

export function defaultBallotViewFilter(
  computerCode: string,
  selectedLocationGuid: string | null,
): string {
  if (!computerCode) {
    return ALL_BALLOTS_FILTER;
  }

  if (selectedLocationGuid) {
    return computerFilterValue(selectedLocationGuid, computerCode);
  }

  return computerFilterValue(null, computerCode);
}
