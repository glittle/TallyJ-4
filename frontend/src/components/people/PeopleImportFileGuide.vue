<script setup lang="ts">
import { useNotifications } from "@/composables/useNotifications";
import { useEligibilityStore } from "@/stores/eligibilityStore";
import { CopyDocument } from "@element-plus/icons-vue";
import { onMounted, ref } from "vue";
import { useI18n } from "vue-i18n";

const { t } = useI18n();
const { showSuccessMessage, showErrorMessage } = useNotifications();
const eligibilityStore = useEligibilityStore();
const codesVisible = ref(false);

onMounted(() => {
  void eligibilityStore.fetchReasons();
});

async function copyText(text: string) {
  if (!text) {
    return;
  }

  try {
    await navigator.clipboard.writeText(text);
    showSuccessMessage(t("common.copied"));
  } catch (error: unknown) {
    const message = error instanceof Error ? error.message : String(error);
    showErrorMessage(`${t("common.error")} ${message}`);
  }
}
</script>

<template>
  <div class="people-import-file-guide">
    <p>{{ $t("people.import.fileGuideIntro") }}</p>
    <ul class="guide-list">
      <li>
        <strong>{{ $t("people.import.fileGuideRequiredLabel") }}</strong>
        {{ $t("people.import.fileGuideRequired") }}
      </li>
      <li>
        <strong>{{ $t("people.import.fileGuideOptionalLabel") }}</strong>
        {{ $t("people.import.fileGuideOptional") }}
      </li>
    </ul>
    <p>{{ $t("people.import.fileGuideEligibility") }}</p>
    <el-button type="primary" plain @click="codesVisible = true">
      {{ $t("people.import.showEligibilityCodes") }}
    </el-button>

    <el-dialog
      v-model="codesVisible"
      :title="$t('people.import.eligibilityCodesTitle')"
      width="640px"
      class="eligibility-codes-dialog"
      append-to-body
    >
      <p class="eligibility-codes-intro">
        {{ $t("people.import.eligibilityCodesIntro") }}
      </p>

      <section class="eligibility-code-group">
        <h4>{{ $t("eligibility.groupEligible") }}</h4>
        <ul>
          <li>
            <span class="eligibility-desc user-selectable">{{
              $t("eligibility.eligible")
            }}</span>
            <el-button
              text
              circle
              size="small"
              :icon="CopyDocument"
              :title="$t('people.import.copyDescription')"
              :aria-label="$t('people.import.copyDescription')"
              @click="copyText($t('eligibility.eligible'))"
            />
          </li>
        </ul>
      </section>

      <section
        v-for="(reasons, group) in eligibilityStore.groupedReasons"
        v-show="reasons.length > 0"
        :key="group"
        class="eligibility-code-group"
      >
        <h4>{{ $t(`eligibility.group${group}`) }}</h4>
        <ul>
          <li v-for="reason in reasons" :key="reason.reasonGuid">
            <span class="eligibility-code user-selectable">{{
              reason.code
            }}</span>
            <el-button
              text
              circle
              size="small"
              :icon="CopyDocument"
              :title="$t('people.import.copyCode')"
              :aria-label="$t('people.import.copyCode')"
              @click="copyText(reason.code)"
            />
            <span class="eligibility-desc user-selectable">{{
              $t(`eligibility.${reason.code}`)
            }}</span>
            <el-button
              text
              circle
              size="small"
              :icon="CopyDocument"
              :title="$t('people.import.copyDescription')"
              :aria-label="$t('people.import.copyDescription')"
              @click="copyText($t(`eligibility.${reason.code}`))"
            />
          </li>
        </ul>
      </section>
    </el-dialog>
  </div>
</template>

<style lang="less">
.people-import-file-guide {
  p {
    margin: 0 0 12px;
    color: var(--el-text-color-regular);
  }

  .guide-list {
    margin: 0 0 12px;
    padding-left: 20px;

    li {
      margin-bottom: 6px;
    }
  }
}

.eligibility-codes-dialog {
  .eligibility-codes-intro {
    margin: 0 0 16px;
    color: var(--el-text-color-regular);
  }

  .eligibility-code-group {
    margin-bottom: 16px;

    h4 {
      margin: 0 0 8px;
      font-size: var(--font-size-sm);
      font-weight: 600;
      color: var(--el-text-color-secondary);
    }

    ul {
      margin: 0;
      padding: 0;
      list-style: none;
    }

    li {
      display: flex;
      align-items: center;
      gap: 4px;
      min-height: 32px;
      padding: 2px 0;
      border-bottom: 1px solid var(--el-border-color-extra-light);
    }
  }

  .eligibility-code {
    font-family: var(--el-font-family);
    font-weight: 600;
  }

  .eligibility-desc {
    flex: 1 1 auto;
    min-width: 0;
    padding-left: 1em;
  }

  .user-selectable {
    user-select: text;
    cursor: text;
  }
}
</style>
