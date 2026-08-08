<script setup lang="ts">
import type { GoogleCredentialResponse } from "../../types/google-one-tap";
import { useGoogleOneTap } from "../../composables/useGoogleOneTap";
import { useLocalStorage } from "@/composables/useLocalStorage";
import { useVoterAuthSocialProviders } from "@/composables/useVoterAuthSocialProviders";
import { ArrowLeft, Lock } from "@element-plus/icons-vue";
import { ElButton, ElCard, ElIcon } from "element-plus";
import { nextTick, onBeforeUnmount, onMounted, ref, watch } from "vue";
import { useI18n } from "vue-i18n";
import { useRoute, useRouter } from "vue-router";
import VoterAuthFaq from "@/components/voting/VoterAuthFaq.vue";
import VoterAuthRequestTabs from "@/components/voting/VoterAuthRequestTabs.vue";
import VoterAuthVerifyStep from "@/components/voting/VoterAuthVerifyStep.vue";
import { useApiErrorHandler } from "../../composables/useApiErrorHandler";
import { useNotifications } from "../../composables/useNotifications";
import { useOnlineVotingStore } from "../../stores/onlineVotingStore";

const router = useRouter();
const route = useRoute();
const { t } = useI18n();
const onlineVotingStore = useOnlineVotingStore();
const { showSuccessMessage, showErrorMessage } = useNotifications();
const { handleApiError } = useApiErrorHandler();

const activeTab = useLocalStorage("voterLoginTab", "google");
const step = ref<"request" | "verify">("request");

const googleButtonContainer = ref<HTMLElement>();
const requestTabsRef = ref<{ googleButtonEl?: HTMLElement } | null>(null);

function syncGoogleButtonRef() {
  const el = requestTabsRef.value?.googleButtonEl;
  if (el) {
    googleButtonContainer.value = el;
  }
}

const emailForm = ref({ email: "" });
const phoneForm = ref({
  phone: "",
  deliveryMethod: "sms" as "sms" | "voice" | "whatsapp",
});
const codeForm = ref({ code: "" });
const verificationForm = ref({ voterId: "", verifyCode: "" });
const loading = ref(false);

const emailRules = {
  email: [
    {
      required: true,
      validator: (_rule: any, value: any, callback: any) => {
        if (!value) {
          callback(new Error(t("auth.emailRequired")));
        } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value)) {
          callback(new Error(t("auth.emailInvalid")));
        } else {
          callback();
        }
      },
      trigger: ["blur", "change"],
    },
  ],
};

const phoneRules = {
  phone: [
    {
      required: true,
      validator: (_rule: any, value: any, callback: any) => {
        if (!value) {
          callback(new Error(t("voting.auth.phone.label") + " is required"));
        } else {
          callback();
        }
      },
      trigger: ["blur", "change"],
    },
  ],
};

const codeRules = {
  code: [
    {
      required: true,
      validator: (_rule: any, value: any, callback: any) => {
        if (!value) {
          callback(new Error(t("voting.auth.code.label") + " is required"));
        } else {
          callback();
        }
      },
      trigger: ["blur", "change"],
    },
  ],
};

const handleKeydown = (event: KeyboardEvent) => {
  if (event.key === "Escape") {
    router.push("/");
  }
};

async function handleRequestEmailCode() {
  try {
    loading.value = true;
    const messageKey = await onlineVotingStore.requestVerificationCode({
      voterId: emailForm.value.email,
      voterIdType: "E",
      deliveryMethod: "email",
    });
    verificationForm.value.voterId = emailForm.value.email;
    step.value = "verify";
    showSuccessMessage(t(messageKey));
  } catch (error) {
    handleApiError(error, t("voting.auth.email.sendFailed"));
  } finally {
    loading.value = false;
  }
}

async function handleRequestPhoneCode() {
  try {
    loading.value = true;
    const messageKey = await onlineVotingStore.requestVerificationCode({
      voterId: phoneForm.value.phone,
      voterIdType: "P",
      deliveryMethod: phoneForm.value.deliveryMethod,
    });
    verificationForm.value.voterId = phoneForm.value.phone;
    step.value = "verify";
    showSuccessMessage(t(messageKey));
  } catch (error) {
    handleApiError(error, t("voting.auth.phone.sendFailed"));
  } finally {
    loading.value = false;
  }
}

async function handleDirectCodeLogin() {
  try {
    loading.value = true;
    await onlineVotingStore.verifyCode({
      voterId: codeForm.value.code,
      verifyCode: codeForm.value.code,
    });
    await redirectAfterAuth();
  } catch (error) {
    handleApiError(error, t("voting.auth.code.failed"));
  } finally {
    loading.value = false;
  }
}

async function handleVerifyCode() {
  try {
    loading.value = true;
    await onlineVotingStore.verifyCode(verificationForm.value);
    await redirectAfterAuth();
  } catch (error) {
    handleApiError(error, t("voting.auth.verify.failed"));
  } finally {
    loading.value = false;
  }
}

