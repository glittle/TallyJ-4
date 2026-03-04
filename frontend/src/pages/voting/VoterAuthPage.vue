<script setup lang="ts">
import { ref, onMounted, onBeforeUnmount, watch, nextTick } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import { useI18n } from 'vue-i18n';
import {
  ElCard, ElTabs, ElTabPane, ElForm, ElFormItem, ElInput, ElButton,
  ElRadioGroup, ElRadio, ElCollapse, ElCollapseItem, ElIcon, ElAlert
} from 'element-plus';
import { Message, Phone, Key, ChromeFilled, Lock, QuestionFilled } from '@element-plus/icons-vue';
import { useOnlineVotingStore } from '../../stores/onlineVotingStore';
import { useNotifications } from '../../composables/useNotifications';

const router = useRouter();
const route = useRoute();
const { t } = useI18n();
const onlineVotingStore = useOnlineVotingStore();
const { showSuccessMessage, showErrorMessage } = useNotifications();

const activeTab = ref('email');
const step = ref<'request' | 'verify'>('request');

const googleButtonContainer = ref<HTMLElement | null>(null);
const googleReady = ref(false);
const googleError = ref(false);
const gisScriptLoaded = ref(false);

const emailForm = ref({ email: '' });
const phoneForm = ref({ phone: '', deliveryMethod: 'sms' as 'sms' | 'voice' });
const codeForm = ref({ code: '' });
const verificationForm = ref({ voterId: '', verifyCode: '' });
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

const handleGoogleCredentialCallback = async (response: GoogleCredentialResponse) => {
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
    if (gisScriptLoaded.value || typeof google !== 'undefined') {
      gisScriptLoaded.value = true;
      resolve();
      return;
    }
    const script = document.createElement('script');
    script.src = 'https://accounts.google.com/gsi/client';
    script.async = true;
    script.defer = true;
    script.onload = () => { gisScriptLoaded.value = true; resolve(); };
    script.onerror = () => reject(new Error('Failed to load Google Identity Services script'));
    document.head.appendChild(script);
  });
};

const fetchGoogleClientId = async (): Promise<string | null> => {
  try {
    const apiUrl = import.meta.env.VITE_API_URL || 'http://localhost:5016';
    const resp = await fetch(`${apiUrl}/api/public/auth-config`);
    if (!resp.ok) return null;
    const json = await resp.json();
    return json?.data?.googleClientId || null;
  } catch {
    return null;
  }
};

const initGoogleSignIn = async () => {
  try {
    googleError.value = false;
    await loadGisScript();
    const clientId = await fetchGoogleClientId();
    if (!clientId || typeof google === 'undefined') {
      googleError.value = true;
      return;
    }
    google.accounts.id.initialize({
      client_id: clientId,
      callback: handleGoogleCredentialCallback,
      auto_select: false,
      cancel_on_tap_outside: false,
    });
    await nextTick();
    if (googleButtonContainer.value) {
      google.accounts.id.renderButton(googleButtonContainer.value, {
        theme: 'outline',
        size: 'large',
        text: 'signin_with',
        shape: 'rectangular',
        width: 300,
      });
      googleReady.value = true;
    }
  } catch (error) {
    console.error('Failed to initialize Google Sign-In:', error);
    googleError.value = true;
  }
};

watch(activeTab, async (newTab) => {
  if (newTab === 'google') {
    await nextTick();
    await initGoogleSignIn();
  }
});

onMounted(() => {
  if (activeTab.value === 'google') {
    initGoogleSignIn();
  }
});

