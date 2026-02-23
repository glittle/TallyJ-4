<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import { type FormInstance, type FormRules } from 'element-plus';
import { useNotifications } from '@/composables/useNotifications';
import { useBallotStore } from '../../stores/ballotStore';
import { usePeopleStore } from '../../stores/peopleStore';
import type { CreateVoteDto, PersonDto } from '../../types';

const props = defineProps<{
  modelValue: boolean;
  ballotGuid: string;
  electionGuid: string;
  nextPosition: number;
}>();

const emit = defineEmits<{
  'update:modelValue': [value: boolean];
  success: [];
}>();

const { t } = useI18n();
const ballotStore = useBallotStore();
const peopleStore = usePeopleStore();
const { showSuccessMessage, showErrorMessage } = useNotifications();

const formRef = ref<FormInstance>();
const submitting = ref(false);
const searching = ref(false);
const candidates = ref<PersonDto[]>([]);

const form = reactive({
  positionOnBallot: props.nextPosition,
  personGuid: ''
});

const rules = reactive<FormRules>({
  positionOnBallot: [
    { required: true, message: t('ballots.positionRequired'), trigger: 'blur' }
  ],
  personGuid: [
    { required: true, message: t('ballots.candidateRequired'), trigger: 'change' }
  ]
});

onMounted(async () => {
  try {
    await peopleStore.fetchPeople(props.electionGuid);
    candidates.value = peopleStore.candidates;
  } catch (error) {
    showErrorMessage(t('people.loadError'));
  }
});

async function searchPeople(query: string) {
  if (!query) {
    candidates.value = peopleStore.candidates;
    return;
  }
  
  searching.value = true;
  try {
    const results = await peopleStore.searchPeople(props.electionGuid, query);
    candidates.value = results.filter(p => p.canReceiveVotes);
  } catch (error) {
    showErrorMessage(t('people.searchError'));
  } finally {
    searching.value = false;
  }
}

async function handleSubmit() {
  if (!formRef.value) return;
  
  await formRef.value.validate(async (valid) => {
    if (valid) {
      submitting.value = true;
      try {
        const dto: CreateVoteDto = {
          ballotGuid: props.ballotGuid,
          positionOnBallot: form.positionOnBallot,
          personGuid: form.personGuid,
          statusCode: 'Ok'
        };
        await ballotStore.createVote(dto);
        showSuccessMessage(t('ballots.voteCreateSuccess'));
        emit('success');
      } catch (error: any) {
        showErrorMessage(error.message || t('ballots.voteSaveError'));
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
    :title="$t('ballots.addVote')"
    width="400px"
    @update:model-value="$emit('update:modelValue', $event)"
    @close="handleClose"
  >
    <el-form
      ref="formRef"
      :model="form"
      :rules="rules"
      label-width="100px"
      label-position="left"
    >
      <el-form-item :label="$t('ballots.position')" prop="positionOnBallot">
        <el-input-number v-model="form.positionOnBallot" :min="1" :max="50" />
      </el-form-item>

      <el-form-item :label="$t('ballots.candidate')" prop="personGuid">
        <el-select
          v-model="form.personGuid"
          filterable
          remote
          :remote-method="searchPeople"
          :loading="searching"
          style="width: 100%"
        >
          <el-option
            v-for="person in candidates"
            :key="person.personGuid"
            :label="person.fullName"
            :value="person.personGuid"
          />
        </el-select>
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
