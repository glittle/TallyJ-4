<script setup lang="ts">
import { useNotifications } from "@/composables/useNotifications";
import { isFullTeller } from "@/domain/guestTellerAccess";
import { useElectionStore } from "@/stores/electionStore";
import { buildTellerJoinUrl } from "@/utils/tellerJoinUrl";
import { CopyDocument, Iphone, Link } from "@element-plus/icons-vue";
import QRCode from "qrcode";
import { computed, ref, watch } from "vue";
import { useI18n } from "vue-i18n";
import { useRoute } from "vue-router";

const { t } = useI18n();
const route = useRoute();
const electionStore = useElectionStore();
const { showSuccessMessage, showErrorMessage } = useNotifications();

const toggling = ref(false);
const shareDrawerOpen = ref(false);
const qrCodeUrl = ref("");

const electionGuid = computed(() => route.params.id as string | undefined);

const showToggle = computed(
  () => isFullTeller() && Boolean(electionGuid.value),
);

const election = computed(() => {
  const current = electionStore.currentElection;
  if (!current || current.electionGuid !== electionGuid.value) {
    return null;
  }
  return current;
});

const isOpen = computed(() => election.value?.isTellerAccessOpen ?? false);

const passcode = computed(() => election.value?.electionPasscode ?? "");

const shareableUrl = computed(() => {
  if (!electionGuid.value || !passcode.value) {
    return "";
  }
  return buildTellerJoinUrl(
    globalThis.location.origin,
    electionGuid.value,
    passcode.value,
  );
});

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
        // Header control is optional; page content may surface errors.
      }
    }
  },
  { immediate: true },
);

// Only generate QR while the share drawer is open (avoids work on every page load).
watch([shareableUrl, shareDrawerOpen], async ([url, drawerOpen]) => {
  if (!drawerOpen) {
    qrCodeUrl.value = "";
    return;
  }
  if (!url) {
    qrCodeUrl.value = "";
    return;
  }
  await generateQrCode(url);
});

async function handleToggle(nextValue: string | number | boolean) {
  const guid = electionGuid.value;
  if (!guid || toggling.value) {
    return;
  }

  const open = Boolean(nextValue);
  toggling.value = true;
  try {
    await electionStore.toggleTellerAccess(guid, open);
  } catch (error: unknown) {
    const message = error instanceof Error ? error.message : String(error);
    showErrorMessage(`${t("common.error")} ${message}`);
  } finally {
    toggling.value = false;
  }
}

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

async function generateQrCode(url: string) {
  try {
    qrCodeUrl.value = await QRCode.toDataURL(url, {
      width: 180,
      margin: 1,
      color: {
        dark: "#000000",
        light: "#FFFFFF",
      },
    });
  } catch {
    qrCodeUrl.value = "";
  }
}

function openShareDrawer() {
  shareDrawerOpen.value = true;
}
</script>

<template>
  <div
    v-if="showToggle"
    class="guest-teller-access-box header-status-box"
    :class="isOpen ? 'is-open' : 'is-closed'"
  >
    <span class="guest-teller-access-label header-status-label">
      {{ t("elections.guestTellerAccess") }}
    </span>
    <el-switch
      :model-value="isOpen"
      :loading="toggling"
      size="small"
      :aria-label="t('elections.guestTellerAccess')"
      @change="handleToggle"
    />
    <el-button
      type="primary"
      size="small"
      :icon="Iphone"
      class="guest-teller-share-btn"
      :disabled="!passcode"
      :title="
        passcode
          ? t('elections.showShareLink')
          : t('elections.form.electionPasscodeHelp')
      "
      @click="openShareDrawer"
    >
      {{ t("elections.showShareLink") }}
    </el-button>

    <el-drawer
      v-model="shareDrawerOpen"
      :title="t('elections.shareTellerAccess')"
      direction="ttb"
      size="auto"
      class="teller-share-drawer"
      modal-class="teller-share-drawer-modal"
      :lock-scroll="false"
      append-to-body
    >
      <div v-if="passcode" class="share-drawer-body">
        <div class="share-fields">
          <div class="share-field">
            <label>{{ t("elections.tellerAccessCode") }}</label>
            <div class="share-field-row">
              <el-input :model-value="passcode" readonly size="small" />
              <el-button
                size="small"
                :icon="CopyDocument"
                :title="t('elections.copyAccessCode')"
                :aria-label="t('elections.copyAccessCode')"
                @click="copyText(passcode)"
              />
            </div>
          </div>

          <div class="share-field">
            <label>{{ t("elections.tellerAccessUrl") }}</label>
            <div class="share-field-row">
              <el-input :model-value="shareableUrl" readonly size="small" />
              <el-button
                type="primary"
                size="small"
                :icon="CopyDocument"
                @click="copyText(shareableUrl)"
              >
                {{ t("elections.copyUrl") }}
              </el-button>
            </div>
          </div>

          <p class="share-hint">{{ t("elections.shareTellerAccessHint") }}</p>
        </div>

        <div class="share-qr-block">
          <label>{{ t("elections.tellerAccessQrCode") }}</label>
          <div class="qr-container">
            <img
              v-if="qrCodeUrl"
              :src="qrCodeUrl"
              :alt="t('elections.tellerAccessQrCode')"
              class="qr-code"
            />
            <div v-else class="qr-placeholder">
              <el-icon size="40">
                <Link />
              </el-icon>
              <p>{{ t("common.loading") }}</p>
            </div>
          </div>
        </div>
      </div>

      <div v-else class="share-drawer-empty">
        <el-alert
          type="warning"
          :title="t('common.warning')"
          :description="t('elections.form.electionPasscodeHelp')"
          show-icon
          :closable="false"
        />
      </div>
    </el-drawer>
  </div>
