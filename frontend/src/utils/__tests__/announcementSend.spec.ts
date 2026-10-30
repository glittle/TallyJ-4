import { describe, expect, it } from "vitest";
import {
  canSendAnnouncement,
  formatSendOutcome,
  isEmptyAnnouncementRun,
  MAX_ANNOUNCEMENT_SELECTED,
  personHasEmail,
} from "../announcementSend";

describe("announcementSend", () => {
  it("enables send when a selected person has an address and the list is in bound", () => {
    expect(canSendAnnouncement(2, 1)).toBe(true);
    expect(canSendAnnouncement(0, 0)).toBe(false);
    expect(canSendAnnouncement(1, 0)).toBe(false);
    expect(canSendAnnouncement(MAX_ANNOUNCEMENT_SELECTED + 1, 1)).toBe(false);
    expect(personHasEmail({ email: " ada@example.com " })).toBe(true);
    expect(personHasEmail({ email: "  " })).toBe(false);
  });

  it("labels known outcomes and block reasons, and keeps sanitized provider text", () => {
    const t = (key: string) => {
      const labels: Record<string, string> = {
        "people.notifyWhatsAppOutcome.failed": "Failed",
        "people.notifyWhatsAppOutcome.sent": "Sent",
        "people.sendOutcome.skipped-no-email": "Skipped (no email)",
        "people.announcementError.election-cap": "Election cap",
      };
      return labels[key] ?? key;
    };

    expect(formatSendOutcome("failed", "election-cap", t)).toBe(
      "Failed — Election cap",
    );
    expect(formatSendOutcome("sent", null, t)).toBe("Sent");
    expect(formatSendOutcome("skipped-no-email", undefined, t)).toBe(
      "Skipped (no email)",
    );
    expect(formatSendOutcome("failed", "HTTP 500", t)).toBe(
      "Failed — HTTP 500",
    );
    expect(formatSendOutcome("failed", " election-cap ", (key) => key)).toBe(
      "failed — election-cap",
    );
  });

  it("treats a retry with no failed recipients as an empty run", () => {
    expect(
      isEmptyAnnouncementRun({ queueToken: "", running: false, results: [] }),
    ).toBe(true);
    expect(
      isEmptyAnnouncementRun({
        queueToken: "token",
        running: false,
        results: [],
      }),
    ).toBe(false);
    expect(
      isEmptyAnnouncementRun({
        queueToken: "",
        running: true,
        results: [],
      }),
    ).toBe(false);
  });
});
