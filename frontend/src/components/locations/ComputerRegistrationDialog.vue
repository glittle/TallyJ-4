<script setup lang="ts">
import { ref, reactive, watch } from 'vue';
import { type FormInstance, type FormRules } from 'element-plus';
import { useI18n } from 'vue-i18n';
import { useNotifications } from '@/composables/useNotifications';
import { useApiErrorHandler } from '@/composables/useApiErrorHandler';
import { useLocationStore } from '../../stores/locationStore';
import type { RegisterComputerDto } from '../../types';

const props = defineProps<{
  modelValue: boolean;
  electionGuid: string;
  locationGuid: string;
}>();

const emit = defineEmits<{
  'update:modelValue': [value: boolean];
  success: [];
}>();

const { t } = useI18n();
const locationStore = useLocationStore();
const { showSuccessMessage, showErrorMessage } = useNotifications();
const { handleApiError } = useApiErrorHandler();

const formRef = ref<FormInstance>();
const submitting = ref(false);

const form = reactive({
  computerCode: '',
  browserInfo: '',
  ipAddress: ''
});

const rules = reactive<FormRules>({
  computerCode: [
    {
      len: 2,
      message: t('locations.computer.codeLength'),
      trigger: 'blur'
    },
    {
      pattern: /^[A-Z0-9]{2}$/,
      message: t('locations.computer.codeInvalid'),
      trigger: 'blur'
    }
  ],
  browserInfo: [
    { max: 250, message: t('locations.computer.browserInfoMaxLength'), trigger: 'blur' }
  ],
  ipAddress: [
    { max: 50, message: t('locations.computer.ipAddressMaxLength'), trigger: 'blur' }
  ]
});

watch(() => props.modelValue, (value) => {
  if (!value) {
    resetForm();
  } else {
    detectBrowserInfo();
  }
});

function resetForm() {
  form.computerCode = '';
  form.browserInfo = '';
  form.ipAddress = '';
}

function detectBrowserInfo() {
  form.browserInfo = navigator.userAgent;
}

async function handleSubmit() {
  if (!formRef.value) {
    return;
  }

  await formRef.value.validate(async (valid) => {
    if (valid) {
      submitting.value = true;
      try {
        const dto: RegisterComputerDto = {
          electionGuid: props.electionGuid,
          locationGuid: props.locationGuid,
          computerCode: form.computerCode || undefined,
          browserInfo: form.browserInfo || undefined,
          ipAddress: form.ipAddress || undefined
        };
        await locationStore.registerComputer(props.electionGuid, props.locationGuid, dto);
        showSuccessMessage(t('locations.computer.success'));
        emit('success');
      } catch (error) {
        handleApiError(error);
      } finally {
        submitting.value = false;
      }
    }
  });
}

function handleClose() {
  formRef.value?.resetFields();
  emit('update:modelValue', false);
}
</script>

<template>
  <el-dialog :model-value="modelValue" :title="$t('locations.computer.title')" width="600px"
    @update:model-value="$emit('update:modelValue', $event)" @close="handleClose">
    <el-form ref="formRef" :model="form" :rules="rules" label-width="150px" label-position="left">
      <el-form-item :label="$t('locations.computer.code')" prop="computerCode">
        <el-input v-model="form.computerCode" :placeholder="$t('locations.computer.codePlaceholder')" maxlength="2"
          style="text-transform: uppercase" />
        <div class="form-help-text">{{ $t('locations.computer.codeHelp') }}</div>
      </el-form-item>

      <el-form-item :label="$t('locations.computer.browserInfo')" prop="browserInfo">
        <el-input v-model="form.browserInfo" type="textarea" :rows="3" :placeholder="$t('locations.computer.browserInfoPlaceholder')"
          readonly />
        <div class="form-help-text">{{ $t('locations.computer.browserInfoHelp') }}</div>
      </el-form-item>

      <el-form-item :label="$t('locations.computer.ipAddress')" prop="ipAddress">
        <el-input v-model="form.ipAddress" :placeholder="$t('locations.computer.ipAddressPlaceholder')" />
        <div class="form-help-text">{{ $t('locations.computer.ipAddressHelp') }}</div>
      </el-form-item>
    </el-form>

    <template #footer>
      <el-button @click="handleClose">{{ $t('locations.computer.cancel') }}</el-button>
      <el-button type="primary" @click="handleSubmit" :loading="submitting">
        {{ $t('locations.computer.register') }}
      </el-button>
    </template>
  </el-dialog>
</template>

<style scoped>
.form-help-text {
  font-size: 12px;
  color: var(--el-text-color-secondary);
  margin-top: 4px;
}
</style>
