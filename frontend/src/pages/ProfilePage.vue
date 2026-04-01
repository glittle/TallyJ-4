<script setup lang="ts">
import { ref, computed, onMounted } from "vue";
import { useAuthStore } from "../stores/authStore";
import { useI18n } from "vue-i18n";
import { useNotifications } from "@/composables/useNotifications";
import { authService } from "../services/authService";

const authStore = useAuthStore();
const { t } = useI18n();
const { showErrorMessage, showInfoMessage, showSuccessMessage } =
  useNotifications();

const currentUser = computed(() => ({
  name: authStore.name,
  email: authStore.email,
  authMethod: authStore.authMethod,
  createdAt: null,
}));
const showChangePassword = ref(false);
const passwordForm = ref({
  current: "",
  new: "",
  confirm: "",
});

const twoFactorStatus = ref<{
  isEnabled: boolean;
  method: string | null;
} | null>(null);
const twoFactorLoading = ref(false);

const showSetup2FA = ref(false);
const showDisable2FA = ref(false);
const setupData = ref<{ secret: string; qrCodeDataUrl: string } | null>(null);
const setupCode = ref("");
const disablePassword = ref("");
const disableCode = ref("");
const twoFactorActionLoading = ref(false);

function formatDate(date: any) {
  if (!date) {
    return "-";
  }
  return new Date(date).toLocaleDateString();
}

function handleChangePassword() {
  if (passwordForm.value.new !== passwordForm.value.confirm) {
    showErrorMessage(t("profile.passwordsDoNotMatch"));
    return;
  }
  showInfoMessage("Password change functionality coming soon");
  showChangePassword.value = false;
}

async function load2FAStatus() {
  try {
    twoFactorLoading.value = true;
    twoFactorStatus.value = await authService.get2FAStatus();
  } catch {
    twoFactorStatus.value = null;
  } finally {
    twoFactorLoading.value = false;
  }
}

async function openSetup2FA() {
  try {
    twoFactorActionLoading.value = true;
    setupData.value = await authService.setup2FA();
    setupCode.value = "";
    showSetup2FA.value = true;
  } catch (err: any) {
    showErrorMessage(err?.message || "Failed to initiate 2FA setup");
  } finally {
    twoFactorActionLoading.value = false;
  }
}

async function confirmSetup2FA() {
  if (!setupCode.value) {
    return;
  }
  try {
    twoFactorActionLoading.value = true;
    await authService.enable2FA(setupCode.value);
    showSetup2FA.value = false;
    setupData.value = null;
    setupCode.value = "";
    showSuccessMessage(t("profile.twoFactor.setupSuccess"));
    await load2FAStatus();
  } catch (err: any) {
    showErrorMessage(err?.message || "Failed to enable 2FA");
  } finally {
    twoFactorActionLoading.value = false;
  }
}

async function confirmDisable2FA() {
  if (!disableCode.value) {
    return;
  }
  try {
    twoFactorActionLoading.value = true;
    await authService.disable2FA(disablePassword.value, disableCode.value);
    showDisable2FA.value = false;
    disablePassword.value = "";
    disableCode.value = "";
    showSuccessMessage(t("profile.twoFactor.disableSuccess"));
    await load2FAStatus();
  } catch (err: any) {
    showErrorMessage(err?.message || "Failed to disable 2FA");
  } finally {
    twoFactorActionLoading.value = false;
  }
}

onMounted(() => {
  load2FAStatus();
});
</script>

