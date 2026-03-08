<script setup lang="ts">
import LoadingSkeleton from "./LoadingSkeleton.vue";

interface Props {
  columns?: number;
  rows?: number;
}

withDefaults(defineProps<Props>(), {
  columns: 4,
  rows: 5,
});
</script>

<template>
  <div class="table-skeleton">
    <div class="table-header">
      <div v-for="i in columns" :key="i" class="header-cell">
        <LoadingSkeleton width="80%" height="16px" />
      </div>
    </div>
    <div v-for="row in rows" :key="row" class="table-row">
      <div v-for="col in columns" :key="col" class="table-cell">
        <LoadingSkeleton width="70%" height="14px" />
      </div>
    </div>
  </div>
</template>

<style lang="less">
.table-skeleton {
  background: var(--el-bg-color);
  border-radius: 8px;
  overflow: hidden;
  box-shadow: 0 2px 12px rgba(0, 0, 0, 0.1);
}

.table-header {
  display: flex;
  padding: 16px;
  background: var(--el-fill-color-light);
  border-bottom: 1px solid var(--el-border-color-lighter);
}

.header-cell {
  flex: 1;
  padding: 0 8px;
}

.table-row {
  display: flex;
  padding: 16px;
  border-bottom: 1px solid var(--el-border-color-lighter);
}

.table-row:last-child {
  border-bottom: none;
}

.table-cell {
  flex: 1;
  padding: 0 8px;
}
</style>
