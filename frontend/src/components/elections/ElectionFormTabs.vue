<script setup lang="ts">
import { useLocalStorage } from "@/composables/useLocalStorage";
import { computed } from "vue";
import type {
  CreateElectionDto,
  ElectionSummaryDto,
  UpdateElectionDto,
} from "../../types";

const model = defineModel<CreateElectionDto | UpdateElectionDto>({
  required: true,
});

const props = defineProps<{
  availableElections: ElectionSummaryDto[];
  formRef?: any;
  ballotCount?: number;
}>();

const hasBallotsEntered = computed(() => (props.ballotCount ?? 0) > 0);

const activeTab = useLocalStorage<string>("activeTab", "basic");

// Map tab names to their form fields
const tabFields: Record<string, string[]> = {
  basic: ["name", "dateOfElection", "convenor", "electionType", "electionMode"],
  rules: ["numberToElect", "numberExtra"],
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
    "showAsTest",
    "hidePreBallotPages",
    "maskVotingMethod",
  ],
  advanced: [
    "electionPasscode",
    "linkedElectionGuid",
    "linkedElectionKind",
    "flags",
  ],
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
    (f: any) => fields.includes(f.prop) && f.validateState === "error",
  );
}

function tabLabel(tab: string, label: string) {
  return tabHasError(tab)
    ? `${label} ` + '<span class="tab-error-dot"></span>'
    : label;
}
</script>

