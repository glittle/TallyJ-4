<script setup lang="ts">
import { useNotifications } from "@/composables/useNotifications";
import { isFullTeller } from "@/domain/guestTellerAccess";
import { ArrowDown, Menu, SwitchButton, User } from "@element-plus/icons-vue";
import { computed } from "vue";
import { useI18n } from "vue-i18n";
import { useRoute, useRouter } from "vue-router";
import { useAuthStore } from "../stores/authStore";
import ComputerCodeBadge from "./common/ComputerCodeBadge.vue";
import GuestTellerAccessToggle from "./common/GuestTellerAccessToggle.vue";
import OnlineVotingHeaderControl from "./common/OnlineVotingHeaderControl.vue";
import LanguageSelector from "./common/LanguageSelector.vue";
import ThemeSelector from "./common/ThemeSelector.vue";

const props = withDefaults(
  defineProps<{
    /** Show hamburger (overlay / mobile layout). */
    showMenuButton?: boolean;
    /** Whether the overlay sidebar is currently open. */
    menuOpen?: boolean;
  }>(),
  {
    showMenuButton: false,
    menuOpen: false,
  },
);

const emit = defineEmits<{
  "toggle-mobile-menu": [];
}>();

const router = useRouter();
const route = useRoute();
const authStore = useAuthStore();
const { t } = useI18n();
const { showSuccessMessage } = useNotifications();

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

// List (/ballots), open ballot (/ballot/{guid}), and related ballot routes.
const showComputerCode = computed(() => /\/ballot(?:s|\/|$)/.test(route.path));

/** Online voting and Guest tellers (and the ballot computer code) sit on their own row when the header is narrow. */
const showStatusRow = computed(
  () => showComputerCode.value || (isFullTeller() && Boolean(route.params.id)),
);

async function handleCommand(command: string) {
  if (command === "logout") {
    await authStore.logout();
    showSuccessMessage(t("auth.logoutSuccess"));
    router.push("/");
  } else if (command === "profile") {
    router.push("/profile");
  }
}

function toggleMobileMenu() {
  emit("toggle-mobile-menu");
}
</script>

<template>
  <header class="app-header" role="banner">
    <div class="header-bar">
      <div class="header-left">
        <button
          v-if="props.showMenuButton"
          class="mobile-menu-btn"
          type="button"
          :aria-label="t('common.toggleNavigation')"
          :aria-expanded="props.menuOpen"
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
      <nav
        class="header-right"
        role="navigation"
        :aria-label="t('common.userMenu')"
      >
        <ThemeSelector />
        <LanguageSelector />
        <el-dropdown trigger="click" @command="handleCommand">
          <!-- The dropdown trigger owns aria-describedby, so the visible email is the name. -->
          <button
            class="user-dropdown"
            type="button"
            aria-haspopup="menu"
            :aria-expanded="false"
          >
            <span class="username">{{
              currentUser?.name || currentUser?.email || t("common.user")
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
    </div>
    <div v-if="showStatusRow" class="header-status">
      <ComputerCodeBadge v-if="showComputerCode" />
      <OnlineVotingHeaderControl />
      <GuestTellerAccessToggle />
    </div>
  </header>
</template>

<style lang="less">
.app-header {
  width: 100%;
  min-width: 0;
  display: flex;
  flex-wrap: wrap;
  justify-content: space-between;
  align-items: center;
  column-gap: 12px;
  row-gap: 8px;
  font-size: var(--el-font-size-base);
  container-type: inline-size;
  container-name: app-header;

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

  .header-bar {
    display: flex;
    align-items: center;
    gap: 12px;
    flex: 1 1 auto;
    min-width: 0;
  }

  .header-left {
    display: flex;
    align-items: center;
    gap: 16px;
    flex: 1 1 auto;
    min-width: 0;
  }

  .page-title {
    margin: 0;
    min-width: 0;
    max-width: 100%;
    font-size: var(--font-size-app-title);
    font-weight: var(--font-weight-app-title);
    color: var(--el-text-color-primary);
    white-space: nowrap;
    line-height: var(--line-height-tight);
    overflow: hidden;
    text-overflow: ellipsis;
  }

  .header-right {
    display: flex;
    align-items: center;
    gap: 12px;
    flex: 0 0 auto;
  }

  /* Status chips, not navigation. Under about 1100px they take the next row
     so the title stays on one line and does not run into them. */
  .header-status {
    display: flex;
    align-items: center;
    justify-content: flex-end;
    gap: 12px;
    flex: 0 1 auto;
    flex-wrap: wrap;
    margin-inline-start: auto;
  }

  @container app-header (max-width: 1100px) {
    .header-status {
      flex: 1 0 100%;
      margin-inline-start: 0;
    }
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
    color: var(--el-text-color-primary);
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
