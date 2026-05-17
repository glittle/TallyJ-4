import { describe, it, expect, vi, beforeEach } from "vitest";
import { mount } from "@vue/test-utils";
import { setActivePinia, createPinia } from "pinia";
import SidebarStageHeader from "../SidebarStageHeader.vue";
import StageControl from "../StageControl.vue";

vi.mock("vue-i18n", () => ({
  useI18n: () => ({
    t: (key: string) => key,
  }),
}));

vi.mock("@/composables/useNotifications", () => ({
  useNotifications: () => ({
    showSuccessMessage: vi.fn(),
    showErrorMessage: vi.fn(),
  }),
}));

vi.mock("@/stores/electionStore", () => ({
  useElectionStore: () => ({
    setStage: vi.fn(),
  }),
}));

const globalStubs = {
  ElIcon: { template: "<span />" },
  Setting: { template: "<span />" },
  Monitor: { template: "<span />" },
  PieChart: { template: "<span />" },
};

describe("SidebarStageHeader", () => {
  beforeEach(() => {
    setActivePinia(createPinia());
  });

  it("mounts StageControl with the correct electionGuid", () => {
    const wrapper = mount(SidebarStageHeader, {
      props: { electionGuid: "test-guid-123", stage: "SettingUp" },
      global: { stubs: globalStubs },
    });
    const control = wrapper.findComponent(StageControl);
    expect(control.exists()).toBe(true);
    expect(control.props("electionGuid")).toBe("test-guid-123");
  });

  it("mounts StageControl with the correct stage", () => {
    const wrapper = mount(SidebarStageHeader, {
      props: { electionGuid: "test-guid-123", stage: "GatheringBallots" },
      global: { stubs: globalStubs },
    });
    const control = wrapper.findComponent(StageControl);
    expect(control.props("stage")).toBe("GatheringBallots");
  });

  it("applies stage color CSS variable as border-left", () => {
    const wrapper = mount(SidebarStageHeader, {
      props: { electionGuid: "test-guid-123", stage: "GatheringBallots" },
      global: { stubs: globalStubs },
    });
    const section = wrapper.find("section");
    expect(section.attributes("style")).toContain("--color-stage-gather");
  });

  it("applies stage background CSS variable", () => {
    const wrapper = mount(SidebarStageHeader, {
      props: { electionGuid: "test-guid-123", stage: "ProcessingBallots" },
      global: { stubs: globalStubs },
    });
    const section = wrapper.find("section");
    expect(section.attributes("style")).toContain("--color-stage-process-bg");
  });

  it("renders the mode label", () => {
    const wrapper = mount(SidebarStageHeader, {
      props: { electionGuid: "test-guid-123", stage: "SettingUp" },
      global: { stubs: globalStubs },
    });
    expect(wrapper.find(".ssh-label").text()).toBe("elections.stage.modeLabel");
  });

  it("updates StageControl stage prop when stage changes", async () => {
    const wrapper = mount(SidebarStageHeader, {
      props: { electionGuid: "test-guid-123", stage: "SettingUp" },
      global: { stubs: globalStubs },
    });
    await wrapper.setProps({ stage: "ProcessingBallots" });
    const control = wrapper.findComponent(StageControl);
    expect(control.props("stage")).toBe("ProcessingBallots");
  });
});
