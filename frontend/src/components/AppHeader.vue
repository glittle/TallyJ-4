<template>
  <div class="app-header">
    <div class="header-left">
      <h3>TallyJ 4 - {{ currentPageTitle }}</h3>
    </div>
    <div class="header-right">
      <LanguageSelector />
      <el-dropdown trigger="click" @command="handleCommand">
        <span class="user-dropdown">
          <el-avatar :size="32" icon="UserFilled" />
          <span class="username">{{ currentUser?.email || 'User' }}</span>
          <el-icon><ArrowDown /></el-icon>
        </span>
        <template #dropdown>
          <el-dropdown-menu>
            <el-dropdown-item command="profile">
              <el-icon><User /></el-icon>
              {{ $t('common.profile') }}
            </el-dropdown-item>
            <el-dropdown-item command="settings">
              <el-icon><Setting /></el-icon>
              {{ $t('common.settings') }}
            </el-dropdown-item>
            <el-dropdown-item divided command="logout">
              <el-icon><SwitchButton /></el-icon>
              {{ $t('auth.logout') }}
            </el-dropdown-item>
          </el-dropdown-menu>
        </template>
      </el-dropdown>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import { useAuthStore } from '../stores/authStore';
import { useI18n } from 'vue-i18n';
import { ElMessage } from 'element-plus';
import { ArrowDown, User, Setting, SwitchButton } from '@element-plus/icons-vue';
import LanguageSelector from './common/LanguageSelector.vue';

const router = useRouter();
const route = useRoute();
const authStore = useAuthStore();
const { t } = useI18n();

const currentUser = computed(() => ({ email: authStore.email }));

const currentPageTitle = computed(() => {
  const titleMap: Record<string, string> = {
    '/dashboard': t('nav.dashboard'),
    '/elections': t('nav.elections'),
    '/profile': t('nav.profile')
  };
  
  return titleMap[route.path] || route.meta.title as string || '';
});

function handleCommand(command: string) {
  if (command === 'logout') {
    authStore.logout();
    ElMessage.success(t('auth.logoutSuccess'));
    router.push('/login');
  } else if (command === 'profile') {
    router.push('/profile');
  } else if (command === 'settings') {
    ElMessage.info('Settings page coming soon');
  }
}
</script>

<style scoped>
.app-header {
  width: 100%;
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.header-left h3 {
  margin: 0;
  font-size: 18px;
  font-weight: 500;
  color: #303133;
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
  transition: background-color 0.3s;
}

.user-dropdown:hover {
  background-color: #f5f7fa;
}

.username {
  font-size: 14px;
  color: #606266;
}

:deep(.el-dropdown-menu__item) {
  display: flex;
  align-items: center;
  gap: 8px;
}
</style>
