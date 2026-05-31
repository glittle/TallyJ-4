import { defineStore } from "pinia";
import { reactive, readonly } from "vue";
import type { ElectionStage } from "../domain/electionStages";

export interface StorageAdapter {
  getItem(key: string): string | null;
  setItem(key: string, value: string): void;
  removeItem(key: string): void;
}

export const sessionStorageAdapter: StorageAdapter = {
  getItem: (key) => {
    try {
      return sessionStorage.getItem(key);
    } catch {
      return null;
    }
  },
  setItem: (key, value) => {
    try {
      sessionStorage.setItem(key, value);
    } catch {
      /* ignore */
    }
  },
  removeItem: (key) => {
    try {
      sessionStorage.removeItem(key);
    } catch {
      /* ignore */
    }
  },
};

const STORAGE_KEY_GROUP_EXPANSION = "navUi:sidebarGroupExpansion";
const STORAGE_KEY_DISMISSED_TIPS = "navUi:dismissedTips";

function loadGroupExpansion(storage: StorageAdapter): Record<string, boolean> {
  try {
    const raw = storage.getItem(STORAGE_KEY_GROUP_EXPANSION);
    if (raw) {
      return JSON.parse(raw) as Record<string, boolean>;
    }
  } catch {
    /* ignore */
  }
  return {};
}

function loadDismissedTips(storage: StorageAdapter): string[] {
  try {
    const raw = storage.getItem(STORAGE_KEY_DISMISSED_TIPS);
    if (raw) {
      return JSON.parse(raw) as string[];
    }
  } catch {
    /* ignore */
  }
  return [];
}

export const useNavUiStore = defineStore("navUi", () => {
  let _storage: StorageAdapter = sessionStorageAdapter;

  const sidebarGroupExpansion = reactive<Record<string, boolean>>(
    loadGroupExpansion(_storage),
  );

  const dismissedTips = reactive<Set<string>>(
    new Set(loadDismissedTips(_storage)),
  );

  function _persistGroupExpansion() {
    _storage.setItem(
      STORAGE_KEY_GROUP_EXPANSION,
      JSON.stringify({ ...sidebarGroupExpansion }),
    );
  }

  function _persistDismissedTips() {
    _storage.setItem(
      STORAGE_KEY_DISMISSED_TIPS,
      JSON.stringify([...dismissedTips]),
    );
  }

  function toggleGroup(stage: ElectionStage | string) {
    sidebarGroupExpansion[stage] = !sidebarGroupExpansion[stage];
    _persistGroupExpansion();
  }

  function setGroupExpanded(stage: ElectionStage | string, expanded: boolean) {
    sidebarGroupExpansion[stage] = expanded;
    _persistGroupExpansion();
  }

  function dismissTip(tipId: string) {
    dismissedTips.add(tipId);
    _persistDismissedTips();
  }

  function isTipDismissed(tipId: string): boolean {
    return dismissedTips.has(tipId);
  }

  function _setStorage(adapter: StorageAdapter) {
    _storage = adapter;
    const expanded = loadGroupExpansion(adapter);
    for (const key of Object.keys(sidebarGroupExpansion)) {
      delete sidebarGroupExpansion[key];
    }
    Object.assign(sidebarGroupExpansion, expanded);

    dismissedTips.clear();
    for (const tip of loadDismissedTips(adapter)) {
      dismissedTips.add(tip);
    }
  }

  return {
    sidebarGroupExpansion: readonly(sidebarGroupExpansion),
    dismissedTips: readonly(dismissedTips),
    toggleGroup,
    setGroupExpanded,
    dismissTip,
    isTipDismissed,
    _setStorage,
  };
});
