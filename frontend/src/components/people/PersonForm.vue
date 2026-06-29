<script setup lang="ts">
import { useApiErrorHandler } from "@/composables/useApiErrorHandler";
import { useNotifications } from "@/composables/useNotifications";
import type { RegistrationHistoryEntryDto } from "@/types/FrontDesk";
import {
  formatRegistrationHistoryDetails,
  formatRegistrationHistoryTime,
  parseRegistrationHistory,
  sortRegistrationHistoryNewestFirst,
} from "@/utils/formatRegistrationHistory";
import { type FormInstance, type FormRules, ElMessageBox } from "element-plus";
import { computed, onMounted, reactive, ref, watch } from "vue";
import { useI18n } from "vue-i18n";
import { peopleService } from "../../services/peopleService";
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
  email: "",
  phone: "",
  area: "",
  bahaiId: "",
  ageGroup: "A",
  ineligibleReasonGuid: null as string | null,
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
  return formatRegistrationHistoryDetails(entry, { t });
}

onMounted(async () => {
  await eligibilityStore.fetchReasons();
});

function resetForm() {
  form.firstName = "";
  form.lastName = "";
  form.email = "";
  form.phone = "";
  form.area = "";
  form.bahaiId = "";
  form.ageGroup = "A";
  form.ineligibleReasonGuid = null;
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
      form.email = personDetails.value.email || "";
      form.phone = personDetails.value.phone || "";
      form.area = personDetails.value.area || "";
      form.bahaiId = personDetails.value.bahaiId || "";
      form.ageGroup = personDetails.value.ageGroup || "A";
      form.ineligibleReasonGuid =
        personDetails.value.ineligibleReasonGuid || null;
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
            email: form.email || undefined,
            phone: form.phone || undefined,
            area: form.area || undefined,
            bahaiId: form.bahaiId || undefined,
            ageGroup: form.ageGroup || undefined,
            ineligibleReasonGuid: form.ineligibleReasonGuid || undefined,
          };
          await peopleStore.updatePerson(props.person.personGuid, dto);
          showSuccessMessage(t("people.updateSuccess"));
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
            ineligibleReasonGuid: form.ineligibleReasonGuid || undefined,
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

      <el-form-item :label="$t('eligibility.label')">
        <el-select
          v-model="form.ineligibleReasonGuid"
          :placeholder="$t('eligibility.selectReason')"
          style="width: 100%"
          clearable
        >
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

      <el-form-item :label="$t('people.ageGroupLabel')" prop="ageGroup">
        <el-select v-model="form.ageGroup" style="width: 100%">
          <el-option :label="$t('people.ageGroup.adult')" value="A" />
          <el-option :label="$t('people.ageGroup.youth')" value="Y" />
        </el-select>
      </el-form-item>
    </el-form>

    <div v-if="isEdit && personDetails" class="history-section">
      <el-divider />

      <div v-if="personDetails.voteCount > 0" class="vote-count">
        <p>
          <strong>{{ $t("people.votesReceived") }}:</strong>
          {{ personDetails.voteCount }}
        </p>
      </div>

      <div v-if="registrationHistory.length > 0" class="registration-history">
        <h4>{{ $t("people.registrationHistory") }}</h4>
        <el-timeline>
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

    <div v-if="!hideActions" class="person-form-actions">
      <el-button @click="handleCancel">{{ $t("common.cancel") }}</el-button>
      <el-button type="primary" :loading="submitting" @click="handleSubmit">
        {{ isEdit ? $t("common.save") : $t("common.create") }}
      </el-button>
    </div>

    <div v-if="isEdit && showDelete" class="person-form-delete">
      <el-button type="danger" :loading="deleting" @click="handleDelete">
        {{ $t("common.delete") }}
      </el-button>
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
    justify-content: flex-end;
    gap: var(--spacing-2);
    margin-top: var(--spacing-4);
    padding-top: var(--spacing-4);
    border-top: 1px solid var(--el-border-color-lighter);
  }

  .person-form-delete {
    margin-top: var(--spacing-6);
    padding-top: var(--spacing-4);
    border-top: 1px solid var(--el-border-color-lighter);
  }
}
</style>
