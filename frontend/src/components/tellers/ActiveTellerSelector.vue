<script setup lang="ts">
import { useTellerStore } from "@/stores/tellerStore";
import {
  getActiveTellers,
  setActiveTeller1,
  setActiveTeller2,
  type ActiveTellers,
} from "@/utils/activeTellerStorage";
import { computed, onMounted, ref, watch } from "vue";

const props = defineProps<{
  electionGuid: string;
}>();

const emit = defineEmits<{
  tellersChanged: [tellers: ActiveTellers];
}>();

const tellerStore = useTellerStore();

const teller1 = ref<string | undefined>(undefined);
const teller2 = ref<string | undefined>(undefined);

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
    teller1: teller1.value ?? "",
    teller2: teller2.value ?? "",
  });
}

async function handleTeller1Change(value: string | undefined) {
  teller1.value = toSelectValue(value ?? "");
  setActiveTeller1(value ?? "");
  emitTellersChanged();
  if (value) {
    await ensureTellerListed(value);
  }
}

async function handleTeller2Change(value: string | undefined) {
  teller2.value = toSelectValue(value ?? "");
  setActiveTeller2(value ?? "");
  emitTellersChanged();
  if (value) {
    await ensureTellerListed(value);
  }
}

onMounted(async () => {
  const stored = getActiveTellers();
  teller1.value = toSelectValue(stored.teller1);
  teller2.value = toSelectValue(stored.teller2);
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
  <div class="active-teller-selector">
    <el-select
      v-model="teller1"
      filterable
      allow-create
      clearable
      :placeholder="$t('teller.active.teller1Placeholder')"
      class="teller-select"
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
      v-model="teller2"
      filterable
      allow-create
      clearable
      :placeholder="$t('teller.active.teller2Placeholder')"
      class="teller-select"
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
    <span class="teller-hint">{{ $t("teller.active.hint") }}</span>
  </div>
</template>

<style lang="less">
.active-teller-selector {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 10px;

  .teller-select {
    width: 200px;
  }

  .teller-hint {
    color: var(--el-text-color-secondary);
    font-size: 12px;
    flex: 1 1 220px;
  }
}
</style>
