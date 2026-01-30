<template>
  <div>
    <slot v-if="!hasError" />
    <section v-else class="error-boundary" role="alert" aria-live="assertive" aria-labelledby="error-heading">
      <div class="error-content">
        <div class="error-icon" role="img" aria-label="Warning">⚠️</div>
        <h2 id="error-heading">{{ $t('error.somethingWentWrong') }}</h2>
        <p>{{ $t('error.pageError') }}</p>
        <div class="error-actions">
          <el-button @click="retry" type="primary" aria-label="Retry loading the page">
            {{ $t('error.tryAgain') }}
          </el-button>
          <el-button @click="goHome" type="default" aria-label="Go to home page">
            {{ $t('error.goHome') }}
          </el-button>
        </div>
        <details v-if="showDetails" class="error-details">
          <summary aria-expanded="false">{{ $t('error.errorDetails') }}</summary>
          <pre role="log" aria-label="Error details">{{ errorMessage }}</pre>
        </details>
      </div>
    </section>
  </div>
</template>

<script setup lang="ts">
import { ref, onErrorCaptured } from 'vue';

const hasError = ref(false);
const errorMessage = ref('');
const showDetails = ref(import.meta.env.DEV);

function retry() {
  hasError.value = false;
  errorMessage.value = '';
  window.location.reload();
}

function goHome() {
  window.location.href = '/';
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