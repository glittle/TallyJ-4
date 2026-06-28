import type { ComposerTranslation } from "vue-i18n";
import type { TallyProgressEvent } from "@/types/SignalREvents";

const FALLBACK_KEY = "tally.progress.processingBallots";

export function translateTallyProgressMessage(
  messageKey: string,
  progress: Pick<TallyProgressEvent, "processedBallots" | "totalBallots">,
  t: ComposerTranslation,
): string {
  const key = messageKey?.trim() || FALLBACK_KEY;
  const translated = t(key, {
    processed: progress.processedBallots,
    total: progress.totalBallots,
  });

  return translated !== key ? translated : t(FALLBACK_KEY, {
    processed: progress.processedBallots,
    total: progress.totalBallots,
  });
}