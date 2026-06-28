import { describe, it, expect } from "vitest";
import { createI18n } from "vue-i18n";
import { translateTallyProgressMessage } from "../tallyProgressMessages";

const i18n = createI18n({
  legacy: false,
  locale: "en",
  messages: {
    en: {
      "tally.progress.complete": "Tally calculation complete!",
      "tally.progress.processingBallots": "Processing ballots: {processed}/{total}",
      "tally.progress.starting": "Starting tally calculation...",
    },
  },
});

describe("translateTallyProgressMessage", () => {
  const t = i18n.global.t;
  const progress = { processedBallots: 4, totalBallots: 10 };

  it("translates a server phrase key", () => {
    expect(
      translateTallyProgressMessage("tally.progress.starting", progress, t),
    ).toBe("Starting tally calculation...");
  });

  it("translates phrase keys with ballot counts", () => {
    expect(
      translateTallyProgressMessage(
        "tally.progress.processingBallots",
        progress,
        t,
      ),
    ).toBe("Processing ballots: 4/10");
  });

  it("falls back when the phrase key is missing", () => {
    expect(
      translateTallyProgressMessage("tally.progress.unknown", progress, t),
    ).toBe("Processing ballots: 4/10");
  });
});