<template>
  <div class="create-election-page">
    <el-card>
      <template #header>
        <div class="card-header">
          <h2>{{ $t("elections.createNew") }}</h2>
        </div>
      </template>

      <el-form
        ref="formRef"
        :model="form"
        :rules="rules"
        label-width="200px"
        label-position="left"
      >
        <ElectionFormTabs
          v-model="form"
          :available-elections="availableElections"
          :form-ref="formRef"
        />

        <el-form-item style="margin-top: 20px">
          <el-button type="primary" @click="submitForm" :loading="submitting">
            {{ $t("common.create") }}
          </el-button>
          <el-button @click="cancel">
            {{ $t("common.cancel") }}
          </el-button>
        </el-form-item>
      </el-form>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { useI18n } from 'vue-i18n';
import { type FormInstance, type FormRules } from 'element-plus';
import { useNotifications } from '@/composables/useNotifications';
import { useElectionStore } from '../../stores/electionStore';
import type { CreateElectionDto, ElectionSummaryDto } from '../../types';
import ElectionFormTabs from '../../components/elections/ElectionFormTabs.vue';
import { extractApiErrorMessage } from '../../utils/errorHandler';

const router = useRouter();
const { t } = useI18n();
const electionStore = useElectionStore();
const { showSuccessMessage, showErrorMessage } = useNotifications();

const formRef = ref<FormInstance>();
const submitting = ref(false);
const availableElections = ref<ElectionSummaryDto[]>([]);

const form = reactive<CreateElectionDto>({
  name: '',
  dateOfElection: undefined,
  electionType: 'LSA',
  numberToElect: 9,
  convenor: '',
  electionMode: 'N',
  numberExtra: 0,
  showFullReport: true,
  listForPublic: false,
  showAsTest: false,
  canVote: 'Y',
  canReceive: 'Y'
});

const rules = reactive<FormRules>({
  name: [
    { required: true, message: t('elections.form.nameRequired'), trigger: 'blur' }
  ],
  electionType: [
    { required: true, message: t('elections.form.typeRequired'), trigger: 'change' }
  ],
  numberToElect: [
    { required: true, message: t('elections.form.numberToElectRequired'), trigger: 'blur' }
  ],
  emailFromAddress: [
    { type: 'email', message: t('elections.form.emailInvalid'), trigger: 'blur' }
  ]
});

onMounted(async () => {
  try {
    await electionStore.fetchElections();
    availableElections.value = electionStore.elections || [];
  } catch (error) {
    console.error('Failed to fetch elections for linked election dropdown', error);
  }
});

async function submitForm() {
  if (!formRef.value) { return; }

  await formRef.value.validate(async (valid) => {
    if (valid) {
      submitting.value = true;
      try {
        const dto: CreateElectionDto = {
          ...form,
          dateOfElection: form.dateOfElection
            ? new Date(form.dateOfElection).toISOString()
            : undefined
        };

        const election = await electionStore.createElection(dto);
        showSuccessMessage(t('elections.createSuccess'));
        router.push(`/elections/${election.electionGuid}`);
      } catch (error: any) {
        showErrorMessage(extractApiErrorMessage(error));
      } finally {
        submitting.value = false;
      }
    }
  });
}

function cancel() {
  router.back();
}
</script>

<style lang="less">
.create-election-page {
  max-width: 800px;
  margin: 0 auto;

  .card-header h2 {
    margin: 0;
    font-size: 20px;
    color: #303133;
  }
}
</style>
