<script setup lang="ts">
import { useApiErrorHandler } from "@/composables/useApiErrorHandler";
import { useNotifications } from "@/composables/useNotifications";
import type { RegistrationHistoryEntryDto } from "@/types/FrontDesk";
import {
  ELIGIBLE_REASON_VALUE,
  toApiEligibility,
  toFormEligibility,
} from "@/utils/eligibilityForm";
import {
  formatRegistrationHistoryDetails,
  formatRegistrationHistoryTime,
  parseRegistrationHistory,
  sortRegistrationHistoryNewestFirst,
} from "@/utils/formatRegistrationHistory";
import {
  electionSupportsKiosk,
  getVotingMethodLabel,
} from "@/utils/votingMethodLabels";
import { type FormInstance, type FormRules, ElMessageBox } from "element-plus";
import { computed, onMounted, reactive, ref, watch } from "vue";
import { useI18n } from "vue-i18n";
import { peopleService } from "../../services/peopleService";
import { useElectionStore } from "../../stores/electionStore";
import { useEligibilityStore } from "../../stores/eligibilityStore";
import { usePeopleStore } from "../../stores/peopleStore";
import type {
  CreatePersonDto,
  PersonDetailDto,
  PersonListDto,
  UpdatePersonDto,
} from "../../types";

const props = defineProps<{
  electionGuid: string;
  person?: PersonListDto | null;
  isEdit?: boolean;
  showDelete?: boolean;
  hideActions?: boolean;
}>();

const emit = defineEmits<{
  success: [];
  deleted: [];
  cancel: [];
}>();

const { t } = useI18n();

const peopleStore = usePeopleStore();
const electionStore = useElectionStore();
const eligibilityStore = useEligibilityStore();
const { showSuccessMessage } = useNotifications();
const { handleApiError } = useApiErrorHandler();

const formRef = ref<FormInstance>();
const submitting = ref(false);
const deleting = ref(false);
const loadingDetails = ref(false);
const personDetails = ref<PersonDetailDto | null>(null);

const form = reactive({
  firstName: "",
  lastName: "",
  otherNames: "",
  otherLastNames: "",
  otherInfo: "",
  email: "",
  phone: "",
  area: "",
  bahaiId: "",
  ageGroup: "A",
  ineligibleReasonGuid: ELIGIBLE_REASON_VALUE,
});

const electionHasUnits = computed(
  () => electionStore.currentElection?.hasUnits === true,
);

const electionHasKiosk = computed(() =>
  electionSupportsKiosk(electionStore.currentElection?.votingMethods),
);

const isEditMode = computed(() => props.isEdit === true);

const showKioskCode = computed(
  () =>
    electionHasKiosk.value &&
    isEditMode.value &&
    personDetails.value &&
    !personDetails.value.votingMethod,
);

const canDeletePerson = computed(() => personDetails.value?.canDelete === true);

const selectedEligibility = computed(() => {
  if (form.ineligibleReasonGuid === ELIGIBLE_REASON_VALUE) {
    return { canVote: true, canReceiveVotes: true };
  }

  const reason = eligibilityStore.getByGuid(form.ineligibleReasonGuid);
  return {
    canVote: reason?.canVote ?? false,
    canReceiveVotes: reason?.canReceiveVotes ?? false,
  };
});

const currentVotingMethodLabel = computed(() =>
  getVotingMethodLabel(personDetails.value?.votingMethod, t),
);

const registrationHistoryTitle = computed(() => {
  if (personDetails.value?.votingMethod) {
    return t("people.registrationHistoryWithMethod", {
      method: currentVotingMethodLabel.value,
    });
  }

  return t("people.registrationHistory");
});

const rules = reactive<FormRules>({
  lastName: [
    { required: true, message: t("people.lastNameRequired"), trigger: "blur" },
  ],
  email: [
    { type: "email", message: t("people.emailInvalid"), trigger: "blur" },
  ],
});

const registrationHistory = computed((): RegistrationHistoryEntryDto[] => {
  if (!personDetails.value?.registrationHistory) {
    return [];
  }
  return sortRegistrationHistoryNewestFirst(
    parseRegistrationHistory(personDetails.value.registrationHistory),
  );
});

function formatHistoryDetails(entry: RegistrationHistoryEntryDto): string {
  return formatRegistrationHistoryDetails(entry, {
    t,
    getVotingMethodLabel: (method) => getVotingMethodLabel(method, t),
  });
}

onMounted(async () => {
  await Promise.all([
    eligibilityStore.fetchReasons(),
    electionStore.fetchElectionById(props.electionGuid),
  ]);
});

function resetForm() {
  form.firstName = "";
  form.lastName = "";
  form.otherNames = "";
  form.otherLastNames = "";
  form.otherInfo = "";
  form.email = "";
  form.phone = "";
  form.area = "";
  form.bahaiId = "";
  form.ageGroup = "A";
  form.ineligibleReasonGuid = ELIGIBLE_REASON_VALUE;
}

