<script setup lang="ts">
import { useNotifications } from "@/composables/useNotifications";
import {
  ArrowDown,
  Menu,
  Setting,
  SwitchButton,
  User,
} from "@element-plus/icons-vue";
import { computed, ref } from "vue";
import { useI18n } from "vue-i18n";
import { useRoute, useRouter } from "vue-router";
import { useAuthStore } from "../stores/authStore";
import { useElectionStore } from "../stores/electionStore";
import ComputerCodeBadge from "./common/ComputerCodeBadge.vue";
import LanguageSelector from "./common/LanguageSelector.vue";
import ThemeSelector from "./common/ThemeSelector.vue";

const router = useRouter();
const route = useRoute();
const authStore = useAuthStore();
const electionStore = useElectionStore();
const { t } = useI18n();
const { showSuccessMessage, showInfoMessage } = useNotifications();

const emit = defineEmits<{
  "toggle-mobile-menu": [];
}>();

const mobileMenuOpen = ref(false);
const isMobile = ref(false);

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
  const records = [...route.matched].reverse();
  for (const record of records) {
    const titleKey = record.meta.titleKey;
    if (typeof titleKey === "string") {
      return t(titleKey);
    }
  }
  return "";
});

// Get current election GUID from route
const showComputerCode = computed(() => route.path.includes("/ballots"));

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
  emit("toggle-mobile-menu");
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
      <h2 v-if="currentPageTitle" class="page-title" aria-live="polite">
        {{ currentPageTitle }}
      </h2>
    </div>
    <nav class="header-right" role="navigation" aria-label="User menu">
      <ComputerCodeBadge v-if="showComputerCode" />
      <ThemeSelector />
      <LanguageSelector />
      <el-dropdown trigger="click" @command="handleCommand">
        <button
          class="user-dropdown"
          aria-haspopup="menu"
          :aria-expanded="false"
          :aria-label="t('common.userMenu')"
        >
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
  font-size: var(--el-font-size-base);

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

  .page-title {
    margin: 0;
    font-size: var(--font-size-app-title);
    font-weight: var(--font-weight-app-title);
    color: var(--el-text-color-primary);
    white-space: nowrap;
    line-height: var(--line-height-tight);
    flex-shrink: 0;
  }

  .header-right {
    display: flex;
    align-items: center;
    gap: 12px;
  }

  .user-dropdown {
    display: flex;
    align-items: center;
    gap: 8px;
    cursor: pointer;
    padding: 5px 10px;
    border-radius: var(--el-border-radius-base);
    border: 1px solid transparent;
    background: transparent;
    font-size: inherit;
    font-weight: var(--font-weight-normal);
    transition:
      border-color 0.2s ease,
      background-color 0.2s ease;
  }

  .user-dropdown:hover,
  .user-dropdown:focus-visible {
    border-color: var(--el-border-color);
    background-color: var(--color-bg-secondary);
    outline: none;
  }

  .username {
    font-size: inherit;
    font-weight: var(--font-weight-normal);
    color: var(--el-text-color-regular);
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
    .page-title {
      max-width: 120px;
      overflow: hidden;
      text-overflow: ellipsis;
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
