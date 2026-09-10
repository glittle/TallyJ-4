import { describe, it, expect, vi, beforeEach } from "vitest";
import type { VNode } from "vue";
import {
  notifyElectionStageChanged,
  STAGE_NOTICE_GO_THERE_MS,
  STAGE_NOTICE_TEXT_MS,
} from "../electionStageNotice";

const elMessageMock = vi.fn();

vi.mock("element-plus", () => ({
  ElMessage: (opts: unknown) => elMessageMock(opts),
}));

vi.mock("@/locales", () => ({
  i18n: {
    global: {
      t: (key: string, opts?: Record<string, string>) => {
        if (key === "elections.stageAdvanced" && opts?.stage) {
          return `Election changed to ${opts.stage}`;
        }
        if (key === "elections.stage.GatheringBallots") {
          return "Gathering Ballots";
        }
        if (key === "elections.stage.ProcessingBallots") {
          return "Processing Ballots";
        }
        if (key === "elections.stage.Finalized") {
          return "Finalized";
        }
        if (key === "elections.goToStagePage") {
          return "Go there";
        }
        return key;
      },
    },
  },
}));

vi.mock("@/domain/guestTellerAccess", () => ({
  isFullTeller: vi.fn(() => true),
  isGuestTeller: vi.fn(() => false),
}));

vi.mock("@/router/router", () => ({
  router: {
    push: vi.fn(),
    currentRoute: { value: { path: "/elections/elec-1/people" } },
  },
}));

function noticeOpts() {
  return elMessageMock.mock.calls[0]![0] as {
    message: string | VNode;
    type: string;
    duration: number;
    showClose?: boolean;
  };
}

function noticeText(message: string | VNode): string {
  if (typeof message === "string") {
    return message;
  }
  const children = message.children;
  if (!Array.isArray(children) || children.length === 0) {
    return "";
  }
  const textNode = children[0] as VNode;
  return typeof textNode.children === "string" ? textNode.children : "";
}

function goThereClick(message: string | VNode): (() => void) | undefined {
  if (typeof message === "string") {
    return undefined;
  }
  const children = message.children;
  if (!Array.isArray(children) || children.length < 2) {
    return undefined;
  }
  const button = children[1] as VNode;
  const onClick = button.props?.onClick as
    | ((event: MouseEvent) => void)
    | undefined;
  if (!onClick) {
    return undefined;
  }
  return () =>
    onClick(new MouseEvent("click", { bubbles: true, cancelable: true }));
}

describe("notifyElectionStageChanged", () => {
  const navigate = vi.fn();

  beforeEach(() => {
    elMessageMock.mockClear();
    navigate.mockReset();
  });

  it("FullTeller toast stays put and wires Go there to the stage work page", () => {
    notifyElectionStageChanged("elec-1", "GatheringBallots", {
      isFullTeller: true,
      currentPath: "/elections/elec-1/people",
      navigate,
    });

    const opts = noticeOpts();
    expect(opts.type).toBe("info");
    expect(opts.duration).toBe(STAGE_NOTICE_GO_THERE_MS);
    expect(opts.showClose).toBe(true);
    expect(noticeText(opts.message)).toBe(
      "Election changed to Gathering Ballots",
    );
    expect(navigate).not.toHaveBeenCalled();

    const click = goThereClick(opts.message);
    expect(click).toBeDefined();
    click!();
    expect(navigate).toHaveBeenCalledTimes(1);
    expect(navigate).toHaveBeenCalledWith("/elections/elec-1/frontdesk");
  });

  it("FullTeller Go there opens Enter Ballots for ProcessingBallots", () => {
    notifyElectionStageChanged("elec-1", "ProcessingBallots", {
      isFullTeller: true,
      currentPath: "/elections/elec-1/frontdesk",
      navigate,
    });

    const click = goThereClick(noticeOpts().message);
    expect(click).toBeDefined();
    click!();
    expect(navigate).toHaveBeenCalledWith("/elections/elec-1/ballots");
  });

  it("FullTeller already on the work page gets text only (no Go there)", () => {
    notifyElectionStageChanged("elec-1", "GatheringBallots", {
      isFullTeller: true,
      currentPath: "/elections/elec-1/frontdesk",
      navigate,
    });

    const opts = noticeOpts();
    expect(opts.message).toBe("Election changed to Gathering Ballots");
    expect(opts.duration).toBe(STAGE_NOTICE_TEXT_MS);
    expect(goThereClick(opts.message)).toBeUndefined();
    expect(navigate).not.toHaveBeenCalled();
  });

  it("GuestTeller toast is text only (auto-redirect is separate)", () => {
    notifyElectionStageChanged("elec-1", "ProcessingBallots", {
      isFullTeller: false,
      currentPath: "/elections/elec-1/frontdesk",
      navigate,
    });

    const opts = noticeOpts();
    expect(opts.message).toBe("Election changed to Processing Ballots");
    expect(opts.duration).toBe(STAGE_NOTICE_TEXT_MS);
    expect(opts.showClose).toBeFalsy();
    expect(goThereClick(opts.message)).toBeUndefined();
    expect(navigate).not.toHaveBeenCalled();
  });
});
