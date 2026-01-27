<template>
  <nav class="app-sidebar" role="navigation" aria-label="Main navigation">
    <div class="logo">
      <h2>TallyJ 4</h2>
    </div>
    <el-menu
      :default-active="activeRoute"
      :router="true"
      background-color="#304156"
      text-color="#bfcbd9"
      active-text-color="#409eff"
      aria-label="Main menu"
      @select="handleMenuSelect"
    >
      <el-menu-item index="/dashboard" role="menuitem">
        <el-icon aria-hidden="true"><HomeFilled /></el-icon>
        <span>{{ $t('nav.dashboard') }}</span>
      </el-menu-item>

      <el-menu-item index="/elections" role="menuitem">
        <el-icon aria-hidden="true"><Document /></el-icon>
        <span>{{ $t('nav.elections') }}</span>
      </el-menu-item>

      <el-menu-item index="/profile" role="menuitem">
        <el-icon aria-hidden="true"><User /></el-icon>
        <span>{{ $t('nav.profile') }}</span>
      </el-menu-item>
    </el-menu>
  </nav>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { useRoute } from 'vue-router';
import { HomeFilled, Document, User } from '@element-plus/icons-vue';

const emit = defineEmits<{
  'close-mobile-sidebar': [];
}>();

const route = useRoute();

const activeRoute = computed(() => {
  const path = route.path;
  if (path.startsWith('/elections')) return '/elections';
  if (path.startsWith('/profile')) return '/profile';
  return '/dashboard';
});

function handleMenuSelect() {
  // Emit event to close mobile sidebar when menu item is selected
  emit('close-mobile-sidebar');
}
</script>

<style scoped>
.app-sidebar {
  height: 100%;
  display: flex;
  flex-direction: column;
}

.logo {
  padding: 20px;
  text-align: center;
  border-bottom: 1px solid #263445;
}

.logo h2 {
  color: #fff;
  margin: 0;
  font-size: 20px;
  font-weight: 600;
}

:deep(.el-menu) {
  border-right: none;
}

:deep(.el-menu-item) {
  height: 50px;
  line-height: 50px;
}

:deep(.el-menu-item:hover) {
  background-color: #263445 !important;
}

:deep(.el-menu-item.is-active) {
  background-color: #001528 !important;
}
</style>
