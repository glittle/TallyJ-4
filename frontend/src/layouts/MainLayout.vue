<script setup lang="ts">
import { useResponsive } from "@/composables/useResponsive";
import { computed, onMounted, ref, watch } from "vue";
import { useI18n } from "vue-i18n";
import { useRoute } from "vue-router";
import AppHeader from "../components/AppHeader.vue";
import AppSidebar from "../components/AppSidebar.vue";
import { useElectionStore } from "../stores/electionStore";
import { useNavUiStore } from "../stores/navUiStore";

const { t } = useI18n();

/** Docked aside width; Element Plus sets --el-aside-width from the width prop. */
const SIDEBAR_DOCKED_WIDTH = "300px";

const mobileSidebarOpen = ref(false);
const route = useRoute();
const electionStore = useElectionStore();
const navUiStore = useNavUiStore();
const { isMobile } = useResponsive();

const isFrontDeskLayout = computed(() => route.path.includes("/frontdesk"));

/** Overlay (hamburger) layout: narrow viewport or user chose to hide the docked sidebar. */
const sidebarOverlayMode = computed(
  () => isMobile.value || navUiStore.sidebarCollapsed,
);

/** Layout width for el-aside: 0 in overlay mode so main content fills; docked otherwise. */
const asideWidth = computed(() =>
  sidebarOverlayMode.value ? "0px" : SIDEBAR_DOCKED_WIDTH,
);

watch(
  () => route.params.id as string | undefined,
  async (newId) => {
    if (newId) {
      await electionStore.setActiveElectionHub(newId);
      if (electionStore.currentElection?.electionGuid !== newId) {
        try {
          await electionStore.fetchElectionById(newId);
        } catch {
          // Individual pages may surface errors; sidebar stays in loading state.
        }
      }
      return;
    }

    await electionStore.ensureActiveElectionHubConnection();
  },
  { immediate: true },
);

// Close the temporary drawer when leaving overlay mode (e.g. docking sidebar).
watch(sidebarOverlayMode, (overlay) => {
  if (!overlay) {
    mobileSidebarOpen.value = false;
  }
});

onMounted(async () => {
  if (!route.params.id) {
    await electionStore.ensureActiveElectionHubConnection();
  }
});

function toggleMobileSidebar() {
  mobileSidebarOpen.value = !mobileSidebarOpen.value;
}

function closeMobileSidebar() {
  mobileSidebarOpen.value = false;
}

function hideSidebar() {
  navUiStore.setSidebarCollapsed(true);
  mobileSidebarOpen.value = false;
}

function dockSidebar() {
  navUiStore.setSidebarCollapsed(false);
  mobileSidebarOpen.value = false;
}
</script>

<template>
  <el-container
    class="main-layout"
    :class="{
      'front-desk-layout': isFrontDeskLayout,
      'sidebar-overlay-mode': sidebarOverlayMode,
    }"
  >
    <!-- Skip link for keyboard navigation -->
    <a href="#main-content" class="skip-link">{{
      t("common.skipToMainContent")
    }}</a>

    <!-- Sidebar overlay (mobile or user-collapsed desktop) -->
    <div
      v-if="sidebarOverlayMode && mobileSidebarOpen"
      class="mobile-sidebar-overlay"
      @click="closeMobileSidebar"
    ></div>

    <el-aside
      :width="asideWidth"
      class="sidebar"
      role="complementary"
      :aria-label="t('common.mainNavigation')"
      :class="{ 'mobile-sidebar-open': mobileSidebarOpen }"
    >
      <AppSidebar
        :can-hide-sidebar="!sidebarOverlayMode"
        :can-dock-sidebar="!isMobile && navUiStore.sidebarCollapsed"
        @close-mobile-sidebar="closeMobileSidebar"
        @hide-sidebar="hideSidebar"
        @dock-sidebar="dockSidebar"
      />
    </el-aside>
    <el-container>
      <el-header height="60px" role="banner">
        <AppHeader
          :show-menu-button="sidebarOverlayMode"
          :menu-open="mobileSidebarOpen"
          @toggle-mobile-menu="toggleMobileSidebar"
        />
      </el-header>
      <el-main id="main-content" role="main" tabindex="-1">
        <router-view />
      </el-main>
    </el-container>
  </el-container>
</template>

<style lang="less">
.main-layout {
  height: 100vh;
  background: var(--color-public-bg-gradient);
  /* Drawer width when sidebar is open in overlay mode (not the docked el-aside width). */
  --sidebar-drawer-width: 300px;

  .skip-link {
    position: absolute;
    top: -40px;
    left: 6px;
    padding: 8px;
    text-decoration: none;
    border-radius: 4px;
    z-index: 1000;
    font-weight: 500;
  }

  .skip-link:focus {
    top: 6px;
  }

  .sidebar {
    background-color: var(--color-sidebar-bg);
    box-shadow: 2px 0 6px rgba(0, 0, 0, 0.1);
    overflow-x: hidden;
  }

  /* Mobile / collapsed sidebar overlay */
  .mobile-sidebar-overlay {
    position: fixed;
    top: 0;
    left: 0;
    width: 100%;
    height: 100%;
    background-color: rgba(0, 0, 0, 0.5);
    z-index: 1000;
  }

  .sidebar.mobile-sidebar-open {
    transform: translateX(0);
    z-index: 1001;
  }

  .el-header {
    background: var(--color-public-header-bg);
    backdrop-filter: blur(10px);
    -webkit-backdrop-filter: blur(10px);
    display: flex;
    align-items: center;
    padding: 0 20px;
  }

  .el-main {
    background-color: var(--color-public-bg-gradient);
    padding: 20px;
    overflow-y: auto;
    max-width: none;
  }

  /* Overlay layout: same behavior for narrow viewports and user-collapsed desktop. */
  &.sidebar-overlay-mode {
    .sidebar {
      width: var(--sidebar-drawer-width) !important;
      position: fixed;
      top: 60px;
      left: 0;
      height: calc(100vh - 60px);
      transform: translateX(-100%);
      transition: transform 0.3s ease;
      z-index: 1001;
    }

    .sidebar.mobile-sidebar-open {
      transform: translateX(0);
    }
  }

  &.front-desk-layout {
    .sidebar {
      background-color: var(--color-frontdesk-sidebar-bg);
      box-shadow: none;
      border-right: 1px solid var(--color-frontdesk-sidebar-border);
    }

    .el-header {
      min-height: 48px;
      height: 48px !important;
      background: var(--color-frontdesk-toolbar-bg);
      border-bottom: 1px solid var(--color-frontdesk-toolbar-border);
    }

    .el-main {
      background-color: var(--color-frontdesk-main-bg);
      padding: 12px 16px;
    }

    &.sidebar-overlay-mode .sidebar {
      top: 48px;
      height: calc(100vh - 48px);
    }
  }
}

@media (max-width: 768px) {
  .main-layout {
    --sidebar-drawer-width: 180px;

    .el-main {
      padding: 10px;
    }
  }
}

@media (max-width: 480px) {
  .main-layout {
    --sidebar-drawer-width: 250px;
  }
}

/* On very wide screens, constrain main content for readability */
@media (min-width: 1400px) {
  .main-layout {
    .el-main {
      // max-width: 1400px;
      margin: 0 auto;
      width: 100%;
    }
  }
}
</style>
