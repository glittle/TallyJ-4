<template>
  <div class="profile-page">
    <el-card>
      <template #header>
        <h2>{{ $t('common.profile') }}</h2>
      </template>
      
      <div class="profile-content">
        <el-descriptions :column="1" border>
          <el-descriptions-item :label="$t('auth.email')">
            {{ currentUser?.email || '-' }}
          </el-descriptions-item>
          <el-descriptions-item :label="$t('profile.createdAt')">
            {{ formatDate(currentUser?.createdAt) }}
          </el-descriptions-item>
        </el-descriptions>
        
        <div class="actions">
          <el-button type="primary" @click="showChangePassword = true">
            {{ $t('profile.changePassword') }}
          </el-button>
        </div>
      </div>
    </el-card>
    
    <el-dialog
      v-model="showChangePassword"
      :title="$t('profile.changePassword')"
      width="400px"
    >
      <el-form :model="passwordForm" label-position="top">
        <el-form-item :label="$t('profile.currentPassword')">
          <el-input v-model="passwordForm.current" type="password" />
        </el-form-item>
        <el-form-item :label="$t('profile.newPassword')">
          <el-input v-model="passwordForm.new" type="password" />
        </el-form-item>
        <el-form-item :label="$t('profile.confirmPassword')">
          <el-input v-model="passwordForm.confirm" type="password" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showChangePassword = false">
          {{ $t('common.cancel') }}
        </el-button>
        <el-button type="primary" @click="handleChangePassword">
          {{ $t('common.save') }}
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';
import { useAuthStore } from '../stores/authStore';
import { useI18n } from 'vue-i18n';
import { ElMessage } from 'element-plus';

const authStore = useAuthStore();
const { t } = useI18n();

const currentUser = computed(() => ({ email: authStore.email, createdAt: null }));
const showChangePassword = ref(false);
const passwordForm = ref({
  current: '',
  new: '',
  confirm: ''
});

function formatDate(date: any) {
  if (!date) return '-';
  return new Date(date).toLocaleDateString();
}

function handleChangePassword() {
  if (passwordForm.value.new !== passwordForm.value.confirm) {
    ElMessage.error(t('profile.passwordsDoNotMatch'));
    return;
  }
  
  ElMessage.info('Password change functionality coming soon');
  showChangePassword.value = false;
}
</script>

<style lang="less">
.profile-page {
  max-width: 800px;
  margin: 0 auto;

  .profile-content {
    padding: 20px 0;
  }

  .actions {
    margin-top: 30px;
    display: flex;
    gap: 10px;
  }
}
</style>
