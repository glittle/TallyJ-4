import { beforeEach, describe, expect, it } from "vitest";
import {
  getActiveTellers,
  setActiveTeller1,
  setActiveTeller2,
} from "@/utils/activeTellerStorage";
import { useActiveTellers } from "../useActiveTellers";

describe("useActiveTellers", () => {
  beforeEach(() => {
    localStorage.clear();
    useActiveTellers().refreshActiveTellers();
  });

  it("writes Teller 1/2 to the same session storage as the listing", () => {
    const session = useActiveTellers();
    session.setTeller1("Pat");
    session.setTeller2("Sam");

    expect(getActiveTellers()).toEqual({ teller1: "Pat", teller2: "Sam" });
    expect(session.tellers.value).toEqual({ teller1: "Pat", teller2: "Sam" });
  });

  it("shares one session object across callers", () => {
    const listing = useActiveTellers();
    const openBallot = useActiveTellers();

    listing.setTeller1("Keyboard");
    openBallot.setTeller2("Assistant");

    expect(listing.tellers.value).toEqual({
      teller1: "Keyboard",
      teller2: "Assistant",
    });
    expect(openBallot.tellers.value).toBe(listing.tellers.value);
  });

  it("refreshes from storage when another path wrote the keys", () => {
    setActiveTeller1("Stored1");
    setActiveTeller2("Stored2");

    const session = useActiveTellers();
    session.refreshActiveTellers();

    expect(session.tellers.value).toEqual({
      teller1: "Stored1",
      teller2: "Stored2",
    });
  });
});
