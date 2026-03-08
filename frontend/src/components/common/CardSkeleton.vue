<template>
  <div class="card-skeleton">
    <div class="card-header">
      <LoadingSkeleton width="60%" height="24px" />
      <LoadingSkeleton width="40%" height="16px" />
    </div>
    <div class="card-content">
      <LoadingSkeleton width="100%" height="16px" />
      <LoadingSkeleton width="80%" height="16px" />
      <LoadingSkeleton width="90%" height="16px" />
    </div>
    <div class="card-actions">
      <LoadingSkeleton width="80px" height="32px" />
      <LoadingSkeleton width="80px" height="32px" />
    </div>
  </div>
</template>

<script setup lang="ts">
import LoadingSkeleton from "./LoadingSkeleton.vue";
</script>

<style lang="less">
.card-skeleton {
  background: white;
  border-radius: 8px;
  padding: 20px;
  box-shadow: 0 2px 12px rgba(0, 0, 0, 0.1);

  .card-header {
    margin-bottom: 16px;
  }

  .card-header > * + * {
    margin-top: 8px;
  }

  .card-content > * + * {
    margin-top: 8px;
  }

  .card-actions {
    display: flex;
    gap: 12px;
    margin-top: 20px;
  }
}
</style>
