<script setup lang="ts">
import type { GoogleCredentialResponse } from "../../types/google-one-tap";

declare global {
  interface Window {
    google: any;
  }
}

import {
  ChromeFilled,
  Key,
  Lock,
  Message,
  Phone,
  QuestionFilled,
} from "@element-plus/icons-vue";
import { useStorage } from "@vueuse/core";
import {
  ElAlert,
  ElButton,
  ElCard,
  ElCollapse,
  ElCollapseItem,
  ElForm,
  ElFormItem,
  ElIcon,
  ElInput,
  ElRadio,
  ElRadioGroup,
  ElTabPane,
  ElTabs,
} from "element-plus";
import { nextTick, onBeforeUnmount, onMounted, ref, watch } from "vue";
import { useI18n } from "vue-i18n";
import { useRoute, useRouter } from "vue-router";
import TelegramLoginButton from "../../components/auth/TelegramLoginButton.vue";
import { useNotifications } from "../../composables/useNotifications";
import { useOnlineVotingStore } from "../../stores/onlineVotingStore";

declare const FB: any;
declare const Kakao: any;

const router = useRouter();
const route = useRoute();
const { t } = useI18n();
const onlineVotingStore = useOnlineVotingStore();
const { showSuccessMessage, showErrorMessage } = useNotifications();

const activeTab = useStorage("voterLoginTab", "google");
const step = ref<"request" | "verify">("request");

const googleButtonContainer = ref<HTMLElement | null>(null);
const googleReady = ref(false);
const googleError = ref(false);
const gisScriptLoaded = ref(false);

const fbReady = ref(false);
const fbError = ref(false);
const fbScriptLoaded = ref(false);

const kakaoReady = ref(false);
const kakaoError = ref(false);
const kakaoScriptLoaded = ref(false);

const telegramReady = ref(false);
const telegramError = ref(false);
const telegramBotUsername = ref<string | null>(null);

const authConfig = ref<{
  googleClientId?: string;
  facebookAppId?: string;
  kakaoJsKey?: string;
  telegramBotUsername?: string;
} | null>(null);

const emailForm = ref({ email: "" });
const phoneForm = ref({
  phone: "",
  deliveryMethod: "sms" as "sms" | "voice" | "whatsapp",
});
const codeForm = ref({ code: "" });
const verificationForm = ref({ voterId: "", verifyCode: "" });
const loading = ref(false);

// add handler so if ESC is pressed, we go back to landing page
const handleKeydown = (event: KeyboardEvent) => {
  if (event.key === "Escape") {
    router.push("/");
  }
};

const fetchAuthConfig = async () => {
  if (authConfig.value) {
    return authConfig.value;

    return authConfig.value;
  }
  try {
    const apiUrl =
      return null;
    env.VITE_API_URL || "http://localhost:5016";
    const resp = await fetch(`${apiUrl}/api/public/auth-config`);
    if (!resp.ok) {
      return null;
    }
    const json = await resp.json();
    authConfig.value = json?.data ?? null;
    telegramBotUsername.value = authConfig.value?.telegramBotUsername ?? null;
    telegramReady.value = Boolean(authConfig.value?.telegramBotUsername);
    return authConfig.value;
  } catch {
    return null;
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
    console.error("Error requesting email code:", error);
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
    console.error("Error requesting phone code:", error);
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
    const electionGuid = route.query.election as string;
    if (electionGuid) {
      router.push(`/vote/${electionGuid}`);
    } else {
      showErrorMessage(t("voting.auth.error.noElection"));
    }
  } catch (error) {
    console.error("Error with direct code:", error);
  } finally {
    loading.value = false;
  }
}

async function handleVerifyCode() {
  try {
    loading.value = true;
    await onlineVotingStore.verifyCode(verificationForm.value);
    const electionGuid = route.query.election as string;
    if (electionGuid) {
      router.push(`/vote/${electionGuid}`);
    } else {
      showErrorMessage(t("voting.auth.error.noElection"));
    }
  } catch (error) {
    console.error("Error verifying code:", error);
  } finally {
    loading.value = false;
  }
}

function backToRequest() {
  step.value = "request";
  verificationForm.value.verifyCode = "";
}

const handleGoogleCredentialCallback = async (
  response: GoogleCredentialResponse,
) => {
  try {
    loading.value = true;
    await onlineVotingStore.googleAuth({ credential: response.credential });
    const electionGuid = route.query.election as string;
    if (electionGuid) {
      router.push(`/vote/${electionGuid}`);
    } else {
      showErrorMessage(t("voting.auth.error.noElection"));
    }
  } catch (error) {
    console.error("Error with Google authentication:", error);
  } finally {
    loading.value = false;
  }
};