<template>
  <div class="profile-page">
    <el-card>
      <template #header>
        <h2>{{ $t("common.profile") }}</h2>
      </template>

      <div class="profile-content">
        <el-descriptions :column="1" border>
          <el-descriptions-item :label="$t('profile.name')">
            {{ currentUser?.name || "-" }}
          </el-descriptions-item>
          <el-descriptions-item :label="$t('auth.email')">
            {{ currentUser?.email || "-" }}
          </el-descriptions-item>
          <el-descriptions-item :label="$t('profile.loginMethod')">
            {{ currentUser?.authMethod || "-" }}
          </el-descriptions-item>
          <el-descriptions-item :label="$t('profile.createdAt')">
            {{ formatDate(currentUser?.createdAt) }}
          </el-descriptions-item>
        </el-descriptions>

        <div class="actions">
          <el-button type="primary" @click="showChangePassword = true">
            {{ $t("profile.changePassword") }}
          </el-button>
        </div>
      </div>
    </el-card>

    <el-card class="two-factor-card">
      <template #header>
        <h2>{{ $t("profile.twoFactor.title") }}</h2>
      </template>

      <div class="two-factor-content">
        <div v-if="twoFactorLoading" class="loading-text">
          {{ $t("profile.twoFactor.loading") }}
        </div>
        <template v-else-if="twoFactorStatus !== null">
          <el-descriptions :column="1" border>
            <el-descriptions-item :label="$t('profile.twoFactor.status')">
              <el-tag :type="twoFactorStatus.isEnabled ? 'success' : 'info'">
                {{
                  twoFactorStatus.isEnabled
                    ? $t("profile.twoFactor.enabled")
                    : $t("profile.twoFactor.disabled")
                }}
              </el-tag>
            </el-descriptions-item>
            <el-descriptions-item
              v-if="twoFactorStatus.isEnabled"
              :label="$t('profile.twoFactor.method')"
            >
              {{ $t("profile.twoFactor.methodTotp") }}
            </el-descriptions-item>
          </el-descriptions>

          <div class="two-factor-actions">
            <el-button
              v-if="!twoFactorStatus.isEnabled"
              type="primary"
              :loading="twoFactorActionLoading"
              @click="openSetup2FA"
            >
              {{ $t("profile.twoFactor.setup") }}
            </el-button>
            <el-button
              v-else
              type="danger"
              :loading="twoFactorActionLoading"
              @click="showDisable2FA = true"
            >
              {{ $t("profile.twoFactor.disable") }}
            </el-button>
          </div>
        </template>
      </div>
    </el-card>

    <el-dialog
      v-model="showChangePassword"
      :title="$t('profile.changePassword')"
      width="400px"
    >
      <el-form :model="passwordForm" label-position="top">
        <el-form-item :label="$t('profile.currentPassword')">
          <el-input v-model="passwordForm.current" type="password" />
        </el-form-item>
        <el-form-item :label="$t('profile.newPassword')">
          <el-input v-model="passwordForm.new" type="password" />
        </el-form-item>
        <el-form-item :label="$t('profile.confirmPassword')">
          <el-input v-model="passwordForm.confirm" type="password" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showChangePassword = false">
          {{ $t("common.cancel") }}
        </el-button>
        <el-button type="primary" @click="handleChangePassword">
          {{ $t("common.save") }}
        </el-button>
      </template>
    </el-dialog>

    <el-dialog
      v-model="showSetup2FA"
      :title="$t('profile.twoFactor.setupTitle')"
      width="480px"
    >
      <div v-if="setupData" class="setup-2fa-content">
        <p>{{ $t("profile.twoFactor.scanQr") }}</p>
        <div class="qr-wrapper">
          <img :src="setupData.qrCodeDataUrl" alt="QR Code" class="qr-image" />
        </div>
        <p class="secret-label">{{ $t("profile.twoFactor.secretKey") }}</p>
        <el-input
          :model-value="setupData.secret"
          readonly
          class="secret-input"
        />
        <p class="confirm-label">{{ $t("profile.twoFactor.confirmCode") }}</p>
        <el-input
          v-model="setupCode"
          :placeholder="$t('profile.twoFactor.confirmPlaceholder')"
          maxlength="6"
          size="large"
        />
      </div>
      <template #footer>
        <el-button @click="showSetup2FA = false">{{
          $t("common.cancel")
        }}</el-button>
        <el-button
          type="primary"
          :loading="twoFactorActionLoading"
          :disabled="setupCode.length !== 6"
          @click="confirmSetup2FA"
        >
          {{ $t("profile.twoFactor.confirmSetup") }}
        </el-button>
      </template>
    </el-dialog>

    <el-dialog
      v-model="showDisable2FA"
      :title="$t('profile.twoFactor.disableTitle')"
      width="400px"
    >
      <el-form label-position="top">
        <el-form-item :label="$t('profile.currentPassword')">
          <el-input v-model="disablePassword" type="password" />
        </el-form-item>
        <el-form-item :label="$t('profile.twoFactor.disableConfirm')">
          <el-input
            v-model="disableCode"
            :placeholder="$t('profile.twoFactor.confirmPlaceholder')"
            maxlength="6"
          />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showDisable2FA = false">{{
          $t("common.cancel")
        }}</el-button>
        <el-button
          type="danger"
          :loading="twoFactorActionLoading"
          :disabled="disableCode.length !== 6"
          @click="confirmDisable2FA"
        >
          {{ $t("profile.twoFactor.disable") }}
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<style lang="less">
.profile-page {
  max-width: 800px;
  margin: 0 auto;
  display: flex;
  flex-direction: column;
  gap: 24px;

  .profile-content {
    padding: 20px 0;
  }

  .actions {
    margin-top: 30px;
    display: flex;
    gap: 10px;
  }

  .two-factor-card {
    .two-factor-content {
      padding: 8px 0;

      .loading-text {
        color: var(--el-text-color-secondary);
        font-size: 0.9rem;
      }

      .two-factor-actions {
        margin-top: 20px;
      }
    }
  }

  .setup-2fa-content {
    p {
      margin: 0 0 12px;
      font-size: 0.95rem;
      line-height: 1.5;
    }

    .secret-label,
    .confirm-label {
      margin-top: 16px;
    }

    .qr-wrapper {
      display: flex;
      justify-content: center;
      margin: 16px 0;

      .qr-image {
        border: 1px solid var(--el-border-color);
        border-radius: 8px;
        padding: 8px;
        background: #fff;
        max-width: 220px;
      }
    }

    .secret-input {
      font-family: monospace;
      letter-spacing: 0.1em;
    }
  }
}
</style>
