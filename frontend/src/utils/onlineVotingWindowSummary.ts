import { DateTime, Duration } from "luxon";

export type OnlineWindowSummaryLines = {
  openLine: string | null;
  closeLine: string | null;
  durationLine: string | null;
};

function toDateTime(value: Date | string | null | undefined): DateTime | null {
  if (value === null || value === undefined || value === "") {
    return null;
  }
  if (value instanceof Date) {
    const dt = DateTime.fromJSDate(value);
    return dt.isValid ? dt : null;
  }
  const dt = DateTime.fromISO(String(value));
  if (dt.isValid) {
    return dt;
  }
  const fromJs = DateTime.fromJSDate(new Date(value));
  return fromJs.isValid ? fromJs : null;
}

/**
 * Human-readable relative span for durations (e.g. "5 days", "1 hour 30 minutes").
 * Uses the largest useful units without being overly precise.
 */
export function formatDurationHuman(start: DateTime, end: DateTime): string {
  if (end <= start) {
    return "";
  }

  const diff = end.diff(start, ["days", "hours", "minutes"]).toObject();
  const days = Math.floor(diff.days ?? 0);
  const hours = Math.floor(diff.hours ?? 0);
  const minutes = Math.floor(diff.minutes ?? 0);

  const parts: Record<string, number> = {};
  if (days > 0) {
    parts.days = days;
    if (hours > 0 && days < 3) {
      parts.hours = hours;
    }
  } else if (hours > 0) {
    parts.hours = hours;
    if (minutes > 0) {
      parts.minutes = minutes;
    }
  } else {
    parts.minutes = Math.max(minutes, 1);
  }

  return Duration.fromObject(parts)
    .reconfigure({ locale: "en" })
    .toHuman({ listStyle: "long", unitDisplay: "long" });
}

/**
 * Build three summary lines for the online voting window from form values
 * (including unsaved changes).
 */
export function buildOnlineWindowSummary(
  openValue: Date | string | null | undefined,
  closeValue: Date | string | null | undefined,
  now: DateTime = DateTime.now(),
  t: (key: string, params?: Record<string, string>) => string = (key, params) =>
    // Fallback English for unit tests without i18n
    defaultMessage(key, params),
): OnlineWindowSummaryLines {
  const open = toDateTime(openValue);
  const close = toDateTime(closeValue);

  let openLine: string | null = null;
  if (open) {
    const relative = open.toRelative({ base: now }) ?? "";
    if (open > now) {
      openLine = t("elections.onlineWindow.willOpen", { relative });
    } else {
      // toRelative for past is "3 days ago" — "Opened 3 days ago."
      openLine = t("elections.onlineWindow.opened", { relative });
    }
  }

  let closeLine: string | null = null;
  if (close) {
    const relative = close.toRelative({ base: now }) ?? "";
    if (close > now) {
      closeLine = t("elections.onlineWindow.willClose", { relative });
    } else {
      closeLine = t("elections.onlineWindow.closed", { relative });
    }
  }

  let durationLine: string | null = null;
  if (open && close && close > open) {
    const duration = formatDurationHuman(open, close);
    if (duration) {
      durationLine = t("elections.onlineWindow.duration", { duration });
    }
  }

  return { openLine, closeLine, durationLine };
}

function defaultMessage(key: string, params?: Record<string, string>): string {
  const relative = params?.relative ?? "";
  const duration = params?.duration ?? "";
  switch (key) {
    case "elections.onlineWindow.willOpen":
      return `Will open ${relative}.`;
    case "elections.onlineWindow.opened":
      return `Opened ${relative}.`;
    case "elections.onlineWindow.willClose":
      return `Will close ${relative}.`;
    case "elections.onlineWindow.closed":
      return `Closed ${relative}.`;
    case "elections.onlineWindow.duration":
      return `Open for ${duration}.`;
    default:
      return key;
  }
}