const loadGisScript = (): Promise<void> => {
  return new Promise((resolve, reject) => {
    if (gisScriptLoaded.value || typeof google !== "undefined") {
      gisScriptLoaded.value = true;
      resolve();
      return;
    }
    const script = document.createElement("script");
    script.src = "https://accounts.google.com/gsi/client";
    script.async = true;
    script.defer = true;
    script.onload = () => {
      gisScriptLoaded.value = true;
      resolve();
    };
    script.onerror = () =>
      reject(new Error("Failed to load Google Identity Services script"));
    document.head.appendChild(script);
  });
};

const fetchGoogleClientId = async (): Promise<string | null> => {
  const config = await fetchAuthConfig();
  return config?.googleClientId ?? null;
};

const initGoogleSignIn = async () => {
  try {
    googleError.value = false;
    await loadGisScript();
    const clientId = await fetchGoogleClientId();
    if (!clientId || typeof window.google === "undefined") {
      googleError.value = true;
      return;
    }
    window.google.accounts.id.initialize({
      client_id: clientId,
      callback: handleGoogleCredentialCallback,
      auto_select: false,
      cancel_on_tap_outside: false,
    });
    await nextTick();
    if (googleButtonContainer.value) {
      window.google.accounts.id.renderButton(googleButtonContainer.value, {
        theme: "outline",
        size: "large",
        text: "signin_with",
        shape: "rectangular",
        width: 300,
      });
      googleReady.value = true;
    }
  } catch (error) {
    console.error("Failed to initialize Google Sign-In:", error);
    googleError.value = true;
  }
};

const handleFacebookLogin = async () => {
  try {
    loading.value = true;

    // Check if FB is available
    if (typeof FB === "undefined") {
      console.error("Facebook SDK not loaded");
      showErrorMessage(t("voting.auth.facebook.error"));
      loading.value = false;
      return;
    }

    // Set a timeout to reset loading if FB.login callback is never called
    const timeoutId = setTimeout(() => {
      console.warn("Facebook login timeout - callback never called");
      loading.value = false;
      showErrorMessage(t("voting.auth.facebook.popupBlocked"));
    }, 10000); // 10 seconds timeout

    console.log("Calling FB.login...");
    try {
      FB.login(
        async (res: any) => {
          console.log("FB.login callback called with result:", res);
          clearTimeout(timeoutId); // Clear the timeout since callback was called
          if (res.authResponse?.accessToken) {
            await onlineVotingStore.facebookAuth({
              accessToken: res.authResponse.accessToken,
            });
            const electionGuid = route.query.election as string;
            if (electionGuid) {
              router.push(`/vote/${electionGuid}`);
            } else {
              showErrorMessage(t("voting.auth.error.noElection"));
            }
          } else {
            showErrorMessage(t("voting.auth.facebook.cancelled"));
          }
          loading.value = false;
        },
        { scope: "email" },
      );
    } catch (syncError) {
      console.error("FB.login threw synchronously:", syncError);
      clearTimeout(timeoutId);
      showErrorMessage(t("voting.auth.facebook.error"));
      loading.value = false;
    }
  } catch (error) {
    console.error("Error with Facebook authentication:", error);
    loading.value = false;
  }
};

const loadFacebookSdk = (): Promise<void> => {
  return new Promise((resolve, reject) => {
    if (fbScriptLoaded.value || typeof FB !== "undefined") {
      fbScriptLoaded.value = true;
      resolve();
      return;
    }
    const script = document.createElement("script");
    script.src = "https://connect.facebook.net/en_US/sdk.js";
    script.async = true;
    script.defer = true;
    script.crossOrigin = "anonymous";
    script.onload = () => {
      fbScriptLoaded.value = true;
      resolve();
    };
    script.onerror = () => reject(new Error("Failed to load Facebook SDK"));
    document.head.appendChild(script);
  });
};

const initFacebookSdk = async () => {
  try {
    fbError.value = false;
    console.log("Initializing Facebook SDK...");
    const config = await fetchAuthConfig();
    console.log("Auth config:", config);
    if (!config?.facebookAppId) {
      console.error("No Facebook App ID in config");
      fbError.value = true;
      return;
    }

    // Check if we're on localhost and show a warning
    const isLocalhost =
      window.location.hostname === "localhost" ||
      window.location.hostname === "127.0.0.1";
    if (isLocalhost) {
      console.warn(
        "Facebook login may not work on localhost. Configure your Facebook App with a local domain like local.tallyj.com and update your hosts file, or use ngrok for testing.",
      );
    }

    console.log("Loading Facebook SDK...");
    await loadFacebookSdk();
    console.log("Facebook SDK loaded, initializing...");
    FB.init({
      appId: config.facebookAppId,
      cookie: true,
      xfbml: true,
      version: "v18.0",
    });
    console.log("Facebook SDK initialized successfully");
    fbReady.value = true;
  } catch (error) {
    console.error("Failed to initialize Facebook SDK:", error);
    fbError.value = true;
  }
};

