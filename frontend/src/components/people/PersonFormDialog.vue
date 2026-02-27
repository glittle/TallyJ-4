<script setup lang="ts">
import { ref, reactive, watch, onMounted, computed } from 'vue';
import { useI18n } from 'vue-i18n';
import { type FormInstance, type FormRules } from 'element-plus';
import { useNotifications } from '@/composables/useNotifications';
import { useApiErrorHandler } from '@/composables/useApiErrorHandler';
import { usePeopleStore } from '../../stores/peopleStore';
import { useEligibilityStore } from '../../stores/eligibilityStore';
import { peopleService } from '../../services/peopleService';
import type { PersonListDto, PersonDetailDto, CreatePersonDto, UpdatePersonDto } from '../../types';

const props = defineProps<{
  modelValue: boolean;
  electionGuid: string;
  person?: PersonListDto | null;
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
const { handleApiError } = useApiErrorHandler();

const formRef = ref<FormInstance>();
const submitting = ref(false);
const loadingDetails = ref(false);
const personDetails = ref<PersonDetailDto | null>(null);

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

const registrationHistory = computed(() => {
  if (!personDetails.value?.registrationHistory) return [];
  try {
    return JSON.parse(personDetails.value.registrationHistory);
  } catch {
    return [];
  }
});

onMounted(async () => {
  await eligibilityStore.fetchReasons();
});

function resetForm() {
  form.firstName = '';
  form.lastName = '';
  form.email = '';
  form.phone = '';
  form.area = '';
  form.bahaiId = '';
  form.ageGroup = 'A';
  form.ineligibleReasonGuid = null;
}

watch(() => props.modelValue, async (visible) => {
  if (visible && props.isEdit && props.person) {
    await loadPersonDetails();
  } else if (visible && !props.isEdit) {
    resetForm();
  }
});

watch(() => props.person, (person) => {
  if (person && !props.isEdit) {
    // For create, use reset form
    resetForm();
  }
});

async function loadPersonDetails() {
  if (!props.person) return;
  
  loadingDetails.value = true;
  try {
    personDetails.value = await peopleService.getDetails(props.person.personGuid);
    
    if (personDetails.value) {
      form.firstName = personDetails.value.firstName || '';
      form.lastName = personDetails.value.lastName;
      form.email = personDetails.value.email || '';
      form.phone = personDetails.value.phone || '';
      form.area = personDetails.value.area || '';
      form.bahaiId = personDetails.value.bahaiId || '';
      form.ageGroup = personDetails.value.ageGroup || 'A';
      form.ineligibleReasonGuid = personDetails.value.ineligibleReasonGuid || null;
    }
  } catch (error) {
    handleApiError(error);
  } finally {
    loadingDetails.value = false;
  }
}

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
  personDetails.value = null;
  emit('update:modelValue', false);
}
</script>

<template>
  <el-dialog
    :model-value="modelValue"
    :title="isEdit ? $t('people.editPerson') : $t('people.addPerson')"
    width="700px"
    @update:model-value="$emit('update:modelValue', $event)"
    @close="handleClose"
  >
    <div v-loading="loadingDetails">
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

      <div v-if="isEdit && personDetails" class="history-section">
        <el-divider />
        
        <div v-if="personDetails.voteHistory && personDetails.voteHistory.length > 0" class="vote-history">
          <h4>{{ $t('people.voteHistory') }}</h4>
          <el-table :data="personDetails.voteHistory" size="small" max-height="200">
            <el-table-column prop="ballotNumber" :label="$t('ballot.number')" width="80" />
            <el-table-column prop="positionOnBallot" :label="$t('ballot.position')" width="80" />
            <el-table-column prop="personName" :label="$t('people.votedFor')" />
            <el-table-column prop="statusCode" :label="$t('ballot.status')" width="100" />
          </el-table>
        </div>
        
        <div v-if="personDetails.voteCount > 0" class="vote-count">
          <p><strong>{{ $t('people.votesReceived') }}:</strong> {{ personDetails.voteCount }}</p>
        </div>
        
        <div v-if="registrationHistory.length > 0" class="registration-history">
          <h4>{{ $t('people.registrationHistory') }}</h4>
          <el-timeline>
            <el-timeline-item
              v-for="(entry, index) in registrationHistory"
              :key="index"
              :timestamp="entry.timestamp"
            >
              {{ entry.action }}
            </el-timeline-item>
          </el-timeline>
        </div>
      </div>
    </div>

    <template #footer>
      <el-button @click="handleClose">{{ $t('common.cancel') }}</el-button>
      <el-button type="primary" @click="handleSubmit" :loading="submitting">
        {{ isEdit ? $t('common.save') : $t('common.create') }}
      </el-button>
    </template>
  </el-dialog>
</template>

<style lang="less">
.history-section {
  .history-section {
    margin-top: var(--spacing-4);
    
    h4 {
      margin: var(--spacing-3) 0 var(--spacing-2);
      font-size: var(--font-size-base);
      font-weight: var(--font-weight-semibold);
    }
    
    .vote-history,
    .registration-history,
    .vote-count {
      margin-bottom: var(--spacing-4);
    }
    
    .vote-count {
      padding: var(--spacing-2);
      background-color: var(--color-neutral-50);
      border-radius: var(--border-radius-sm);
    }
  }
}
</style>
