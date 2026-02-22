<template>
  <el-dialog
    v-model="visible"
    :title="$t('import.progressTitle')"
    :close-on-click-modal="false"
    :close-on-press-escape="false"
    :show-close="false"
    width="500px"
    class="import-progress-dialog"
  >
    <div v-if="importProgress" class="import-progress">
      <el-progress
        :percentage="importProgress.percentComplete"
        :text-inside="true"
        :stroke-width="20"
        status="success"
      />

      <div class="progress-details">
        <p>
          {{
            $t("import.processingRows", {
              processed: importProgress.processedRows,
              total: importProgress.totalRows,
            })
          }}
        </p>
        <p>{{ $t("import.successCount", { count: importProgress.successCount }) }}</p>
        <p v-if="importProgress.errorCount > 0" class="error-count">
          {{ $t("import.errorCount", { count: importProgress.errorCount }) }}
        </p>
        <p class="status">{{ importProgress.currentStatus }}</p>
      </div>
    </div>

    <div v-if="importErrors.length > 0" class="import-errors">
      <h4>{{ $t("import.errors") }}</h4>
      <el-alert
        v-for="(error, index) in importErrors.slice(0, 5)"
        :key="index"
        :title="error"
        type="error"
        :closable="false"
        style="margin-bottom: 10px"
      />
      <p v-if="importErrors.length > 5">
        {{ $t("import.moreErrors", { count: importErrors.length - 5 }) }}
      </p>
    </div>

    <div v-if="importComplete" class="import-complete">
      <el-alert :title="$t('import.completed')" type="success" :closable="false" />
    </div>

    <template #footer>
      <el-button v-if="importComplete" type="primary" @click="handleClose">
        {{ $t("common.close") }}
      </el-button>
      <el-button v-else @click="handleCancel" :disabled="true">
        {{ $t("common.cancel") }}
      </el-button>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { computed } from "vue";
import { useImportStore } from "../../stores/importStore";

const importStore = useImportStore();

interface Props {
  modelValue: boolean;
}

type Emits = (e: "update:modelValue", value: boolean) => void;

const props = defineProps<Props>();
const emit = defineEmits<Emits>();

const visible = computed({
  get: () => props.modelValue,
  set: (value) => emit("update:modelValue", value),
});

const importProgress = computed(() => importStore.importProgress);
const importErrors = computed(() => importStore.importErrors);
const importComplete = computed(() => importStore.importComplete);

function handleClose() {
  importStore.clearImportState();
  visible.value = false;
}

function handleCancel() {
  // Import cancellation could be implemented here if needed
  // For now, just close the dialog
  visible.value = false;
}
</script>

<style lang="less">
.import-progress-dialog {
  .import-progress {
    text-align: center;
    margin-bottom: 20px;
  }
  .progress-details {
    margin-top: 20px;
  }

  .progress-details p {
    margin: 5px 0;
    font-size: 14px;
  }

  .error-count {
    color: var(--el-color-error);
  }

  .status {
    font-weight: bold;
    color: var(--el-color-primary);
  }

  .import-errors {
    margin-top: 20px;
  }

  .import-errors h4 {
    margin-bottom: 10px;
    color: var(--el-color-error);
  }

  .import-complete {
    margin-top: 20px;
  }
}
</style>
