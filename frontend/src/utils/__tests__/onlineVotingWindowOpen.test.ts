import { describe, expect, it } from "vitest";
import { isOnlineVotingCurrentlyOpen } from "../onlineVotingWindowOpen";

const now = Date.parse("2026-09-09T12:00:00.000Z");

describe("isOnlineVotingCurrentlyOpen", () => {
  it("is closed when online voting is not enabled", () => {
    expect(
      isOnlineVotingCurrentlyOpen(
        {
          useOnlineVoting: false,
          onlineWhenOpen: "2026-09-09T11:00:00.000Z",
          onlineWhenClose: "2026-09-09T13:00:00.000Z",
        },
        now,
      ),
    ).toBe(false);
  });

  it("treats UseOnlineVoting plus a null window as open", () => {
    expect(
      isOnlineVotingCurrentlyOpen(
        { useOnlineVoting: true, onlineWhenOpen: null, onlineWhenClose: null },
        now,
      ),
    ).toBe(true);
  });

  it("is closed before the scheduled open", () => {
    expect(
      isOnlineVotingCurrentlyOpen(
        {
          useOnlineVoting: true,
          onlineWhenOpen: "2026-09-09T13:00:00.000Z",
          onlineWhenClose: "2026-09-09T14:00:00.000Z",
        },
        now,
      ),
    ).toBe(false);
  });

  it("is closed at or after the close time", () => {
    expect(
      isOnlineVotingCurrentlyOpen(
        {
          useOnlineVoting: true,
          onlineWhenOpen: "2026-09-09T11:00:00.000Z",
          onlineWhenClose: "2026-09-09T12:00:00.000Z",
        },
        now,
      ),
    ).toBe(false);
  });

  it("is open inside a scheduled window", () => {
    expect(
      isOnlineVotingCurrentlyOpen(
        {
          useOnlineVoting: true,
          onlineWhenOpen: "2026-09-09T11:00:00.000Z",
          onlineWhenClose: "2026-09-09T13:00:00.000Z",
        },
        now,
      ),
    ).toBe(true);
  });
});
