import { describe, it, expect, beforeEach } from "vitest";
import { setActivePinia, createPinia } from "pinia";
import { useNavUiStore } from "../navUiStore";
import type { StorageAdapter } from "../navUiStore";

function makeMemoryAdapter(): StorageAdapter & {
  _store: Record<string, string>;
} {
  const _store: Record<string, string> = {};
  return {
    _store,
    getItem: (key) => _store[key] ?? null,
    setItem: (key, value) => {
      _store[key] = value;
    },
    removeItem: (key) => {
      delete _store[key];
    },
  };
}

describe("navUiStore", () => {
  let store: ReturnType<typeof useNavUiStore>;
  let adapter: ReturnType<typeof makeMemoryAdapter>;

  beforeEach(() => {
    setActivePinia(createPinia());
    adapter = makeMemoryAdapter();
    store = useNavUiStore();
    store._setStorage(adapter);
  });

  describe("toggleGroup", () => {
    it("expands a collapsed group", () => {
      expect(store.sidebarGroupExpansion["SettingUp"]).toBeFalsy();
      store.toggleGroup("SettingUp");
      expect(store.sidebarGroupExpansion["SettingUp"]).toBe(true);
    });

    it("collapses an expanded group", () => {
      store.toggleGroup("SettingUp");
      store.toggleGroup("SettingUp");
      expect(store.sidebarGroupExpansion["SettingUp"]).toBe(false);
    });

    it("toggles groups independently", () => {
      store.toggleGroup("SettingUp");
      store.toggleGroup("GatheringBallots");
      expect(store.sidebarGroupExpansion["SettingUp"]).toBe(true);
      expect(store.sidebarGroupExpansion["GatheringBallots"]).toBe(true);
      store.toggleGroup("SettingUp");
      expect(store.sidebarGroupExpansion["SettingUp"]).toBe(false);
      expect(store.sidebarGroupExpansion["GatheringBallots"]).toBe(true);
    });
  });

  describe("setGroupExpanded", () => {
    it("sets the expansion state explicitly", () => {
      store.setGroupExpanded("ProcessingBallots", true);
      expect(store.sidebarGroupExpansion["ProcessingBallots"]).toBe(true);
      store.setGroupExpanded("ProcessingBallots", false);
      expect(store.sidebarGroupExpansion["ProcessingBallots"]).toBe(false);
    });
  });

  describe("dismissTip / isTipDismissed", () => {
    it("tip is not dismissed initially", () => {
      expect(store.isTipDismissed("tip-setup-1")).toBe(false);
    });

    it("dismisses a tip", () => {
      store.dismissTip("tip-setup-1");
      expect(store.isTipDismissed("tip-setup-1")).toBe(true);
    });

    it("dismisses tips independently", () => {
      store.dismissTip("tip-a");
      expect(store.isTipDismissed("tip-a")).toBe(true);
      expect(store.isTipDismissed("tip-b")).toBe(false);
    });
  });

  describe("sessionStorage round-trip", () => {
    it("persists group expansion to the adapter", () => {
      store.toggleGroup("SettingUp");
      const raw = adapter.getItem("navUi:sidebarGroupExpansion");
      expect(raw).not.toBeNull();
      const parsed = JSON.parse(raw!);
      expect(parsed["SettingUp"]).toBe(true);
    });

    it("persists dismissed tips to the adapter", () => {
      store.dismissTip("tip-1");
      const raw = adapter.getItem("navUi:dismissedTips");
      expect(raw).not.toBeNull();
      const parsed = JSON.parse(raw!) as string[];
      expect(parsed).toContain("tip-1");
    });

    it("restores group expansion from a pre-populated adapter", () => {
      const prePopulated = makeMemoryAdapter();
      prePopulated.setItem(
        "navUi:sidebarGroupExpansion",
        JSON.stringify({ GatheringBallots: true }),
      );

      setActivePinia(createPinia());
      const freshStore = useNavUiStore();
      freshStore._setStorage(prePopulated);

      expect(freshStore.sidebarGroupExpansion["GatheringBallots"]).toBe(true);
    });

    it("restores dismissed tips from a pre-populated adapter", () => {
      const prePopulated = makeMemoryAdapter();
      prePopulated.setItem(
        "navUi:dismissedTips",
        JSON.stringify(["tip-persisted"]),
      );

      setActivePinia(createPinia());
      const freshStore = useNavUiStore();
      freshStore._setStorage(prePopulated);

      expect(freshStore.isTipDismissed("tip-persisted")).toBe(true);
    });
  });
});
