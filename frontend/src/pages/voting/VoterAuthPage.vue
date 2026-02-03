<script setup lang="ts">
import { ref } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import { ElCard, ElTabs, ElTabPane, ElForm, ElFormItem, ElInput, ElButton, ElRadioGroup, ElRadio } from 'element-plus';
import { useOnlineVotingStore } from '../../stores/onlineVotingStore';
import { useNotifications } from '../../composables/useNotifications';

const router = useRouter();
const route = useRoute();
const onlineVotingStore = useOnlineVotingStore();
const { showSuccess, showError } = useNotifications();

const activeTab = ref('email');
const step = ref<'request' | 'verify'>('request');

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
    showSuccess('Verification code sent to your email');
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
    showSuccess(`Verification code sent via ${phoneForm.value.deliveryMethod}`);
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
      showError('No election specified');
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
      showError('No election specified');
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
                  <ElInput
                    v-model="emailForm.email"
                    type="email"
                    placeholder="Enter your email address"
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
                    style="width: 100%"
                  >
                    Send Verification Code
                  </ElButton>
                </ElFormItem>
              </ElForm>
            </ElTabPane>

            <ElTabPane label="Phone" name="phone">
              <ElForm :model="phoneForm" @submit.prevent="handleRequestPhoneCode">
                <ElFormItem label="Phone Number">
                  <ElInput
                    v-model="phoneForm.phone"
                    type="tel"
                    placeholder="+1234567890"
                    size="large"
                    required
                  />
                </ElFormItem>
                <ElFormItem label="Delivery Method">
                  <ElRadioGroup v-model="phoneForm.deliveryMethod">
                    <ElRadio value="sms">SMS Text</ElRadio>
                    <ElRadio value="voice">Voice Call</ElRadio>
                  </ElRadioGroup>
                </ElFormItem>
                <ElFormItem>
                  <ElButton 
                    type="primary" 
                    native-type="submit" 
                    :loading="loading"
                    size="large"
                    style="width: 100%"
                  >
                    Send Verification Code
                  </ElButton>
                </ElFormItem>
              </ElForm>
            </ElTabPane>

            <ElTabPane label="Code" name="code">
              <ElForm :model="codeForm" @submit.prevent="handleDirectCodeLogin">
                <ElFormItem label="Voting Code">
                  <ElInput
                    v-model="codeForm.code"
                    placeholder="Enter your pre-provided voting code"
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
                    style="width: 100%"
                  >
                    Proceed to Vote
                  </ElButton>
                </ElFormItem>
              </ElForm>
            </ElTabPane>
          </ElTabs>
        </div>

        <div v-else-if="step === 'verify'">
          <ElForm :model="verificationForm" @submit.prevent="handleVerifyCode">
            <p class="verify-message">
              We've sent a verification code to <strong>{{ verificationForm.voterId }}</strong>
            </p>
            <ElFormItem label="Verification Code">
              <ElInput
                v-model="verificationForm.verifyCode"
                placeholder="Enter the code you received"
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
                style="width: 100%; margin-bottom: 10px;"
              >
                Verify and Continue
              </ElButton>
              <ElButton 
                @click="backToRequest"
                size="large"
                style="width: 100%"
              >
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
</style>