<template>
  <el-tabs v-model="activeTab" class="electionFormTabs" type="border-card">
    <el-tab-pane
      :label="tabLabel('basic', $t('elections.tabs.basicInfo'))"
      name="basic"
    >
      <el-form-item :label="$t('elections.form.name')" prop="name">
        <el-input
          v-model="model.name"
          :placeholder="$t('elections.form.namePlaceholder')"
        />
      </el-form-item>

      <el-form-item :label="$t('elections.form.date')" prop="dateOfElection">
        <el-date-picker
          v-model="model.dateOfElection"
          type="date"
          :placeholder="$t('elections.form.datePlaceholder')"
          style="width: 100%"
        />
      </el-form-item>

      <el-form-item :label="$t('elections.form.convenor')" prop="convenor">
        <el-input
          v-model="model.convenor"
          :placeholder="$t('elections.form.convenorPlaceholder')"
        />
      </el-form-item>

      <el-form-item :label="$t('elections.form.type')" prop="electionType">
        <el-select
          v-model="model.electionType"
          :placeholder="$t('elections.form.typePlaceholder')"
          :disabled="hasBallotsEntered"
        >
          <el-option :label="$t('elections.electionTypes.LSA')" value="LSA" />
          <el-option :label="$t('elections.electionTypes.LSA1')" value="LSA1" />
          <el-option :label="$t('elections.electionTypes.LSA2')" value="LSA2" />
          <el-option :label="$t('elections.electionTypes.NSA')" value="NSA" />
          <el-option :label="$t('elections.electionTypes.Con')" value="Con" />
          <el-option :label="$t('elections.electionTypes.Reg')" value="Reg" />
          <el-option :label="$t('elections.electionTypes.Oth')" value="Oth" />
        </el-select>
        <span v-if="hasBallotsEntered" class="locked-hint">{{
          $t("elections.form.lockedByBallots")
        }}</span>
      </el-form-item>

      <el-form-item
        :label="$t('elections.form.electionMode')"
        prop="electionMode"
      >
        <el-select
          v-model="model.electionMode"
          :placeholder="$t('elections.form.modePlaceholder')"
          :disabled="hasBallotsEntered"
        >
          <el-option :label="$t('elections.electionModes.N')" value="N" />
          <el-option :label="$t('elections.electionModes.T')" value="T" />
          <el-option :label="$t('elections.electionModes.B')" value="B" />
        </el-select>
        <span v-if="hasBallotsEntered" class="locked-hint">{{
          $t("elections.form.lockedByBallots")
        }}</span>
      </el-form-item>
    </el-tab-pane>

    <el-tab-pane
      :label="tabLabel('rules', $t('elections.tabs.electionRules'))"
      name="rules"
    >
      <el-form-item
        :label="$t('elections.form.numberToElect')"
        prop="numberToElect"
      >
        <el-input-number
          v-model="model.numberToElect"
          :min="1"
          :max="50"
          :disabled="hasBallotsEntered"
        />
        <span v-if="hasBallotsEntered" class="locked-hint">{{
          $t("elections.form.lockedByBallots")
        }}</span>
      </el-form-item>

      <el-form-item
        :label="$t('elections.form.numberExtra')"
        prop="numberExtra"
      >
        <el-input-number v-model="model.numberExtra" :min="0" :max="20" />
      </el-form-item>
    </el-tab-pane>

    <el-tab-pane
      :label="tabLabel('voting-methods', $t('elections.tabs.votingMethods'))"
      name="voting-methods"
    >
      <el-form-item :label="$t('elections.form.useCallInButton')">
        <el-switch v-model="model.useCallInButton" />
      </el-form-item>

      <el-form-item
        :label="$t('elections.form.customMethods')"
        prop="customMethods"
      >
        <el-input
          v-model="model.customMethods"
          :placeholder="$t('elections.form.customMethodsPlaceholder')"
        />
        <template #help>
          <span class="form-help">{{
            $t("elections.form.customMethodsHelp")
          }}</span>
        </template>
      </el-form-item>

      <el-form-item
        :label="$t('elections.form.votingMethods')"
        prop="votingMethods"
      >
        <el-input
          v-model="model.votingMethods"
          :placeholder="$t('elections.form.votingMethodsPlaceholder')"
        />
        <template #help>
          <span class="form-help">{{
            $t("elections.form.votingMethodsHelp")
          }}</span>
        </template>
      </el-form-item>
    </el-tab-pane>

    <el-tab-pane
      :label="tabLabel('online-voting', $t('elections.tabs.onlineVoting'))"
      name="online-voting"
    >
      <el-form-item
        :label="$t('elections.form.onlineWhenOpen')"
        prop="onlineWhenOpen"
      >
        <el-date-picker
          v-model="model.onlineWhenOpen"
          type="datetime"
          :placeholder="$t('elections.form.onlineWhenOpenPlaceholder')"
          style="width: 100%"
        />
      </el-form-item>

      <el-form-item
        :label="$t('elections.form.onlineWhenClose')"
        prop="onlineWhenClose"
      >
        <el-date-picker
          v-model="model.onlineWhenClose"
          type="datetime"
          :placeholder="$t('elections.form.onlineWhenClosePlaceholder')"
          style="width: 100%"
        />
      </el-form-item>

      <el-form-item :label="$t('elections.form.onlineCloseIsEstimate')">
        <el-switch v-model="model.onlineCloseIsEstimate" />
      </el-form-item>

      <el-form-item
        :label="$t('elections.form.onlineSelectionProcess')"
        prop="onlineSelectionProcess"
      >
        <el-select
          v-model="model.onlineSelectionProcess"
          :placeholder="$t('elections.form.onlineSelectionProcessPlaceholder')"
        >
          <el-option label="Simultaneous" value="S" />
          <el-option label="Sequential" value="Q" />
        </el-select>
      </el-form-item>

      <el-form-item
        :label="$t('elections.form.onlineAnnounced')"
        prop="onlineAnnounced"
      >
        <el-date-picker
          v-model="model.onlineAnnounced"
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
          v-model="model.emailFromAddress"
          :placeholder="$t('elections.form.emailFromAddressPlaceholder')"
        />
      </el-form-item>

      <el-form-item
        :label="$t('elections.form.emailFromName')"
        prop="emailFromName"
      >
        <el-input
          v-model="model.emailFromName"
          :placeholder="$t('elections.form.emailFromNamePlaceholder')"
        />
      </el-form-item>

      <el-form-item
        :label="$t('elections.form.emailSubject')"
        prop="emailSubject"
      >
        <el-input
          v-model="model.emailSubject"
          :placeholder="$t('elections.form.emailSubjectPlaceholder')"
        />
      </el-form-item>

      <el-form-item :label="$t('elections.form.emailText')" prop="emailText">
        <el-input
          v-model="model.emailText"
          type="textarea"
          :rows="6"
          :placeholder="$t('elections.form.emailTextPlaceholder')"
        />
      </el-form-item>

      <el-form-item :label="$t('elections.form.smsText')" prop="smsText">
        <el-input
          v-model="model.smsText"
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
        <el-switch v-model="model.showFullReport" />
      </el-form-item>

      <el-form-item :label="$t('elections.form.showAsTest')">
        <el-switch v-model="model.showAsTest" />
      </el-form-item>

      <el-form-item :label="$t('elections.form.hidePreBallotPages')">
        <el-switch v-model="model.hidePreBallotPages" />
      </el-form-item>

      <el-form-item :label="$t('elections.form.maskVotingMethod')">
        <el-switch v-model="model.maskVotingMethod" />
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
          v-model="model.electionPasscode"
          type="password"
          show-password
          :placeholder="$t('elections.form.electionPasscodePlaceholder')"
        />
        <template #help>
          <span class="form-help">{{
            $t("elections.form.electionPasscodeHelp")
          }}</span>
        </template>
      </el-form-item>

      <el-form-item
        :label="$t('elections.form.linkedElection')"
        prop="linkedElectionGuid"
      >
        {{ availableElections.length }}
        {{
          $t("elections.form.linkedElectionCount", {
            count: availableElections.length,
          })
        }}
        <el-select
          v-model="model.linkedElectionGuid"
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
          <span class="form-help">{{
            $t("elections.form.linkedElectionHelp")
          }}</span>
        </template>
      </el-form-item>

      <el-form-item
        :label="$t('elections.form.linkedElectionKind')"
        prop="linkedElectionKind"
      >
        <el-select
          v-model="model.linkedElectionKind"
          :placeholder="$t('elections.form.linkedElectionKindPlaceholder')"
        >
          <el-option label="Tie-break" value="TB" />
          <el-option label="Run-off" value="RO" />
        </el-select>
      </el-form-item>

      <el-form-item :label="$t('elections.form.flags')" prop="flags">
        <el-input
          v-model="model.flags"
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

<style lang="less">
.electionFormTabs {
  .form-help {
    font-size: 12px;
    color: #909399;
    display: block;
    margin-top: 4px;
  }

  .el-tabs__content {
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

  .locked-hint {
    font-size: 12px;
    color: #909399;
    margin-left: 8px;
  }
}
</style>
