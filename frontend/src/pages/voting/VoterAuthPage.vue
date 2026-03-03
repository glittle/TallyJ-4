<script setup lang="ts">
import { ref, onMounted, onBeforeUnmount, watch } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import { ElCard, ElTabs, ElTabPane, ElForm, ElFormItem, ElInput, ElButton, ElRadioGroup, ElRadio } from 'element-plus';
import { useOnlineVotingStore } from '../../stores/onlineVotingStore';
import { useNotifications } from '../../composables/useNotifications';

const router = useRouter();
const route = useRoute();
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
    showSuccessMessage('Verification code sent to your email');
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
    showSuccessMessage(`Verification code sent via ${phoneForm.value.deliveryMethod}`);
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
      showErrorMessage('No election specified');
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
      showErrorMessage('No election specified');
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
      showErrorMessage('No election specified');
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
            <h2>TallyJ Online Voting</h2>
            <p>Please authenticate to cast your ballot</p>
          </div>
        </template>

        <div v-if="step === 'request'">
          <ElTabs v-model="activeTab">
            <ElTabPane label="Email" name="email">
              <ElForm :model="emailForm" @submit.prevent="handleRequestEmailCode">
                <ElFormItem label="Email Address">
                  <ElInput v-model="emailForm.email" type="email" placeholder="Enter your email address" size="large"
                    required />
                </ElFormItem>
                <ElFormItem>
                  <ElButton type="primary" native-type="submit" :loading="loading" size="large" style="width: 100%">
                    Send Verification Code
                  </ElButton>
                </ElFormItem>
              </ElForm>
            </ElTabPane>

            <ElTabPane label="Phone" name="phone">
              <ElForm :model="phoneForm" @submit.prevent="handleRequestPhoneCode">
                <ElFormItem label="Phone Number">
                  <ElInput v-model="phoneForm.phone" type="tel" placeholder="+1234567890" size="large" required />
                </ElFormItem>
                <ElFormItem label="Delivery Method">
                  <ElRadioGroup v-model="phoneForm.deliveryMethod">
                    <ElRadio value="sms">SMS Text</ElRadio>
                    <ElRadio value="voice">Voice Call</ElRadio>
                  </ElRadioGroup>
                </ElFormItem>
                <ElFormItem>
                  <ElButton type="primary" native-type="submit" :loading="loading" size="large" style="width: 100%">
                    Send Verification Code
                  </ElButton>
                </ElFormItem>
              </ElForm>
            </ElTabPane>

            <ElTabPane label="Code" name="code">
              <ElForm :model="codeForm" @submit.prevent="handleDirectCodeLogin">
                <ElFormItem label="Voting Code">
                  <ElInput v-model="codeForm.code" placeholder="Enter your pre-provided voting code" size="large"
                    required />
                </ElFormItem>
                <ElFormItem>
                  <ElButton type="primary" native-type="submit" :loading="loading" size="large" style="width: 100%">
                    Proceed to Vote
                  </ElButton>
                </ElFormItem>
              </ElForm>
            </ElTabPane>

            <ElTabPane label="Google" name="google">
              <div class="google-auth-section">
                <p class="google-description">
                  Sign in with your Google account to vote. You must be registered in an open election to proceed.
                </p>
                <div v-if="!googleReady" class="google-loading">
                  <p>Loading Google authentication...</p>
                </div>
                <div v-else class="google-prompt">
                  <p>Google One Tap will appear automatically. If it doesn't, check your browser settings.</p>
                </div>
              </div>
            </ElTabPane>
          </ElTabs>
        </div>

        <div v-else-if="step === 'verify'">
          <ElForm :model="verificationForm" @submit.prevent="handleVerifyCode">
            <p class="verify-message">
              We've sent a verification code to <strong>{{ verificationForm.voterId }}</strong>
            </p>
            <ElFormItem label="Verification Code">
              <ElInput v-model="verificationForm.verifyCode" placeholder="Enter the code you received" size="large"
                required />
            </ElFormItem>
            <ElFormItem>
              <ElButton type="primary" native-type="submit" :loading="loading" size="large"
                style="width: 100%; margin-bottom: 10px;">
                Verify and Continue
              </ElButton>
              <ElButton @click="backToRequest" size="large" style="width: 100%">
                Back
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
