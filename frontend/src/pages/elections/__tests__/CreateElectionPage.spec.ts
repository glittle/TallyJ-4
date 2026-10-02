import { flushPromises, mount } from "@vue/test-utils";
import ElementPlus, { ElFormItem } from "element-plus";
import { defineComponent } from "vue";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { i18n } from "@/test/setup";
import CreateElectionPage from "../CreateElectionPage.vue";

const createElection = vi.fn();
const handleApiError = vi.fn();

vi.mock("vue-router", () => ({
  useRouter: () => ({ push: vi.fn(), back: vi.fn() }),
}));

vi.mock("@/stores/electionStore", () => ({
  useElectionStore: () => ({
    elections: [],
    fetchElections: vi.fn().mockResolvedValue(undefined),
    createElection,
  }),
}));

vi.mock("@/composables/useApiErrorHandler", () => ({
  useApiErrorHandler: () => ({ handleApiError }),
}));

vi.mock("@/composables/useNotifications", () => ({
  useNotifications: () => ({
    showSuccessMessage: vi.fn(),
    showErrorMessage: vi.fn(),
  }),
}));

const FormTabsStub = defineComponent({
  name: "ElectionFormTabs",
  props: {
    modelValue: { type: Object, required: true },
  },
  setup(props) {
    const form = props.modelValue as {
      name: string;
      dateOfElection?: string;
      electionType?: string;
      numberToElect?: number;
    };
    form.name = "Annual Meeting";
    form.dateOfElection = "2026-06-01";
    form.electionType = "LSA";
    form.numberToElect = 9;
  },
  template: `<el-form-item prop="name"><input class="name-input" /></el-form-item>`,
});

describe("CreateElectionPage", () => {
  beforeEach(() => {
    createElection.mockReset();
    handleApiError.mockReset();
  });

  it("shows a server validation error on the matching form field", async () => {
    createElection.mockRejectedValue({
      status: 400,
      errors: { Name: ["Election name is already in use"] },
    });

    const wrapper = mount(CreateElectionPage, {
      global: {
        plugins: [i18n, ElementPlus],
        stubs: { ElectionFormTabs: FormTabsStub },
      },
    });

    await wrapper.get("button.el-button--primary").trigger("click");
    await flushPromises();

    expect(createElection).toHaveBeenCalled();
    expect(handleApiError).not.toHaveBeenCalled();

    const nameField = wrapper
      .findAllComponents(ElFormItem)
      .find((item) => item.props("prop") === "name");
    expect(nameField).toBeTruthy();
    expect(nameField!.vm.validateState).toBe("error");
    expect(nameField!.vm.validateMessage).toBe(
      "Election name is already in use",
    );
  });
});
