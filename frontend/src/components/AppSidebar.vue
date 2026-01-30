<script setup lang="ts">
import { computed } from "vue";
import { useRoute } from "vue-router";
import { HomeFilled, Document, User } from "@element-plus/icons-vue";

const emit = defineEmits<{
  "close-mobile-sidebar": [];
}>();

const route = useRoute();

const activeRoute = computed(() => {
  const path = route.path;
  if (path.startsWith("/elections")) return "/elections";
  if (path.startsWith("/profile")) return "/profile";
  return "/dashboard";
});

function handleMenuSelect() {
  // Emit event to close mobile sidebar when menu item is selected
  emit("close-mobile-sidebar");
}
</script>
<template>
  <nav class="app-sidebar" role="navigation" aria-label="Main navigation">
    <div class="logo">
      <h2>TallyJ 4</h2>
    </div>
    <el-menu
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

      <el-menu-item index="/profile" role="menuitem">
        <el-icon aria-hidden="true"><User /></el-icon>
        <span>{{ $t("nav.profile") }}</span>
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

  :deep(.el-menu) {
    border-right: none;
    background-color: var(--color-sidebar-bg) !important;
    color: var(--color-sidebar-text) !important;
  }

  :deep(.el-menu-item) {
    height: 50px;
    line-height: 50px;
  }

  :deep(.el-menu-item:hover) {
    background-color: var(--color-sidebar-hover) !important;
  }

  :deep(.el-menu-item.is-active) {
    background-color: var(--color-sidebar-active) !important;
    color: var(--color-sidebar-text-active) !important;
  }
}
</style>