</template>

<style lang="less">
// Base .header-status-box styles live in styles/utilities/box.less
.guest-teller-access-box {
  .guest-teller-share-btn {
    margin-left: 2px;
  }
}

// Drawer is append-to-body; keep styles global under these class names.
.teller-share-drawer.el-drawer {
  width: min(560px, calc(100vw - 32px)) !important;
  // Hug content height instead of a fixed tall panel.
  height: auto !important;
  left: 50% !important;
  right: auto !important;
  transform: translateX(-50%);
  border-radius: 0 0 12px 12px;
  overflow: hidden;
  box-shadow: var(--el-box-shadow-light);

  .el-drawer__header {
    margin-bottom: 12px;
    padding: 20px 36px 0;
  }

  .el-drawer__body {
    overflow: visible;
    padding: 4px 36px 24px;
  }
}

.teller-share-drawer {
  .share-drawer-body {
    display: flex;
    flex-wrap: nowrap;
    align-items: flex-start;
    gap: 28px;
  }

  .share-fields {
    flex: 1 1 auto;
    min-width: 0;
    display: flex;
    flex-direction: column;
    gap: 12px;
  }

  .share-field {
    label {
      display: block;
      font-weight: 600;
      margin-bottom: 6px;
      color: var(--el-text-color-primary);
      font-size: var(--el-font-size-small);
    }
  }

  .share-field-row {
    display: flex;
    align-items: center;
    gap: 8px;

    .el-input {
      flex: 1;
      min-width: 0;
    }
  }

  .share-hint {
    margin: 0;
    font-size: 12px;
    color: var(--el-text-color-secondary);
    line-height: 1.4;
  }

  .share-qr-block {
    flex: 0 0 auto;
    text-align: center;

    label {
      display: block;
      font-weight: 600;
      margin-bottom: 6px;
      font-size: var(--el-font-size-small);
    }

    .qr-code {
      width: 180px;
      height: 180px;
      border: 1px solid var(--el-border-color);
      border-radius: 4px;
      background: #fff;
    }

    .qr-placeholder {
      width: 180px;
      height: 180px;
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      border: 1px dashed var(--el-border-color);
      border-radius: 4px;
      color: var(--el-text-color-secondary);

      p {
        margin: 8px 0 0;
        font-size: 13px;
      }
    }
  }

  .share-drawer-empty {
    padding: 4px 0 8px;
  }

  @media (max-width: 560px) {
    .share-drawer-body {
      flex-direction: column;
      align-items: stretch;
    }

    .share-qr-block {
      align-self: center;
    }
  }
}

@media (max-width: 768px) {
  .guest-teller-access-box {
    .guest-teller-access-label {
      // Keep the control compact on narrow headers.
      max-width: 5.5rem;
      overflow: hidden;
      text-overflow: ellipsis;
    }

    .guest-teller-share-btn .el-icon + span {
      display: none;
    }
  }
}
</style>