watch(
  () => props.person,
  async (person) => {
    if (props.isEdit && person) {
      await loadPersonDetails();
    } else if (!props.isEdit) {
      resetForm();
      personDetails.value = null;
    }
  },
  { immediate: true },
);

async function loadPersonDetails() {
  if (!props.person) {
    return;
  }

  loadingDetails.value = true;
  try {
    personDetails.value = await peopleService.getDetails(
      props.person.personGuid,
    );

    if (personDetails.value) {
      form.firstName = personDetails.value.firstName || "";
      form.lastName = personDetails.value.lastName;
      form.otherNames = personDetails.value.otherNames || "";
      form.otherLastNames = personDetails.value.otherLastNames || "";
      form.otherInfo = personDetails.value.otherInfo || "";
      form.email = personDetails.value.email || "";
      form.phone = personDetails.value.phone || "";
      form.area = personDetails.value.area || "";
      form.bahaiId = personDetails.value.bahaiId || "";
      form.ageGroup = personDetails.value.ageGroup || "A";
      form.ineligibleReasonGuid = toFormEligibility(
        personDetails.value.ineligibleReasonGuid,
      );
    }
  } catch (error) {
    handleApiError(error);
  } finally {
    loadingDetails.value = false;
  }
}

async function handleSubmit() {
  if (!formRef.value) {
    return;
  }

  await formRef.value.validate(async (valid) => {
    if (valid) {
      submitting.value = true;
      try {
        if (props.isEdit && props.person) {
          const dto: UpdatePersonDto = {
            firstName: form.firstName || undefined,
            lastName: form.lastName,
            otherNames: form.otherNames || undefined,
            otherLastNames: form.otherLastNames || undefined,
            otherInfo: form.otherInfo || undefined,
            email: form.email || undefined,
            phone: form.phone || undefined,
            area: form.area || undefined,
            bahaiId: form.bahaiId || undefined,
            ageGroup: form.ageGroup || undefined,
            ineligibleReasonGuid: toApiEligibility(form.ineligibleReasonGuid),
          };
          await peopleStore.updatePerson(props.person.personGuid, dto);
          showSuccessMessage(t("people.updateSuccess"));
        } else {
          const dto: CreatePersonDto = {
            electionGuid: props.electionGuid,
            firstName: form.firstName || undefined,
            lastName: form.lastName,
            otherNames: form.otherNames || undefined,
            otherLastNames: form.otherLastNames || undefined,
            otherInfo: form.otherInfo || undefined,
            email: form.email || undefined,
            phone: form.phone || undefined,
            area: form.area || undefined,
            bahaiId: form.bahaiId || undefined,
            ageGroup: form.ageGroup || undefined,
            ineligibleReasonGuid: toApiEligibility(form.ineligibleReasonGuid),
          };
          await peopleStore.createPerson(dto);
          showSuccessMessage(t("people.createSuccess"));
        }
        emit("success");
      } catch (error) {
        handleApiError(error);
      } finally {
        submitting.value = false;
      }
    }
  });
}

async function handleDelete() {
  if (!props.person) {
    return;
  }

  try {
    await ElMessageBox.confirm(
      t("people.deleteConfirm", { name: props.person.fullName }),
      t("common.warning"),
      {
        confirmButtonText: t("common.delete"),
        cancelButtonText: t("common.cancel"),
        type: "warning",
      },
    );

    deleting.value = true;
    await peopleStore.deletePerson(props.person.personGuid);
    showSuccessMessage(t("people.deleteSuccess"));
    emit("deleted");
  } catch (error: unknown) {
    if (error !== "cancel") {
      handleApiError(error);
    }
  } finally {
    deleting.value = false;
  }
}

function handleCancel() {
  formRef.value?.resetFields();
  personDetails.value = null;
  emit("cancel");
}

defineExpose({
  form,
  formRef,
  personDetails,
  handleSubmit,
  resetForm,
  loadPersonDetails,
});
</script>

