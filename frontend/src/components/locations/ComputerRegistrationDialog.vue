<script setup lang="ts">
import { ref, reactive, watch } from 'vue';
import { ElMessage, type FormInstance, type FormRules } from 'element-plus';
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

const locationStore = useLocationStore();

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
      message: 'Computer code must be exactly 2 characters', 
      trigger: 'blur' 
    },
    { 
      pattern: /^[A-Z0-9]{2}$/, 
      message: 'Computer code must be 2 uppercase letters or numbers', 
      trigger: 'blur' 
    }
  ],
  browserInfo: [
    { max: 250, message: 'Browser info cannot exceed 250 characters', trigger: 'blur' }
  ],
  ipAddress: [
    { max: 50, message: 'IP address cannot exceed 50 characters', trigger: 'blur' }
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
  if (!formRef.value) return;
  
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
        ElMessage.success('Computer registered successfully');
        emit('success');
      } catch (error: any) {
        ElMessage.error(error.message || 'Failed to register computer');
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
  <el-dialog
    :model-value="modelValue"
    title="Register Computer"
    width="600px"
    @update:model-value="$emit('update:modelValue', $event)"
    @close="handleClose"
  >
    <el-form
      ref="formRef"
      :model="form"
      :rules="rules"
      label-width="150px"
      label-position="left"
    >
      <el-form-item label="Computer Code" prop="computerCode">
        <el-input 
          v-model="form.computerCode" 
          placeholder="AA (leave empty to auto-generate)"
          maxlength="2"
          style="text-transform: uppercase"
        />
        <div class="form-help-text">2-character code (e.g., AA, AB, A1). Leave empty to auto-generate.</div>
      </el-form-item>

      <el-form-item label="Browser Info" prop="browserInfo">
        <el-input 
          v-model="form.browserInfo" 
          type="textarea" 
          :rows="3"
          placeholder="Auto-detected browser information"
          readonly
        />
        <div class="form-help-text">Automatically detected from your browser</div>
      </el-form-item>

      <el-form-item label="IP Address" prop="ipAddress">
        <el-input 
          v-model="form.ipAddress" 
          placeholder="Optional: Enter IP address"
        />
        <div class="form-help-text">Optional: Computer's IP address</div>
      </el-form-item>
    </el-form>

    <template #footer>
      <el-button @click="handleClose">Cancel</el-button>
      <el-button type="primary" @click="handleSubmit" :loading="submitting">
        Register
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
