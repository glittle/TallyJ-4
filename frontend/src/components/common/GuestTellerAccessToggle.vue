<script setup lang="ts">
import { isFullTeller } from "@/domain/guestTellerAccess";
import { useNotifications } from "@/composables/useNotifications";
import { User } from "@element-plus/icons-vue";
import { computed, ref, watch } from "vue";
import { useI18n } from "vue-i18n";
import { useRoute } from "vue-router";
import { useElectionStore } from "@/stores/electionStore";

const { t } = useI18n();
const route = useRoute();
const electionStore = useElectionStore();
const { showSuccessMessage, showErrorMessage } = useNotifications();

const toggling = ref(false);

const electionGuid = computed(() => route.params.id as string | undefined);

const showToggle = computed(
  () => isFullTeller() && Boolean(electionGuid.value),
);

const isOpen = computed(
  () => electionStore.currentElection?.isTellerAccessOpen ?? false,
);

watch(
  electionGuid,
  async (guid) => {
    if (!guid || !isFullTeller()) {
      return;
    }

    if (electionStore.currentElection?.electionGuid !== guid) {
      try {
        await electionStore.fetchElectionById(guid);
      } catch {
        // Header toggle is optional; page content may surface errors.
      }
    }
  },
  { immediate: true },
);

async function handleToggle(nextValue: string | number | boolean) {
  const guid = electionGuid.value;
  if (!guid || toggling.value) {
    return;
  }

  const open = Boolean(nextValue);
  toggling.value = true;
  try {
    await electionStore.toggleTellerAccess(guid, open);
    showSuccessMessage(
      open
        ? t("elections.tellerAccessOpen")
        : t("elections.tellerAccessClosed"),
    );
  } catch (error: unknown) {
    const message = error instanceof Error ? error.message : String(error);
    showErrorMessage(`${t("common.error")} ${message}`);
  } finally {
    toggling.value = false;
  }
}
</script>

<template>
  <div v-if="showToggle" class="guest-teller-access-toggle">
    <el-icon aria-hidden="true">
      <User />
    </el-icon>
    <span class="guest-teller-access-label">
      {{ t("elections.guestTellerAccess") }}
    </span>
    <el-switch
      :model-value="isOpen"
      :loading="toggling"
      :aria-label="t('elections.guestTellerAccess')"
      @change="handleToggle"
    />
  </div>
</template>

<style lang="less">
.guest-teller-access-toggle {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 0 4px;
  color: var(--el-text-color-secondary);
  font-size: var(--el-font-size-small);

  .guest-teller-access-label {
    white-space: nowrap;
  }
}
</style>
