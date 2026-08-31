import { flushPromises, mount } from "@vue/test-utils";
import { createPinia, setActivePinia } from "pinia";
import { createI18n } from "vue-i18n";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { getActiveTellers } from "@/utils/activeTellerStorage";
import { useActiveTellers } from "@/composables/useActiveTellers";
import { useTellerStore } from "@/stores/tellerStore";
import type { Teller } from "@/types/teller";
import ActiveTellerSelector from "../ActiveTellerSelector.vue";

vi.mock("@/services/tellerService", () => ({
  tellerService: {
    getTellersByElection: vi.fn(),
    createTeller: vi.fn(),
    deleteTeller: vi.fn(),
    getTellerById: vi.fn(),
    updateTeller: vi.fn(),
  },
}));

vi.mock("@/services/signalrService", () => ({
  signalrService: {
    connectToMainHub: vi.fn().mockResolvedValue({
      on: vi.fn(),
    }),
  },
}));

import { tellerService } from "@/services/tellerService";

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

function listedTeller(name: string, rowId: number): Teller {
  return {
    rowId,
    electionGuid: "elec-1",
    name,
  };
}

function mountSelector(
  props: {
    field?: "all" | "teller1" | "teller2";
    highlightTeller1?: boolean;
  } = {},
) {
  const pinia = createPinia();
  setActivePinia(pinia);
  return mount(ActiveTellerSelector, {
    props: {
      electionGuid: "elec-1",
      ...props,
    },
    global: {
      plugins: [i18n, pinia],
      stubs: {
        ElIcon: true,
        ElOption: {
          props: ["label", "value"],
          template: '<div class="el-option-stub">{{ label }}</div>',
        },
        ElSelect: {
          props: ["modelValue", "placeholder"],
          emits: ["change"],
          template: `
            <div class="el-select-stub" :data-placeholder="placeholder">
              <slot />
              <button type="button" class="emit-named" @click="$emit('change', 'Pat')">set</button>
              <button type="button" class="emit-clear" @click="$emit('change', undefined)">clear</button>
            </div>
          `,
        },
      },
    },
  });
}

function optionLabels(wrapper: ReturnType<typeof mount>, selectClass: string) {
  return wrapper
    .findAll(`${selectClass} .el-option-stub`)
    .map((node) => node.text())
    .filter((label) => label && label !== "Type to add");
}

describe("ActiveTellerSelector", () => {
  beforeEach(() => {
    localStorage.clear();
    setActivePinia(createPinia());
    useActiveTellers().refreshActiveTellers();
    vi.mocked(tellerService.getTellersByElection).mockResolvedValue({
      items: [],
      totalCount: 0,
      pageNumber: 1,
      pageSize: 200,
    });
    vi.mocked(tellerService.createTeller).mockResolvedValue(
      listedTeller("Pat", 11),
    );
    vi.mocked(tellerService.deleteTeller).mockResolvedValue(true);
  });

  it("writes Teller 1 to the browser-session globals", async () => {
    const wrapper = mountSelector();
    await flushPromises();

    await wrapper.find(".teller1-select .emit-named").trigger("click");
    await flushPromises();

    expect(getActiveTellers().teller1).toBe("Pat");
    expect(useActiveTellers().tellers.value.teller1).toBe("Pat");
  });

  it("writes Teller 2 to the same session globals as the listing", async () => {
    const wrapper = mountSelector();
    await flushPromises();

    await wrapper.find(".teller2-select .emit-named").trigger("click");
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

  it("adds an entered name to the election list and both dropdowns, alphabetically", async () => {
    vi.mocked(tellerService.getTellersByElection).mockResolvedValue({
      items: [listedTeller("Zoe", 1)],
      totalCount: 1,
      pageNumber: 1,
      pageSize: 200,
    });

    const wrapper = mountSelector();
    await flushPromises();

    await wrapper.find(".teller1-select .emit-named").trigger("click");
    await flushPromises();

    expect(tellerService.createTeller).toHaveBeenCalledWith("elec-1", {
      electionGuid: "elec-1",
      name: "Pat",
    });
    expect(optionLabels(wrapper, ".teller1-select")).toEqual(["Pat", "Zoe"]);
    expect(optionLabels(wrapper, ".teller2-select")).toEqual(["Pat", "Zoe"]);
  });

  it("does not delete the election name when the dropdown is cleared", async () => {
    vi.mocked(tellerService.getTellersByElection).mockResolvedValue({
      items: [listedTeller("Pat", 11)],
      totalCount: 1,
      pageNumber: 1,
      pageSize: 200,
    });

    const wrapper = mountSelector();
    await flushPromises();

    await wrapper.find(".teller1-select .emit-named").trigger("click");
    await flushPromises();
    await wrapper.find(".teller1-select .emit-clear").trigger("click");
    await flushPromises();

    expect(getActiveTellers().teller1).toBe("");
    expect(tellerService.deleteTeller).not.toHaveBeenCalled();
    expect(useTellerStore().tellers.map((t) => t.name)).toEqual(["Pat"]);
    expect(optionLabels(wrapper, ".teller1-select")).toEqual(["Pat"]);
    expect(optionLabels(wrapper, ".teller2-select")).toEqual(["Pat"]);
  });

  it("shows a SignalR-added name in both dropdowns alphabetically", async () => {
    vi.mocked(tellerService.getTellersByElection).mockResolvedValue({
      items: [listedTeller("Zoe", 1)],
      totalCount: 1,
      pageNumber: 1,
      pageSize: 200,
    });

    const wrapper = mountSelector();
    await flushPromises();

    useTellerStore().applyTellerUpdate({
      electionGuid: "elec-1",
      rowId: 8,
      name: "Ann",
      action: "added",
    });
    await flushPromises();

    expect(optionLabels(wrapper, ".teller1-select")).toEqual(["Ann", "Zoe"]);
    expect(optionLabels(wrapper, ".teller2-select")).toEqual(["Ann", "Zoe"]);
  });
});
