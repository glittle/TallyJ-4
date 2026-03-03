<script setup lang="ts">
import { ref, onMounted, onBeforeUnmount, watch } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import { useI18n } from 'vue-i18n';
import { ElCard, ElTabs, ElTabPane, ElForm, ElFormItem, ElInput, ElButton, ElRadioGroup, ElRadio } from 'element-plus';
import { useOnlineVotingStore } from '../../stores/onlineVotingStore';
import { useNotifications } from '../../composables/useNotifications';

const router = useRouter();
const route = useRoute();
const { t } = useI18n();
const onlineVotingStore = useOnlineVotingStore();
const { showSuccessMessage, showErrorMessage } = useNotifications();

const activeTab = ref('email');
const step = ref<'request' | 'verify'>('request');

// Google One Tap variables
const googleReady = ref(false);
const gisScriptLoaded = ref(false);
let gisCleanup: (() => void) | null = null;

const emailForm = ref({
  email: ''
});

const phoneForm = ref({
  phone: '',
  deliveryMethod: 'sms' as 'sms' | 'voice'
});

const codeForm = ref({
  code: ''
});

const verificationForm = ref({
  voterId: '',
  verifyCode: ''
});

const loading = ref(false);

async function handleRequestEmailCode() {
  try {
    loading.value = true;
    await onlineVotingStore.requestVerificationCode({
      voterId: emailForm.value.email,
      voterIdType: 'E',
      deliveryMethod: 'email'
    });
    verificationForm.value.voterId = emailForm.value.email;
    step.value = 'verify';
    showSuccessMessage(t('voting.auth.email.codeSent'));
  } catch (error) {
    console.error('Error requesting email code:', error);
  } finally {
    loading.value = false;
  }
}

async function handleRequestPhoneCode() {
  try {
    loading.value = true;
    await onlineVotingStore.requestVerificationCode({
      voterId: phoneForm.value.phone,
      voterIdType: 'P',
      deliveryMethod: phoneForm.value.deliveryMethod
    });
    verificationForm.value.voterId = phoneForm.value.phone;
    step.value = 'verify';
    showSuccessMessage(t('voting.auth.phone.codeSent', { method: phoneForm.value.deliveryMethod }));
  } catch (error) {
    console.error('Error requesting phone code:', error);
  } finally {
    loading.value = false;
  }
}

async function handleDirectCodeLogin() {
  try {
    loading.value = true;
    await onlineVotingStore.verifyCode({
      voterId: codeForm.value.code,
      verifyCode: codeForm.value.code
    });
    const electionGuid = route.query.election as string;
    if (electionGuid) {
      router.push(`/vote/${electionGuid}`);
    } else {
      showErrorMessage(t('voting.auth.error.noElection'));
    }
  } catch (error) {
    console.error('Error with direct code:', error);
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
      showErrorMessage(t('voting.auth.error.noElection'));
    }
  } catch (error) {
    console.error('Error verifying code:', error);
  } finally {
    loading.value = false;
  }
}

function backToRequest() {
  step.value = 'request';
  verificationForm.value.verifyCode = '';
}

// Google One Tap functions
const handleGoogleOneTapCallback = async (response: GoogleCredentialResponse) => {
  try {
    loading.value = true;
    await onlineVotingStore.googleAuth({ credential: response.credential });
    const electionGuid = route.query.election as string;
    if (electionGuid) {
      router.push(`/vote/${electionGuid}`);
    } else {
      showErrorMessage(t('voting.auth.error.noElection'));
    }
  } catch (error) {
    console.error('Error with Google authentication:', error);
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

    script.onerror = () => {
      reject(new Error("Failed to load Google Identity Services script"));
    };

    document.head.appendChild(script);
  });
};

const fetchGoogleClientId = async (): Promise<string | null> => {
  try {
    const apiUrl = import.meta.env.VITE_API_URL || "http://localhost:5016";
    const resp = await fetch(`${apiUrl}/api/public/auth-config`);
    if (!resp.ok) return null;
    const json = await resp.json();
    return json?.data?.googleClientId || null;
  } catch {
    return null;
  }
};

const initGoogleOneTap = async () => {
  try {
    // Lazy load the GIS script only when needed
    await loadGisScript();
    const clientId = await fetchGoogleClientId();

    if (!clientId || typeof google === "undefined") {
      googleReady.value = false;
      return;
    }

    google.accounts.id.initialize({
      client_id: clientId,
      callback: handleGoogleOneTapCallback,
      auto_select: false,
      cancel_on_tap_outside: true,
      use_fedcm_for_prompt: true,
    });

    googleReady.value = true;
    google.accounts.id.prompt();

    // Store cleanup function
    gisCleanup = () => {
      if (typeof google !== "undefined" && googleReady.value) {
        try {
          google.accounts.id.cancel();
        } catch {
          // Ignore errors from cancel
        }
      }
      googleReady.value = false;
    };
  } catch (error) {
    console.error("Failed to initialize Google One Tap:", error);
    googleReady.value = false;
  }
};

const teardownGoogleOneTap = () => {
  if (gisCleanup) {
    gisCleanup();
    gisCleanup = null;
  }
};

// Watch for tab changes to initialize Google One Tap
watch(activeTab, (newTab, oldTab) => {
  if (oldTab === 'google') {
    teardownGoogleOneTap();
  }
  if (newTab === 'google') {
    initGoogleOneTap();
  }
});

