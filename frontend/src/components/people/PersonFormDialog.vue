<script setup lang="ts">
import { ref, reactive, watch, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import { type FormInstance, type FormRules } from 'element-plus';
import { useNotifications } from '@/composables/useNotifications';
import { usePeopleStore } from '../../stores/peopleStore';
import { useEligibilityStore } from '../../stores/eligibilityStore';
import type { PersonDto, CreatePersonDto, UpdatePersonDto } from '../../types';

const props = defineProps<{
  modelValue: boolean;
  electionGuid: string;
  person?: PersonDto | null;
  isEdit?: boolean;
}>();

const emit = defineEmits<{
  'update:modelValue': [value: boolean];
  success: [];
}>();

const { t } = useI18n();
const peopleStore = usePeopleStore();
const eligibilityStore = useEligibilityStore();
const { showSuccessMessage, showErrorMessage } = useNotifications();

const formRef = ref<FormInstance>();
const submitting = ref(false);

const form = reactive({
  firstName: '',
  lastName: '',
  email: '',
  phone: '',
  area: '',
  bahaiId: '',
  ageGroup: 'A',
  ineligibleReasonGuid: null as string | null
});

const rules = reactive<FormRules>({
  lastName: [
    { required: true, message: t('people.lastNameRequired'), trigger: 'blur' }
  ],
  email: [
    { type: 'email', message: t('people.emailInvalid'), trigger: 'blur' }
  ]
});

onMounted(async () => {
  await eligibilityStore.fetchReasons();
});

watch(() => props.person, (person) => {
  if (person) {
    form.firstName = person.firstName || '';
    form.lastName = person.lastName;
    form.email = person.email || '';
    form.phone = person.phone || '';
    form.area = person.area || '';
    form.bahaiId = person.bahaiId || '';
    form.ageGroup = person.ageGroup || 'A';
    form.ineligibleReasonGuid = person.ineligibleReasonGuid || null;
  }
}, { immediate: true });

async function handleSubmit() {
  if (!formRef.value) return;
  
  await formRef.value.validate(async (valid) => {
    if (valid) {
      submitting.value = true;
      try {
        if (props.isEdit && props.person) {
          const dto: UpdatePersonDto = {
            firstName: form.firstName || undefined,
            lastName: form.lastName,
            email: form.email || undefined,
            phone: form.phone || undefined,
            area: form.area || undefined,
            bahaiId: form.bahaiId || undefined,
            ageGroup: form.ageGroup || undefined,
            ineligibleReasonGuid: form.ineligibleReasonGuid || undefined
          };
          await peopleStore.updatePerson(props.person.personGuid, dto);
          showSuccessMessage(t('people.updateSuccess'));
        } else {
          const dto: CreatePersonDto = {
            electionGuid: props.electionGuid,
            firstName: form.firstName || undefined,
            lastName: form.lastName,
            email: form.email || undefined,
            phone: form.phone || undefined,
            area: form.area || undefined,
            bahaiId: form.bahaiId || undefined,
            ageGroup: form.ageGroup || undefined,
            ineligibleReasonGuid: form.ineligibleReasonGuid || undefined
          };
          await peopleStore.createPerson(dto);
          showSuccessMessage(t('people.createSuccess'));
        }
        emit('success');
      } catch (error: any) {
        showErrorMessage(error.message || t('people.saveError'));
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
    :title="isEdit ? $t('people.editPerson') : $t('people.addPerson')"
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
      <el-form-item :label="$t('people.firstName')" prop="firstName">
        <el-input v-model="form.firstName" />
      </el-form-item>

      <el-form-item :label="$t('people.lastName')" prop="lastName">
        <el-input v-model="form.lastName" />
      </el-form-item>

      <el-form-item :label="$t('people.email')" prop="email">
        <el-input v-model="form.email" type="email" />
      </el-form-item>

      <el-form-item :label="$t('people.phone')" prop="phone">
        <el-input v-model="form.phone" />
      </el-form-item>

      <el-form-item :label="$t('people.area')" prop="area">
        <el-input v-model="form.area" />
      </el-form-item>

      <el-form-item :label="$t('people.bahaiId')" prop="bahaiId">
        <el-input v-model="form.bahaiId" />
      </el-form-item>

      <el-form-item :label="$t('people.ageGroup')" prop="ageGroup">
        <el-select v-model="form.ageGroup" style="width: 100%">
          <el-option label="Adult" value="A" />
          <el-option label="Youth" value="Y" />
        </el-select>
      </el-form-item>

      <el-form-item :label="$t('eligibility.label')">
        <el-select v-model="form.ineligibleReasonGuid" :placeholder="$t('eligibility.selectReason')" style="width: 100%" clearable>
          <el-option :label="$t('eligibility.eligible')" :value="null" />
          <el-option-group
            v-for="(reasons, group) in eligibilityStore.groupedReasons"
            :key="group"
            :label="$t(`eligibility.group${group}`)"
          >
            <el-option
              v-for="reason in reasons"
              :key="reason.reasonGuid"
              :label="$t(`eligibility.${reason.code}`)"
              :value="reason.reasonGuid"
            />
          </el-option-group>
        </el-select>
      </el-form-item>
    </el-form>

    <template #footer>
      <el-button @click="handleClose">{{ $t('common.cancel') }}</el-button>
      <el-button type="primary" @click="handleSubmit" :loading="submitting">
        {{ isEdit ? $t('common.save') : $t('common.create') }}
      </el-button>
    </template>
  </el-dialog>
</template>
