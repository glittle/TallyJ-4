<script setup lang="ts">
import { ref, reactive, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { ElMessage, type FormInstance, type FormRules } from 'element-plus';
import { usePeopleStore } from '../../stores/peopleStore';
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

const formRef = ref<FormInstance>();
const submitting = ref(false);

const form = reactive({
  firstName: '',
  lastName: '',
  email: '',
  phone: '',
  area: '',
  bahaiId: '',
  ageGroup: 'Adult',
  canVote: true,
  canReceiveVotes: true
});

const rules = reactive<FormRules>({
  lastName: [
    { required: true, message: t('people.lastNameRequired'), trigger: 'blur' }
  ],
  email: [
    { type: 'email', message: t('people.emailInvalid'), trigger: 'blur' }
  ]
});

watch(() => props.person, (person) => {
  if (person) {
    form.firstName = person.firstName || '';
    form.lastName = person.lastName;
    form.email = person.email || '';
    form.phone = person.phone || '';
    form.area = person.area || '';
    form.bahaiId = person.bahaiId || '';
    form.ageGroup = person.ageGroup || 'Adult';
    form.canVote = person.canVote ?? true;
    form.canReceiveVotes = person.canReceiveVotes ?? true;
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
            canVote: form.canVote,
            canReceiveVotes: form.canReceiveVotes
          };
          await peopleStore.updatePerson(props.person.personGuid, dto);
          ElMessage.success(t('people.updateSuccess'));
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
            canVote: form.canVote,
            canReceiveVotes: form.canReceiveVotes
          };
          await peopleStore.createPerson(dto);
          ElMessage.success(t('people.createSuccess'));
        }
        emit('success');
      } catch (error: any) {
        ElMessage.error(error.message || t('people.saveError'));
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
          <el-option label="Adult" value="Adult" />
          <el-option label="Youth" value="Youth" />
        </el-select>
      </el-form-item>

      <el-form-item :label="$t('people.canVote')">
        <el-switch v-model="form.canVote" />
      </el-form-item>

      <el-form-item :label="$t('people.canReceiveVotes')">
        <el-switch v-model="form.canReceiveVotes" />
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
