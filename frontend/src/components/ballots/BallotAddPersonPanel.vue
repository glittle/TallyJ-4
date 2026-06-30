<script setup lang="ts">
import { useApiErrorHandler } from "@/composables/useApiErrorHandler";
import { toApiEligibility } from "@/utils/eligibilityForm";
import PersonForm from "../people/PersonForm.vue";
import { usePeopleStore } from "../../stores/peopleStore";
import type { CreatePersonDto } from "../../types";
import type { SearchablePersonDto } from "../../types/Person";
import type { VoteDto } from "../../types/Vote";
import { computed, ref } from "vue";

export type BallotVoteEntryType = "normal" | "U01" | "U02";

const props = defineProps<{
  electionGuid: string;
  ballotGuid: string;
}>();

const emit = defineEmits<{
  "person-added": [vote: VoteDto];
  cancel: [];
}>();

const peopleStore = usePeopleStore();
const { handleApiError } = useApiErrorHandler();

const personFormRef = ref<InstanceType<typeof PersonForm>>();
const voteEntryType = ref<BallotVoteEntryType>("normal");
const submitting = ref(false);

const isPersonLessVote = computed(
  () => voteEntryType.value === "U01" || voteEntryType.value === "U02",
);

function getIneligibleReasonCode(): string | undefined {
  if (voteEntryType.value === "U01") {
    return "U01";
  }
  if (voteEntryType.value === "U02") {
    return "U02";
  }
  return undefined;
}

function buildPersonLessVote(): VoteDto {
  const reasonCode = getIneligibleReasonCode()!;
  return {
    rowId: 0,
    ballotGuid: props.ballotGuid,
    positionOnBallot: 0,
    statusCode: "Spoiled",
    ineligibleReasonCode: reasonCode,
  };
}

async function handleSubmit() {
  if (isPersonLessVote.value) {
    submitting.value = true;
    try {
      emit("person-added", buildPersonLessVote());
    } finally {
      submitting.value = false;
    }
    return;
  }

  const formComponent = personFormRef.value;
  if (!formComponent?.formRef) {
    return;
  }

  await formComponent.formRef.validate(async (valid) => {
    if (!valid) {
      return;
    }

    const form = formComponent.form;
    submitting.value = true;
    try {
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

      const person = await peopleStore.createPerson(dto);
      const searchablePerson: SearchablePersonDto =
        peopleStore.enrichPersonForSearch(person);

      if (peopleStore.isCacheInitialized) {
        peopleStore.candidateCache.push(searchablePerson);
      }

      const isSpoiled = person.canReceiveVotes === false;
      const vote: VoteDto = {
        rowId: 0,
        ballotGuid: props.ballotGuid,
        positionOnBallot: 0,
        personGuid: person.personGuid,
        personFullName: person.fullName,
        statusCode: isSpoiled ? "Spoiled" : "ok",
        ineligibleReasonCode: isSpoiled
          ? (person.ineligibleReasonCode ??
            searchablePerson.ineligibleReasonCode)
          : undefined,
      };

      emit("person-added", vote);
    } catch (error) {
      handleApiError(error);
    } finally {
      submitting.value = false;
    }
  });
}
</script>

<template>
  <div class="ballot-add-person-panel">
    <el-form label-width="150px" label-position="left">
      <el-form-item :label="$t('ballots.voteEntryType')">
        <el-radio-group v-model="voteEntryType">
          <el-radio value="normal">
            {{ $t("ballots.voteEntryNormal") }}
          </el-radio>
          <el-radio value="U01">
            {{ $t("ballots.voteEntryUnidentifiable") }}
          </el-radio>
          <el-radio value="U02">
            {{ $t("ballots.voteEntryUnreadable") }}
          </el-radio>
        </el-radio-group>
      </el-form-item>
    </el-form>

    <p v-if="isPersonLessVote" class="ballot-add-person-panel__hint">
      {{ $t("ballots.personLessVoteHint") }}
    </p>

    <PersonForm
      v-else
      ref="personFormRef"
      :election-guid="electionGuid"
      :is-edit="false"
      :show-delete="false"
      hide-actions
    />

    <div class="ballot-add-person-panel__actions">
      <el-button @click="$emit('cancel')">{{ $t("common.cancel") }}</el-button>
      <el-button type="primary" :loading="submitting" @click="handleSubmit">
        {{ $t("common.save") }}
      </el-button>
    </div>
  </div>
</template>

<style lang="less">
.ballot-add-person-panel {
  .ballot-add-person-panel__hint {
    margin: 0 0 var(--spacing-4);
    color: var(--el-text-color-secondary);
    font-size: var(--el-font-size-small);
  }

  .ballot-add-person-panel__actions {
    display: flex;
    justify-content: flex-end;
    gap: var(--spacing-2);
    margin-top: var(--spacing-4);
    padding-top: var(--spacing-4);
    border-top: 1px solid var(--el-border-color-lighter);
  }
}
</style>