function backToRequest() {
  step.value = "request";
  verificationForm.value.verifyCode = "";
}

const {
  fbReady,
  fbError,
  kakaoReady,
  kakaoError,
  telegramReady,
  telegramError,
  telegramBotUsername,
  initFacebookSdk,
  handleFacebookLogin,
  initKakaoSdk,
  handleKakaoLogin,
  handleTelegramLogin,
  redirectAfterAuth,
} = useVoterAuthSocialProviders({
  onlineVotingStore,
  router,
  route,
  t,
  showErrorMessage,
  setLoading: (v) => {
    loading.value = v;
  },
});

const handleGoogleCredentialCallback = async (
  response: GoogleCredentialResponse,
) => {
  try {
    loading.value = true;
    await onlineVotingStore.googleAuth({ credential: response.credential });
    await redirectAfterAuth();
  } catch (error) {
    handleApiError(error, t("voting.auth.google.error"));
  } finally {
    loading.value = false;
  }
};

const { googleReady, googleError, initGoogleOneTap } = useGoogleOneTap({
  buttonRef: googleButtonContainer,
  onCredential: handleGoogleCredentialCallback,
  promptOnInit: true,
});

watch(activeTab, async (newTab) => {
  if (newTab === "google") {
    await nextTick();
    syncGoogleButtonRef();
    await initGoogleOneTap();
  } else if (newTab === "facebook") {
    await nextTick();
    await initFacebookSdk();
  } else if (newTab === "kakao") {
    await nextTick();
    await initKakaoSdk();
  }
});

onMounted(() => {
  if (activeTab.value === "google") {
    nextTick(() => {
      syncGoogleButtonRef();
      void initGoogleOneTap();
    });
  } else if (activeTab.value === "facebook") {
    void initFacebookSdk();
  } else if (activeTab.value === "kakao") {
    void initKakaoSdk();
  }
  globalThis.addEventListener("keydown", handleKeydown);
});

onBeforeUnmount(() => {
  globalThis.removeEventListener("keydown", handleKeydown);
});
</script>
<template>
  <div class="voter-auth-page">
    <div class="auth-container">
      <ElButton link class="back-nav" @click="router.push('/')">
        <ElIcon><ArrowLeft /></ElIcon>
        {{ $t("common.back") }}
      </ElButton>
      <div class="welcome-section">
        <div class="welcome-icon">
          <ElIcon :size="56" color="var(--color-public-text)">
            <Lock />
          </ElIcon>
        </div>
        <h1>{{ $t("voting.auth.welcome.heading") }}</h1>
        <p>{{ $t("voting.auth.welcome.subheading") }}</p>
      </div>

      <ElCard class="auth-card" shadow="always">
        <VoterAuthRequestTabs
          v-if="step === 'request'"
          ref="requestTabsRef"
          v-model:active-tab="activeTab"
          v-model:email-form="emailForm"
          v-model:phone-form="phoneForm"
          v-model:code-form="codeForm"
          :loading="loading"
          :google-ready="googleReady"
          :google-error="googleError"
          :fb-ready="fbReady"
          :fb-error="fbError"
          :kakao-ready="kakaoReady"
          :kakao-error="kakaoError"
          :telegram-ready="telegramReady"
          :telegram-error="telegramError"
          :telegram-bot-username="telegramBotUsername"
          :email-rules="emailRules"
          :phone-rules="phoneRules"
          :code-rules="codeRules"
          @request-email="handleRequestEmailCode"
          @request-phone="handleRequestPhoneCode"
          @request-code="handleDirectCodeLogin"
          @facebook="handleFacebookLogin"
          @kakao="handleKakaoLogin"
          @telegram="handleTelegramLogin"
        />
        <VoterAuthVerifyStep
          v-else-if="step === 'verify'"
          v-model:verification-form="verificationForm"
          :loading="loading"
          @verify="handleVerifyCode"
          @back="backToRequest"
        />
      </ElCard>

      <VoterAuthFaq />
    </div>
  </div>
</template>

