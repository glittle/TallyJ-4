<script setup lang="ts">
import { Message } from "@element-plus/icons-vue";
import { ElButton, ElForm, ElFormItem, ElIcon, ElInput } from "element-plus";

const verificationForm = defineModel<{
  voterId: string;
  verifyCode: string;
}>("verificationForm", { required: true });

defineProps<{
  loading: boolean;
}>();

const emit = defineEmits<{
  verify: [];
  back: [];
}>();
</script>

<template>
  <div class="voter-auth-verify-step">
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
    <ElForm :model="verificationForm" @submit.prevent="emit('verify')">
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
        <ElButton size="large" class="full-width-btn" @click="emit('back')">
          {{ $t("voting.auth.verify.back") }}
        </ElButton>
      </ElFormItem>
    </ElForm>
  </div>
</template>

<style lang="less">
.voter-auth-verify-step {
  .verify-header {
    text-align: center;
    margin-bottom: 24px;

    h3 {
      margin: 12px 0 8px;
      font-size: 1.1rem;
    }

    p {
      color: var(--el-text-color-secondary);
      margin: 0;
    }
  }

  .full-width-btn {
    width: 100%;
  }
}
</style>
