import { describe, expect, it } from "vitest";
import {
  BALLOT_START_BLOCK_MESSAGE_KEY,
  getBallotStartBlockReason,
  locationTypeForGuid,
} from "../ballotStartRequirements";

describe("getBallotStartBlockReason", () => {
  const ready = {
    computerCode: "AA",
    locationGuid: "loc-1",
    teller1: "Alice",
  };

  it("allows a ballot to start when computer, location, and teller 1 are set", () => {
    expect(getBallotStartBlockReason(ready)).toBeNull();
  });

  it("allows a ballot to start at a manual location", () => {
    expect(
      getBallotStartBlockReason({
        ...ready,
        locationType: "Manual",
      }),
    ).toBeNull();
  });

  it("does not require teller 2", () => {
    expect(
      getBallotStartBlockReason({
        ...ready,
        teller1: "Alice",
      }),
    ).toBeNull();
  });

  it("blocks when the computer code is unset", () => {
    expect(getBallotStartBlockReason({ ...ready, computerCode: "" })).toBe(
      "computerCode",
    );
    expect(getBallotStartBlockReason({ ...ready, computerCode: "  " })).toBe(
      "computerCode",
    );
    expect(getBallotStartBlockReason({ ...ready, computerCode: null })).toBe(
      "computerCode",
    );
  });

  it("blocks when the location is unset", () => {
    expect(getBallotStartBlockReason({ ...ready, locationGuid: "" })).toBe(
      "location",
    );
    expect(getBallotStartBlockReason({ ...ready, locationGuid: null })).toBe(
      "location",
    );
  });

  it("blocks when the selected location is Online", () => {
    expect(
      getBallotStartBlockReason({
        ...ready,
        locationType: "Online",
      }),
    ).toBe("onlineLocation");
    expect(
      getBallotStartBlockReason({
        ...ready,
        locationType: "online",
      }),
    ).toBe("onlineLocation");
  });

  it("blocks when the main teller is unset", () => {
    expect(getBallotStartBlockReason({ ...ready, teller1: "" })).toBe("teller");
    expect(getBallotStartBlockReason({ ...ready, teller1: "  " })).toBe(
      "teller",
    );
    expect(getBallotStartBlockReason({ ...ready, teller1: null })).toBe(
      "teller",
    );
  });

  it("checks computer code before location and teller", () => {
    expect(
      getBallotStartBlockReason({
        computerCode: "",
        locationGuid: "",
        teller1: "",
      }),
    ).toBe("computerCode");
  });

  it("checks location before teller when the computer code is set", () => {
    expect(
      getBallotStartBlockReason({
        computerCode: "AA",
        locationGuid: "",
        teller1: "",
      }),
    ).toBe("location");
  });

  it("checks Online location before teller when a location is set", () => {
    expect(
      getBallotStartBlockReason({
        computerCode: "AA",
        locationGuid: "loc-online",
        locationType: "Online",
        teller1: "",
      }),
    ).toBe("onlineLocation");
  });

  it("maps each block reason to the matching i18n key", () => {
    expect(BALLOT_START_BLOCK_MESSAGE_KEY.location).toBe(
      "ballots.locationRequired",
    );
    expect(BALLOT_START_BLOCK_MESSAGE_KEY.onlineLocation).toBe(
      "ballots.onlineLocationNotAllowed",
    );
    expect(BALLOT_START_BLOCK_MESSAGE_KEY.teller).toBe(
      "ballots.tellerRequired",
    );
    expect(BALLOT_START_BLOCK_MESSAGE_KEY.computerCode).toBe(
      "ballots.computerCodeRequired",
    );
  });

  it("looks up location type by guid", () => {
    const locations = [
      { locationGuid: "loc-1", locationType: "Manual" },
      { locationGuid: "loc-online", locationType: "Online" },
    ];
    expect(locationTypeForGuid(locations, "loc-1")).toBe("Manual");
    expect(locationTypeForGuid(locations, "loc-online")).toBe("Online");
    expect(locationTypeForGuid(locations, "missing")).toBeNull();
    expect(locationTypeForGuid(locations, null)).toBeNull();
  });
});
