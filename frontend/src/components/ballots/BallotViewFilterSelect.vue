<script setup lang="ts">
import type { BallotSummaryDto } from "@/utils/ballotSummary";
import {
  ALL_BALLOTS_FILTER,
  buildBallotViewFilterGroups,
  computerFilterValue,
  isBallotViewFilterInOptions,
  locationFilterValue,
  parseBallotViewFilter,
  type BallotViewFilterGroup,
} from "@/utils/ballotViewFilter";
import type { ComputerDto } from "@/types/Computer";
import type { LocationDto } from "@/types/Location";
import { formatComputerCodeLabel } from "@/utils/ballotDisplayCode";
import { computed } from "vue";
import { useI18n } from "vue-i18n";

const props = withDefaults(
  defineProps<{
    modelValue: string;
    locations: LocationDto[];
    ballots: BallotSummaryDto[];
    computersByLocation: Record<string, ComputerDto[]>;
    /** Keep this workstation's code selectable even with zero ballots yet. */
    ensureComputerCode?: string;
    ensureLocationGuid?: string | null;
  }>(),
  {
    ensureComputerCode: "",
    ensureLocationGuid: null,
  },
);

const emit = defineEmits<{
  "update:modelValue": [value: string];
}>();

const { t } = useI18n();

const filterGroups = computed<BallotViewFilterGroup[]>(() =>
  buildBallotViewFilterGroups(
    props.locations,
    props.ballots,
    props.computersByLocation,
    props.ensureComputerCode,
    props.ensureLocationGuid,
  ),
);

const hasLocationGroups = computed(() => filterGroups.value.length > 0);

function locationNameForGuid(locationGuid: string): string {
  return (
    filterGroups.value.find((group) => group.locationGuid === locationGuid)
      ?.locationName ??
    props.locations.find((location) => location.locationGuid === locationGuid)
      ?.name ??
    locationGuid
  );
}

/**
 * Closed-state label only. Dropdown options keep their existing short labels
 * (computer code under a location group); when closed we show "Location / Code".
 */
const selectedDisplayLabel = computed(() => {
  const filter = parseBallotViewFilter(props.modelValue);

  switch (filter.type) {
    case "all":
      return t("ballots.allBallots");
    case "location":
      return t("ballots.allAtLocation", {
        name: locationNameForGuid(filter.locationGuid),
      });
    case "computer":
      if (!filter.locationGuid) {
        return formatComputerCodeLabel(t, filter.computerCode);
      }
      return `${locationNameForGuid(filter.locationGuid)} / ${formatComputerCodeLabel(t, filter.computerCode)}`;
    default:
      return t("ballots.allBallots");
  }
});

/** Global computer filter (computer|*|CODE) or any orphan value still shown cleanly. */
const supplementalComputerOption = computed(() => {
  const filter = parseBallotViewFilter(props.modelValue);
  if (filter.type !== "computer") {
    return null;
  }

  if (!filter.locationGuid) {
    return {
      label: formatComputerCodeLabel(t, filter.computerCode),
      value: computerFilterValue(null, filter.computerCode),
    };
  }

  if (isBallotViewFilterInOptions(props.modelValue, filterGroups.value)) {
    return null;
  }

  return {
    label: formatComputerCodeLabel(t, filter.computerCode),
    value: props.modelValue,
  };
});

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
    <template #label>
      {{ selectedDisplayLabel }}
    </template>

    <el-option :label="$t('ballots.allBallots')" :value="ALL_BALLOTS_FILTER" />

    <el-option
      v-if="supplementalComputerOption"
      :label="supplementalComputerOption.label"
      :value="supplementalComputerOption.value"
    />

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
          :label="formatComputerCodeLabel(t, code)"
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
