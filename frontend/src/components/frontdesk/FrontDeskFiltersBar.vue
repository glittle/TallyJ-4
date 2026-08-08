<script setup lang="ts">
import {
  getFlagAbbr,
  getFlagFilterStyle,
  getMethodFilterStyle,
} from "@/utils/frontDeskStyles";

defineProps<{
  registrationTypes: { value: string; label: string }[];
  electionFlags: string[];
  selectedMethodFilters: string[];
  selectedFlagFilters: string[];
  methodCounts: Record<string, number>;
  flagCounts: Record<string, number>;
  hasActiveFilters: boolean;
}>();

const emit = defineEmits<{
  "toggle-method": [method: string];
  "toggle-flag": [flag: string];
  clear: [];
}>();
</script>

<template>
  <div
    v-if="registrationTypes.length > 0 || electionFlags.length > 0"
    class="front-desk-filters-bar"
  >
    <div>
      <div class="filter-group">
        <span class="filter-label">{{
          $t("frontDesk.filters.votingMethods")
        }}</span>

        <el-button
          v-for="method in registrationTypes"
          :key="method.value"
          size="small"
          :style="
            getMethodFilterStyle(
              method.label,
              selectedMethodFilters.includes(method.value),
            )
          "
          :class="{
            'filter-active': selectedMethodFilters.includes(method.value),
          }"
          @click="emit('toggle-method', method.value)"
        >
          {{ method.label }} ({{ methodCounts[method.value] || 0 }})
        </el-button>
      </div>

      <div v-if="electionFlags.length > 0" class="filter-group">
        <span class="filter-label">{{ $t("frontDesk.filters.flags") }}</span>

        <el-button
          v-for="flag in electionFlags"
          :key="flag"
          size="small"
          :style="
            getFlagFilterStyle(
              flag,
              electionFlags,
              selectedFlagFilters.includes(flag),
            )
          "
          :class="{ 'filter-active': selectedFlagFilters.includes(flag) }"
          @click="emit('toggle-flag', flag)"
        >
          {{ flag }} - {{ getFlagAbbr(flag, electionFlags) }} ({{
            flagCounts[flag] || 0
          }})
        </el-button>
      </div>
    </div>

    <el-button
      :class="{ hasActive: hasActiveFilters }"
      size="small"
      class="clear-btn"
      @click="emit('clear')"
    >
      {{ $t("common.clearFilters") }}
    </el-button>
  </div>
</template>

<style lang="less">
.front-desk-filters-bar {
  display: flex;
  padding: 0 var(--front-desk-content-padding-x, var(--spacing-3));
  border-bottom: 1px solid var(--el-border-color-lighter);
  font-size: var(--font-size-sm);

  > div {
    flex: 1;
  }

  .filter-group {
    display: flex;
    flex-wrap: wrap;
    align-items: center;
    justify-content: center;
    gap: 6px;
    margin-bottom: var(--spacing-2);
  }

  .filter-label {
    color: var(--el-text-color-secondary);
    margin-right: 4px;
  }

  .clear-btn {
    opacity: 0.25;
    &.hasActive {
      opacity: 1;
    }
  }
}
</style>
