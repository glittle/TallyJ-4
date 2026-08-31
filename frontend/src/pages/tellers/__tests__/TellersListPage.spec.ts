import { flushPromises, mount } from "@vue/test-utils";
import { createPinia, setActivePinia } from "pinia";
import { createI18n } from "vue-i18n";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { Teller } from "@/types/teller";
import TellersListPage from "../TellersListPage.vue";

const mockFetchTellers = vi.fn();
const mockTellers: Teller[] = [
  { rowId: 1, electionGuid: "elec-1", name: "Ann" },
  { rowId: 2, electionGuid: "elec-1", name: "Pat" },
];

vi.mock("vue-router", () => ({
  useRoute: () => ({ params: { id: "elec-1" } }),
}));

vi.mock("@/stores/tellerStore", () => ({
  useTellerStore: () => ({
    tellers: mockTellers,
    loading: false,
    totalCount: 2,
    currentPage: 1,
    pageSize: 50,
    fetchTellers: mockFetchTellers,
  }),
}));

vi.mock("@/composables/useApiErrorHandler", () => ({
  useApiErrorHandler: () => ({
    handleApiError: vi.fn(),
  }),
}));

vi.mock("@/components/tellers/TellerForm.vue", () => ({
  default: {
    name: "TellerForm",
    props: ["electionGuid", "teller", "isEdit", "showDelete"],
    template:
      '<div class="teller-form-stub" :data-show-delete="showDelete" :data-name="teller?.name"></div>',
  },
}));

const i18n = createI18n({
  legacy: false,
  locale: "en",
  messages: {
    en: {
      teller: {
        form: {
          titleAdd: "Add Teller",
          titleEdit: "Edit Teller",
          name: "Teller Name",
        },
        editDrawerTitle: "Edit {name}",
      },
    },
  },
});

describe("TellersListPage", () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    mockFetchTellers.mockReset().mockResolvedValue(undefined);
  });

  it("lists election teller names and opens the existing delete form", async () => {
    const wrapper = mount(TellersListPage, {
      global: {
        plugins: [i18n],
        stubs: {
          ElCard: { template: "<div><slot name='header' /><slot /></div>" },
          ElTable: {
            props: ["data"],
            template: '<div class="el-table-stub"><slot /></div>',
          },
          ElTableColumn: {
            template: `
              <div>
                <div v-for="row in [{ name: 'Ann', rowId: 1 }, { name: 'Pat', rowId: 2 }]" :key="row.rowId">
                  <slot name="default" :row="row" />
                </div>
              </div>
            `,
          },
          ElButton: {
            template: '<button type="button" @click="$emit(\'click\')"><slot /></button>',
          },
          ElIcon: true,
          ElDrawer: {
            props: ["modelValue"],
            template: '<div v-if="modelValue" class="drawer-stub"><slot /></div>',
          },
          ElPagination: true,
        },
      },
    });
    await flushPromises();

    expect(mockFetchTellers).toHaveBeenCalledWith("elec-1", 1, 50);
    expect(wrapper.text()).toContain("Ann");
    expect(wrapper.text()).toContain("Pat");

    const nameButtons = wrapper
      .findAll("button")
      .filter((button) => button.text() === "Ann");
    expect(nameButtons.length).toBeGreaterThan(0);
    await nameButtons[0]!.trigger("click");
    await flushPromises();

    const form = wrapper.findComponent({ name: "TellerForm" });
    expect(form.exists()).toBe(true);
    expect(form.props("showDelete")).toBe(true);
    expect(form.props("isEdit")).toBe(true);
    expect(form.props("teller")).toMatchObject({ name: "Ann", rowId: 1 });
  });
});