<style lang="less">
.voter-auth-page {
  min-height: calc(100vh - 100px);
  display: flex;
  justify-content: center;
  padding: 20px 20px 60px;

  .auth-container {
    width: 100%;
    max-width: 780px;
  }

  .back-nav {
    margin-bottom: 8px;
    font-size: 1rem;
    padding: 0;
  }

  .phone-form {
    flex-direction: column;
    align-items: flex-start;
    gap: 0;

    .el-form-item__content {
      align-self: center;
    }
  }

  .welcome-section {
    text-align: center;
    padding: 20px 0 32px;
    color: var(--color-public-text);

    .welcome-icon {
      margin-bottom: 16px;
      display: flex;
      justify-content: center;
      align-items: center;
      width: 80px;
      height: 80px;
      border-radius: 50%;
      background: var(--color-public-header-bg);
      backdrop-filter: blur(8px);
      margin-left: auto;
      margin-right: auto;
    }

    h1 {
      font-size: 2rem;
      margin: 0 0 16px;
      font-weight: 700;
      text-shadow: 0 1px 3px rgba(0, 0, 0, 0.2);
    }

    .welcome-intro {
      font-size: 1.05rem;
      line-height: 1.7;
      margin: 0 0 12px;
      opacity: 0.95;
    }

    .welcome-detail {
      font-size: 0.95rem;
      line-height: 1.6;
      margin: 0 0 20px;
      opacity: 0.85;
    }

    .welcome-choose {
      font-size: 1rem;
      font-weight: 600;
      margin: 0;
      opacity: 0.9;
    }
  }

  .auth-card {
    border-radius: 12px;
    border: none;
    box-shadow: 0 8px 32px rgba(0, 0, 0, 0.18);

    .auth-tabs {
      .el-tabs__header {
        margin-bottom: 0;
      }

      .el-tabs__nav-wrap {
        margin-bottom: 0;
      }

      .tab-label {
        display: flex;
        align-items: center;
        gap: 6px;
        font-size: 0.95rem;
        padding: 2px 4px;
      }
    }

    .method-section {
      padding: 24px 8px 8px;

      .method-description {
        font-size: 0.95rem;
        line-height: 1.65;
        color: var(--el-text-color-regular);
        margin: 0 0 20px;
        padding: 14px 16px;
        background: var(--el-fill-color-light);
        border-radius: 8px;
        // border-left: 3px solid var(--el-color-primary);
      }

      .delivery-options {
        display: flex;
        gap: 24px;
      }
    }

    .google-section {
      .google-button-wrapper {
        display: flex;
        flex-direction: column;
        align-items: center;
        padding: 16px 0;

        .google-loading {
          color: var(--el-text-color-secondary);
          font-size: 0.9rem;
          margin-bottom: 12px;
        }

        .google-button-container {
          min-height: 44px;
          display: flex;
          justify-content: center;
        }
      }
    }

    .whatsapp-note {
      font-size: 0.88rem;
      color: #25d366;
      margin: 0;
      font-style: italic;
    }

    .sso-button-wrapper {
      display: flex;
      flex-direction: column;
      align-items: center;
      padding: 16px 0;

      .sso-loading {
        color: var(--el-text-color-secondary);
        font-size: 0.9rem;
        margin-bottom: 12px;
      }
    }

    .facebook-section {
      .facebook-login-btn {
        background-color: #1877f2;
        border-color: #1877f2;
        color: #ffffff;
        display: flex;
        align-items: center;
        justify-content: center;
        min-width: 260px;
        font-weight: 600;

        &:hover {
          background-color: #166fe5;
          border-color: #166fe5;
        }
      }
    }

    .kakao-section {
      .kakao-login-btn {
        background-color: #fee500;
        border-color: #fee500;
        color: #3c1e1e;
        min-width: 260px;
        font-weight: 600;

        &:hover {
          background-color: #fada0f;
          border-color: #fada0f;
        }
      }
    }

    .telegram-section {
      .telegram-login-container {
        width: 100%;
        display: flex;
        justify-content: center;
      }
    }

    .verify-section {
      padding: 16px 8px;

      .verify-header {
        text-align: center;
        margin-bottom: 28px;

        .el-icon {
          margin-bottom: 12px;
        }

        h3 {
          font-size: 1.1rem;
          color: var(--el-text-color-primary);
          margin: 0 0 10px;
          word-break: break-all;
        }

        p {
          font-size: 0.9rem;
          color: var(--el-text-color-secondary);
          margin: 0;
          line-height: 1.5;
        }
      }
    }

    .full-width-btn {
      width: 100%;
    }
  }

  .faq-section {
    margin-top: 40px;

    .faq-header {
      display: flex;
      align-items: center;
      gap: 10px;
      margin-bottom: 16px;

      h2 {
        color: var(--color-public-text);
        font-size: 1.3rem;
        margin: 0;
        font-weight: 600;
      }
    }

    .faq-collapse {
      border-radius: 10px;
      overflow: hidden;
      background: var(--el-bg-color);
      border: none;
      box-shadow: var(--el-box-shadow-light);

      .el-collapse-item__header {
        font-size: 0.95rem;
        font-weight: 600;
        color: var(--el-text-color-primary);
        padding: 0 20px;
        height: auto;
        min-height: 52px;
        line-height: 1.4;
        white-space: normal;
        padding-top: 14px;
        padding-bottom: 14px;
        align-items: flex-start;
      }

      .el-collapse-item__content {
        font-size: 0.9rem;
        line-height: 1.7;
        color: var(--el-text-color-regular);
        padding: 0 20px 18px;
      }
    }
  }
}
</style>
