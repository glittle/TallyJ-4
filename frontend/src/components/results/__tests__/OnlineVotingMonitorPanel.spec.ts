import { mount } from "@vue/test-utils";
import { describe, expect, it } from "vitest";
import { i18n } from "@/test/setup";
import type { OnlineVotingInfoDto } from "@/types";
import type { OnlineVotingCloseCountdown } from "@/utils/onlineVotingCloseCountdown";
import OnlineVotingMonitorPanel from "../OnlineVotingMonitorPanel.vue";

const stubs = {
  ElCard: {
    template: "<div class='el-card'><slot name='header' /><slot /></div>",
  },
  ElButton: {
    props: ["type"],
    template:
      "<button v-bind='$attrs' :data-button-type='type'><slot /></button>",
  },
  ElTag: { template: "<span><slot /></span>" },
  ElTable: {
    props: ["data"],
    template: "<table><slot /></table>",
  },
  ElTableColumn: { template: "<td />" },
};

function info(
  overrides: Partial<OnlineVotingInfoDto> = {},
): OnlineVotingInfoDto {
  return {
    totalOnlineBallots: 4,
    processedOnlineBallots: 1,
    pendingOnlineBallots: 3,
    submittedOnlineBallots: 2,
    processingOnlineBallots: 1,
    pendingOnlineVotedAnotherWay: 0,
    onlineVotingEnabled: true,
    connectedOnlineVoterSessions: 2,
    acceptAllRuns: [],
    ...overrides,
  };
}

function countdown(
  overrides: Partial<OnlineVotingCloseCountdown> = {},
): OnlineVotingCloseCountdown {
  return {
    hasCloseTime: true,
    isWindowOpen: true,
    isClosed: false,
    isClosingSoon: false,
    remainingMs: 60 * 60 * 1000,
    ...overrides,
  };
}

function mountPanel(
  infoOverrides: Partial<OnlineVotingInfoDto> = {},
  countdownOverrides: Partial<OnlineVotingCloseCountdown> = {},
) {
  return mount(OnlineVotingMonitorPanel, {
    props: {
      info: info(infoOverrides),
      closeCountdown: countdown(countdownOverrides),
      closeLine: "Will close in 8 hours.",
      closeRemainingClock: "4:20",
      canManage: true,
      accepting: false,
      updatingWindow: false,
    },
    global: { plugins: [i18n], stubs },
  });
}

describe("OnlineVotingMonitorPanel", () => {
  it("puts window status and actions above the count strip", () => {
    const wrapper = mountPanel();
    const html = wrapper.html();
    const statusAt = html.indexOf('data-testid="online-close-status"');
    const countsAt = html.indexOf(
      'data-testid="online-ballot-status-breakdown"',
    );
    const historyAt = html.indexOf("Accept-all record");

    expect(statusAt).toBeGreaterThan(-1);
    expect(countsAt).toBeGreaterThan(statusAt);
    expect(historyAt).toBeGreaterThan(countsAt);
    expect(wrapper.findAll(".el-descriptions").length).toBe(0);
    expect(wrapper.text()).not.toContain("Pending vs accepted");
    expect(wrapper.findAll("h3").map((heading) => heading.text())).toEqual([
      "Accept-all record",
    ]);
  });

  it("keeps pending, submitted, processing, accepted, sessions, and voted-another-way counts", () => {
    const wrapper = mountPanel({ pendingOnlineVotedAnotherWay: 2 });

    expect(
      wrapper.find("[data-testid='pending-online-ballots-count']").text(),
    ).toBe("3");
    expect(
      wrapper.find("[data-testid='submitted-online-ballots-count']").text(),
    ).toBe("2");
    expect(
      wrapper.find("[data-testid='processing-online-ballots-count']").text(),
    ).toBe("1");
    expect(
      wrapper.find("[data-testid='accepted-online-ballots-count']").text(),
    ).toBe("1");
    expect(
      wrapper
        .find("[data-testid='connected-online-voter-sessions-count']")
        .text(),
    ).toBe("2");
    expect(
      wrapper
        .find("[data-testid='pending-online-voted-another-way-count']")
        .text(),
    ).toBe("2");
    expect(wrapper.text()).toContain("Online ballots");
    expect(wrapper.text()).toContain("4");
    expect(
      wrapper.find("[data-testid='online-ballots-unexplained']").exists(),
    ).toBe(false);
    expect(
      wrapper.find("[data-testid='total-online-ballots']").text(),
    ).toContain("4");
  });

  it("explains an online-ballots total that is not one of the status counts", () => {
    const wrapper = mountPanel({
      totalOnlineBallots: 1,
      processedOnlineBallots: 0,
      pendingOnlineBallots: 0,
      submittedOnlineBallots: 0,
      processingOnlineBallots: 0,
      pendingOnlineVotedAnotherWay: 0,
      connectedOnlineVoterSessions: 0,
    });

    expect(
      wrapper.get("[data-testid='online-ballots-unexplained']").text(),
    ).toBe(
      "1 online ballot is not still changeable, processing, or accepted, so it is only in this total.",
    );
  });

  it("uses the plural when more than one online ballot is outside those statuses", () => {
    const wrapper = mountPanel({
      totalOnlineBallots: 5,
      processedOnlineBallots: 1,
      submittedOnlineBallots: 2,
      processingOnlineBallots: 0,
    });

    expect(
      wrapper.get("[data-testid='online-ballots-unexplained']").text(),
    ).toBe(
      "2 online ballots are not still changeable, processing, or accepted, so they are only in this total.",
    );
  });

  it("makes Close now the danger button and keeps the 5-minute shortcuts", async () => {
    const open = mountPanel();
    const closeNow = open.get("[data-testid='close-online-voting-now']");
    const schedule = open.get(
      "[data-testid='schedule-close-online-5-minutes']",
    );

    expect(closeNow.attributes("data-button-type")).toBe("danger");
    expect(schedule.attributes("data-button-type")).toBeUndefined();
    expect(closeNow.text()).toBe("Close now");
    expect(schedule.text()).toBe("Schedule close in 5 minutes");

    const closed = mountPanel({}, { isWindowOpen: false, isClosed: true });
    const reopen = closed.get("[data-testid='open-online-voting-5-minutes']");
    expect(reopen.text()).toBe("Open for 5 minutes");
    expect(reopen.attributes("data-button-type")).toBeUndefined();
    expect(
      closed.find("[data-testid='close-online-voting-now']").exists(),
    ).toBe(false);
  });

  it("emits the same window and accept-all actions as before", async () => {
    const wrapper = mountPanel();

    await wrapper
      .find("[data-testid='accept-all-online-ballots']")
      .trigger("click");
    await wrapper
      .find("[data-testid='schedule-close-online-5-minutes']")
      .trigger("click");
    await wrapper
      .find("[data-testid='close-online-voting-now']")
      .trigger("click");

    expect(wrapper.emitted("acceptAll")).toHaveLength(1);
    expect(wrapper.emitted("scheduleClose")).toHaveLength(1);
    expect(wrapper.emitted("closeNow")).toHaveLength(1);
    expect(wrapper.emitted("openForMinutes")).toBeUndefined();
  });
});
