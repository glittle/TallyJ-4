<template>
  <div
    class="loading-skeleton"
    :style="{ width, height }"
    role="progressbar"
    aria-label="Loading content"
    aria-valuemin="0"
    aria-valuemax="100"
    aria-valuenow="50"
  >
    <div class="skeleton-shimmer" aria-hidden="true"></div>
  </div>
</template>

<script setup lang="ts">
interface Props {
  width?: string;
  height?: string;
}

withDefaults(defineProps<Props>(), {
  width: "100%",
  height: "20px",
});
</script>

<style lang="less">
.loading-skeleton {
  background: var(--el-fill-color-light);
  border-radius: 4px;
  position: relative;
  overflow: hidden;
}

.skeleton-shimmer {
  position: absolute;
  top: 0;
  left: -100%;
  width: 100%;
  height: 100%;
  background: linear-gradient(
    90deg,
    transparent,
    rgba(255, 255, 255, 0.4),
    transparent
  );
  animation: shimmer 1.5s infinite;
}

.dark .skeleton-shimmer {
  background: linear-gradient(
    90deg,
    transparent,
    rgba(255, 255, 255, 0.1),
    transparent
  );
}

@keyframes shimmer {
  0% {
    left: -100%;
  }
  100% {
    left: 100%;
  }
}
</style>
