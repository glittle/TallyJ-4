<script setup lang="ts">
import { useNotifications } from "@/composables/useNotifications";
import {
  ArrowDown,
  Location,
  Menu,
  Setting,
  SwitchButton,
  User,
} from "@element-plus/icons-vue";
import { computed, ref, watch } from "vue";
import { useI18n } from "vue-i18n";
import { useRoute, useRouter } from "vue-router";
import { useAuthStore } from "../stores/authStore";
import { useElectionStore } from "../stores/electionStore";
import { useLocationStore } from "../stores/locationStore";
import LanguageSelector from "./common/LanguageSelector.vue";
import ThemeSelector from "./common/ThemeSelector.vue";
import { BUILD_DATE, VERSION } from "./version";

const router = useRouter();
const route = useRoute();
const authStore = useAuthStore();
const locationStore = useLocationStore();
const electionStore = useElectionStore();
const { t } = useI18n();
const { showSuccessMessage, showInfoMessage } = useNotifications();

const mobileMenuOpen = ref(false);
const isMobile = ref(false);

// Version tooltip - dynamically localized
const versionName = computed(() => VERSION);
const versionDate = computed(() => BUILD_DATE);

// Check if we're on mobile
const checkMobile = () => {
  isMobile.value = window.innerWidth <= 768;
};

checkMobile();
window.addEventListener("resize", checkMobile);

const currentUser = computed(() => ({
  name: authStore.name,
  email: authStore.email,
}));

const currentPageTitle = computed(() => {
  const titleMap: Record<string, string> = {
    "/dashboard": t("nav.dashboard"),
    "/elections": t("nav.elections"),
    "/profile": t("nav.profile"),
  };

  // Check if there's a mapped title first
  if (titleMap[route.path]) {
    return titleMap[route.path];
  }

  // Handle dynamic title from route meta using titleKey
  const titleKey = route.meta.titleKey;
  if (typeof titleKey === "string") {
    return t(titleKey);
  }

  return "";
});

// Get current election GUID from route
const currentElectionGuid = computed(() => {
  // Try to get from route params
  if (route.params.id) {
    return route.params.id as string;
  }
  // Try to get from current election in store
  if (electionStore.currentElection?.electionGuid) {
    return electionStore.currentElection.electionGuid;
  }
  return null;
});

// Get selected location details
const _selectedLocation = computed(() => {
  if (!locationStore.selectedLocationGuid) {
    return null;
  }
  return locationStore.locations.find(
    (loc) => loc.locationGuid === locationStore.selectedLocationGuid,
  );
});

// Load locations when election is available
watch(
  currentElectionGuid,
  async (electionGuid) => {
    if (electionGuid && locationStore.locations.length === 0) {
      try {
        await locationStore.fetchLocations(electionGuid);
      } catch (error) {
        console.error("Failed to load locations:", error);
      }
    }
  },
  { immediate: true },
);

function handleLocationChange(locationGuid: string) {
  locationStore.selectLocation(locationGuid);
  showSuccessMessage(t("locations.locationSelected"));
}

async function handleCommand(command: string) {
  if (command === "logout") {
    await authStore.logout();
    showSuccessMessage(t("auth.logoutSuccess"));
    router.push("/");
  } else if (command === "profile") {
    router.push("/profile");
  } else if (command === "settings") {
    showInfoMessage("Settings page coming soon");
  }
}

function toggleMobileMenu() {
  mobileMenuOpen.value = !mobileMenuOpen.value;
  // Emit event to parent component to toggle sidebar
  // This will be handled by MainLayout
}
</script>

