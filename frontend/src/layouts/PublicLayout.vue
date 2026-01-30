<script setup lang="ts">
import LanguageSelector from "../components/common/LanguageSelector.vue";
import ThemeSelector from "../components/common/ThemeSelector.vue";
</script>

<template>
  <div class="public-layout">
    <div class="public-header">
      <div class="logo">
        <h2>TallyJ 4</h2>
      </div>
      <div class="header-right">
        <ThemeSelector />
        <LanguageSelector />
      </div>
    </div>
    <div class="public-content">
      <router-view />
    </div>
  </div>
</template>

<style lang="less">
.public-layout {
  min-height: 100vh;
  background: var(--color-public-bg-gradient);

  .public-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 20px 40px;
    backdrop-filter: blur(10px);
    /* Fallback for browsers that don't support backdrop-filter */
    background: var(--color-public-header-bg);
    -webkit-backdrop-filter: blur(10px); /* Safari support */
  }

  .logo h2 {
    color: var(--color-public-text);
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
}
</style>
