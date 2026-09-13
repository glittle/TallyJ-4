import { mount } from "@vue/test-utils";
import { describe, expect, it } from "vitest";
import { i18n } from "@/test/setup";
import ReportingVoterReports from "../ReportingVoterReports.vue";
import type { VotersByAreaReport } from "@/types";

const report: VotersByAreaReport = {
  electionName: "Unit",
  showImported: true,
  areas: [
    {
      areaName: "North",
      totalEligible: 3,
      eligible18Plus: 3,
      eligible18To21: 1,
      voted: 2,
      inPerson: 1,
      mailedIn: 0,
      droppedOff: 0,
      calledIn: 0,
      custom1: 0,
      custom2: 0,
      custom3: 0,
      online: 0,
      onlineKiosk: 0,
      imported: 1,
    },
  ],
  total: {
    areaName: "Total",
    totalEligible: 3,
    eligible18Plus: 3,
    eligible18To21: 1,
    voted: 2,
    inPerson: 1,
    mailedIn: 0,
    droppedOff: 0,
    calledIn: 0,
    custom1: 0,
    custom2: 0,
    custom3: 0,
    online: 0,
    onlineKiosk: 0,
    imported: 1,
  },
};

describe("ReportingVoterReports VotersByArea", () => {
  it("shows 18+, 18-21, and imported when the flag is on", () => {
    const wrapper = mount(ReportingVoterReports, {
      props: {
        selectedReport: "VotersByArea",
        selectedReportName: "Eligible and Voted by Area",
        reportData: report,
      },
      global: { plugins: [i18n] },
    });

    const text = wrapper.text();
    expect(text).toContain("18+");
    expect(text).toContain("18–21");
    expect(text).toContain("Imported");
    expect(text).toContain("North");
  });

  it("hides imported when showImported is false", () => {
    const wrapper = mount(ReportingVoterReports, {
      props: {
        selectedReport: "VotersByArea",
        selectedReportName: "Eligible and Voted by Area",
        reportData: { ...report, showImported: false },
      },
      global: { plugins: [i18n] },
    });

    expect(wrapper.text()).not.toContain("Imported");
    expect(wrapper.text()).toContain("18+");
  });
});