<template>
  <header class="app-header" role="banner">
    <div class="header-left">
      <button
        v-if="isMobile"
        class="mobile-menu-btn"
        aria-label="Toggle navigation menu"
        :aria-expanded="mobileMenuOpen"
        @click="toggleMobileMenu"
      >
        <el-icon>
          <Menu />
        </el-icon>
      </button>
      <h2 aria-live="polite">
        {{ currentPageTitle }}
      </h2>
      <div class="versionName">
        {{ versionName }}
      </div>
      <div class="versionDate">
        {{ versionDate }}
      </div>

      <!-- Location Selector -->
      <div
        v-if="currentElectionGuid && locationStore.locations?.length > 0"
        class="location-selector"
      >
        <el-icon class="location-icon">
          <Location />
        </el-icon>
        <el-select
          :model-value="locationStore.selectedLocationGuid"
          :placeholder="$t('locations.selectLocation')"
          clearable
          :aria-label="$t('locations.currentLocation')"
          size="small"
          class="location-select"
          @update:model-value="handleLocationChange"
        >
          <el-option
            v-for="location in locationStore.sortedLocations"
            :key="location.locationGuid"
            :label="location.name"
            :value="location.locationGuid"
          />
        </el-select>
      </div>
    </div>
    <nav class="header-right" role="navigation" aria-label="User menu">
      <ThemeSelector />
      <LanguageSelector />
      <el-dropdown trigger="click" @command="handleCommand">
        <button
          class="user-dropdown"
          aria-haspopup="menu"
          :aria-expanded="false"
          :aria-label="t('common.userMenu')"
        >
          <el-avatar :size="32" icon="UserFilled" aria-hidden="true" />
          <span class="username">{{
            currentUser?.name || currentUser?.email || "User"
          }}</span>
          <el-icon aria-hidden="true">
            <ArrowDown />
          </el-icon>
        </button>
        <template #dropdown>
          <el-dropdown-menu>
            <el-dropdown-item command="profile">
              <el-icon>
                <User />
              </el-icon>
              {{ $t("common.profile") }}
            </el-dropdown-item>
            <el-dropdown-item command="settings">
              <el-icon>
                <Setting />
              </el-icon>
              {{ $t("common.settings") }}
            </el-dropdown-item>
            <el-dropdown-item divided command="logout">
              <el-icon>
                <SwitchButton />
              </el-icon>
              {{ $t("auth.logout") }}
            </el-dropdown-item>
          </el-dropdown-menu>
        </template>
      </el-dropdown>
    </nav>
  </header>
</template>

<style lang="less">
.app-header {
  width: 100%;
  display: flex;
  justify-content: space-between;
  align-items: center;

  .versionName {
    font-size: 0.9em;
    color: var(--color-text-secondary);
    margin-left: 16px;
  }
  .versionDate {
    font-size: 0.8em;
    color: var(--color-text-secondary);
    margin-left: 8px;
  }

  .header-left {
    display: flex;
    align-items: center;
    gap: 16px;
    flex: 1;
  }

  .header-left h3 {
    margin: 0;
    font-size: 18px;
    font-weight: 500;
    color: var(--color-text-primary);
    cursor: help;
    white-space: nowrap;
  }

  .location-selector {
    display: flex;
    align-items: center;
    gap: 8px;
    margin-left: 16px;
    padding-left: 16px;
    border-left: 1px solid var(--el-border-color-lighter);

    .location-icon {
      color: var(--el-color-primary);
      font-size: 16px;
    }

    .location-select {
      min-width: 180px;
      max-width: 250px;
    }
  }

  .header-right {
    display: flex;
    align-items: center;
    gap: 20px;
  }

  .user-dropdown {
    display: flex;
    align-items: center;
    gap: 8px;
    cursor: pointer;
    padding: 5px 10px;
    border-radius: 4px;
    border: none;
    background: transparent;
    transition: background-color 0.3s;
    font-size: inherit;
  }

  .user-dropdown:hover,
  .user-dropdown:focus {
    background-color: var(--color-bg-secondary);
    outline: 2px solid var(--color-primary-700);
    outline-offset: 2px;
  }

  .username {
    font-size: 14px;
    color: var(--color-text-secondary);
  }

  .mobile-menu-btn {
    background: none;
    border: none;
    padding: 8px;
    margin-right: 10px;
    cursor: pointer;
    border-radius: 4px;
    transition: background-color 0.3s;
  }

  .mobile-menu-btn:hover,
  .mobile-menu-btn:focus {
    background-color: var(--color-bg-secondary);
    outline: 2px solid var(--color-primary-700);
    outline-offset: 2px;
  }

  /* Mobile responsiveness */
  @media (max-width: 768px) {
    .app-header {
      padding: 0 15px;
    }

    .header-left {
      display: flex;
      align-items: center;
      gap: 8px;
    }

    .header-left h3 {
      font-size: 16px;
    }

    .location-selector {
      margin-left: 8px;
      padding-left: 8px;

      .location-select {
        min-width: 120px;
        max-width: 150px;
      }
    }

    .header-right {
      gap: 15px;
    }

    .username {
      display: none;
      /* Hide username on mobile to save space */
    }
  }

  @media (max-width: 480px) {
    .header-left h3 {
      font-size: 14px;
    }

    .location-selector {
      .location-icon {
        display: none;
      }

      .location-select {
        min-width: 100px;
        max-width: 120px;
      }
    }

    .user-dropdown {
      padding: 8px;
    }
  }

  .el-dropdown-menu__item {
    display: flex;
    align-items: center;
    gap: 8px;
  }
}
</style>
