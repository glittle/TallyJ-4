<script setup lang="ts">
import { useComputerCode } from "@/composables/useComputerCode";
import { Monitor } from "@element-plus/icons-vue";
import { useI18n } from "vue-i18n";

const { t } = useI18n();
const { computerCode } = useComputerCode();
</script>

<template>
  <div class="computer-code-badge" :aria-label="t('ballots.computerCodeBadge')">
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
  </div>
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

  .computer-code-label {
    font-weight: var(--font-weight-medium, 500);
    letter-spacing: 0.04em;
  }
}
</style>
