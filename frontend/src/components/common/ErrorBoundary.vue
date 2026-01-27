<template>
  <div>
    <slot v-if="!hasError" />
    <div v-else class="error-boundary">
      <div class="error-content">
        <el-icon size="48" class="error-icon">
          <Warning />
        </el-icon>
        <h2>{{ $t('error.somethingWentWrong') }}</h2>
        <p>{{ $t('error.pageError') }}</p>
        <div class="error-actions">
          <el-button @click="retry" type="primary">
            {{ $t('error.tryAgain') }}
          </el-button>
          <el-button @click="goHome" type="default">
            {{ $t('error.goHome') }}
          </el-button>
        </div>
        <details v-if="showDetails" class="error-details">
          <summary>{{ $t('error.errorDetails') }}</summary>
          <pre>{{ errorMessage }}</pre>
        </details>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onErrorCaptured } from 'vue';
import { useRouter } from 'vue-router';
import { Warning } from '@element-plus/icons-vue';

const router = useRouter();

const hasError = ref(false);
const errorMessage = ref('');
const showDetails = ref(import.meta.env.DEV);

function retry() {
  hasError.value = false;
  errorMessage.value = '';
  window.location.reload();
}

function goHome() {
  router.push('/');
}

onErrorCaptured((error, instance, info) => {
  hasError.value = true;
  errorMessage.value = `${error}\n\nComponent: ${instance?.$?.type?.name || 'Unknown'}\nInfo: ${info}`;

  // Log error for monitoring
  console.error('Error Boundary caught an error:', error, instance, info);

  // Prevent error from propagating
  return false;
});
</script>

<style scoped>
.error-boundary {
  display: flex;
  justify-content: center;
  align-items: center;
  min-height: 400px;
  padding: 20px;
}

.error-content {
  text-align: center;
  max-width: 500px;
}

.error-icon {
  color: #f56c6c;
  margin-bottom: 16px;
}

.error-content h2 {
  color: #303133;
  margin-bottom: 8px;
}

.error-content p {
  color: #606266;
  margin-bottom: 24px;
}

.error-actions {
  display: flex;
  gap: 12px;
  justify-content: center;
  margin-bottom: 16px;
}

.error-details {
  text-align: left;
  margin-top: 20px;
}

.error-details summary {
  cursor: pointer;
  color: #909399;
  margin-bottom: 8px;
}

.error-details pre {
  background: #f5f7fa;
  padding: 12px;
  border-radius: 4px;
  font-size: 12px;
  color: #606266;
  overflow-x: auto;
  white-space: pre-wrap;
}
</style>