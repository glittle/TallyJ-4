<script setup lang="ts">
import { computed } from "vue";
import type {
  AllCanReceiveReport,
  AllNonEligibleReport,
  ChangedPeopleReport,
  FlagsReport,
  VoterEmailsReport,
  VotersByAreaReport,
  VotersByLocationAreaReport,
  VotersByLocationReport,
  VotersOnlineReport,
  VotersReport,
} from "@/types";
import {
  formatReportDate,
  formatReportDateTime,
} from "@/utils/reportFormatters";

const props = defineProps<{
  selectedReport: string;
  selectedReportName: string;
  reportData: unknown;
}>();

const formatDate = formatReportDate;
const formatDateTime = formatReportDateTime;

const allCanReceiveData = computed(
  () => props.reportData as AllCanReceiveReport | null,
);
const votersData = computed(() => props.reportData as VotersReport | null);
const flagsData = computed(() => props.reportData as FlagsReport | null);
const votersOnlineData = computed(
  () => props.reportData as VotersOnlineReport | null,
);
const votersByAreaData = computed(
  () => props.reportData as VotersByAreaReport | null,
);
const votersByLocationData = computed(
  () => props.reportData as VotersByLocationReport | null,
);
const votersByLocationAreaData = computed(
  () => props.reportData as VotersByLocationAreaReport | null,
);
const changedPeopleData = computed(
  () => props.reportData as ChangedPeopleReport | null,
);
const allNonEligibleData = computed(
  () => props.reportData as AllNonEligibleReport | null,
);
const voterEmailsData = computed(
  () => props.reportData as VoterEmailsReport | null,
);
const selectedReport = computed(() => props.selectedReport);
const selectedReportName = computed(() => props.selectedReportName);
</script>

