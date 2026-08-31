<script setup lang="ts">
import { useActiveTellers } from "@/composables/useActiveTellers";
import { useTellerStore } from "@/stores/tellerStore";
import type { ActiveTellers } from "@/utils/activeTellerStorage";
import { User } from "@element-plus/icons-vue";
import { computed, onMounted, watch } from "vue";

const props = withDefaults(
  defineProps<{
    electionGuid: string;
    highlightTeller1?: boolean;
    /** Which session teller inputs to show. Default is both (listing toolbar). */
    field?: "all" | "teller1" | "teller2";
    showIcon?: boolean;
  }>(),
  {
    highlightTeller1: false,
    field: "all",
  },
);

const emit = defineEmits<{
  tellersChanged: [tellers: ActiveTellers];
}>();

const tellerStore = useTellerStore();
const { tellers, setTeller1, setTeller2, refreshActiveTellers } =
  useActiveTellers();

const showIcon = computed(() => props.showIcon ?? props.field === "all");
const showTeller1 = computed(
  () => props.field === "all" || props.field === "teller1",
);
const showTeller2 = computed(
  () => props.field === "all" || props.field === "teller2",
);

const teller1 = computed(() => toSelectValue(tellers.value.teller1));
const teller2 = computed(() => toSelectValue(tellers.value.teller2));

const tellerOptions = computed(() =>
  tellerStore.tellers
    .map((teller) => teller.name.trim())
    .filter(Boolean)
    .sort(),
);

async function loadTellers() {
  try {
    await tellerStore.fetchTellers(props.electionGuid, 1, 200);
  } catch (error) {
    console.error("Failed to load tellers:", error);
  }
}

async function ensureTellerListed(name: string) {
  const trimmed = name.trim();
  if (!trimmed) {
    return;
  }
  if (tellerOptions.value.includes(trimmed)) {
    return;
  }
  try {
    await tellerStore.createTeller(props.electionGuid, {
      electionGuid: props.electionGuid,
      name: trimmed,
    });
  } catch {
    // Duplicate or validation errors are acceptable when racing other clients.
  }
}

function toSelectValue(name: string): string | undefined {
  return name.trim() || undefined;
}

function emitTellersChanged() {
  emit("tellersChanged", {
    teller1: tellers.value.teller1,
    teller2: tellers.value.teller2,
  });
}

async function handleTeller1Change(value: string | undefined) {
  setTeller1(value ?? "");
  emitTellersChanged();
  if (value) {
    await ensureTellerListed(value);
  }
}

async function handleTeller2Change(value: string | undefined) {
  setTeller2(value ?? "");
  emitTellersChanged();
  if (value) {
    await ensureTellerListed(value);
  }
}

onMounted(async () => {
  refreshActiveTellers();
  emitTellersChanged();
  await loadTellers();
});

watch(
  () => props.electionGuid,
  async () => {
    await loadTellers();
  },
);
</script>

<template>
  <div
    class="active-teller-selector"
    :class="{ 'is-single-field': field !== 'all' }"
  >
    <el-icon
      v-if="showIcon"
      class="teller-icon"
      aria-hidden="true"
      :title="$t('teller.active.hint')"
    >
      <User />
    </el-icon>
    <el-select
      v-if="showTeller1"
      :model-value="teller1"
      filterable
      allow-create
      clearable
      :placeholder="$t('teller.active.teller1Placeholder')"
      class="teller-select teller1-select"
      :class="{ 'required-field-flash': highlightTeller1 }"
      @change="handleTeller1Change"
    >
      <el-option value="" disabled :label="$t('teller.active.typeToAdd')" />
      <el-option
        v-for="name in tellerOptions"
        :key="`teller1-${name}`"
        :label="name"
        :value="name"
      />
    </el-select>
    <el-select
      v-if="showTeller2"
      :model-value="teller2"
      filterable
      allow-create
      clearable
      :placeholder="$t('teller.active.teller2Placeholder')"
      class="teller-select teller2-select"
      @change="handleTeller2Change"
    >
      <el-option value="" disabled :label="$t('teller.active.typeToAdd')" />
      <el-option
        v-for="name in tellerOptions"
        :key="`teller2-${name}`"
        :label="name"
        :value="name"
      />
    </el-select>
  </div>
</template>

<style lang="less">
.active-teller-selector {
  display: inline-flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 10px;

  .teller-select {
    width: 150px;
  }

  .teller-icon {
    color: var(--el-color-primary);
    font-size: 16px;
  }

  &.is-single-field {
    display: flex;
    width: 100%;

    .teller-select {
      width: 100%;
    }
  }
}
</style>
