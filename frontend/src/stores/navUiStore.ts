import { defineStore } from "pinia";
import { reactive, readonly, ref } from "vue";
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
const STORAGE_KEY_GROUP_EXPANSION_ELECTION =
  "navUi:sidebarGroupExpansionElection";
const STORAGE_KEY_DISMISSED_TIPS = "navUi:dismissedTips";
const STORAGE_KEY_SIDEBAR_COLLAPSED = "navUi:sidebarCollapsed";

/** Stage keys for group expansion; kept local to avoid pulling icon-heavy domain modules into this store. */
const ALL_STAGES: readonly ElectionStage[] = [
  "SettingUp",
  "GatheringBallots",
  "ProcessingBallots",
  "Finalized",
] as const;

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

function loadExpansionElectionGuid(storage: StorageAdapter): string | null {
  try {
    return storage.getItem(STORAGE_KEY_GROUP_EXPANSION_ELECTION);
  } catch {
    return null;
  }
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

function loadSidebarCollapsed(storage: StorageAdapter): boolean {
  try {
    return storage.getItem(STORAGE_KEY_SIDEBAR_COLLAPSED) === "true";
  } catch {
    return false;
  }
}

export const useNavUiStore = defineStore("navUi", () => {
  let _storage: StorageAdapter = sessionStorageAdapter;

  const sidebarGroupExpansion = reactive<Record<string, boolean>>(
    loadGroupExpansion(_storage),
  );

  /** Election the stored group-expansion prefs apply to (session-scoped). */
  const expansionElectionGuid = ref<string | null>(
    loadExpansionElectionGuid(_storage),
  );

  const dismissedTips = reactive<Set<string>>(
    new Set(loadDismissedTips(_storage)),
  );

  /** User preference: use overlay (hamburger) layout even on wide viewports. */
  const sidebarCollapsed = ref(loadSidebarCollapsed(_storage));

  function _persistGroupExpansion() {
    _storage.setItem(
      STORAGE_KEY_GROUP_EXPANSION,
      JSON.stringify({ ...sidebarGroupExpansion }),
    );
  }

  function _persistExpansionElection() {
    if (expansionElectionGuid.value) {
      _storage.setItem(
        STORAGE_KEY_GROUP_EXPANSION_ELECTION,
        expansionElectionGuid.value,
      );
    } else {
      _storage.removeItem(STORAGE_KEY_GROUP_EXPANSION_ELECTION);
    }
  }

  function _persistDismissedTips() {
    _storage.setItem(
      STORAGE_KEY_DISMISSED_TIPS,
      JSON.stringify([...dismissedTips]),
    );
  }

  function _persistSidebarCollapsed() {
    _storage.setItem(
      STORAGE_KEY_SIDEBAR_COLLAPSED,
      sidebarCollapsed.value ? "true" : "false",
    );
  }

  function _resetGroupsToStage(currentStage: ElectionStage | string) {
    for (const key of Object.keys(sidebarGroupExpansion)) {
      delete sidebarGroupExpansion[key];
    }
    for (const stage of ALL_STAGES) {
      sidebarGroupExpansion[stage] = stage === currentStage;
    }
    _persistGroupExpansion();
  }

  function toggleGroup(stage: ElectionStage | string) {
    sidebarGroupExpansion[stage] = !sidebarGroupExpansion[stage];
    _persistGroupExpansion();
  }

  function setGroupExpanded(stage: ElectionStage | string, expanded: boolean) {
    sidebarGroupExpansion[stage] = expanded;
    _persistGroupExpansion();
  }

  /**
   * Keep group expansion prefs only for the active election. When the user
   * switches elections, ignore stored multi-group state and open only the
   * stage the new election is currently in. Same-election reloads restore
   * the previous expansion prefs.
   */
  function syncExpansionForElection(
    electionGuid: string,
    currentStage: ElectionStage | string,
  ) {
    if (!electionGuid) {
      return;
    }
    if (expansionElectionGuid.value === electionGuid) {
      return;
    }

    _resetGroupsToStage(currentStage);
    expansionElectionGuid.value = electionGuid;
    _persistExpansionElection();
  }

  function setSidebarCollapsed(collapsed: boolean) {
    sidebarCollapsed.value = collapsed;
    _persistSidebarCollapsed();
  }

  function toggleSidebarCollapsed() {
    setSidebarCollapsed(!sidebarCollapsed.value);
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

    expansionElectionGuid.value = loadExpansionElectionGuid(adapter);

    dismissedTips.clear();
    for (const tip of loadDismissedTips(adapter)) {
      dismissedTips.add(tip);
    }

    sidebarCollapsed.value = loadSidebarCollapsed(adapter);
  }

  return {
    sidebarGroupExpansion: readonly(sidebarGroupExpansion),
    expansionElectionGuid: readonly(expansionElectionGuid),
    dismissedTips: readonly(dismissedTips),
    // Read-only so consumers must use set/toggle helpers (which persist).
    sidebarCollapsed: readonly(sidebarCollapsed),
    toggleGroup,
    setGroupExpanded,
    syncExpansionForElection,
    setSidebarCollapsed,
    toggleSidebarCollapsed,
    dismissTip,
    isTipDismissed,
    _setStorage,
  };
});
