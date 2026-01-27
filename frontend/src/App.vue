<script setup lang="ts">
import ErrorBoundary from './components/common/ErrorBoundary.vue';
</script>

<template>
  <ErrorBoundary>
    <RouterView />
  </ErrorBoundary>
</template>

<style scoped>
</style>
