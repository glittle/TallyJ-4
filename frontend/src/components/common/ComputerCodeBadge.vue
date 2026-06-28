<script setup lang="ts">
import { useComputerCode } from "@/composables/useComputerCode";
import { useNotifications } from "@/composables/useNotifications";
import { isValidComputerCode } from "@/utils/computerCodeStorage";
import { Monitor } from "@element-plus/icons-vue";
import { ref, watch } from "vue";
import { useI18n } from "vue-i18n";

const { t } = useI18n();
const { computerCode, updateComputerCode } = useComputerCode();
const { showSuccessMessage, showWarningMessage } = useNotifications();

const popoverOpen = ref(false);
const draftCode = ref(computerCode.value);

watch(popoverOpen, (open) => {
  if (open) {
    draftCode.value = computerCode.value;
  }
});

function saveComputerCode() {
  const normalized = draftCode.value.trim().toUpperCase();
  if (!normalized) {
    updateComputerCode("");
    popoverOpen.value = false;
    showSuccessMessage(t("ballots.computerCodeCleared"));
    return;
  }

  if (!isValidComputerCode(normalized)) {
    showWarningMessage(t("ballots.computerCodeInvalid"));
    return;
  }

  updateComputerCode(normalized);
  popoverOpen.value = false;
  showSuccessMessage(t("ballots.computerCodeSaved"));
}
</script>

<template>
  <el-popover
    v-model:visible="popoverOpen"
    placement="bottom-end"
    :width="240"
    trigger="click"
  >
    <template #reference>
      <button
        type="button"
        class="computer-code-badge"
        :aria-label="t('ballots.computerCodeBadge')"
      >
        <el-icon aria-hidden="true">
          <Monitor />
        </el-icon>
        <span class="computer-code-label">
          {{
            computerCode
              ? t("ballots.computerCodeShort", { code: computerCode })
              : t("ballots.computerCodeUnset")
          }}
        </span>
      </button>
    </template>

    <div class="computer-code-editor">
      <label class="computer-code-editor-label" for="computer-code-input">
        {{ $t("ballots.computerCodeLabel") }}
      </label>
      <el-input
        id="computer-code-input"
        v-model="draftCode"
        maxlength="2"
        :placeholder="$t('ballots.computerCodePlaceholder')"
        @keyup.enter="saveComputerCode"
      />
      <p class="computer-code-editor-help">
        {{ $t("ballots.computerCodeHelp") }}
      </p>
      <div class="computer-code-editor-actions">
        <el-button size="small" @click="popoverOpen = false">
          {{ $t("common.cancel") }}
        </el-button>
        <el-button size="small" type="primary" @click="saveComputerCode">
          {{ $t("common.save") }}
        </el-button>
      </div>
    </div>
  </el-popover>
</template>

<style lang="less">
.computer-code-badge {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 6px 10px;
  border: 1px solid transparent;
  border-radius: var(--el-border-radius-base);
  background: transparent;
  color: var(--el-text-color-secondary);
  font-size: var(--el-font-size-small);
  cursor: pointer;
  transition:
    border-color 0.2s ease,
    background-color 0.2s ease,
    color 0.2s ease;

  &:hover,
  &:focus-visible {
    border-color: var(--el-border-color);
    background-color: var(--color-bg-secondary);
    color: var(--el-text-color-regular);
    outline: none;
  }

  .computer-code-label {
    font-weight: var(--font-weight-medium, 500);
    letter-spacing: 0.04em;
  }
}

.computer-code-editor {
  display: flex;
  flex-direction: column;
  gap: 8px;

  .computer-code-editor-label {
    font-size: var(--el-font-size-small);
    color: var(--el-text-color-regular);
  }

  .computer-code-editor-help {
    margin: 0;
    font-size: var(--el-font-size-extra-small);
    color: var(--el-text-color-secondary);
    line-height: 1.4;
  }

  .computer-code-editor-actions {
    display: flex;
    justify-content: flex-end;
    gap: 8px;
    margin-top: 4px;
  }
}
</style>
