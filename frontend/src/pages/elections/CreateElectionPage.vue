<template>
  <div class="create-election-page">
    <el-card>
      <template #header>
        <div class="card-header">
          <h2>{{ $t('elections.createNew') }}</h2>
        </div>
      </template>
      
      <el-form
        ref="formRef"
        :model="form"
        :rules="rules"
        label-width="180px"
        label-position="left"
      >
        <el-form-item :label="$t('elections.form.name')" prop="name">
          <el-input v-model="form.name" :placeholder="$t('elections.form.namePlaceholder')" />
        </el-form-item>

        <el-form-item :label="$t('elections.form.date')" prop="dateOfElection">
          <el-date-picker
            v-model="form.dateOfElection"
            type="date"
            :placeholder="$t('elections.form.datePlaceholder')"
            style="width: 100%"
          />
        </el-form-item>

        <el-form-item :label="$t('elections.form.type')" prop="electionType">
          <el-select v-model="form.electionType" :placeholder="$t('elections.form.typePlaceholder')">
            <el-option label="STV - Single Transferable Vote" value="STV" />
            <el-option label="Condorcet" value="Cond" />
            <el-option label="Multi-Winner" value="Multi" />
          </el-select>
        </el-form-item>

        <el-form-item :label="$t('elections.form.numberToElect')" prop="numberToElect">
          <el-input-number v-model="form.numberToElect" :min="1" :max="50" />
        </el-form-item>

        <el-form-item :label="$t('elections.form.numberExtra')" prop="numberExtra">
          <el-input-number v-model="form.numberExtra" :min="0" :max="20" />
        </el-form-item>

        <el-form-item :label="$t('elections.form.convenor')" prop="convenor">
          <el-input v-model="form.convenor" :placeholder="$t('elections.form.convenorPlaceholder')" />
        </el-form-item>

        <el-form-item :label="$t('elections.form.electionMode')" prop="electionMode">
          <el-select v-model="form.electionMode" :placeholder="$t('elections.form.modePlaceholder')">
            <el-option label="Normal" value="N" />
            <el-option label="International" value="I" />
          </el-select>
        </el-form-item>

        <el-form-item :label="$t('elections.form.showFullReport')">
          <el-switch v-model="form.showFullReport" />
        </el-form-item>

        <el-form-item :label="$t('elections.form.listForPublic')">
          <el-switch v-model="form.listForPublic" />
        </el-form-item>

        <el-form-item :label="$t('elections.form.showAsTest')">
          <el-switch v-model="form.showAsTest" />
        </el-form-item>

        <el-form-item>
          <el-button type="primary" @click="submitForm" :loading="submitting">
            {{ $t('common.create') }}
          </el-button>
          <el-button @click="cancel">
            {{ $t('common.cancel') }}
          </el-button>
        </el-form-item>
      </el-form>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive } from 'vue';
import { useRouter } from 'vue-router';
import { useI18n } from 'vue-i18n';
import { ElMessage, type FormInstance, type FormRules } from 'element-plus';
import { useElectionStore } from '../../stores/electionStore';
import type { CreateElectionDto } from '../../types';

const router = useRouter();
const { t } = useI18n();
const electionStore = useElectionStore();

const formRef = ref<FormInstance>();
const submitting = ref(false);

const form = reactive<CreateElectionDto>({
  name: '',
  dateOfElection: undefined,
  electionType: 'STV',
  numberToElect: 9,
  convenor: '',
  electionMode: 'N',
  numberExtra: 0,
  showFullReport: true,
  listForPublic: false,
  showAsTest: false
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
  ]
});

async function submitForm() {
  if (!formRef.value) return;
  
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
        ElMessage.success(t('elections.createSuccess'));
        router.push(`/elections/${election.electionGuid}`);
      } catch (error: any) {
        ElMessage.error(error.message || t('elections.createError'));
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

<style scoped>
.create-election-page {
  max-width: 800px;
  margin: 0 auto;
}

.card-header h2 {
  margin: 0;
  font-size: 20px;
  color: #303133;
}
</style>
