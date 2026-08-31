import { flushPromises, mount } from "@vue/test-utils";
import { createPinia, setActivePinia } from "pinia";
import { createI18n } from "vue-i18n";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { getActiveTellers } from "@/utils/activeTellerStorage";
import { useActiveTellers } from "@/composables/useActiveTellers";
import ActiveTellerSelector from "../ActiveTellerSelector.vue";

const mockFetchTellers = vi.fn();
const mockCreateTeller = vi.fn();

vi.mock("@/stores/tellerStore", () => ({
  useTellerStore: () => ({
    tellers: [],
    fetchTellers: mockFetchTellers,
    createTeller: mockCreateTeller,
  }),
}));

const i18n = createI18n({
  legacy: false,
  locale: "en",
  messages: {
    en: {
      teller: {
        active: {
          typeToAdd: "Type to add",
          teller1Placeholder: "Teller at keyboard",
          teller2Placeholder: "Second teller (optional)",
          hint: "Hint",
        },
      },
    },
  },
});

function mountSelector(
  props: {
    field?: "all" | "teller1" | "teller2";
    highlightTeller1?: boolean;
  } = {},
) {
  return mount(ActiveTellerSelector, {
    props: {
      electionGuid: "elec-1",
      ...props,
    },
    global: {
      plugins: [i18n, createPinia()],
      stubs: {
        ElIcon: true,
        ElOption: true,
        ElSelect: {
          props: ["modelValue", "placeholder"],
          emits: ["change"],
          template:
            '<button type="button" class="el-select-stub" :data-placeholder="placeholder" @click="$emit(\'change\', \'Pat\')">{{ modelValue }}</button>',
        },
      },
    },
  });
}

describe("ActiveTellerSelector", () => {
  beforeEach(() => {
    localStorage.clear();
    setActivePinia(createPinia());
    mockFetchTellers.mockReset().mockResolvedValue(undefined);
    mockCreateTeller.mockReset().mockResolvedValue(undefined);
    useActiveTellers().refreshActiveTellers();
  });

  it("writes Teller 1 to the browser-session globals", async () => {
    const wrapper = mountSelector();
    await flushPromises();

    await wrapper.find(".teller1-select").trigger("click");
    await flushPromises();

    expect(getActiveTellers().teller1).toBe("Pat");
    expect(useActiveTellers().tellers.value.teller1).toBe("Pat");
  });

  it("writes Teller 2 to the same session globals as the listing", async () => {
    const wrapper = mountSelector();
    await flushPromises();

    await wrapper.find(".teller2-select").trigger("click");
    await flushPromises();

    expect(getActiveTellers().teller2).toBe("Pat");
    expect(useActiveTellers().tellers.value.teller2).toBe("Pat");
  });

  it("can show only Teller 1, matching a ballot metadata cell", async () => {
    const wrapper = mountSelector({ field: "teller1" });
    await flushPromises();

    expect(wrapper.find(".teller1-select").exists()).toBe(true);
    expect(wrapper.find(".teller2-select").exists()).toBe(false);
  });
});