const handleKakaoLogin = async () => {
  try {
    loading.value = true;

    // Set a timeout to reset loading if Kakao.Auth.login callback is never called
    const timeoutId = setTimeout(() => {
      loading.value = false;
      showErrorMessage(t("voting.auth.kakao.popupBlocked"));
    }, 10000); // 10 seconds timeout

    Kakao.Auth.login({
      success: async (authObj: any) => {
        clearTimeout(timeoutId); // Clear the timeout since callback was called
        await onlineVotingStore.kakaoAuth({
          accessToken: authObj.access_token,
        });
        const electionGuid = route.query.election as string;
        if (electionGuid) {
          router.push(`/vote/${electionGuid}`);
        } else {
          showErrorMessage(t("voting.auth.error.noElection"));
        }
        loading.value = false;
      },
      fail: (err: any) => {
        clearTimeout(timeoutId);
        console.error("Kakao login failed:", err);
        loading.value = false;
      },
    });
  } catch (error) {
    console.error("Error with Kakao authentication:", error);
    loading.value = false;
  }
};

const handleTelegramLogin = async (user: any) => {
  try {
    loading.value = true;
    await onlineVotingStore.telegramAuth({
      id: user.id,
      firstName: user.first_name,
      lastName: user.last_name,
      username: user.username,
      photoUrl: user.photo_url,
      authDate: user.auth_date,
      hash: user.hash,
    });
    const electionGuid = route.query.election as string;
    if (electionGuid) {
      router.push(`/vote/${electionGuid}`);
    } else {
      showErrorMessage(t("voting.auth.error.noElection"));
    }
  } catch (error) {
    console.error("Error with Telegram authentication:", error);
  } finally {
    loading.value = false;
  }
};

const loadKakaoSdk = (): Promise<void> => {
  return new Promise((resolve, reject) => {
    if (kakaoScriptLoaded.value || typeof Kakao !== "undefined") {
      kakaoScriptLoaded.value = true;
      resolve();
      return;
    }
    const script = document.createElement("script");
    script.src = "https://t1.kakaocdn.net/kakao_js_sdk/2.7.2/kakao.min.js";
    script.async = true;
    script.onload = () => {
      kakaoScriptLoaded.value = true;
      resolve();
    };
    script.onerror = () => reject(new Error("Failed to load Kakao SDK"));
    document.head.appendChild(script);
  });
};

const initKakaoSdk = async () => {
  try {
    kakaoError.value = false;
    const config = await fetchAuthConfig();
    if (!config?.kakaoJsKey) {
      kakaoError.value = true;
      return;
    }
    await loadKakaoSdk();
    if (!Kakao.isInitialized()) {
      Kakao.init(config.kakaoJsKey);
    }
    kakaoReady.value = true;
  } catch (error) {
    console.error("Failed to initialize Kakao SDK:", error);
    kakaoError.value = true;
  }
};

watch(activeTab, async (newTab) => {
  if (newTab === "google") {
    await nextTick();
    await initGoogleSignIn();
  } else if (newTab === "facebook") {
    await nextTick();
    await initFacebookSdk();
  } else if (newTab === "kakao") {
    await nextTick();
    await initKakaoSdk();
  }
});

onMounted(() => {
  fetchAuthConfig();
  if (activeTab.value === "google") {
    initGoogleSignIn();
  } else if (activeTab.value === "facebook") {
    initFacebookSdk();
  } else if (activeTab.value === "kakao") {
    initKakaoSdk();
  }
  globalThis.addEventListener("keydown", handleKeydown);
});

onBeforeUnmount(() => {
  if (window.google !== undefined && googleReady.value) {
    try {
      window.google.accounts.id.cancel();
    } catch {
      /* ignore */
    }
  }
  googleReady.value = false;
  globalThis.removeEventListener("keydown", handleKeydown);
});
</script>

