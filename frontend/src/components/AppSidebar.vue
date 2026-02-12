<script setup lang="ts">
import { computed } from "vue";
import { useRoute, useRouter } from "vue-router";
import { HomeFilled, Document, ArrowLeft } from "@element-plus/icons-vue";
import { useElectionStore } from "../stores/electionStore";

const emit = defineEmits<{
  "close-mobile-sidebar": [];
}>();

const route = useRoute();
const router = useRouter();
const electionStore = useElectionStore();

const isInElectionContext = computed(() => {
  return route.path.startsWith("/elections/") && route.params.id;
});

const electionName = computed(() => {
  return electionStore.currentElection?.name || "Election";
});

const activeRoute = computed(() => {
  const path = route.path;
  if (path.startsWith("/elections")) return "/elections";
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
      <h2>TallyJ 4</h2>
    </div>

    <!-- Election breadcrumb navigation -->
    <div v-if="isInElectionContext" class="breadcrumb-nav">
      <div class="breadcrumb-item" @click="goBackToElections">
        <el-icon><ArrowLeft /></el-icon>
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
        <el-icon aria-hidden="true"><HomeFilled /></el-icon>
        <span>{{ $t("nav.dashboard") }}</span>
      </el-menu-item>

      <el-menu-item index="/elections" role="menuitem">
        <el-icon aria-hidden="true"><Document /></el-icon>
        <span>{{ $t("nav.elections") }}</span>
      </el-menu-item>
    </el-menu>
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
}
</style>
