<template>
  <div class="edit-election-page">
    <div v-if="loading" class="loading-container">
      <el-skeleton :rows="5" animated />
    </div>

    <el-card v-else-if="election">
      <template #header>
        <div class="card-header">
          <h2>{{ $t("elections.edit") }}</h2>
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
          :ballot-count="election?.ballotCount"
        />

        <el-form-item style="margin-top: 20px">
          <el-button type="primary" :loading="submitting" @click="submitForm">
            {{ $t("common.save") }}
          </el-button>
          <el-button @click="cancel">
            {{ $t("common.cancel") }}
          </el-button>
        </el-form-item>
      </el-form>
    </el-card>

    <el-empty v-else :description="$t('elections.notFound')" />
  </div>
</template>

<script setup lang="ts">
import { useApiErrorHandler } from "@/composables/useApiErrorHandler";
import { useNotifications } from "@/composables/useNotifications";
import { type FormInstance, type FormRules } from "element-plus";
import { computed, onMounted, reactive, ref } from "vue";
import { useI18n } from "vue-i18n";
import { useRoute, useRouter } from "vue-router";
import ElectionFormTabs from "../../components/elections/ElectionFormTabs.vue";
import { useElectionStore } from "../../stores/electionStore";
import type { ElectionSummaryDto, UpdateElectionDto } from "../../types";

const router = useRouter();
const route = useRoute();
const { t } = useI18n();
const electionStore = useElectionStore();
const { showSuccessMessage } = useNotifications();
const { handleApiError } = useApiErrorHandler();

const electionGuid = route.params.id as string;
const formRef = ref<FormInstance>();
const submitting = ref(false);
const loading = computed(() => electionStore.loading);
const election = computed(() => electionStore.currentElection);
const availableElections = ref<ElectionSummaryDto[]>([]);

const form = reactive<UpdateElectionDto>({
  name: "",
  dateOfElection: undefined,
  electionType: undefined,
  numberToElect: undefined,
  convenor: undefined,
  electionMode: undefined,
  numberExtra: undefined,
  showFullReport: undefined,
  listForPublic: undefined,
  showAsTest: undefined,
  tallyStatus: undefined,
  onlineWhenOpen: undefined,
  onlineWhenClose: undefined,
  canVote: undefined,
  canReceive: undefined,
  electionPasscode: undefined,
  linkedElectionGuid: undefined,
  linkedElectionKind: undefined,
  useCallInButton: undefined,
  hidePreBallotPages: undefined,
  maskVotingMethod: undefined,
  onlineCloseIsEstimate: undefined,
  onlineSelectionProcess: undefined,
  onlineAnnounced: undefined,
  emailFromAddress: undefined,
  emailFromName: undefined,
  emailText: undefined,
  emailSubject: undefined,
  smsText: undefined,
  customMethods: undefined,
  votingMethods: undefined,
  flags: undefined,
});

const rules = reactive<FormRules>({
  name: [
    {
      required: true,
      message: t("elections.form.nameRequired"),
      trigger: "blur",
    },
  ],
  numberToElect: [
    {
      required: true,
      message: t("elections.form.numberToElectRequired"),
      trigger: "blur",
    },
  ],
  dateOfElection: [
    {
      required: true,
      message: t("elections.form.dateRequired"),
      trigger: "change",
    },
  ],
  emailFromAddress: [
    {
      type: "email",
      message: t("elections.form.emailInvalid"),
      trigger: "blur",
    },
  ],
});

onMounted(async () => {
  try {
    await electionStore.fetchElectionById(electionGuid);
    await electionStore.fetchElections();
    availableElections.value =
      electionStore.elections?.filter((e) => e.electionGuid !== electionGuid) ||
      [];

    if (election.value) {
      Object.assign(form, {
        name: election.value.name,
        dateOfElection: election.value.dateOfElection,
        electionType: election.value.electionType,
        numberToElect: election.value.numberToElect,
        convenor: election.value.convenor,
        electionMode: election.value.electionMode,
        numberExtra: election.value.numberExtra,
        showFullReport: election.value.showFullReport,
        listForPublic: election.value.listForPublic,
        showAsTest: election.value.showAsTest,
        tallyStatus: election.value.tallyStatus,
        onlineWhenOpen: election.value.onlineWhenOpen,
        onlineWhenClose: election.value.onlineWhenClose,
        canVote: election.value.canVote,
        canReceive: election.value.canReceive,
        electionPasscode: election.value.electionPasscode,
        linkedElectionGuid: election.value.linkedElectionGuid,
        linkedElectionKind: election.value.linkedElectionKind,
        useCallInButton: election.value.useCallInButton,
        hidePreBallotPages: election.value.hidePreBallotPages,
        maskVotingMethod: election.value.maskVotingMethod,
        onlineCloseIsEstimate: election.value.onlineCloseIsEstimate,
        onlineSelectionProcess: election.value.onlineSelectionProcess,
        onlineAnnounced: election.value.onlineAnnounced,
        emailFromAddress: election.value.emailFromAddress,
        emailFromName: election.value.emailFromName,
        emailText: election.value.emailText,
        emailSubject: election.value.emailSubject,
        smsText: election.value.smsText,
        customMethods: election.value.customMethods,
        votingMethods: election.value.votingMethods,
        flags: election.value.flags,
      });
    }
  } catch (error) {
    handleApiError(error);
  }
});

async function submitForm() {
  if (!formRef.value) {
    return;
  }

  await formRef.value.validate(async (valid) => {
    if (valid) {
      submitting.value = true;
      try {
        await electionStore.updateElection(electionGuid, form);
        showSuccessMessage(t("elections.updateSuccess"));
        router.push(`/elections/${electionGuid}`);
      } catch (error) {
        handleApiError(error);
      } finally {
        submitting.value = false;
      }
    }
  });
}

function cancel() {
  router.push(`/elections/${electionGuid}`);
}
</script>

<style lang="less">
.edit-election-page {
  max-width: var(--normal-max-width);
  margin: 0 auto;
}

.loading-container {
  padding: 40px;
}

.card-header h2 {
  margin: 0;
  font-size: 20px;
  color: #303133;
}
</style>