<template>
  <div class="voter-auth-page">
    <div class="auth-container">
      <div class="welcome-section">
        <div class="welcome-icon">
          <ElIcon :size="56" color="#ffffff">
            <Lock />
          </ElIcon>
        </div>
        <h1>{{ $t("voting.auth.welcome.heading") }}</h1>
        <p class="welcome-intro">{{ $t("voting.auth.welcome.intro") }}</p>
        <p class="welcome-detail">{{ $t("voting.auth.welcome.detail") }}</p>
        <p class="welcome-choose">{{ $t("voting.auth.welcome.choose") }}</p>
      </div>

      <ElCard class="auth-card" shadow="always">
        <div v-if="step === 'request'">
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
                  <div
                    ref="googleButtonContainer"
                    class="google-button-container"
                  ></div>
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
                  @submit.prevent="handleRequestEmailCode"
                >
                  <ElFormItem :label="$t('voting.auth.email.label')">
                    <ElInput
                      v-model="emailForm.email"
                      type="email"
                      :placeholder="$t('voting.auth.email.placeholder')"
                      size="large"
                      required
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
                  @submit.prevent="handleRequestPhoneCode"
                >
                  <ElFormItem :label="$t('voting.auth.phone.label')">
                    <ElInput
                      v-model="phoneForm.phone"
                      type="tel"
                      :placeholder="$t('voting.auth.phone.placeholder')"
                      size="large"
                      required
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
                      <ElRadio value="sms">{{
                        $t("voting.auth.phone.sms")
                      }}</ElRadio>
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
                  @submit.prevent="handleDirectCodeLogin"
                >
                  <ElFormItem :label="$t('voting.auth.code.label')">
                    <ElInput
                      v-model="codeForm.code"
                      :placeholder="$t('voting.auth.code.placeholder')"
                      size="large"
                      required
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
                    @click="handleFacebookLogin"
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
                    @click="handleKakaoLogin"
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
                    @success="handleTelegramLogin"
                  />
                </div>
              </div>
            </ElTabPane>
          </ElTabs>
        </div>

        <div v-else-if="step === 'verify'" class="verify-section">
          <div class="verify-header">
            <ElIcon :size="40" color="#409EFF">
              <Message />
            </ElIcon>
            <h3>
              {{
                $t("voting.auth.verify.message", {
                  voterId: verificationForm.voterId,
                })
              }}
            </h3>
            <p>{{ $t("voting.auth.verify.detail") }}</p>
          </div>
          <ElForm :model="verificationForm" @submit.prevent="handleVerifyCode">
            <ElFormItem :label="$t('voting.auth.verify.label')">
              <ElInput
                v-model="verificationForm.verifyCode"
                :placeholder="$t('voting.auth.verify.placeholder')"
                size="large"
                required
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
                {{ $t("voting.auth.verify.submit") }}
              </ElButton>
            </ElFormItem>
            <ElFormItem>
              <ElButton
                size="large"
                class="full-width-btn"
                @click="backToRequest"
              >
                {{ $t("voting.auth.verify.back") }}
              </ElButton>
            </ElFormItem>
          </ElForm>
        </div>
      </ElCard>

      <div class="faq-section">
        <div class="faq-header">
          <ElIcon :size="24" color="rgba(255,255,255,0.85)">
            <QuestionFilled />
          </ElIcon>
          <h2>{{ $t("voting.auth.faq.title") }}</h2>
        </div>
        <ElCollapse class="faq-collapse">
          <ElCollapseItem :title="$t('voting.auth.faq.q1')" name="1">
            {{ $t("voting.auth.faq.a1") }}
          </ElCollapseItem>
          <ElCollapseItem :title="$t('voting.auth.faq.q2')" name="2">
            {{ $t("voting.auth.faq.a2") }}
          </ElCollapseItem>
          <ElCollapseItem :title="$t('voting.auth.faq.q3')" name="3">
            {{ $t("voting.auth.faq.a3") }}
          </ElCollapseItem>
          <ElCollapseItem :title="$t('voting.auth.faq.q4')" name="4">
            {{ $t("voting.auth.faq.a4") }}
          </ElCollapseItem>
          <ElCollapseItem :title="$t('voting.auth.faq.q5')" name="5">
            {{ $t("voting.auth.faq.a5") }}
          </ElCollapseItem>
          <ElCollapseItem :title="$t('voting.auth.faq.q6')" name="6">
            {{ $t("voting.auth.faq.a6") }}
          </ElCollapseItem>
          <ElCollapseItem :title="$t('voting.auth.faq.q7')" name="7">
            {{ $t("voting.auth.faq.a7") }}
          </ElCollapseItem>
        </ElCollapse>
      </div>
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
    color: #ffffff;

    .welcome-icon {
      margin-bottom: 16px;
      display: flex;
      justify-content: center;
      align-items: center;
      width: 80px;
      height: 80px;
      border-radius: 50%;
      background: rgba(255, 255, 255, 0.15);
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
        border-left: 3px solid var(--el-color-primary);
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
        color: rgba(255, 255, 255, 0.92);
        font-size: 1.3rem;
        margin: 0;
        font-weight: 600;
      }
    }

    .faq-collapse {
      border-radius: 10px;
      overflow: hidden;
      background: rgba(255, 255, 255, 0.97);
      border: none;
      box-shadow: 0 4px 16px rgba(0, 0, 0, 0.12);

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
