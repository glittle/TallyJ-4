<script setup lang="ts">
import LanguageSelector from './components/common/LanguageSelector.vue';
</script>

<template>
  <div class="app-container">
    <header class="app-header">
      <h1>TallyJ 4!</h1>
      <LanguageSelector class="language-selector" />
    </header>
    <p><strong>Current route path:</strong> {{ $route.fullPath }}</p>
    <nav>
      <RouterLink to="/">Go to Home</RouterLink>
      <RouterLink to="/about">Go to About</RouterLink>
    </nav>
    <main>
      <RouterView />
    </main>
  </div>
</template>

<style lang="less">
.app-container {
  position: relative;
}

.app-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 1rem;
  
  h1 {
    font-weight: normal;
    margin: 0;
  }
  
  .language-selector {
    position: absolute;
    top: 1rem;
    right: 1rem;
  }
}
</style>
