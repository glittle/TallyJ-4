import { describe, it, expect, vi, beforeEach } from "vitest";
import { mount, flushPromises } from "@vue/test-utils";
import { setActivePinia, createPinia } from "pinia";
import StageControl from "../StageControl.vue";

const stagePhraseMessages: Record<string, string> = {
  "elections.stageChangeError.analysisNotReady":
    "Election analysis is not complete or ready for finalization",
  "elections.stageChangeError.generic": "Failed to change election stage",
};

vi.mock("vue-i18n", () => ({
  useI18n: () => ({
    t: (key: string, opts?: Record<string, string>) => {
      let message = stagePhraseMessages[key] ?? key;
      if (opts) {
        message = Object.entries(opts).reduce(
          (s, [k, v]) => s.replace(`{${k}}`, String(v)),
          message,
        );
      }
      return message;
    },
  }),
}));

const mockShowErrorMessage = vi.fn();

vi.mock("@/composables/useNotifications", () => ({
  useNotifications: () => ({
    showSuccessMessage: vi.fn(),
    showErrorMessage: mockShowErrorMessage,
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
    mockShowErrorMessage.mockReset();
  });

  describe("interactive stage switcher", () => {
    it("renders a radiogroup with 4 stage buttons", () => {
      const wrapper = mount(StageControl, {
        props: { electionGuid: "test-guid", stage: "SettingUp" },
        global: { stubs: globalStubs },
      });
      expect(wrapper.find('[role="radiogroup"]').exists()).toBe(true);
      const radios = wrapper.findAll('[role="radio"]');
      expect(radios).toHaveLength(4);
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

    it("shows translated server error when stage change fails", async () => {
      mockSetStage.mockRejectedValue({
        message: "elections.stageChangeError.analysisNotReady",
      });

      const wrapper = mount(StageControl, {
        props: { electionGuid: "abc-123", stage: "ProcessingBallots" },
        global: { stubs: globalStubs },
      });

      const radios = wrapper.findAll('[role="radio"]');
      await radios[3]!.trigger("click");
      await flushPromises();

      expect(mockShowErrorMessage).toHaveBeenCalledWith(
        "Election analysis is not complete or ready for finalization",
      );
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
