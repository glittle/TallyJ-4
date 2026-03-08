<template>
  <div class="ds-table" :class="{ 'ds-table--loading': loading }">
    <div v-if="loading" class="ds-table__loading">
      <el-skeleton :rows="loadingRows" animated />
    </div>

    <div v-else-if="!data || data.length === 0" class="ds-table__empty">
      <slot name="empty">
        <el-empty :description="emptyText" />
      </slot>
    </div>

    <el-table
      v-else
      :data="data"
      :stripe="stripe"
      :border="border"
      :size="size"
      :highlight-current-row="highlightCurrentRow"
      v-bind="$attrs"
      @selection-change="handleSelectionChange"
      @current-change="handleCurrentChange"
      @sort-change="handleSortChange"
    >
      <slot />
    </el-table>

    <div v-if="pagination && total > 0" class="ds-table__pagination">
      <el-pagination
        v-model:current-page="currentPage"
        v-model:page-size="pageSize"
        :page-sizes="pageSizes"
        :total="total"
        :layout="paginationLayout"
        :background="true"
        @size-change="handleSizeChange"
        @current-change="handlePageChange"
      />
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, watch } from "vue";

interface Props {
  data?: any[];
  loading?: boolean;
  loadingRows?: number;
  emptyText?: string;
  stripe?: boolean;
  border?: boolean;
  size?: "large" | "default" | "small";
  highlightCurrentRow?: boolean;
  pagination?: boolean;
  total?: number;
  defaultPageSize?: number;
  pageSizes?: number[];
  paginationLayout?: string;
}

const props = withDefaults(defineProps<Props>(), {
  data: () => [],
  loading: false,
  loadingRows: 5,
  emptyText: "No data",
  stripe: true,
  border: false,
  size: "default",
  highlightCurrentRow: true,
  pagination: false,
  total: 0,
  defaultPageSize: 10,
  pageSizes: () => [10, 20, 50, 100],
  paginationLayout: "total, sizes, prev, pager, next, jumper",
});

const emit = defineEmits<{
  "selection-change": [selection: any[]];
  "current-change": [currentRow: any, oldCurrentRow: any];
  "sort-change": [{ column: any; prop: string; order: string }];
  "page-change": [page: number];
  "size-change": [size: number];
}>();

const currentPage = ref(1);
const pageSize = ref(props.defaultPageSize);

watch(
  () => props.defaultPageSize,
  (newSize) => {
    pageSize.value = newSize;
  },
);

function handleSelectionChange(selection: any[]) {
  emit("selection-change", selection);
}

function handleCurrentChange(currentRow: any, oldCurrentRow: any) {
  emit("current-change", currentRow, oldCurrentRow);
}

function handleSortChange(sortInfo: {
  column: any;
  prop: string;
  order: string;
}) {
  emit("sort-change", sortInfo);
}

function handlePageChange(page: number) {
  emit("page-change", page);
}

function handleSizeChange(size: number) {
  emit("size-change", size);
}
</script>

<style lang="less">
.ds-table {
  width: 100%;

  &--loading {
    min-height: 300px;
  }

  &__loading {
    padding: var(--spacing-8);
  }

  &__empty {
    padding: var(--spacing-12) var(--spacing-6);
    text-align: center;
  }

  &__pagination {
    margin-top: var(--spacing-6);
    display: flex;
    justify-content: flex-end;
  }
}

.el-table {
  border-radius: var(--radius-lg);
  overflow: hidden;

  th {
    background-color: var(--color-gray-50);
    font-weight: var(--font-weight-semibold);

    .dark & {
      background-color: var(--color-gray-800);
    }
  }

  tr:hover {
    background-color: var(--color-gray-50);

    .dark & {
      background-color: var(--color-gray-800);
    }
  }
}
</style>
