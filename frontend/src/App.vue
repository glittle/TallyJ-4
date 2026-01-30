<script setup lang="ts">
import { RouterView } from "vue-router";
import ErrorBoundary from "./components/common/ErrorBoundary.vue";
</script>

<template>
  <ErrorBoundary>
    <RouterView />
  </ErrorBoundary>
</template>

<style lang="less">
// empty for now
</style>
