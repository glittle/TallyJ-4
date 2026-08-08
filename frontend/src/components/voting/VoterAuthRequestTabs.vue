<script setup lang="ts">
import TelegramLoginButton from "@/components/auth/TelegramLoginButton.vue";
import { ChromeFilled, Key, Message, Phone } from "@element-plus/icons-vue";
import {
  ElAlert,
  ElButton,
  ElForm,
  ElFormItem,
  ElIcon,
  ElInput,
  ElRadio,
  ElRadioGroup,
  ElTabPane,
  ElTabs,
} from "element-plus";
import { ref } from "vue";

const activeTab = defineModel<string>("activeTab", { required: true });
const emailForm = defineModel<{ email: string }>("emailForm", {
  required: true,
});
const phoneForm = defineModel<{
  phone: string;
  deliveryMethod: "sms" | "voice" | "whatsapp";
}>("phoneForm", { required: true });
const codeForm = defineModel<{ code: string }>("codeForm", { required: true });

defineProps<{
  loading: boolean;
  googleReady: boolean;
  googleError: boolean;
  fbReady: boolean;
  fbError: boolean;
  kakaoReady: boolean;
  kakaoError: boolean;
  telegramReady: boolean;
  telegramError: boolean;
  telegramBotUsername: string | null;
  emailRules: object;
  phoneRules: object;
  codeRules: object;
}>();

const emit = defineEmits<{
  "request-email": [];
  "request-phone": [];
  "request-code": [];
  facebook: [];
  kakao: [];
  telegram: [user: unknown];
}>();

const googleButtonEl = ref<HTMLElement>();
defineExpose({
  get googleButtonEl() {
    return googleButtonEl.value;
  },
});
</script>

