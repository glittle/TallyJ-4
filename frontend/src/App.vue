<script setup lang="ts">
import { computed, onMounted, ref, watch } from "vue";
import { useI18n } from "vue-i18n";
import { RouterView } from "vue-router";
import ErrorBoundary from "./components/common/ErrorBoundary.vue";
import { i18n } from "./locales";

const { t } = useI18n();
const nameVisible = ref(true);
const branchName = computed(() => {
  const name = process.env.BRANCH_NAME;
  return name === "HEAD" || !nameVisible.value ? "" : "Branch: " + name;
});

const hideName = () => {
  nameVisible.value = false;
};

// Set document title dynamically based on i18n
const updateDocumentTitle = () => {
  document.title = t("common.appTitle");
};

onMounted(() => {
  updateDocumentTitle();
});

// Watch for locale changes to update the title
watch(
  () => i18n.global.locale,
  () => {
    updateDocumentTitle();
  },
);
</script>

<template>
  <ErrorBoundary>
    <RouterView />
  </ErrorBoundary>

  <div
    v-if="branchName"
    class="bottomCorner"
    title="Click to remove"
    @click="hideName"
  >
    {{ branchName }}
  </div>
</template>

<style lang="less">
.bottomCorner {
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
