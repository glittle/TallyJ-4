import { DateTime } from "luxon";
import { describe, expect, it } from "vitest";
import {
  buildOnlineWindowSummary,
  formatDurationHuman,
} from "../onlineVotingWindowSummary";

describe("onlineVotingWindowSummary", () => {
  const now = DateTime.fromISO("2026-03-10T12:00:00.000Z");

  it("formats future open and close", () => {
    const open = now.plus({ days: 5 }).toJSDate();
    const close = now.plus({ days: 17 }).toJSDate();
    const lines = buildOnlineWindowSummary(open, close, now);
    expect(lines.openLine).toMatch(/Will open in 5 days/i);
    expect(lines.closeLine).toMatch(/Will close in 1[67] days/i);
    expect(lines.durationLine).toMatch(/Open for 12 days/i);
  });

  it("formats past open and future close", () => {
    const open = now.minus({ days: 3 }).toJSDate();
    const close = now.plus({ hours: 1, minutes: 30 }).toJSDate();
    const lines = buildOnlineWindowSummary(open, close, now);
    expect(lines.openLine).toMatch(/Opened 3 days ago/i);
    expect(lines.closeLine).toMatch(/Will close in/i);
    expect(lines.durationLine).toBeTruthy();
  });

  it("formats closed window", () => {
    const open = now.minus({ days: 2 }).toJSDate();
    const close = now.minus({ minutes: 5 }).toJSDate();
    const lines = buildOnlineWindowSummary(open, close, now);
    expect(lines.openLine).toMatch(/Opened/i);
    expect(lines.closeLine).toMatch(/Closed 5 minutes ago/i);
  });

  it("handles missing dates", () => {
    expect(buildOnlineWindowSummary(null, null, now)).toEqual({
      openLine: null,
      closeLine: null,
      durationLine: null,
    });
  });

  it("formatDurationHuman includes hours and minutes under a day", () => {
    const start = DateTime.fromISO("2026-03-10T12:00:00.000Z");
    const end = start.plus({ hours: 1, minutes: 30 });
    expect(formatDurationHuman(start, end)).toMatch(/1 hour.*30 minutes/i);
  });
});