<template>
  <div class="voter-auth-request-tabs">
    <ElTabs v-model="activeTab" class="auth-tabs">
      <ElTabPane name="google">
        <template #label>
          <span class="tab-label">
            <ElIcon>
              <ChromeFilled />
            </ElIcon>
            <span>{{ $t("voting.auth.tabs.google") }}</span>
          </span>
        </template>
        <div class="method-section google-section">
          <p class="method-description">
            {{ $t("voting.auth.google.description") }}
          </p>
          <p class="method-description">
            {{ $t("voting.auth.google.prompt") }}
          </p>
          <div v-if="googleError">
            <ElAlert
              :title="$t('voting.auth.google.error')"
              type="warning"
              :closable="false"
              show-icon
            />
          </div>
          <div v-else class="google-button-wrapper">
            <div v-if="!googleReady" class="google-loading">
              <span>{{ $t("voting.auth.google.loading") }}</span>
            </div>
            <div ref="googleButtonEl" class="google-button-container"></div>
          </div>
        </div>
      </ElTabPane>
      <ElTabPane name="email">
        <template #label>
          <span class="tab-label">
            <ElIcon>
              <Message />
            </ElIcon>
            <span>{{ $t("voting.auth.tabs.email") }}</span>
          </span>
        </template>
        <div class="method-section">
          <p class="method-description">
            {{ $t("voting.auth.email.description") }}
          </p>
          <ElForm
            :model="emailForm"
            :rules="emailRules"
            @submit.prevent="emit('request-email')"
          >
            <ElFormItem :label="$t('voting.auth.email.label')" prop="email">
              <ElInput
                v-model="emailForm.email"
                type="email"
                :placeholder="$t('voting.auth.email.placeholder')"
                size="large"
              />
            </ElFormItem>
            <ElFormItem>
              <ElButton
                type="primary"
                native-type="submit"
                :loading="loading"
                size="large"
                class="full-width-btn"
              >
                {{ $t("voting.auth.email.sendCode") }}
              </ElButton>
            </ElFormItem>
          </ElForm>
        </div>
      </ElTabPane>

      <ElTabPane name="phone">
        <template #label>
          <span class="tab-label">
            <ElIcon>
              <Phone />
            </ElIcon>
            <span>{{ $t("voting.auth.tabs.phone") }}</span>
          </span>
        </template>
        <div class="method-section">
          <p class="method-description">
            {{ $t("voting.auth.phone.description") }}
          </p>
          <ElForm
            :model="phoneForm"
            :rules="phoneRules"
            @submit.prevent="emit('request-phone')"
          >
            <ElFormItem :label="$t('voting.auth.phone.label')" prop="phone">
              <ElInput
                v-model="phoneForm.phone"
                type="tel"
                :placeholder="$t('voting.auth.phone.placeholder')"
                size="large"
              />
            </ElFormItem>
            <ElFormItem
              :label="$t('voting.auth.phone.deliveryMethod')"
              class="phone-form"
            >
              <ElRadioGroup
                v-model="phoneForm.deliveryMethod"
                class="delivery-options"
              >
                <ElRadio value="sms">{{ $t("voting.auth.phone.sms") }}</ElRadio>
                <ElRadio value="voice">{{
                  $t("voting.auth.phone.voice")
                }}</ElRadio>
                <ElRadio value="whatsapp">{{
                  $t("voting.auth.phone.whatsapp")
                }}</ElRadio>
              </ElRadioGroup>
            </ElFormItem>
            <ElFormItem v-if="phoneForm.deliveryMethod === 'whatsapp'">
              <p class="whatsapp-note">
                {{ $t("voting.auth.phone.whatsappNote") }}
              </p>
            </ElFormItem>
            <ElFormItem>
              <ElButton
                type="primary"
                native-type="submit"
                :loading="loading"
                size="large"
                class="full-width-btn"
              >
                {{ $t("voting.auth.phone.sendCode") }}
              </ElButton>
            </ElFormItem>
          </ElForm>
        </div>
      </ElTabPane>

      <ElTabPane name="code">
        <template #label>
          <span class="tab-label">
            <ElIcon>
              <Key />
            </ElIcon>
            <span>{{ $t("voting.auth.tabs.code") }}</span>
          </span>
        </template>
        <div class="method-section">
          <p class="method-description">
            {{ $t("voting.auth.code.description") }}
          </p>
          <ElForm
            :model="codeForm"
            :rules="codeRules"
            @submit.prevent="emit('request-code')"
          >
            <ElFormItem :label="$t('voting.auth.code.label')" prop="code">
              <ElInput
                v-model="codeForm.code"
                :placeholder="$t('voting.auth.code.placeholder')"
                size="large"
              />
            </ElFormItem>
            <ElFormItem>
              <ElButton
                type="primary"
                native-type="submit"
                :loading="loading"
                size="large"
                class="full-width-btn"
              >
                {{ $t("voting.auth.code.proceed") }}
              </ElButton>
            </ElFormItem>
          </ElForm>
        </div>
      </ElTabPane>

      <ElTabPane name="facebook">
        <template #label>
          <span class="tab-label">
            <svg
              class="facebook-icon"
              xmlns="http://www.w3.org/2000/svg"
              viewBox="0 0 24 24"
              width="16"
              height="16"
            >
              <path
                fill="#1877F2"
                d="M24 12.073C24 5.405 18.627 0 12 0S0 5.405 0 12.073C0 18.1 4.388 23.094 10.125 24v-8.437H7.078v-3.49h3.047V9.41c0-3.025 1.792-4.697 4.533-4.697 1.312 0 2.686.236 2.686.236v2.97h-1.513c-1.49 0-1.956.93-1.956 1.874v2.25h3.328l-.532 3.49h-2.796V24C19.612 23.094 24 18.1 24 12.073z"
              />
            </svg>
            <span>{{ $t("voting.auth.tabs.facebook") }}</span>
          </span>
        </template>
        <div class="method-section facebook-section">
          <p class="method-description">
            {{ $t("voting.auth.facebook.description") }}
          </p>
          <div v-if="fbError">
            <ElAlert
              :title="$t('voting.auth.facebook.error')"
              type="warning"
              :closable="false"
              show-icon
            />
          </div>
          <div v-else class="sso-button-wrapper">
            <div v-if="!fbReady" class="sso-loading">
              <span>{{ $t("voting.auth.facebook.loading") }}</span>
            </div>
            <ElButton
              v-else
              class="facebook-login-btn"
              size="large"
              :loading="loading"
              @click="emit('facebook')"
            >
              <svg
                xmlns="http://www.w3.org/2000/svg"
                viewBox="0 0 24 24"
                width="20"
                height="20"
                style="margin-right: 8px"
              >
                <path
                  fill="#ffffff"
                  d="M24 12.073C24 5.405 18.627 0 12 0S0 5.405 0 12.073C0 18.1 4.388 23.094 10.125 24v-8.437H7.078v-3.49h3.047V9.41c0-3.025 1.792-4.697 4.533-4.697 1.312 0 2.686.236 2.686.236v2.97h-1.513c-1.49 0-1.956.93-1.956 1.874v2.25h3.328l-.532 3.49h-2.796V24C19.612 23.094 24 18.1 24 12.073z"
                />
              </svg>
              {{ $t("voting.auth.facebook.button") }}
            </ElButton>
          </div>
        </div>
      </ElTabPane>

      <ElTabPane name="kakao">
        <template #label>
          <span class="tab-label">
            <svg
              class="kakao-icon"
              xmlns="http://www.w3.org/2000/svg"
              viewBox="0 0 24 24"
              width="16"
              height="16"
            >
              <path
                fill="#3C1E1E"
                d="M12 3C6.477 3 2 6.582 2 11c0 2.785 1.682 5.226 4.236 6.73l-.931 3.47a.352.352 0 0 0 .538.378L9.927 18.9A12.3 12.3 0 0 0 12 19c5.523 0 10-3.582 10-8S17.523 3 12 3z"
              />
            </svg>
            <span>{{ $t("voting.auth.tabs.kakao") }}</span>
          </span>
        </template>
        <div class="method-section kakao-section">
          <p class="method-description">
            {{ $t("voting.auth.kakao.description") }}
          </p>
          <div v-if="kakaoError">
            <ElAlert
              :title="$t('voting.auth.kakao.error')"
              type="warning"
              :closable="false"
              show-icon
            />
          </div>
          <div v-else class="sso-button-wrapper">
            <div v-if="!kakaoReady" class="sso-loading">
              <span>{{ $t("voting.auth.kakao.loading") }}</span>
            </div>
            <ElButton
              v-else
              class="kakao-login-btn"
              size="large"
              :loading="loading"
              @click="emit('kakao')"
            >
              {{ $t("voting.auth.kakao.button") }}
            </ElButton>
          </div>
        </div>
      </ElTabPane>

      <ElTabPane v-if="telegramBotUsername" name="telegram">
        <template #label>
          <span class="tab-label">
            <ElIcon>
              <Message />
            </ElIcon>
            <span>{{ $t("voting.auth.tabs.telegram") }}</span>
          </span>
        </template>
        <div class="method-section telegram-section">
          <p class="method-description">
            {{ $t("voting.auth.telegram.description") }}
          </p>
          <div v-if="telegramError || !telegramReady">
            <ElAlert
              :title="$t('voting.auth.telegram.error')"
              type="warning"
              :closable="false"
              show-icon
            />
          </div>
          <div v-else class="sso-button-wrapper">
            <div v-if="loading" class="sso-loading">
              <span>{{ $t("voting.auth.telegram.loading") }}</span>
            </div>
            <TelegramLoginButton
              v-else
              :bot-username="telegramBotUsername!"
              @success="(u) => emit('telegram', u)"
            />
          </div>
        </div>
      </ElTabPane>
    </ElTabs>
  </div>
</template>

<style lang="less">
.voter-auth-request-tabs {
  .tab-label {
    display: inline-flex;
    align-items: center;
    gap: 6px;
  }
  .method-section {
    padding: 8px 0 16px;
  }
  .full-width-btn {
    width: 100%;
  }
  .google-button-host {
    min-height: 44px;
    display: flex;
    justify-content: center;
  }
}
</style>
