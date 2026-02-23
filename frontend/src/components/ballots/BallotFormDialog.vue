<script setup lang="ts">
import { ref, reactive } from 'vue';
import { useI18n } from 'vue-i18n';
import { type FormInstance, type FormRules } from 'element-plus';
import { useBallotStore } from '../../stores/ballotStore';
import type { CreateBallotDto } from '../../types';

import { useNotifications } from '../../composables/useNotifications';
const { showSuccessMessage } = useNotifications();

import { useApiErrorHandler } from '@/composables/useApiErrorHandler';
const { handleApiError } = useApiErrorHandler();

const props = defineProps<{
  modelValue: boolean;
  electionGuid: string;
}>();

const emit = defineEmits<{
  'update:modelValue': [value: boolean];
  success: [];
}>();

const { t } = useI18n();
const ballotStore = useBallotStore();

const formRef = ref<FormInstance>();
const submitting = ref(false);

const form = reactive({
  locationGuid: '',
  computerCode: 'A',
  teller1: '',
  teller2: ''
});

const locations = ref([
  { value: '00000000-0000-0000-0000-000000000001', label: 'Main Hall' },
  { value: '00000000-0000-0000-0000-000000000002', label: 'Room A' },
  { value: '00000000-0000-0000-0000-000000000003', label: 'Room B' }
]);

const rules = reactive<FormRules>({
  locationGuid: [
    { required: true, message: t('ballots.locationRequired'), trigger: 'change' }
  ],
  computerCode: [
    { required: true, message: t('ballots.computerRequired'), trigger: 'blur' },
    { pattern: /^[A-Z]{1,2}$/, message: 'Computer code must be 1-2 uppercase letters', trigger: 'blur' }
  ]
});

async function handleSubmit() {
  if (!formRef.value) {
    return;
  }

  await formRef.value.validate(async (valid) => {
    if (valid) {
      submitting.value = true;
      try {
        const dto: CreateBallotDto = {
          electionGuid: props.electionGuid,
          locationGuid: form.locationGuid,
          computerCode: form.computerCode,
          teller1: form.teller1 || undefined,
          teller2: form.teller2 || undefined,
          statusCode: 'Ok'
        };
        await ballotStore.createBallot(dto);
        showSuccessMessage(t('ballots.createSuccess'));
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
  <el-dialog :model-value="modelValue" :title="$t('ballots.addBallot')" width="500px"
    @update:model-value="$emit('update:modelValue', $event)" @close="handleClose">
    <el-form ref="formRef" :model="form" :rules="rules" label-width="120px" label-position="left">
      <el-form-item :label="$t('ballots.location')" prop="locationGuid">
        <el-select v-model="form.locationGuid" style="width: 100%">
          <el-option v-for="location in locations" :key="location.value" :label="location.label"
            :value="location.value" />
        </el-select>
      </el-form-item>

      <el-form-item :label="$t('ballots.computer')" prop="computerCode">
        <el-input v-model="form.computerCode" />
      </el-form-item>

      <el-form-item :label="$t('ballots.teller1')" prop="teller1">
        <el-input v-model="form.teller1" />
      </el-form-item>

      <el-form-item :label="$t('ballots.teller2')" prop="teller2">
        <el-input v-model="form.teller2" />
      </el-form-item>
    </el-form>

    <template #footer>
      <el-button @click="handleClose">{{ $t('common.cancel') }}</el-button>
      <el-button type="primary" @click="handleSubmit" :loading="submitting">
        {{ $t('common.create') }}
      </el-button>
    </template>
  </el-dialog>
</template>
