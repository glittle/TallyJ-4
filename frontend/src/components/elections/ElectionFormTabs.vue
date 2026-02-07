<template>
  <el-tabs v-model="activeTab" type="border-card">
    <el-tab-pane :label="tabLabel('basic', $t('elections.tabs.basicInfo'))" name="basic">
      <el-form-item :label="$t('elections.form.name')" prop="name">
        <el-input
          v-model="modelValue.name"
          :placeholder="$t('elections.form.namePlaceholder')"
        />
      </el-form-item>

      <el-form-item :label="$t('elections.form.date')" prop="dateOfElection">
        <el-date-picker
          v-model="modelValue.dateOfElection"
          type="date"
          :placeholder="$t('elections.form.datePlaceholder')"
          style="width: 100%"
        />
      </el-form-item>

      <el-form-item :label="$t('elections.form.convenor')" prop="convenor">
        <el-input
          v-model="modelValue.convenor"
          :placeholder="$t('elections.form.convenorPlaceholder')"
        />
      </el-form-item>

      <el-form-item :label="$t('elections.form.type')" prop="electionType">
        <el-select
          v-model="modelValue.electionType"
          :placeholder="$t('elections.form.typePlaceholder')"
        >
          <el-option label="STV - Single Transferable Vote" value="STV" />
          <el-option label="Condorcet" value="Cond" />
          <el-option label="Multi-Winner" value="Multi" />
        </el-select>
      </el-form-item>

      <el-form-item :label="$t('elections.form.electionMode')" prop="electionMode">
        <el-select
          v-model="modelValue.electionMode"
          :placeholder="$t('elections.form.modePlaceholder')"
        >
          <el-option label="Normal" value="N" />
          <el-option label="International" value="I" />
        </el-select>
      </el-form-item>
    </el-tab-pane>

    <el-tab-pane
      :label="tabLabel('rules', $t('elections.tabs.electionRules'))"
      name="rules"
    >
      <el-form-item :label="$t('elections.form.numberToElect')" prop="numberToElect">
        <el-input-number v-model="modelValue.numberToElect" :min="1" :max="50" />
      </el-form-item>

      <el-form-item :label="$t('elections.form.numberExtra')" prop="numberExtra">
        <el-input-number v-model="modelValue.numberExtra" :min="0" :max="20" />
      </el-form-item>

      <el-form-item :label="$t('elections.form.canVote')" prop="canVote">
        <el-select
          v-model="modelValue.canVote"
          :placeholder="$t('elections.form.canVotePlaceholder')"
        >
          <el-option label="Yes" value="Y" />
          <el-option label="No" value="N" />
          <el-option label="Unknown" value="?" />
        </el-select>
      </el-form-item>

      <el-form-item :label="$t('elections.form.canReceive')" prop="canReceive">
        <el-select
          v-model="modelValue.canReceive"
          :placeholder="$t('elections.form.canReceivePlaceholder')"
        >
          <el-option label="Yes" value="Y" />
          <el-option label="No" value="N" />
          <el-option label="Unknown" value="?" />
        </el-select>
      </el-form-item>
    </el-tab-pane>

    <el-tab-pane
      :label="tabLabel('voting-methods', $t('elections.tabs.votingMethods'))"
      name="voting-methods"
    >
      <el-form-item :label="$t('elections.form.useCallInButton')">
        <el-switch v-model="modelValue.useCallInButton" />
      </el-form-item>

      <el-form-item :label="$t('elections.form.customMethods')" prop="customMethods">
        <el-input
          v-model="modelValue.customMethods"
          :placeholder="$t('elections.form.customMethodsPlaceholder')"
        />
        <template #help>
          <span class="form-help">{{ $t("elections.form.customMethodsHelp") }}</span>
        </template>
      </el-form-item>

      <el-form-item :label="$t('elections.form.votingMethods')" prop="votingMethods">
        <el-input
          v-model="modelValue.votingMethods"
          :placeholder="$t('elections.form.votingMethodsPlaceholder')"
        />
        <template #help>
          <span class="form-help">{{ $t("elections.form.votingMethodsHelp") }}</span>
        </template>
      </el-form-item>
    </el-tab-pane>

    <el-tab-pane
      :label="tabLabel('online-voting', $t('elections.tabs.onlineVoting'))"
      name="online-voting"
    >
      <el-form-item :label="$t('elections.form.onlineWhenOpen')" prop="onlineWhenOpen">
        <el-date-picker
          v-model="modelValue.onlineWhenOpen"
          type="datetime"
          :placeholder="$t('elections.form.onlineWhenOpenPlaceholder')"
          style="width: 100%"
        />
      </el-form-item>

      <el-form-item :label="$t('elections.form.onlineWhenClose')" prop="onlineWhenClose">
        <el-date-picker
          v-model="modelValue.onlineWhenClose"
          type="datetime"
          :placeholder="$t('elections.form.onlineWhenClosePlaceholder')"
          style="width: 100%"
        />
      </el-form-item>

      <el-form-item :label="$t('elections.form.onlineCloseIsEstimate')">
        <el-switch v-model="modelValue.onlineCloseIsEstimate" />
      </el-form-item>

      <el-form-item
        :label="$t('elections.form.onlineSelectionProcess')"
        prop="onlineSelectionProcess"
      >
        <el-select
          v-model="modelValue.onlineSelectionProcess"
          :placeholder="$t('elections.form.onlineSelectionProcessPlaceholder')"
        >
          <el-option label="Simultaneous" value="S" />
          <el-option label="Sequential" value="Q" />
        </el-select>
      </el-form-item>

      <el-form-item :label="$t('elections.form.onlineAnnounced')" prop="onlineAnnounced">
        <el-date-picker
          v-model="modelValue.onlineAnnounced"
          type="datetime"
          :placeholder="$t('elections.form.onlineAnnouncedPlaceholder')"
          style="width: 100%"
        />
      </el-form-item>
    </el-tab-pane>

    <el-tab-pane
      :label="tabLabel('communication', $t('elections.tabs.communication'))"
      name="communication"
    >
      <el-form-item
        :label="$t('elections.form.emailFromAddress')"
        prop="emailFromAddress"
      >
        <el-input
          v-model="modelValue.emailFromAddress"
          :placeholder="$t('elections.form.emailFromAddressPlaceholder')"
        />
      </el-form-item>

      <el-form-item :label="$t('elections.form.emailFromName')" prop="emailFromName">
        <el-input
          v-model="modelValue.emailFromName"
          :placeholder="$t('elections.form.emailFromNamePlaceholder')"
        />
      </el-form-item>

      <el-form-item :label="$t('elections.form.emailSubject')" prop="emailSubject">
        <el-input
          v-model="modelValue.emailSubject"
          :placeholder="$t('elections.form.emailSubjectPlaceholder')"
        />
      </el-form-item>

      <el-form-item :label="$t('elections.form.emailText')" prop="emailText">
        <el-input
          v-model="modelValue.emailText"
          type="textarea"
          :rows="6"
          :placeholder="$t('elections.form.emailTextPlaceholder')"
        />
      </el-form-item>

      <el-form-item :label="$t('elections.form.smsText')" prop="smsText">
        <el-input
          v-model="modelValue.smsText"
          type="textarea"
          :rows="3"
          :placeholder="$t('elections.form.smsTextPlaceholder')"
        />
      </el-form-item>
    </el-tab-pane>

    <el-tab-pane
      :label="tabLabel('display-privacy', $t('elections.tabs.displayPrivacy'))"
      name="display-privacy"
    >
      <el-form-item :label="$t('elections.form.showFullReport')">
        <el-switch v-model="modelValue.showFullReport" />
      </el-form-item>

      <el-form-item :label="$t('elections.form.listForPublic')">
        <el-switch v-model="modelValue.listForPublic" />
      </el-form-item>

      <el-form-item :label="$t('elections.form.showAsTest')">
        <el-switch v-model="modelValue.showAsTest" />
      </el-form-item>

      <el-form-item :label="$t('elections.form.hidePreBallotPages')">
        <el-switch v-model="modelValue.hidePreBallotPages" />
      </el-form-item>

      <el-form-item :label="$t('elections.form.maskVotingMethod')">
        <el-switch v-model="modelValue.maskVotingMethod" />
      </el-form-item>
    </el-tab-pane>

    <el-tab-pane
      :label="tabLabel('advanced', $t('elections.tabs.advanced'))"
      name="advanced"
    >
      <el-form-item
        :label="$t('elections.form.electionPasscode')"
        prop="electionPasscode"
      >
        <el-input
          v-model="modelValue.electionPasscode"
          type="password"
          show-password
          :placeholder="$t('elections.form.electionPasscodePlaceholder')"
        />
        <template #help>
          <span class="form-help">{{ $t("elections.form.electionPasscodeHelp") }}</span>
        </template>
      </el-form-item>

      <el-form-item
        :label="$t('elections.form.linkedElection')"
        prop="linkedElectionGuid"
      >
        {{ availableElections.length }}
        {{
          $t("elections.form.linkedElectionCount", { count: availableElections.length })
        }}
        <el-select
          v-model="modelValue.linkedElectionGuid"
          :placeholder="$t('elections.form.linkedElectionPlaceholder')"
          clearable
          filterable
        >
          <el-option
            v-for="election in availableElections.filter((x) => x)"
            :key="election.electionGuid"
            :label="election.name"
            :value="election.electionGuid"
          />
        </el-select>
        <template #help>
          <span class="form-help">{{ $t("elections.form.linkedElectionHelp") }}</span>
        </template>
      </el-form-item>

      <el-form-item
        :label="$t('elections.form.linkedElectionKind')"
        prop="linkedElectionKind"
      >
        <el-select
          v-model="modelValue.linkedElectionKind"
          :placeholder="$t('elections.form.linkedElectionKindPlaceholder')"
        >
          <el-option label="Tie-break" value="TB" />
          <el-option label="Run-off" value="RO" />
        </el-select>
      </el-form-item>

      <el-form-item :label="$t('elections.form.flags')" prop="flags">
        <el-input
          v-model="modelValue.flags"
          type="textarea"
          :rows="4"
          :placeholder="$t('elections.form.flagsPlaceholder')"
        />
        <template #help>
          <span class="form-help">{{ $t("elections.form.flagsHelp") }}</span>
        </template>
      </el-form-item>
    </el-tab-pane>
  </el-tabs>
