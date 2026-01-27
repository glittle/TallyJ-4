<template>
  <div class="public-layout">
    <div class="public-header">
      <div class="logo">
        <h2>TallyJ 4</h2>
      </div>
      <div class="header-right">
        <LanguageSelector />
      </div>
    </div>
    <div class="public-content">
      <router-view />
    </div>
  </div>
</template>

<script setup lang="ts">
import LanguageSelector from '../components/common/LanguageSelector.vue';
</script>

<style scoped>
.public-layout {
  min-height: 100vh;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
}

.public-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 20px 40px;
  background: rgba(255, 255, 255, 0.1);
  backdrop-filter: blur(10px);
  /* Fallback for browsers that don't support backdrop-filter */
  background: rgba(255, 255, 255, 0.1);
  -webkit-backdrop-filter: blur(10px); /* Safari support */
}

.logo h2 {
  color: white;
  margin: 0;
  font-size: 24px;
  font-weight: 600;
}

.header-right {
  display: flex;
  align-items: center;
  gap: 20px;
}

.public-content {
  padding: 20px;
}
</style>
