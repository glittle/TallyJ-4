import { flushPromises, mount } from "@vue/test-utils";
import { createPinia, setActivePinia } from "pinia";
import { createI18n } from "vue-i18n";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { Teller } from "@/types/teller";
import TellerForm from "../TellerForm.vue";

const mockDeleteTeller = vi.fn();
const mockConfirm = vi.fn();

vi.mock("@/stores/tellerStore", () => ({
  useTellerStore: () => ({
    deleteTeller: mockDeleteTeller,
    createTeller: vi.fn(),
    updateTeller: vi.fn(),
  }),
}));

vi.mock("@/composables/useNotifications", () => ({
  useNotifications: () => ({
    showSuccessMessage: vi.fn(),
  }),
}));

vi.mock("@/composables/useApiErrorHandler", () => ({
  useApiErrorHandler: () => ({
    handleApiError: vi.fn(),
  }),
}));

vi.mock("element-plus", async () => {
  const actual =
    await vi.importActual<typeof import("element-plus")>("element-plus");
  return {
    ...actual,
    ElMessageBox: {
      confirm: (...args: unknown[]) => mockConfirm(...args),
    },
  };
});

const i18n = createI18n({
  legacy: false,
  locale: "en",
  messages: {
    en: {
      teller: {
        form: {
          name: "Teller Name",
          namePlaceholder: "Enter teller name",
          nameRequired: "required",
          nameMaxLength: "too long",
          save: "Save",
          create: "Create",
          cancel: "Cancel",
          delete: "Delete",
          saved: "saved",
        },
        confirm: {
          deleteTellerTitle: "Warning",
          deleteTellerMessage:
            'Are you sure you want to delete teller "{name}"?',
          delete: "Delete",
          cancel: "Cancel",
        },
        success: {
          tellerDeleted: "Teller deleted successfully",
        },
      },
    },
  },
});

const existing: Teller = {
  rowId: 4,
  electionGuid: "elec-1",
  name: "Pat",
};

describe("TellerForm", () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    mockDeleteTeller.mockReset().mockResolvedValue(undefined);
    mockConfirm.mockReset().mockResolvedValue(true);
  });

  it("deletes a teller name from the election list on the Tellers page", async () => {
    const wrapper = mount(TellerForm, {
      props: {
        electionGuid: "elec-1",
        teller: existing,
        isEdit: true,
        showDelete: true,
      },
      global: {
        plugins: [i18n],
        stubs: {
          ElForm: { template: "<form><slot /></form>" },
          ElFormItem: { template: "<div><slot /></div>" },
          ElInput: true,
          ElButton: {
            props: ["type"],
            template:
              '<button type="button" :data-type="type" @click="$emit(\'click\')"><slot /></button>',
          },
        },
      },
    });

    const deleteButton = wrapper
      .findAll("button")
      .find((button) => button.text().includes("Delete"));
    expect(deleteButton).toBeTruthy();

    await deleteButton!.trigger("click");
    await flushPromises();

    expect(mockConfirm).toHaveBeenCalled();
    expect(mockDeleteTeller).toHaveBeenCalledWith("elec-1", 4);
    expect(wrapper.emitted("deleted")).toBeTruthy();
  });
});
