import { describe, it, expect, vi, beforeEach } from "vitest";
import { mount } from "@vue/test-utils";
import { setActivePinia, createPinia } from "pinia";
import StageControl from "../StageControl.vue";
import StageIndicator from "../StageIndicator.vue";

vi.mock("vue-i18n", () => ({
  useI18n: () => ({
    t: (key: string, opts?: Record<string, string>) => {
      if (opts) {
        return Object.entries(opts).reduce(
          (s, [k, v]) => s.replace(`{${k}}`, String(v)),
          key,
        );
      }
      return key;
    },
  }),
}));

vi.mock("@/composables/useNotifications", () => ({
  useNotifications: () => ({
    showSuccessMessage: vi.fn(),
    showErrorMessage: vi.fn(),
  }),
}));

const mockSetStage = vi.fn();

vi.mock("@/stores/electionStore", () => ({
  useElectionStore: () => ({
    setStage: mockSetStage,
  }),
}));

const globalStubs = {
  ElIcon: { template: "<span />" },
  Setting: { template: "<span />" },
  Monitor: { template: "<span />" },
  PieChart: { template: "<span />" },
};

describe("StageControl", () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    mockSetStage.mockReset();
  });

  describe("admin mode (readonly=false)", () => {
    it("renders a radiogroup with 3 stage buttons", () => {
      const wrapper = mount(StageControl, {
        props: { electionGuid: "test-guid", stage: "SettingUp" },
        global: { stubs: globalStubs },
      });
      expect(wrapper.find('[role="radiogroup"]').exists()).toBe(true);
      const radios = wrapper.findAll('[role="radio"]');
      expect(radios).toHaveLength(3);
    });

    it("marks the current stage as aria-checked=true", () => {
      const wrapper = mount(StageControl, {
        props: { electionGuid: "test-guid", stage: "GatheringBallots" },
        global: { stubs: globalStubs },
      });
      const radios = wrapper.findAll('[role="radio"]');
      const checked = radios.filter(
        (r) => r.attributes("aria-checked") === "true",
      );
      expect(checked).toHaveLength(1);
      expect(checked[0]!.classes()).toContain("is-selected");
    });

    it("calls setStage when a different stage button is clicked", async () => {
      mockSetStage.mockResolvedValue(undefined);
      const wrapper = mount(StageControl, {
        props: { electionGuid: "abc-123", stage: "SettingUp" },
        global: { stubs: globalStubs },
      });
      const radios = wrapper.findAll('[role="radio"]');
      await radios[1]!.trigger("click");
      expect(mockSetStage).toHaveBeenCalledWith("abc-123", "GatheringBallots");
    });

    it("does not call setStage when the current stage is clicked", async () => {
      const wrapper = mount(StageControl, {
        props: { electionGuid: "abc-123", stage: "SettingUp" },
        global: { stubs: globalStubs },
      });
      const radios = wrapper.findAll('[role="radio"]');
      await radios[0]!.trigger("click");
      expect(mockSetStage).not.toHaveBeenCalled();
    });

    it("does not show a StageIndicator status element", () => {
      const wrapper = mount(StageControl, {
        props: { electionGuid: "test-guid", stage: "ProcessingBallots" },
        global: { stubs: globalStubs },
      });
      expect(wrapper.find('[role="status"]').exists()).toBe(false);
    });
  });

  describe("readonly mode (teller)", () => {
    it("does not render a radiogroup", () => {
      const wrapper = mount(StageControl, {
        props: {
          electionGuid: "test-guid",
          stage: "GatheringBallots",
          readonly: true,
        },
        global: { stubs: { ...globalStubs, StageIndicator: true } },
      });
      expect(wrapper.find('[role="radiogroup"]').exists()).toBe(false);
    });

    it("renders a StageIndicator component", () => {
      const wrapper = mount(StageControl, {
        props: {
          electionGuid: "test-guid",
          stage: "GatheringBallots",
          readonly: true,
        },
        global: { stubs: globalStubs },
      });
      expect(wrapper.findComponent(StageIndicator).exists()).toBe(true);
    });

    it("does not call setStage when rendered in readonly mode", async () => {
      const wrapper = mount(StageControl, {
        props: {
          electionGuid: "test-guid",
          stage: "GatheringBallots",
          readonly: true,
        },
        global: { stubs: globalStubs },
      });
      expect(mockSetStage).not.toHaveBeenCalled();
      await wrapper.trigger("click");
      expect(mockSetStage).not.toHaveBeenCalled();
    });
  });

  describe("reactive updates", () => {
    it("updates aria-checked when stage prop changes", async () => {
      const wrapper = mount(StageControl, {
        props: { electionGuid: "test-guid", stage: "SettingUp" },
        global: { stubs: globalStubs },
      });

      const checkedBefore = wrapper
        .findAll('[role="radio"]')
        .filter((r) => r.attributes("aria-checked") === "true");
      expect(checkedBefore).toHaveLength(1);
      expect(checkedBefore[0]!.text()).toContain("elections.stage.SettingUp");

      await wrapper.setProps({ stage: "ProcessingBallots" });

      const radios = wrapper.findAll('[role="radio"]');
      const checkedAfter = radios.filter(
        (r) => r.attributes("aria-checked") === "true",
      );
      expect(checkedAfter).toHaveLength(1);
      expect(checkedAfter[0]!.text()).toContain(
        "elections.stage.ProcessingBallots",
      );
    });
  });
});
