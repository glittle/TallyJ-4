import { afterEach, describe, expect, it } from "vitest";
import {
  getActiveElectionHubGuid,
  setActiveElectionHubGuid,
} from "../activeElectionHubStorage";

const GUID = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";

describe("activeElectionHubStorage", () => {
  afterEach(() => {
    setActiveElectionHubGuid(null);
  });

  it("stores and retrieves the active election hub guid", () => {
    expect(getActiveElectionHubGuid()).toBeNull();

    setActiveElectionHubGuid(GUID);
    expect(getActiveElectionHubGuid()).toBe(GUID);

    setActiveElectionHubGuid(null);
    expect(getActiveElectionHubGuid()).toBeNull();
  });
});