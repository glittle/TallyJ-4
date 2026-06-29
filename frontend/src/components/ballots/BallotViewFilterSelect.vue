<script setup lang="ts">
import type { BallotSummaryDto } from "@/utils/ballotSummary";
import {
  ALL_BALLOTS_FILTER,
  buildBallotViewFilterGroups,
  computerFilterValue,
  locationFilterValue,
  type BallotViewFilterGroup,
} from "@/utils/ballotViewFilter";
import type { ComputerDto } from "@/types/Computer";
import type { LocationDto } from "@/types/Location";
import { computed } from "vue";
import { useI18n } from "vue-i18n";

const props = defineProps<{
  modelValue: string;
  locations: LocationDto[];
  ballots: BallotSummaryDto[];
  computersByLocation: Record<string, ComputerDto[]>;
}>();

const emit = defineEmits<{
  "update:modelValue": [value: string];
}>();

const { t } = useI18n();

const filterGroups = computed<BallotViewFilterGroup[]>(() =>
  buildBallotViewFilterGroups(
    props.locations,
    props.ballots,
    props.computersByLocation,
  ),
);

const hasLocationGroups = computed(() => filterGroups.value.length > 0);

function updateValue(value: string) {
  emit("update:modelValue", value);
}
</script>

<template>
  <el-select
    :model-value="modelValue"
    class="ballot-view-filter-select"
    filterable
    :placeholder="$t('ballots.viewFilterPlaceholder')"
    :aria-label="$t('ballots.viewFilterLabel')"
    @update:model-value="updateValue"
  >
    <el-option :label="$t('ballots.allBallots')" :value="ALL_BALLOTS_FILTER" />

    <template v-if="hasLocationGroups">
      <el-option-group
        v-for="group in filterGroups"
        :key="group.locationGuid"
        :label="group.locationName"
      >
        <el-option
          :label="$t('ballots.allAtLocation', { name: group.locationName })"
          :value="locationFilterValue(group.locationGuid)"
        />
        <el-option
          v-for="code in group.computerCodes"
          :key="`${group.locationGuid}-${code}`"
          :label="code"
          :value="computerFilterValue(group.locationGuid, code)"
        />
      </el-option-group>
    </template>
  </el-select>
</template>

<style lang="less">
.ballot-view-filter-select {
  width: 100%;
}
</style>
