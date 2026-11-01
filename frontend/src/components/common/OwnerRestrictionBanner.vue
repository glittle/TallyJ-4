<script setup lang="ts">
import { authService, type OwnerRestriction } from "@/services/authService";
import { isGuestTeller } from "@/domain/guestTellerAccess";
import { resolveUserFacingApiError } from "@/utils/errorHandler";
import { computed, onMounted, ref } from "vue";
import { useI18n } from "vue-i18n";

const { t } = useI18n();
const restriction = ref<OwnerRestriction | null>(null);
const requesting = ref(false);
const errorText = ref("");

const showBanner = computed(() => restriction.value?.blocksSends === true);

const whenText = computed(() =>
  formatWhen(restriction.value?.reviewRequestedAt),
);
const nextText = computed(() => formatWhen(restriction.value?.nextRequestAt));

function formatWhen(value?: string | Date | null) {
  if (!value) {
    return "";
  }
  const date = value instanceof Date ? value : new Date(value);
  if (Number.isNaN(date.getTime())) {
    return "";
  }
  return date.toLocaleString();
}

async function load() {
  if (isGuestTeller()) {
    return;
  }
  try {
    restriction.value = await authService.getOwnerRestriction();
  } catch {
    restriction.value = null;
  }
}

async function requestReview() {
  requesting.value = true;
  errorText.value = "";
  try {
    restriction.value = await authService.requestOwnerReview();
  } catch (error) {
    errorText.value = resolveUserFacingApiError(
      error,
      t("elections.ownerRestricted.requestFailed"),
    );
  } finally {
    requesting.value = false;
  }
}

onMounted(load);
</script>

<template>
  <div
    v-if="showBanner"
    class="owner-restriction-banner"
    role="status"
    data-testid="owner-restriction-banner"
  >
    <p v-if="restriction?.status === 'Rejected'">
      {{ t("elections.ownerRestricted.rejected") }}
    </p>
    <p v-else>{{ t("elections.ownerRestricted.banner") }}</p>
    <p v-if="restriction?.note" class="owner-restriction-note">
      {{ t("elections.ownerRestricted.holdNote", { note: restriction.note }) }}
    </p>
    <p v-if="restriction?.status === 'ReviewRequested' && whenText">
      {{
        t("elections.ownerRestricted.requested", {
          when: whenText,
          next: nextText,
        })
      }}
    </p>
    <p v-if="errorText" class="owner-restriction-error">{{ errorText }}</p>
    <el-button
      v-if="restriction?.status !== 'Rejected'"
      type="primary"
      size="small"
      :loading="requesting"
      :disabled="restriction?.canRequestReview !== true"
      @click="requestReview"
    >
      {{ t("elections.ownerRestricted.requestReview") }}
    </el-button>
  </div>
</template>

<style lang="less">
.owner-restriction-banner {
  margin: 0;
  padding: 12px 20px;
  background: var(--el-color-warning-light-9);
  color: var(--color-text-primary);
  border-bottom: 1px solid var(--el-color-warning-light-5);

  p {
    margin: 0 0 8px;
    line-height: 1.45;
  }

  .owner-restriction-note,
  .owner-restriction-error {
    font-weight: 600;
  }
}
</style>
