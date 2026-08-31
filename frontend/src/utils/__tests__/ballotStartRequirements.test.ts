import { describe, expect, it } from "vitest";
import {
  BALLOT_START_BLOCK_MESSAGE_KEY,
  getBallotStartBlockReason,
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

  it("maps each block reason to the matching i18n key", () => {
    expect(BALLOT_START_BLOCK_MESSAGE_KEY.location).toBe(
      "ballots.locationRequired",
    );
    expect(BALLOT_START_BLOCK_MESSAGE_KEY.teller).toBe(
      "ballots.tellerRequired",
    );
    expect(BALLOT_START_BLOCK_MESSAGE_KEY.computerCode).toBe(
      "ballots.computerCodeRequired",
    );
  });
});