<template>
  <div class="reporting-voter-reports">
    v-if="selectedReport === 'AllCanReceive' && allCanReceiveData"
    class="report-generic" >
    <h2>{{ selectedReportName }}</h2>
    <div class="report-meta">
      <div>{{ allCanReceiveData.electionName }}</div>
      <div>{{ formatDate(allCanReceiveData.dateOfElection) }}</div>
    </div>
    <p>
      {{
        $t("reporting.totalVoters", {
          count: allCanReceiveData.people.length,
        })
      }}
    </p>
    <div class="name-columns">
      <div
        v-for="(name, i) in allCanReceiveData.people"
        :key="i"
        class="name-entry"
      >
        {{ name }}
      </div>
    </div>
  </div>

  <!-- Voters (Participation) -->
  <div v-if="selectedReport === 'Voters' && votersData" class="report-generic">
    <h2>{{ selectedReportName }}</h2>
    <div class="report-meta">
      <div>{{ votersData.electionName }}</div>
      <div>{{ formatDate(votersData.dateOfElection) }}</div>
    </div>
    <p>
      {{ $t("reporting.totalVoters", { count: votersData.totalCount }) }}
    </p>
    <table class="data-table">
      <thead>
        <tr>
          <th>{{ $t("reporting.name") }}</th>
          <th>{{ $t("reporting.votingMethod") }}</th>
          <th>{{ $t("reporting.bahaiId") }}</th>
          <th v-if="votersData.hasMultipleLocations">
            {{ $t("reporting.location") }}
          </th>
          <th>{{ $t("reporting.registrationTime") }}</th>
          <th>{{ $t("reporting.teller1") }}</th>
          <th>{{ $t("reporting.teller2") }}</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="(p, i) in votersData.people" :key="i">
          <td>{{ p.personName }}</td>
          <td>{{ p.votingMethod }}</td>
          <td>{{ p.bahaiId }}</td>
          <td v-if="votersData.hasMultipleLocations">{{ p.location }}</td>
          <td>{{ formatDateTime(p.registrationTime) }}</td>
          <td>{{ p.teller1 }}</td>
          <td>{{ p.teller2 }}</td>
        </tr>
      </tbody>
    </table>
  </div>

  <!-- Flags (Attendance Checklists) -->
  <div v-if="selectedReport === 'Flags' && flagsData" class="report-generic">
    <h2>{{ selectedReportName }}</h2>
    <div class="report-meta">
      <div>{{ flagsData.electionName }}</div>
      <div>{{ formatDate(flagsData.dateOfElection) }}</div>
    </div>
    <div v-if="flagsData.flagNames.length === 0" class="empty-msg">
      {{ $t("reporting.noFlags") }}
    </div>
    <table v-else class="data-table">
      <thead>
        <tr>
          <th>{{ $t("reporting.name") }}</th>
          <th v-if="flagsData.hasMultipleLocations">
            {{ $t("reporting.location") }}
          </th>
          <th v-for="f in flagsData.flagNames" :key="f">{{ f }}</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="p in flagsData.people" :key="p.rowId">
          <td>{{ p.personName }}</td>
          <td v-if="flagsData.hasMultipleLocations">{{ p.location }}</td>
          <td
            v-for="(f, fi) in flagsData.flagNames"
            :key="fi"
            class="flag-cell"
          >
            {{ p.flags.includes(f) ? "✓" : "" }}
          </td>
        </tr>
      </tbody>
    </table>
  </div>

  <!-- Voters Online -->
  <div
    v-if="selectedReport === 'VotersOnline' && votersOnlineData"
    class="report-generic"
  >
    <h2>{{ selectedReportName }}</h2>
    <div class="report-meta">
      <div>{{ votersOnlineData.electionName }}</div>
      <div>{{ formatDate(votersOnlineData.dateOfElection) }}</div>
    </div>
    <table class="data-table">
      <thead>
        <tr>
          <th>{{ $t("reporting.name") }}</th>
          <th>{{ $t("reporting.votingMethod") }}</th>
          <th>{{ $t("reporting.onlineStatus") }}</th>
          <th>{{ $t("reporting.whenStatus") }}</th>
          <th>{{ $t("reporting.email") }}</th>
          <th>{{ $t("reporting.whenEmail") }}</th>
          <th>{{ $t("reporting.phone") }}</th>
          <th>{{ $t("reporting.whenPhone") }}</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="p in votersOnlineData.people" :key="p.personId">
          <td>{{ p.fullName }}</td>
          <td>{{ p.votingMethodDisplay }}</td>
          <td>{{ p.status }}</td>
          <td>{{ formatDateTime(p.whenStatus) }}</td>
          <td>{{ p.email }}</td>
          <td>{{ formatDateTime(p.whenEmail) }}</td>
          <td>{{ p.phone }}</td>
          <td>{{ formatDateTime(p.whenPhone) }}</td>
        </tr>
      </tbody>
    </table>
  </div>

  <!-- Voters By Area -->
  <div
    v-if="selectedReport === 'VotersByArea' && votersByAreaData"
    class="report-generic"
  >
    <h2>{{ selectedReportName }}</h2>
    <div class="report-meta">
      <div>{{ votersByAreaData.electionName }}</div>
      <div>{{ formatDate(votersByAreaData.dateOfElection) }}</div>
    </div>
    <table class="data-table">
      <thead>
        <tr>
          <th>{{ $t("reporting.area") }}</th>
          <th>{{ $t("reporting.totalEligible") }}</th>
          <th>{{ $t("reporting.voted") }}</th>
          <th>{{ $t("reporting.inPerson") }}</th>
          <th>{{ $t("reporting.mailedIn") }}</th>
          <th>{{ $t("reporting.droppedOff") }}</th>
          <th>{{ $t("reporting.calledIn") }}</th>
          <th v-if="votersByAreaData.custom1Name">
            {{ votersByAreaData.custom1Name }}
          </th>
          <th v-if="votersByAreaData.custom2Name">
            {{ votersByAreaData.custom2Name }}
          </th>
          <th v-if="votersByAreaData.custom3Name">
            {{ votersByAreaData.custom3Name }}
          </th>
          <th>{{ $t("reporting.onlineKiosk") }}</th>
          <th>{{ $t("reporting.imported") }}</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="a in votersByAreaData.areas" :key="a.areaName">
          <td>{{ a.areaName }}</td>
          <td class="num">{{ a.totalEligible }}</td>
          <td class="num">{{ a.voted }}</td>
          <td class="num">{{ a.inPerson }}</td>
          <td class="num">{{ a.mailedIn }}</td>
          <td class="num">{{ a.droppedOff }}</td>
          <td class="num">{{ a.calledIn }}</td>
          <td v-if="votersByAreaData.custom1Name" class="num">
            {{ a.custom1 }}
          </td>
          <td v-if="votersByAreaData.custom2Name" class="num">
            {{ a.custom2 }}
          </td>
          <td v-if="votersByAreaData.custom3Name" class="num">
            {{ a.custom3 }}
          </td>
          <td class="num">{{ a.online + a.onlineKiosk }}</td>
          <td class="num">{{ a.imported }}</td>
        </tr>
      </tbody>
      <tfoot>
        <tr class="total-row">
          <td>{{ $t("reporting.total") }}</td>
          <td class="num">{{ votersByAreaData.total.totalEligible }}</td>
          <td class="num">{{ votersByAreaData.total.voted }}</td>
          <td class="num">{{ votersByAreaData.total.inPerson }}</td>
          <td class="num">{{ votersByAreaData.total.mailedIn }}</td>
          <td class="num">{{ votersByAreaData.total.droppedOff }}</td>
          <td class="num">{{ votersByAreaData.total.calledIn }}</td>
          <td v-if="votersByAreaData.custom1Name" class="num">
            {{ votersByAreaData.total.custom1 }}
          </td>
          <td v-if="votersByAreaData.custom2Name" class="num">
            {{ votersByAreaData.total.custom2 }}
          </td>
          <td v-if="votersByAreaData.custom3Name" class="num">
            {{ votersByAreaData.total.custom3 }}
          </td>
          <td class="num">
            {{
              votersByAreaData.total.online + votersByAreaData.total.onlineKiosk
            }}
          </td>
          <td class="num">{{ votersByAreaData.total.imported }}</td>
        </tr>
      </tfoot>
    </table>
  </div>

  <!-- Voters By Location -->
  <div
    v-if="selectedReport === 'VotersByLocation' && votersByLocationData"
    class="report-generic"
  >
    <h2>{{ selectedReportName }}</h2>
    <div class="report-meta">
      <div>{{ votersByLocationData.electionName }}</div>
      <div>{{ formatDate(votersByLocationData.dateOfElection) }}</div>
    </div>
    <table class="data-table">
      <thead>
        <tr>
          <th>{{ $t("reporting.location") }}</th>
          <th>{{ $t("reporting.totalVotersHeader") }}</th>
          <th>{{ $t("reporting.inPerson") }}</th>
          <th>{{ $t("reporting.mailedIn") }}</th>
          <th>{{ $t("reporting.droppedOff") }}</th>
          <th>{{ $t("reporting.calledIn") }}</th>
          <th v-if="votersByLocationData.custom1Name">
            {{ votersByLocationData.custom1Name }}
          </th>
          <th v-if="votersByLocationData.custom2Name">
            {{ votersByLocationData.custom2Name }}
          </th>
          <th v-if="votersByLocationData.custom3Name">
            {{ votersByLocationData.custom3Name }}
          </th>
          <th>{{ $t("reporting.onlineKiosk") }}</th>
          <th>{{ $t("reporting.imported") }}</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="l in votersByLocationData.locations" :key="l.locationName">
          <td>{{ l.locationName }}</td>
          <td class="num">{{ l.totalVoters }}</td>
          <td class="num">{{ l.inPerson }}</td>
          <td class="num">{{ l.mailedIn }}</td>
          <td class="num">{{ l.droppedOff }}</td>
          <td class="num">{{ l.calledIn }}</td>
          <td v-if="votersByLocationData.custom1Name" class="num">
            {{ l.custom1 }}
          </td>
          <td v-if="votersByLocationData.custom2Name" class="num">
            {{ l.custom2 }}
          </td>
          <td v-if="votersByLocationData.custom3Name" class="num">
            {{ l.custom3 }}
          </td>
          <td class="num">{{ l.online + l.onlineKiosk }}</td>
          <td class="num">{{ l.imported }}</td>
        </tr>
      </tbody>
      <tfoot>
        <tr class="total-row">
          <td>{{ $t("reporting.total") }}</td>
          <td class="num">
            {{ votersByLocationData.total.totalVoters }}
          </td>
          <td class="num">{{ votersByLocationData.total.inPerson }}</td>
          <td class="num">{{ votersByLocationData.total.mailedIn }}</td>
          <td class="num">{{ votersByLocationData.total.droppedOff }}</td>
          <td class="num">{{ votersByLocationData.total.calledIn }}</td>
          <td v-if="votersByLocationData.custom1Name" class="num">
            {{ votersByLocationData.total.custom1 }}
          </td>
          <td v-if="votersByLocationData.custom2Name" class="num">
            {{ votersByLocationData.total.custom2 }}
          </td>
          <td v-if="votersByLocationData.custom3Name" class="num">
            {{ votersByLocationData.total.custom3 }}
          </td>
          <td class="num">
            {{
              votersByLocationData.total.online +
              votersByLocationData.total.onlineKiosk
            }}
          </td>
          <td class="num">{{ votersByLocationData.total.imported }}</td>
        </tr>
      </tfoot>
    </table>
  </div>

  <!-- Voters By Location Area -->
  <div
    v-if="selectedReport === 'VotersByLocationArea' && votersByLocationAreaData"
    class="report-generic"
  >
    <h2>{{ selectedReportName }}</h2>
    <div class="report-meta">
      <div>{{ votersByLocationAreaData.electionName }}</div>
      <div>{{ formatDate(votersByLocationAreaData.dateOfElection) }}</div>
    </div>
    <div
      v-for="loc in votersByLocationAreaData.locations"
      :key="loc.locationName"
      class="loc-area-group"
    >
      <h4>{{ loc.locationName }} ({{ loc.totalCount }})</h4>
      <table class="data-table compact">
        <thead>
          <tr>
            <th>{{ $t("reporting.area") }}</th>
            <th>{{ $t("reporting.count") }}</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="a in loc.areas" :key="a.areaName">
            <td>{{ a.areaName }}</td>
            <td class="num">{{ a.count }}</td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>

  <!-- Changed People -->
  <div
    v-if="selectedReport === 'ChangedPeople' && changedPeopleData"
    class="report-generic"
  >
    <h2>{{ selectedReportName }}</h2>
    <div class="report-meta">
      <div>{{ changedPeopleData.electionName }}</div>
      <div>{{ formatDate(changedPeopleData.dateOfElection) }}</div>
    </div>
    <div v-if="changedPeopleData.people.length === 0" class="empty-msg">
      {{ $t("reporting.noChanges") }}
    </div>
    <table v-else class="data-table">
      <thead>
        <tr>
          <th>{{ $t("reporting.change") }}</th>
          <th>{{ $t("reporting.firstName") }}</th>
          <th>{{ $t("reporting.lastName") }}</th>
          <th>{{ $t("reporting.bahaiId") }}</th>
          <th>{{ $t("reporting.canVote") }}</th>
          <th>{{ $t("reporting.canReceiveVotes") }}</th>
          <th>{{ $t("reporting.eligibilityReason") }}</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="(p, i) in changedPeopleData.people" :key="i">
          <td>{{ p.change }}</td>
          <td>{{ p.firstName }}</td>
          <td>{{ p.lastName }}</td>
          <td>{{ p.bahaiId }}</td>
          <td>
            {{ p.canVote ? $t("reporting.yes") : $t("reporting.no") }}
          </td>
          <td>
            {{ p.canReceiveVotes ? $t("reporting.yes") : $t("reporting.no") }}
          </td>
          <td>{{ p.invalidReasonDesc }}</td>
        </tr>
      </tbody>
    </table>
  </div>

  <!-- All Non-Eligible -->
  <div
    v-if="selectedReport === 'AllNonEligible' && allNonEligibleData"
    class="report-generic"
  >
    <h2>{{ selectedReportName }}</h2>
    <div class="report-meta">
      <div>{{ allNonEligibleData.electionName }}</div>
      <div>{{ formatDate(allNonEligibleData.dateOfElection) }}</div>
    </div>
    <div v-if="allNonEligibleData.people.length === 0" class="empty-msg">
      {{ $t("reporting.noNonEligible") }}
    </div>
    <table v-else class="data-table">
      <thead>
        <tr>
          <th>{{ $t("reporting.name") }}</th>
          <th>{{ $t("reporting.canVote") }}</th>
          <th>{{ $t("reporting.canReceiveVotes") }}</th>
          <th>{{ $t("reporting.eligibilityReason") }}</th>
          <th>{{ $t("reporting.votingMethod") }}</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="(p, i) in allNonEligibleData.people" :key="i">
          <td>{{ p.personName }}</td>
          <td>
            {{ p.canVote ? $t("reporting.yes") : $t("reporting.no") }}
          </td>
          <td>
            {{ p.canReceiveVotes ? $t("reporting.yes") : $t("reporting.no") }}
          </td>
          <td>{{ p.invalidReasonDesc }}</td>
          <td>{{ p.votingMethod }}</td>
        </tr>
      </tbody>
    </table>
  </div>

  <!-- Voter Emails -->
  <div
    v-if="selectedReport === 'VoterEmails' && voterEmailsData"
    class="report-generic"
  >
    <h2>{{ selectedReportName }}</h2>
    <div class="report-meta">
      <div>{{ voterEmailsData.electionName }}</div>
      <div>{{ formatDate(voterEmailsData.dateOfElection) }}</div>
    </div>
    <div v-if="voterEmailsData.people.length === 0" class="empty-msg">
      {{ $t("reporting.noEmails") }}
    </div>
    <table v-else class="data-table">
      <thead>
        <tr>
          <th>{{ $t("reporting.name") }}</th>
          <th>{{ $t("reporting.bahaiId") }}</th>
          <th>{{ $t("reporting.email") }}</th>
          <th>{{ $t("reporting.phone") }}</th>
          <th>{{ $t("reporting.canVote") }}</th>
          <th>{{ $t("reporting.votingMethod") }}</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="(p, i) in voterEmailsData.people" :key="i">
          <td>{{ p.fullName }}</td>
          <td>{{ p.bahaiId }}</td>
          <td>{{ p.email }}</td>
          <td>{{ p.phone }}</td>
          <td>
            {{ p.canVote ? $t("reporting.yes") : $t("reporting.no") }}
          </td>
          <td>{{ p.votingMethod }}</td>
        </tr>
      </tbody>
    </table>
  </div>
</template>
