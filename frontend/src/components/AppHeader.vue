<script setup lang="ts">
import { computed, ref } from "vue";
import { useRouter, useRoute } from "vue-router";
import { useAuthStore } from "../stores/authStore";
import { useI18n } from "vue-i18n";
import { ElMessage } from "element-plus";
import { ArrowDown, User, Setting, SwitchButton, Menu } from "@element-plus/icons-vue";
import LanguageSelector from "./common/LanguageSelector.vue";
import ThemeSelector from "./common/ThemeSelector.vue";

const router = useRouter();
const route = useRoute();
const authStore = useAuthStore();
const { t } = useI18n();

const mobileMenuOpen = ref(false);
const isMobile = ref(false);

// Check if we're on mobile
const checkMobile = () => {
  isMobile.value = window.innerWidth <= 768;
};

checkMobile();
window.addEventListener("resize", checkMobile);

const currentUser = computed(() => ({ email: authStore.email }));

const currentPageTitle = computed(() => {
  const titleMap: Record<string, string> = {
    "/dashboard": t("nav.dashboard"),
    "/elections": t("nav.elections"),
    "/profile": t("nav.profile"),
  };

  return titleMap[route.path] || (route.meta.title as string) || "";
});

function handleCommand(command: string) {
  if (command === "logout") {
    authStore.logout();
    ElMessage.success(t("auth.logoutSuccess"));
    router.push("/login?mode=officer");
  } else if (command === "profile") {
    router.push("/profile");
  } else if (command === "settings") {
    ElMessage.info("Settings page coming soon");
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
        @click="toggleMobileMenu"
        aria-label="Toggle navigation menu"
        :aria-expanded="mobileMenuOpen"
      >
        <el-icon><Menu /></el-icon>
      </button>
      <h1 class="sr-only">TallyJ 4 - Election Management System</h1>
      <h3 aria-live="polite">TallyJ 4 - {{ currentPageTitle }}</h3>
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
          <span class="username">{{ currentUser?.email || "User" }}</span>
          <el-icon aria-hidden="true"><ArrowDown /></el-icon>
        </button>
        <template #dropdown>
          <el-dropdown-menu>
            <el-dropdown-item command="profile">
              <el-icon><User /></el-icon>
              {{ $t("common.profile") }}
            </el-dropdown-item>
            <el-dropdown-item command="settings">
              <el-icon><Setting /></el-icon>
              {{ $t("common.settings") }}
            </el-dropdown-item>
            <el-dropdown-item divided command="logout">
              <el-icon><SwitchButton /></el-icon>
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

  .header-left h3 {
    margin: 0;
    font-size: 18px;
    font-weight: 500;
    color: var(--color-text-primary);
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
    outline: 2px solid #409eff;
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
    outline: 2px solid #409eff;
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
    }

    .header-left h3 {
      font-size: 16px;
    }

    .header-right {
      gap: 15px;
    }

    .username {
      display: none; /* Hide username on mobile to save space */
    }
  }

  @media (max-width: 480px) {
    .header-left h3 {
      font-size: 14px;
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
