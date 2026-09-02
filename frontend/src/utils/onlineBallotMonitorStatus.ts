export type OnlineBallotMonitorTagType = "info" | "warning" | "success";

export interface OnlineBallotMonitorStatusView {
  labelKey: string;
  tagType: OnlineBallotMonitorTagType;
  /** True only for stored Status Submitted — voter can still change the vote. */
  stillEditable: boolean;
}

/**
 * Teller-facing view of OnlineVotingInfo.Status.
 * Processing is a persisted Accept-all claim (submit already blocked).
 * Processed is accepted. Submitted is still changeable — not the same as
 * summary Pending (Submitted + Processing).
 */
export function onlineBallotMonitorStatus(
  status: string | undefined,
): OnlineBallotMonitorStatusView {
  if (status === "Processing") {
    return {
      labelKey: "monitoring.onlineBallots.status.Processing",
      tagType: "warning",
      stillEditable: false,
    };
  }

  if (status === "Processed") {
    return {
      labelKey: "monitoring.onlineBallots.status.Processed",
      tagType: "success",
      stillEditable: false,
    };
  }

  if (status === "Submitted") {
    return {
      labelKey: "monitoring.onlineBallots.status.Submitted",
      tagType: "info",
      stillEditable: true,
    };
  }

  return {
    labelKey: "monitoring.onlineBallots.status.unknown",
    tagType: "info",
    stillEditable: false,
  };
}
