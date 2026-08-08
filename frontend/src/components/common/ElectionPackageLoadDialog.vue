<script setup lang="ts">
import { computed, nextTick, ref, watch } from "vue";
import type { ElectionPackageLoaderLogLine } from "../../types/SignalREvents";

interface Props {
  modelValue: boolean;
  lines: ElectionPackageLoaderLogLine[];
  /** True while HTTP import request is in flight. */
  loading: boolean;
  /** True after HTTP completed successfully (even if log still shows). */
  succeeded: boolean;
  /** Error message when HTTP import failed. */
  errorMessage?: string | null;
}

type Emits = (e: "update:modelValue", value: boolean) => void;

const props = defineProps<Props>();
const emit = defineEmits<Emits>();

const logContainer = ref<HTMLElement | null>(null);

const visible = computed({
  get: () => props.modelValue,
  set: (value) => emit("update:modelValue", value),
});

const canClose = computed(() => !props.loading);

watch(
  () => props.lines.length,
  async () => {
    await nextTick();
    if (logContainer.value) {
      logContainer.value.scrollTop = logContainer.value.scrollHeight;
    }
  },
);

function handleClose() {
  if (canClose.value) {
    visible.value = false;
  }
}
</script>

<template>
  <el-dialog
    v-model="visible"
    :title="$t('elections.importLoaderTitle')"
    :close-on-click-modal="false"
    :close-on-press-escape="canClose"
    :show-close="canClose"
    width="560px"
    class="election-package-load-dialog"
    @close="handleClose"
  >
    <div
      ref="logContainer"
      class="loader-log"
      role="log"
      aria-live="polite"
      aria-relevant="additions"
    >
      <div
        v-for="line in lines"
        :key="line.id"
        class="loader-log-line"
        :class="{ temporary: line.isTemporary }"
      >
        {{ line.message }}
      </div>
      <div
        v-if="loading && lines.length === 0"
        class="loader-log-line temporary"
      >
        {{ $t("elections.importLoaderStarting") }}
      </div>
    </div>

    <el-alert
      v-if="errorMessage"
      :title="errorMessage"
      type="error"
      :closable="false"
      class="loader-result"
    />
    <el-alert
      v-else-if="succeeded"
      :title="$t('elections.importElectionSuccess')"
      type="success"
      :closable="false"
      class="loader-result"
    />

    <template #footer>
      <el-button v-if="canClose" type="primary" @click="handleClose">
        {{ $t("common.close") }}
      </el-button>
      <el-button v-else :disabled="true" :loading="true">
        {{ $t("elections.importLoaderWorking") }}
      </el-button>
    </template>
  </el-dialog>
</template>

<style lang="less">
.election-package-load-dialog {
  .loader-log {
    max-height: 280px;
    overflow-y: auto;
    padding: 12px 14px;
    border-radius: 6px;
    background: var(--el-fill-color-light);
    border: 1px solid var(--el-border-color-lighter);
    font-family:
      ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace;
    font-size: 13px;
    line-height: 1.45;
  }

  .loader-log-line {
    color: var(--el-text-color-primary);
    margin: 0 0 4px;

    &.temporary {
      color: var(--el-text-color-secondary);
      font-style: italic;
    }
  }

  .loader-result {
    margin-top: 16px;
  }
}
</style>