<template>
  <div v-loading="loadingDetails" class="person-form">
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

      <el-form-item :label="$t('people.otherNames')" prop="otherNames">
        <el-input v-model="form.otherNames" />
      </el-form-item>

      <el-form-item :label="$t('people.otherLastNames')" prop="otherLastNames">
        <el-input v-model="form.otherLastNames" />
      </el-form-item>

      <el-form-item :label="$t('people.otherInfo')" prop="otherInfo">
        <el-input v-model="form.otherInfo" />
      </el-form-item>

      <el-form-item :label="$t('eligibility.label')">
        <div class="eligibility-field">
          <el-select v-model="form.ineligibleReasonGuid" style="width: 100%">
            <el-option-group :label="$t('eligibility.eligible')">
              <el-option
                :label="$t('eligibility.eligible')"
                :value="ELIGIBLE_REASON_VALUE"
              />
            </el-option-group>
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

          <div class="eligibility-interpretation">
            <div
              class="eligibility-interpretation-row"
              :class="{ 'is-no': !selectedEligibility.canVote }"
            >
              <span>{{ $t("people.canVoteInElection") }}</span>
              <span>{{
                selectedEligibility.canVote
                  ? $t("people.eligibilityYes")
                  : $t("people.eligibilityNo")
              }}</span>
            </div>
            <div
              class="eligibility-interpretation-row"
              :class="{ 'is-no': !selectedEligibility.canReceiveVotes }"
            >
              <span>{{ $t("people.canReceiveVotesInElection") }}</span>
              <span>{{
                selectedEligibility.canReceiveVotes
                  ? $t("people.eligibilityYes")
                  : $t("people.eligibilityNo")
              }}</span>
            </div>
          </div>
        </div>
      </el-form-item>

      <el-form-item :label="$t('people.bahaiId')" prop="bahaiId">
        <el-input v-model="form.bahaiId" />
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

      <el-form-item :label="$t('people.ageGroupLabel')" prop="ageGroup">
        <el-select v-model="form.ageGroup" style="width: 100%">
          <el-option :label="$t('people.ageGroup.adult')" value="A" />
          <el-option :label="$t('people.ageGroup.youth')" value="Y" />
        </el-select>
      </el-form-item>

      <el-form-item v-if="showKioskCode" :label="$t('people.kioskCode')">
        <div class="kiosk-code-field">
          <el-input :model-value="personDetails?.kioskCode || ''" disabled />
          <p class="kiosk-code-note">{{ $t("people.kioskCodeNote") }}</p>
        </div>
      </el-form-item>

      <el-form-item v-if="electionHasUnits" :label="$t('people.unitName')">
        <el-input :model-value="personDetails?.unitName || ''" disabled />
      </el-form-item>
    </el-form>
    <div v-if="!hideActions" class="person-form-actions">
      <el-button type="primary" :loading="submitting" @click="handleSubmit">
        {{ isEdit ? $t("common.save") : $t("common.create") }}
      </el-button>
      <el-button @click="handleCancel">{{ $t("common.cancel") }}</el-button>
    </div>
    <div v-if="isEditMode && personDetails" class="history-section">
      <el-divider />

      <div
        v-if="registrationHistory.length > 0 || personDetails.votingMethod"
        class="registration-history"
      >
        <h4>{{ registrationHistoryTitle }}</h4>
        <el-timeline v-if="registrationHistory.length > 0">
          <el-timeline-item
            v-for="(entry, index) in registrationHistory"
            :key="index"
            :timestamp="formatRegistrationHistoryTime(entry.timestamp)"
          >
            {{ formatHistoryDetails(entry) }}
          </el-timeline-item>
        </el-timeline>
      </div>
    </div>

    <div v-if="isEditMode && showDelete" class="person-form-delete">
      <el-button
        v-if="canDeletePerson"
        type="danger"
        :loading="deleting"
        @click="handleDelete"
      >
        {{ $t("common.delete") }}
      </el-button>
      <p v-else class="delete-not-allowed">
        {{ $t("people.deleteNotAllowed") }}
      </p>
    </div>
  </div>
</template>

<style lang="less">
.person-form {
  .history-section {
    margin-top: var(--spacing-4);

    h4 {
      margin: var(--spacing-3) 0 var(--spacing-2);
      font-size: var(--font-size-base);
      font-weight: var(--font-weight-semibold);
    }

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

  .person-form-actions {
    display: flex;
    justify-content: space-between;
    gap: var(--spacing-2);
    margin-top: var(--spacing-4);
    padding-top: var(--spacing-4);
    border-top: 1px solid var(--el-border-color-lighter);
  }

  .eligibility-field {
    width: 100%;
  }

  .eligibility-interpretation {
    margin-top: var(--spacing-2);
    display: grid;
    gap: var(--spacing-1);
    font-size: var(--font-size-sm);
    color: var(--color-neutral-600);
  }

  .eligibility-interpretation-row {
    display: flex;
    justify-content: space-between;
    gap: var(--spacing-3);
    padding: var(--spacing-1) var(--spacing-2);
    border-radius: var(--border-radius-sm);

    &.is-no {
      color: var(--el-color-danger);
      background-color: var(--el-color-danger-light-9);
    }
  }

  .kiosk-code-field {
    width: 100%;
  }

  .kiosk-code-note {
    margin: var(--spacing-1) 0 0;
    font-size: var(--font-size-sm);
    color: var(--color-neutral-500);
  }

  .person-form-delete {
    margin-top: var(--spacing-6);
    padding-top: var(--spacing-4);
    border-top: 1px solid var(--el-border-color-lighter);
    text-align: right;
  }

  .delete-not-allowed {
    margin: 0;
    text-align: left;
    font-size: var(--font-size-sm);
    color: var(--color-neutral-500);
  }
}
</style>
