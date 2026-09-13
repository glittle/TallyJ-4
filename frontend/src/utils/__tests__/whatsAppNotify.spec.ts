import { describe, expect, it } from "vitest";
import {
  canNotifySelectedWhatsApp,
  MAX_WHATSAPP_NOTIFY_SELECTED,
  notifyCancelledCount,
  whatsAppNotifyOutcomeLabel,
} from "../whatsAppNotify";

describe("whatsAppNotify", () => {
  it("enables send when some selected people have phones and the list is in bound", () => {
    expect(canNotifySelectedWhatsApp(2, 1)).toBe(true);
    expect(canNotifySelectedWhatsApp(0, 0)).toBe(false);
    expect(canNotifySelectedWhatsApp(1, 0)).toBe(false);
    expect(canNotifySelectedWhatsApp(MAX_WHATSAPP_NOTIFY_SELECTED + 1, 1)).toBe(
      false,
    );
  });

  it("maps outcomes to i18n keys", () => {
    expect(whatsAppNotifyOutcomeLabel("sent", (key) => key)).toBe(
      "people.notifyWhatsAppOutcome.sent",
    );
    expect(whatsAppNotifyOutcomeLabel("skipped-unchecked", (key) => key)).toBe(
      "people.notifyWhatsAppOutcome.skipped-unchecked",
    );
  });

  it("counts cancelled rows for the per-run summary", () => {
    expect(
      notifyCancelledCount([
        { outcome: "sent" },
        { outcome: "cancelled" },
        { outcome: "skipped-no-wa" },
        { outcome: "cancelled" },
      ]),
    ).toBe(2);
  });
});