onBeforeUnmount(() => {
  if (typeof google !== 'undefined' && googleReady.value) {
    try { google.accounts.id.cancel(); } catch { /* ignore */ }
  }
  googleReady.value = false;
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
        <h1>{{ $t('voting.auth.welcome.heading') }}</h1>
        <p class="welcome-intro">{{ $t('voting.auth.welcome.intro') }}</p>
        <p class="welcome-detail">{{ $t('voting.auth.welcome.detail') }}</p>
        <p class="welcome-choose">{{ $t('voting.auth.welcome.choose') }}</p>
      </div>

      <ElCard class="auth-card" shadow="always">

        <div v-if="step === 'request'">
          <ElTabs v-model="activeTab" class="auth-tabs">

            <ElTabPane name="email">
              <template #label>
                <span class="tab-label">
                  <ElIcon><Message /></ElIcon>
                  <span>{{ $t('voting.auth.tabs.email') }}</span>
                </span>
              </template>
              <div class="method-section">
                <p class="method-description">{{ $t('voting.auth.email.description') }}</p>
                <ElForm :model="emailForm" @submit.prevent="handleRequestEmailCode">
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
                    <ElButton type="primary" native-type="submit" :loading="loading" size="large" class="full-width-btn">
                      {{ $t('voting.auth.email.sendCode') }}
                    </ElButton>
                  </ElFormItem>
                </ElForm>
              </div>
            </ElTabPane>

            <ElTabPane name="phone">
              <template #label>
                <span class="tab-label">
                  <ElIcon><Phone /></ElIcon>
                  <span>{{ $t('voting.auth.tabs.phone') }}</span>
                </span>
              </template>
              <div class="method-section">
                <p class="method-description">{{ $t('voting.auth.phone.description') }}</p>
                <ElForm :model="phoneForm" @submit.prevent="handleRequestPhoneCode">
                  <ElFormItem :label="$t('voting.auth.phone.label')">
                    <ElInput
                      v-model="phoneForm.phone"
                      type="tel"
                      :placeholder="$t('voting.auth.phone.placeholder')"
                      size="large"
                      required
                    />
                  </ElFormItem>
                  <ElFormItem :label="$t('voting.auth.phone.deliveryMethod')">
                    <ElRadioGroup v-model="phoneForm.deliveryMethod" class="delivery-options">
                      <ElRadio value="sms">{{ $t('voting.auth.phone.sms') }}</ElRadio>
                      <ElRadio value="voice">{{ $t('voting.auth.phone.voice') }}</ElRadio>
                    </ElRadioGroup>
                  </ElFormItem>
                  <ElFormItem>
                    <ElButton type="primary" native-type="submit" :loading="loading" size="large" class="full-width-btn">
                      {{ $t('voting.auth.phone.sendCode') }}
                    </ElButton>
                  </ElFormItem>
                </ElForm>
              </div>
            </ElTabPane>

            <ElTabPane name="google">
              <template #label>
                <span class="tab-label">
                  <ElIcon><ChromeFilled /></ElIcon>
                  <span>{{ $t('voting.auth.tabs.google') }}</span>
                </span>
              </template>
              <div class="method-section google-section">
                <p class="method-description">{{ $t('voting.auth.google.description') }}</p>
                <p class="method-description">{{ $t('voting.auth.google.prompt') }}</p>
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
                    <span>{{ $t('voting.auth.google.loading') }}</span>
                  </div>
                  <div ref="googleButtonContainer" class="google-button-container"></div>
                </div>
              </div>
            </ElTabPane>

            <ElTabPane name="code">
              <template #label>
                <span class="tab-label">
                  <ElIcon><Key /></ElIcon>
                  <span>{{ $t('voting.auth.tabs.code') }}</span>
                </span>
              </template>
              <div class="method-section">
                <p class="method-description">{{ $t('voting.auth.code.description') }}</p>
                <ElForm :model="codeForm" @submit.prevent="handleDirectCodeLogin">
                  <ElFormItem :label="$t('voting.auth.code.label')">
                    <ElInput
                      v-model="codeForm.code"
                      :placeholder="$t('voting.auth.code.placeholder')"
                      size="large"
                      required
                    />
                  </ElFormItem>
                  <ElFormItem>
                    <ElButton type="primary" native-type="submit" :loading="loading" size="large" class="full-width-btn">
                      {{ $t('voting.auth.code.proceed') }}
                    </ElButton>
                  </ElFormItem>
                </ElForm>
              </div>
            </ElTabPane>

          </ElTabs>
        </div>

        <div v-else-if="step === 'verify'" class="verify-section">
          <div class="verify-header">
            <ElIcon :size="40" color="#409EFF"><Message /></ElIcon>
            <h3>{{ $t('voting.auth.verify.message', { voterId: verificationForm.voterId }) }}</h3>
            <p>{{ $t('voting.auth.verify.detail') }}</p>
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
              <ElButton type="primary" native-type="submit" :loading="loading" size="large" class="full-width-btn">
                {{ $t('voting.auth.verify.submit') }}
              </ElButton>
            </ElFormItem>
            <ElFormItem>
              <ElButton @click="backToRequest" size="large" class="full-width-btn">
                {{ $t('voting.auth.verify.back') }}
              </ElButton>
            </ElFormItem>
          </ElForm>
        </div>

      </ElCard>

      <div class="faq-section">
        <div class="faq-header">
          <ElIcon :size="24" color="rgba(255,255,255,0.85)"><QuestionFilled /></ElIcon>
          <h2>{{ $t('voting.auth.faq.title') }}</h2>
        </div>
        <ElCollapse class="faq-collapse">
          <ElCollapseItem :title="$t('voting.auth.faq.q1')" name="1">
            {{ $t('voting.auth.faq.a1') }}
          </ElCollapseItem>
          <ElCollapseItem :title="$t('voting.auth.faq.q2')" name="2">
            {{ $t('voting.auth.faq.a2') }}
          </ElCollapseItem>
          <ElCollapseItem :title="$t('voting.auth.faq.q3')" name="3">
            {{ $t('voting.auth.faq.a3') }}
          </ElCollapseItem>
          <ElCollapseItem :title="$t('voting.auth.faq.q4')" name="4">
            {{ $t('voting.auth.faq.a4') }}
          </ElCollapseItem>
          <ElCollapseItem :title="$t('voting.auth.faq.q5')" name="5">
            {{ $t('voting.auth.faq.a5') }}
          </ElCollapseItem>
          <ElCollapseItem :title="$t('voting.auth.faq.q6')" name="6">
            {{ $t('voting.auth.faq.a6') }}
          </ElCollapseItem>
          <ElCollapseItem :title="$t('voting.auth.faq.q7')" name="7">
            {{ $t('voting.auth.faq.a7') }}
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
        padding: 8px 0;
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
