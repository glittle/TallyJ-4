import { describe, it, expect, beforeEach } from "vitest";
import { mount } from "@vue/test-utils";
import { setActivePinia, createPinia } from "pinia";
import TipsPanel from "../TipsPanel.vue";
import { useNavUiStore } from "@/stores/navUiStore";
import type { StorageAdapter } from "@/stores/navUiStore";

vi.mock("vue-i18n", () => ({
  useI18n: () => ({ t: (key: string) => key }),
}));

function makeMemoryAdapter(): StorageAdapter {
  const _store: Record<string, string> = {};
  return {
    getItem: (key) => _store[key] ?? null,
    setItem: (key, value) => {
      _store[key] = value;
    },
    removeItem: (key) => {
      delete _store[key];
    },
  };
}

const globalStubs = {
  ElIcon: { template: "<span />" },
  Close: { template: "<span />" },
};

describe("TipsPanel", () => {
  let store: ReturnType<typeof useNavUiStore>;

  beforeEach(() => {
    setActivePinia(createPinia());
    store = useNavUiStore();
    store._setStorage(makeMemoryAdapter());
  });

  it("renders slot content when not dismissed", () => {
    const wrapper = mount(TipsPanel, {
      props: { tipId: "tip-test-1" },
      slots: { default: "<p class='content'>Hello tips</p>" },
      global: { stubs: globalStubs },
    });
    expect(wrapper.find(".content").exists()).toBe(true);
  });

  it("is hidden after dismiss button click", async () => {
    const wrapper = mount(TipsPanel, {
      props: { tipId: "tip-test-2" },
      slots: { default: "<p class='content'>tip text</p>" },
      global: { stubs: globalStubs },
    });
    expect(wrapper.find(".content").exists()).toBe(true);
    await wrapper.find(".tips-panel__dismiss").trigger("click");
    expect(wrapper.find(".content").exists()).toBe(false);
  });

  it("marks tip as dismissed in the store after dismiss", async () => {
    const wrapper = mount(TipsPanel, {
      props: { tipId: "tip-test-3" },
      slots: { default: "<p>tip</p>" },
      global: { stubs: globalStubs },
    });
    expect(store.isTipDismissed("tip-test-3")).toBe(false);
    await wrapper.find(".tips-panel__dismiss").trigger("click");
    expect(store.isTipDismissed("tip-test-3")).toBe(true);
  });

  it("starts hidden when the tip is already dismissed", () => {
    store.dismissTip("tip-already-gone");
    const wrapper = mount(TipsPanel, {
      props: { tipId: "tip-already-gone" },
      slots: { default: "<p class='content'>text</p>" },
      global: { stubs: globalStubs },
    });
    expect(wrapper.find(".content").exists()).toBe(false);
  });

  it("renders header slot content", () => {
    const wrapper = mount(TipsPanel, {
      props: { tipId: "tip-header-slot" },
      slots: {
        header: "<span class='custom-header'>My Header</span>",
        default: "<p>body</p>",
      },
      global: { stubs: globalStubs },
    });
    expect(wrapper.find(".custom-header").exists()).toBe(true);
    expect(wrapper.find(".custom-header").text()).toBe("My Header");
  });
});
