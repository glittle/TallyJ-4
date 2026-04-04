<script setup lang="ts">
import {
  ArrowLeft,
  Document,
  HomeFilled,
  Setting,
} from "@element-plus/icons-vue";
import { computed } from "vue";
import { useI18n } from "vue-i18n";
import { useRoute, useRouter } from "vue-router";
import { useElectionStore } from "../stores/electionStore";
import { useSuperAdminStore } from "../stores/superAdminStore";
import { BUILD_DATE, VERSION } from "./version";
const { t } = useI18n();

const emit = defineEmits<{
  "close-mobile-sidebar": [];
}>();

const route = useRoute();
const router = useRouter();
const electionStore = useElectionStore();
const superAdminStore = useSuperAdminStore();

const isSuperAdmin = computed(() => superAdminStore.isSuperAdmin);

const isInElectionContext = computed(() => {
  return route.path.startsWith("/elections/") && route.params.id;
});

const versionName = computed(() => VERSION);
const versionDate = computed(() => BUILD_DATE);

const electionName = computed(() => {
  return electionStore.currentElection?.name || t("common.election");
});

const activeRoute = computed(() => {
  const path = route.path;
  if (path.startsWith("/super-admin")) {
    return "/super-admin";
  }
  if (path.startsWith("/elections")) {
    return "/elections";
  }
  return "/dashboard";
});

function handleMenuSelect() {
  // Emit event to close mobile sidebar when menu item is selected
  emit("close-mobile-sidebar");
}

function goBackToElections() {
  router.push("/elections");
  emit("close-mobile-sidebar");
}
</script>
<template>
  <nav class="app-sidebar" role="navigation" aria-label="Main navigation">
    <div class="logo">
      <div class="logoTop">
        <img src="/assets/logo-trans.png" :alt="$t('common.logoAlt')" />
        <h2>{{ $t("common.appTitle") }}</h2>
      </div>
      <div class="version-tooltip" :title="versionDate">
        {{ $t("common.versionDisplay", { version: versionName }) }}
      </div>
    </div>
    <div class="testOnlyWarning">
      {{ $t("common.testOnlyShort") }}
    </div>

    <!-- Election breadcrumb navigation -->
    <div v-if="isInElectionContext" class="breadcrumb-nav">
      <div class="breadcrumb-item" @click="goBackToElections">
        <el-icon>
          <ArrowLeft />
        </el-icon>
        <span>{{ $t("nav.elections") }}</span>
      </div>
      <div class="breadcrumb-separator">/</div>
      <div class="breadcrumb-item current">
        <span>{{ electionName }}</span>
      </div>
    </div>

    <!-- Main navigation menu -->
    <el-menu
      v-else
      :default-active="activeRoute"
      :router="true"
      aria-label="Main menu"
      @select="handleMenuSelect"
    >
      <el-menu-item index="/dashboard" role="menuitem">
        <el-icon aria-hidden="true">
          <HomeFilled />
        </el-icon>
        <span>{{ $t("nav.dashboard") }}</span>
      </el-menu-item>

      <el-menu-item index="/elections" role="menuitem">
        <el-icon aria-hidden="true">
          <Document />
        </el-icon>
        <span>{{ $t("nav.elections") }}</span>
      </el-menu-item>

      <el-menu-item v-if="isSuperAdmin" index="/super-admin" role="menuitem">
        <el-icon aria-hidden="true">
          <Setting />
        </el-icon>
        <span>{{ $t("nav.superAdmin") }}</span>
      </el-menu-item>
    </el-menu>

    <div class="statusDocLink">
      <a
        href="https://docs.google.com/document/d/1WXrVy2Jl3Lk-Vs1k77t2QnrrdK5zjzSeHMXnOx0Deao/edit?usp=sharing"
        target="statusDoc"
        >Status & Feedback Document V4</a
      >
    </div>
  </nav>
</template>

<style lang="less">
.app-sidebar {
  height: 100%;
  display: flex;
  flex-direction: column;

  .logo {
    padding: 20px;
    text-align: center;
    border-bottom: 1px solid var(--color-sidebar-border);
  }
  .logoTop {
    display: flex;
    justify-content: center;
    align-items: center;
    gap: 10px;
    img {
      height: 2.5em;
    }
  }

  .version-tooltip {
    text-align: center;
    font-size: 0.75em;
    color: var(--color-gray-500);
    margin: 4px 0 6px 0;
  }

  .logo h2 {
    color: var(--color-text-primary);
    margin: 0;
    font-size: 20px;
    font-weight: 600;
  }

  .el-menu {
    border-right: none;
    background-color: var(--color-sidebar-bg) !important;
    color: var(--color-sidebar-text) !important;
  }

  .el-menu-item {
    height: 50px;
    line-height: 50px;
  }

  .el-menu-item:hover {
    background-color: var(--color-sidebar-hover) !important;
  }

  .el-menu-item.is-active {
    background-color: var(--color-sidebar-active) !important;
    color: var(--color-sidebar-text-active) !important;
  }

  .breadcrumb-nav {
    padding: 20px;
    color: var(--color-sidebar-text);
  }

  .breadcrumb-item {
    display: flex;
    align-items: center;
    gap: 8px;
    padding: 8px 0;
    cursor: pointer;
    transition: color 0.2s ease;

    &:hover {
      color: var(--color-sidebar-text-active);
    }

    &.current {
      cursor: default;
      font-weight: 600;
      color: var(--color-sidebar-text-active);

      &:hover {
        color: var(--color-sidebar-text-active);
      }
    }

    .el-icon {
      font-size: 16px;
    }

    span {
      font-size: 14px;
    }
  }

  .breadcrumb-separator {
    color: var(--color-gray-400);
    font-size: 14px;
    margin: 0 8px;
  }

  .testOnlyWarning {
    padding: 0.5em 0.25em;
    margin: -0.5em 0.5em 1em;
    text-align: center;
    background-color: var(--el-color-error);
    color: var(--color-error-50);
    font-size: 1.5em;
    font-weight: bold;
    border-radius: 4px;
  }

  .statusDocLink {
    padding: 0 0.5em;
    text-align: center;
    margin: auto 0.5em 1em;
    border: 2px solid var(--el-color-error);
    border-radius: 4px;
  }
}
</style>
