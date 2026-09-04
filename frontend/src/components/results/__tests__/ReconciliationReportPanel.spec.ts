import { mount } from "@vue/test-utils";
import { describe, expect, it } from "vitest";
import { i18n } from "@/test/setup";
import type { CountReconciliationReportDto } from "@/types";
import ReconciliationReportPanel from "../ReconciliationReportPanel.vue";

const stubs = {
  ElSkeleton: { template: "<div class='skeleton' />" },
  ElAlert: {
    props: ["title", "type"],
    template: "<div class='alert' :data-type='type'>{{ title }}</div>",
  },
  ElDescriptions: { template: "<div class='desc'><slot /></div>" },
  ElDescriptionsItem: {
    props: ["label"],
    template: "<div class='desc-item'>{{ label }}: <slot /></div>",
  },
  ElTable: {
    props: ["data"],
    template:
      "<div class='table'><div v-for='row in data' :key='row.kind + (row.personName || \"\")'>{{ row.kind }} {{ row.personName }}</div></div>",
  },
  ElTableColumn: { template: "<span />" },
};

function report(
  overrides: Partial<CountReconciliationReportDto> = {},
): CountReconciliationReportDto {
  return {
    isReconciled: false,
    frontDeskCount: 2,
    ballotCount: 1,
    pendingOnlineCount: 1,
    spoiledBallotCount: 0,
    mismatches: [
      {
        kind: "PendingOnline",
        personName: "Ada Lovelace",
        onlineStatus: "Submitted",
      },
      {
        kind: "FrontDeskVsBallots",
        frontDeskCount: 2,
        ballotCount: 1,
      },
    ],
    ...overrides,
  };
}

describe("ReconciliationReportPanel", () => {
  it("shows the ready banner when counts reconcile", () => {
    const wrapper = mount(ReconciliationReportPanel, {
      props: {
        report: report({ isReconciled: true, mismatches: [] }),
      },
      global: { plugins: [i18n], stubs },
    });

    expect(wrapper.find(".alert").text()).toContain(
      "Counts reconcile. Analyze and Finalize may proceed if other gates are clear.",
    );
    expect(wrapper.find(".alert").attributes("data-type")).toBe("success");
  });

  it("lists mismatch kinds and named voters", () => {
    const wrapper = mount(ReconciliationReportPanel, {
      props: { report: report() },
      global: { plugins: [i18n], stubs },
    });

    expect(wrapper.find(".alert").text()).toContain("2");
    expect(wrapper.text()).toContain("Pending online ballot");
    expect(wrapper.text()).toContain("Ada Lovelace");
  });

  it("shows a skeleton while loading", () => {
    const wrapper = mount(ReconciliationReportPanel, {
      props: { report: null, loading: true },
      global: { plugins: [i18n], stubs },
    });

    expect(wrapper.find(".skeleton").exists()).toBe(true);
  });
});
