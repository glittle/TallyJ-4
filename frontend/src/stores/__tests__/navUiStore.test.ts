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

  describe("sidebarCollapsed", () => {
    it("defaults to false", () => {
      expect(store.sidebarCollapsed).toBe(false);
    });

    it("setSidebarCollapsed updates and persists", () => {
      store.setSidebarCollapsed(true);
      expect(store.sidebarCollapsed).toBe(true);
      expect(adapter.getItem("navUi:sidebarCollapsed")).toBe("true");

      store.setSidebarCollapsed(false);
      expect(store.sidebarCollapsed).toBe(false);
      expect(adapter.getItem("navUi:sidebarCollapsed")).toBe("false");
    });

    it("toggleSidebarCollapsed flips the value", () => {
      store.toggleSidebarCollapsed();
      expect(store.sidebarCollapsed).toBe(true);
      store.toggleSidebarCollapsed();
      expect(store.sidebarCollapsed).toBe(false);
    });

    it("restores from a pre-populated adapter", () => {
      const prePopulated = makeMemoryAdapter();
      prePopulated.setItem("navUi:sidebarCollapsed", "true");

      setActivePinia(createPinia());
      const freshStore = useNavUiStore();
      freshStore._setStorage(prePopulated);

      expect(freshStore.sidebarCollapsed).toBe(true);
    });
  });

  describe("syncExpansionForElection", () => {
    it("opens only the current stage when binding to an election", () => {
      store.syncExpansionForElection("election-a", "GatheringBallots");

      expect(store.sidebarGroupExpansion["SettingUp"]).toBe(false);
      expect(store.sidebarGroupExpansion["GatheringBallots"]).toBe(true);
      expect(store.sidebarGroupExpansion["ProcessingBallots"]).toBe(false);
      expect(store.sidebarGroupExpansion["Finalized"]).toBe(false);
      expect(store.expansionElectionGuid).toBe("election-a");
    });

    it("preserves manual expansion while on the same election", () => {
      store.syncExpansionForElection("election-a", "SettingUp");
      store.setGroupExpanded("GatheringBallots", true);
      store.setGroupExpanded("SettingUp", false);

      store.syncExpansionForElection("election-a", "SettingUp");

      expect(store.sidebarGroupExpansion["SettingUp"]).toBe(false);
      expect(store.sidebarGroupExpansion["GatheringBallots"]).toBe(true);
    });

    it("resets to only the new election stage when switching elections", () => {
      store.syncExpansionForElection("election-a", "SettingUp");
      store.setGroupExpanded("GatheringBallots", true);
      store.setGroupExpanded("ProcessingBallots", true);

      store.syncExpansionForElection("election-b", "Finalized");

      expect(store.sidebarGroupExpansion["SettingUp"]).toBe(false);
      expect(store.sidebarGroupExpansion["GatheringBallots"]).toBe(false);
      expect(store.sidebarGroupExpansion["ProcessingBallots"]).toBe(false);
      expect(store.sidebarGroupExpansion["Finalized"]).toBe(true);
      expect(store.expansionElectionGuid).toBe("election-b");
    });

    it("persists expansion election guid with group state", () => {
      store.syncExpansionForElection("election-a", "ProcessingBallots");

      expect(adapter.getItem("navUi:sidebarGroupExpansionElection")).toBe(
        "election-a",
      );
      const raw = adapter.getItem("navUi:sidebarGroupExpansion");
      expect(raw).not.toBeNull();
      const parsed = JSON.parse(raw!);
      expect(parsed["ProcessingBallots"]).toBe(true);
      expect(parsed["SettingUp"]).toBe(false);
    });

    it("restores same-election expansion after reload without resetting", () => {
      const prePopulated = makeMemoryAdapter();
      prePopulated.setItem(
        "navUi:sidebarGroupExpansion",
        JSON.stringify({
          SettingUp: true,
          GatheringBallots: true,
          ProcessingBallots: false,
          Finalized: false,
        }),
      );
      prePopulated.setItem(
        "navUi:sidebarGroupExpansionElection",
        "election-a",
      );

      setActivePinia(createPinia());
      const freshStore = useNavUiStore();
      freshStore._setStorage(prePopulated);
      freshStore.syncExpansionForElection("election-a", "GatheringBallots");

      expect(freshStore.sidebarGroupExpansion["SettingUp"]).toBe(true);
      expect(freshStore.sidebarGroupExpansion["GatheringBallots"]).toBe(true);
    });

    it("resets when reloading into a different election than stored", () => {
      const prePopulated = makeMemoryAdapter();
      prePopulated.setItem(
        "navUi:sidebarGroupExpansion",
        JSON.stringify({
          SettingUp: true,
          GatheringBallots: true,
          ProcessingBallots: true,
          Finalized: false,
        }),
      );
      prePopulated.setItem(
        "navUi:sidebarGroupExpansionElection",
        "election-a",
      );

      setActivePinia(createPinia());
      const freshStore = useNavUiStore();
      freshStore._setStorage(prePopulated);
      freshStore.syncExpansionForElection("election-b", "ProcessingBallots");

      expect(freshStore.sidebarGroupExpansion["SettingUp"]).toBe(false);
      expect(freshStore.sidebarGroupExpansion["GatheringBallots"]).toBe(false);
      expect(freshStore.sidebarGroupExpansion["ProcessingBallots"]).toBe(true);
      expect(freshStore.sidebarGroupExpansion["Finalized"]).toBe(false);
    });

    it("no-ops for empty election guid", () => {
      store.setGroupExpanded("SettingUp", true);
      store.syncExpansionForElection("", "GatheringBallots");
      expect(store.sidebarGroupExpansion["SettingUp"]).toBe(true);
      expect(store.expansionElectionGuid).toBeNull();
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
