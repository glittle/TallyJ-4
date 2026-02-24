<script setup lang="ts">
import { RouterView } from "vue-router";
import ErrorBoundary from "./components/common/ErrorBoundary.vue";
import { computed, ref } from "vue";

const nameVisible = ref(true);

const branchName = computed(() => {
  const name = process.env.BRANCH_NAME;
  return name === "main" || !nameVisible.value ? "" : "Branch: " + name;
});

const hideName = () => {
  nameVisible.value = false;
};
</script>

<template>
  <ErrorBoundary>
    <RouterView />
  </ErrorBoundary>
  <div
    v-if="branchName"
    class="devBranchName"
    title="Click to remove"
    @click="hideName"
  >
    {{ branchName }}
  </div>
</template>

<style lang="less">
.devBranchName {
  position: fixed;
  bottom: 0;
  left: 0;
  background-color: #f0f0f0;
  padding: 1px 0.5em;
  font-size: 0.75em;
  color: #666;
  cursor: pointer;
}
</style>