// Lifecycle hooks
onMounted(() => {
  if (activeTab.value === 'google') {
    initGoogleOneTap();
  }
});

onBeforeUnmount(() => {
  teardownGoogleOneTap();
});
</script>

<template>
  <div class="voter-auth-page">
    <div class="auth-container">
      <ElCard class="auth-card">
        <template #header>
          <div class="card-header">
            <h2>{{ $t('voting.auth.title') }}</h2>
            <p>{{ $t('voting.auth.subtitle') }}</p>
          </div>
        </template>

        <div v-if="step === 'request'">
          <ElTabs v-model="activeTab">
            <ElTabPane :label="$t('voting.auth.tabs.email')" name="email">
              <ElForm :model="emailForm" @submit.prevent="handleRequestEmailCode">
                <ElFormItem :label="$t('voting.auth.email.label')">
                  <ElInput v-model="emailForm.email" type="email" :placeholder="$t('voting.auth.email.placeholder')" size="large"
                    required />
                </ElFormItem>
                <ElFormItem>
                  <ElButton type="primary" native-type="submit" :loading="loading" size="large" style="width: 100%">
                    {{ $t('voting.auth.email.sendCode') }}
                  </ElButton>
                </ElFormItem>
              </ElForm>
            </ElTabPane>

            <ElTabPane :label="$t('voting.auth.tabs.phone')" name="phone">
              <ElForm :model="phoneForm" @submit.prevent="handleRequestPhoneCode">
                <ElFormItem :label="$t('voting.auth.phone.label')">
                  <ElInput v-model="phoneForm.phone" type="tel" :placeholder="$t('voting.auth.phone.placeholder')" size="large" required />
                </ElFormItem>
                <ElFormItem :label="$t('voting.auth.phone.deliveryMethod')">
                  <ElRadioGroup v-model="phoneForm.deliveryMethod">
                    <ElRadio value="sms">{{ $t('voting.auth.phone.sms') }}</ElRadio>
                    <ElRadio value="voice">{{ $t('voting.auth.phone.voice') }}</ElRadio>
                  </ElRadioGroup>
                </ElFormItem>
                <ElFormItem>
                  <ElButton type="primary" native-type="submit" :loading="loading" size="large" style="width: 100%">
                    {{ $t('voting.auth.phone.sendCode') }}
                  </ElButton>
                </ElFormItem>
              </ElForm>
            </ElTabPane>

            <ElTabPane :label="$t('voting.auth.tabs.code')" name="code">
              <ElForm :model="codeForm" @submit.prevent="handleDirectCodeLogin">
                <ElFormItem :label="$t('voting.auth.code.label')">
                  <ElInput v-model="codeForm.code" :placeholder="$t('voting.auth.code.placeholder')" size="large"
                    required />
                </ElFormItem>
                <ElFormItem>
                  <ElButton type="primary" native-type="submit" :loading="loading" size="large" style="width: 100%">
                    {{ $t('voting.auth.code.proceed') }}
                  </ElButton>
                </ElFormItem>
              </ElForm>
            </ElTabPane>

            <ElTabPane :label="$t('voting.auth.tabs.google')" name="google">
              <div class="google-auth-section">
                <p class="google-description">
                  {{ $t('voting.auth.google.description') }}
                </p>
                <div v-if="!googleReady" class="google-loading">
                  <p>{{ $t('voting.auth.google.loading') }}</p>
                </div>
                <div v-else class="google-prompt">
                  <p>{{ $t('voting.auth.google.prompt') }}</p>
                </div>
              </div>
            </ElTabPane>
          </ElTabs>
        </div>

        <div v-else-if="step === 'verify'">
          <ElForm :model="verificationForm" @submit.prevent="handleVerifyCode">
            <p class="verify-message">
              {{ $t('voting.auth.verify.message', { voterId: verificationForm.voterId }) }}
            </p>
            <ElFormItem :label="$t('voting.auth.verify.label')">
              <ElInput v-model="verificationForm.verifyCode" :placeholder="$t('voting.auth.verify.placeholder')" size="large"
                required />
            </ElFormItem>
            <ElFormItem>
              <ElButton type="primary" native-type="submit" :loading="loading" size="large"
                style="width: 100%; margin-bottom: 10px;">
                {{ $t('voting.auth.verify.submit') }}
              </ElButton>
              <ElButton @click="backToRequest" size="large" style="width: 100%">
                {{ $t('voting.auth.verify.back') }}
              </ElButton>
            </ElFormItem>
          </ElForm>
        </div>
      </ElCard>
    </div>
  </div>
</template>

<style lang="less" scoped>
.voter-auth-page {
  min-height: calc(100vh - 100px);
  display: flex;
  justify-content: center;
  align-items: center;
  padding: 20px;
}

.auth-container {
  width: 100%;
  max-width: 500px;
}

.auth-card {
  .card-header {
    text-align: center;

    h2 {
      margin: 0 0 10px 0;
      color: var(--el-color-primary);
    }

    p {
      margin: 0;
      color: var(--el-text-color-secondary);
    }
  }
}

.verify-message {
  text-align: center;
  margin-bottom: 20px;
  color: var(--el-text-color-regular);
}

.google-auth-section {
  text-align: center;

  .google-description {
    margin-bottom: 20px;
    color: var(--el-text-color-regular);
    font-size: 14px;
  }

  .google-loading,
  .google-prompt {
    p {
      margin: 0;
      color: var(--el-text-color-secondary);
      font-size: 13px;
    }
  }
}
</style>