</template>

<script setup lang="ts">
import { onMounted, ref } from "vue";
import type {
  CreateElectionDto,
  UpdateElectionDto,
  ElectionSummaryDto,
} from "../../types";

interface Props {
  modelValue: CreateElectionDto | UpdateElectionDto;
  availableElections: ElectionSummaryDto[];
  formRef?: any;
}

const props = defineProps<Props>();
defineEmits<
  (e: "update:modelValue", value: CreateElectionDto | UpdateElectionDto) => void
>();

onMounted(() => {
  console.log("ElectionFormTabs mounted with props:", {
    modelValue: props.modelValue,
    availableElections: props.availableElections,
    formRef: props.formRef,
  });
});

const activeTab = ref("basic");

// Map tab names to their form fields
const tabFields: Record<string, string[]> = {
  basic: ["name", "dateOfElection", "convenor", "electionType", "electionMode"],
  rules: ["numberToElect", "numberExtra", "canVote", "canReceive"],
  "voting-methods": ["customMethods", "votingMethods"],
  "online-voting": [
    "onlineWhenOpen",
    "onlineWhenClose",
    "onlineCloseIsEstimate",
    "onlineSelectionProcess",
    "onlineAnnounced",
  ],
  communication: [
    "emailFromAddress",
    "emailFromName",
    "emailSubject",
    "emailText",
    "smsText",
  ],
  "display-privacy": [
    "showFullReport",
    "listForPublic",
    "showAsTest",
    "hidePreBallotPages",
    "maskVotingMethod",
  ],
  advanced: ["electionPasscode", "linkedElectionGuid", "linkedElectionKind", "flags"],
};

function tabHasError(tab: string) {
  // Defensive: formRef may be undefined or not a ref
  const formRef = props.formRef;
  const form =
    formRef && typeof formRef === "object" && "value" in formRef
      ? formRef.value
      : undefined;
  if (!form || !form.fields) {
    return false;
  }
  const fields = tabFields[tab] || [];
  return form.fields.some(
    (f: any) => fields.includes(f.prop) && f.validateState === "error"
  );
}

function tabLabel(tab: string, label: string) {
  return tabHasError(tab) ? `${label} ` + '<span class="tab-error-dot"></span>' : label;
}
</script>

<style lang="less" scoped>
.form-help {
  font-size: 12px;
  color: #909399;
  display: block;
  margin-top: 4px;
}

:deep(.el-tabs__content) {
  padding: 20px;
}

.tab-error-dot {
  display: inline-block;
  width: 8px;
  height: 8px;
  background: #f56c6c;
  border-radius: 50%;
  margin-left: 6px;
  vertical-align: middle;
}
</style>
