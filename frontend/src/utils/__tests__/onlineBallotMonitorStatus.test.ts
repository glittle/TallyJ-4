import { describe, expect, it } from "vitest";
import { onlineBallotMonitorStatus } from "../onlineBallotMonitorStatus";

describe("onlineBallotMonitorStatus", () => {
  it("treats Submitted as still changeable", () => {
    const view = onlineBallotMonitorStatus("Submitted");
    expect(view.stillEditable).toBe(true);
    expect(view.labelKey).toBe("monitoring.onlineBallots.status.Submitted");
    expect(view.tagType).toBe("info");
  });

  it("does not treat Processing as still changeable", () => {
    const view = onlineBallotMonitorStatus("Processing");
    expect(view.stillEditable).toBe(false);
    expect(view.labelKey).toBe("monitoring.onlineBallots.status.Processing");
    expect(view.tagType).toBe("warning");
  });

  it("treats Processed as accepted", () => {
    const view = onlineBallotMonitorStatus("Processed");
    expect(view.stillEditable).toBe(false);
    expect(view.labelKey).toBe("monitoring.onlineBallots.status.Processed");
    expect(view.tagType).toBe("success");
  });

  it("does not treat an unknown status as still changeable", () => {
    const view = onlineBallotMonitorStatus("Draft");
    expect(view.stillEditable).toBe(false);
    expect(view.labelKey).toBe("monitoring.onlineBallots.status.unknown");
  });
});
