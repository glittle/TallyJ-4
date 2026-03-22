<script setup lang="ts">
import { computed, onMounted, ref } from "vue";
import { useI18n } from "vue-i18n";
import { useRoute, useRouter } from "vue-router";
import { reportService } from "../../services/reportService";
import type {
  AllCanReceiveReport,
  AllNonEligibleReport,
  BallotAlignmentReport,
  BallotsReport,
  BallotsSameReport,
  BallotsSummaryReport,
  ChangedPeopleReport,
  FlagsReport,
  MainReport,
  ReportListItem,
  SpoiledVotesReport,
  VoterEmailsReport,
  VotersByAreaReport,
  VotersByLocationAreaReport,
  VotersByLocationReport,
  VotersOnlineReport,
  VotersReport,
  VotesByNameReport,
  VotesByNumReport,
} from "../../types";

const route = useRoute();
const router = useRouter();
const { t } = useI18n();

const electionGuid = computed(() => route.params.id as string);
const availableReports = ref<ReportListItem[]>([]);
const selectedReport = ref<string>("");
const reportData = ref<unknown>(null);
const loading = ref(false);
const loadingList = ref(true);
const error = ref("");

const ballotReports = computed(() =>
  availableReports.value.filter((r) => r.category === "Ballot Reports"),
);
const voterReports = computed(() =>
  availableReports.value.filter((r) => r.category === "Voter Reports"),
);

const selectedReportName = computed(
  () =>
    availableReports.value.find((r) => r.code === selectedReport.value)?.name ??
    "",
);

onMounted(async () => {
  try {
    availableReports.value = await reportService.getAvailableReports(
      electionGuid.value,
    );
  } catch {
    error.value = t("reporting.error");
  } finally {
    loadingList.value = false;
  }
});

async function selectReport(code: string) {
  if (selectedReport.value === code && reportData.value) {
    return;
  }
  selectedReport.value = code;
  reportData.value = null;
  loading.value = true;
  error.value = "";
  try {
    reportData.value = await reportService.getReport(electionGuid.value, code);
  } catch {
    error.value = t("reporting.error");
  } finally {
    loading.value = false;
  }
}

function goBack() {
  router.back();
}

function printPage() {
  globalThis.print();
}

function formatDate(d?: string) {
  if (!d) {
    return "";
  }
  return new Date(d).toLocaleDateString();
}

function formatDateTime(d?: string) {
  if (!d) {
    return "";
  }
  return new Date(d).toLocaleString();
}

function formatPercent(v: number) {
  return (v / 100).toLocaleString(undefined, {
    style: "percent",
    minimumFractionDigits: 0,
  });
}

const mainData = computed(() => reportData.value as MainReport | null);
const votesByNumData = computed(
  () => reportData.value as VotesByNumReport | null,
);
const votesByNameData = computed(
  () => reportData.value as VotesByNameReport | null,
);
const ballotsData = computed(() => reportData.value as BallotsReport | null);
const spoiledVotesData = computed(
  () => reportData.value as SpoiledVotesReport | null,
);
const alignmentData = computed(
  () => reportData.value as BallotAlignmentReport | null,
);
const ballotsSameData = computed(
  () => reportData.value as BallotsSameReport | null,
);
const ballotsSummaryData = computed(
  () => reportData.value as BallotsSummaryReport | null,
);
const allCanReceiveData = computed(
  () => reportData.value as AllCanReceiveReport | null,
);
const votersData = computed(() => reportData.value as VotersReport | null);
const flagsData = computed(() => reportData.value as FlagsReport | null);
const votersOnlineData = computed(
  () => reportData.value as VotersOnlineReport | null,
);
const votersByAreaData = computed(
  () => reportData.value as VotersByAreaReport | null,
);
const votersByLocationData = computed(
  () => reportData.value as VotersByLocationReport | null,
);
const votersByLocationAreaData = computed(
  () => reportData.value as VotersByLocationAreaReport | null,
);
const changedPeopleData = computed(
  () => reportData.value as ChangedPeopleReport | null,
);
const allNonEligibleData = computed(
  () => reportData.value as AllNonEligibleReport | null,
);
const voterEmailsData = computed(
  () => reportData.value as VoterEmailsReport | null,
);

const isBallotReport = computed(() =>
  ["Ballots", "BallotsOnline", "BallotsImported", "BallotsTied"].includes(
    selectedReport.value,
  ),
);
</script>

<template>
  <div class="reports-page">
    <div class="reports-chooser no-print">
      <div class="chooser-inner">
        <div v-if="ballotReports.length">
          <h3>{{ $t("reporting.ballotReports") }}</h3>
          <ul>
            <li
              v-for="r in ballotReports"
              :key="r.code"
              :class="{ active: selectedReport === r.code }"
            >
              <a href="#" @click.prevent="selectReport(r.code)">{{ r.name }}</a>
            </li>
          </ul>
        </div>
        <div v-if="voterReports.length">
          <h3>{{ $t("reporting.voterReports") }}</h3>
          <ul>
            <li
              v-for="r in voterReports"
              :key="r.code"
              :class="{ active: selectedReport === r.code }"
            >
              <a href="#" @click.prevent="selectReport(r.code)">{{ r.name }}</a>
            </li>
          </ul>
        </div>
        <div class="chooser-actions">
          <el-button type="primary" @click="printPage">
            {{ $t("reporting.print") }}
          </el-button>
          <p class="print-hint">{{ $t("reporting.printHint") }}</p>
        </div>
        <div class="chooser-actions">
          <el-button @click="goBack">{{ $t("common.back") }}</el-button>
        </div>
      </div>
    </div>

    <div class="reports-panel">
      <div v-if="!selectedReport" class="placeholder">
        {{ $t("reporting.selectReport") }}
      </div>

      <div v-else-if="loading" class="placeholder">
        <el-skeleton :rows="8" animated />
      </div>

      <div v-else-if="error" class="placeholder error-text">
        {{ error }}
      </div>

      <div v-else-if="reportData" class="report-content">
        <!-- Main Election Report -->
        <div v-if="selectedReport === 'Main' && mainData" class="report-main">
          <h2>{{ mainData.electionName }}</h2>
          <div class="report-meta">
            <div v-if="mainData.convenor">
              {{ $t("reporting.convenor") }}: {{ mainData.convenor }}
            </div>
            <div>
              {{ $t("reporting.dateOfElection") }}:
              {{ formatDate(mainData.dateOfElection) }}
            </div>
          </div>

          <table class="info-table">
            <tr>
              <td>{{ $t("reporting.numEligibleToVote") }}</td>
              <td class="num">
                {{ mainData.numEligibleToVote.toLocaleString() }}
              </td>
            </tr>
            <tr>
              <td>{{ $t("reporting.voted") }}</td>
              <td class="num">
                {{ mainData.sumOfEnvelopesCollected.toLocaleString() }}
              </td>
            </tr>
            <tr
              v-if="
                mainData.sumOfEnvelopesCollected !==
                mainData.numBallotsWithManual
              "
              class="warn-row"
            >
              <td>Ballots received ≠ Voted</td>
              <td class="num">
                {{ mainData.numBallotsWithManual.toLocaleString() }}
              </td>
            </tr>
            <tr class="spacer">
              <td colspan="2"></td>
            </tr>
            <tr>
              <td>{{ $t("reporting.percentParticipation") }}</td>
              <td class="num">
                {{ formatPercent(mainData.percentParticipation) }}
              </td>
            </tr>
            <tr class="spacer">
              <td colspan="2"></td>
            </tr>
            <tr>
              <td>Did not vote</td>
              <td class="num">
                {{
                  (
                    mainData.numEligibleToVote -
                    mainData.sumOfEnvelopesCollected
                  ).toLocaleString()
                }}
              </td>
            </tr>
            <tr class="divider">
              <td colspan="2"><div></div></td>
            </tr>
            <tr>
              <td>Ballots cast in person</td>
              <td class="num">
                {{ mainData.inPersonBallots.toLocaleString() }}
              </td>
            </tr>
            <tr>
              <td>Ballots received by mail</td>
              <td class="num">
                {{ mainData.mailedInBallots.toLocaleString() }}
              </td>
            </tr>
            <tr>
              <td>Ballots hand-delivered</td>
              <td class="num">
                {{ mainData.droppedOffBallots.toLocaleString() }}
              </td>
            </tr>
            <tr v-if="mainData.onlineBallots > 0">
              <td>Ballots cast online</td>
              <td class="num">{{ mainData.onlineBallots.toLocaleString() }}</td>
            </tr>
            <tr v-if="mainData.importedBallots > 0">
              <td>Ballots imported</td>
              <td class="num">
                {{ mainData.importedBallots.toLocaleString() }}
              </td>
            </tr>
            <tr v-if="mainData.calledInBallots > 0">
              <td>Ballots phoned-in</td>
              <td class="num">
                {{ mainData.calledInBallots.toLocaleString() }}
              </td>
            </tr>
            <tr v-if="mainData.custom1Ballots > 0">
              <td>Ballots: {{ mainData.custom1Name }}</td>
              <td class="num">
                {{ mainData.custom1Ballots.toLocaleString() }}
              </td>
            </tr>
            <tr v-if="mainData.custom2Ballots > 0">
              <td>Ballots: {{ mainData.custom2Name }}</td>
              <td class="num">
                {{ mainData.custom2Ballots.toLocaleString() }}
              </td>
            </tr>
            <tr v-if="mainData.custom3Ballots > 0">
              <td>Ballots: {{ mainData.custom3Name }}</td>
              <td class="num">
                {{ mainData.custom3Ballots.toLocaleString() }}
              </td>
            </tr>
            <tr class="divider">
              <td colspan="2"><div></div></td>
            </tr>
            <tr>
              <td>{{ $t("reporting.spoiledBallots") }}</td>
              <td class="num">
                {{ mainData.spoiledBallots.toLocaleString() }}
              </td>
            </tr>
            <tr
              v-for="sb in mainData.spoiledBallotReasons"
              :key="sb.reason"
              class="sub-row"
            >
              <td colspan="2">{{ sb.ballotCount }} - {{ sb.reason }}</td>
            </tr>
            <tr class="divider">
              <td colspan="2"><div></div></td>
            </tr>
            <tr>
              <td>{{ $t("reporting.spoiledVotes") }}</td>
              <td class="num">{{ mainData.spoiledVotes.toLocaleString() }}</td>
            </tr>
            <tr
              v-for="sv in mainData.spoiledVoteReasons"
              :key="sv.reason"
              class="sub-row"
            >
              <td colspan="2">{{ sv.voteCount }} - {{ sv.reason }}</td>
            </tr>
          </table>

          <div class="page-break"></div>
          <h3>{{ $t("reporting.electedPersons") }}</h3>
          <table class="data-table">
            <thead>
              <tr>
                <th>#</th>
                <th>{{ $t("reporting.name") }}</th>
                <th>{{ $t("reporting.bahaiId") }}</th>
                <th>
                  {{ $t("reporting.votes")
                  }}{{ mainData.hasTies ? " / Tie Break" : "" }}
                </th>
              </tr>
            </thead>
            <tbody>
              <tr
                v-for="p in mainData.elected"
                :key="p.rank + p.name"
                :class="'section-' + p.section"
              >
                <td>{{ p.rank }}</td>
                <td>{{ p.name }}</td>
                <td>{{ p.bahaiId }}</td>
                <td>{{ p.voteCountDisplay }}</td>
              </tr>
            </tbody>
          </table>
        </div>

        <!-- Votes By Number -->
        <div
          v-if="selectedReport === 'VotesByNum' && votesByNumData"
          class="report-votes"
        >
          <h2>{{ selectedReportName }}</h2>
          <div class="report-meta">
            <div>{{ votesByNumData.electionName }}</div>
            <div>{{ formatDate(votesByNumData.dateOfElection) }}</div>
          </div>
          <div class="votes-list">
            <template v-for="(p, i) in votesByNumData.people" :key="i">
              <div v-if="p.showBreak" class="section-break"></div>
              <div class="vote-person" :class="{ elected: p.section === 'E' }">
                <span class="vote-count"
                  >{{ p.voteCount
                  }}{{ p.tieBreakRequired ? " / " + p.tieBreakCount : "" }} -
                </span>
                <span class="vote-name">{{ p.personName }}</span>
              </div>
            </template>
          </div>
        </div>

        <!-- Votes By Name -->
        <div
          v-if="selectedReport === 'VotesByName' && votesByNameData"
          class="report-votes"
        >
          <h2>{{ selectedReportName }}</h2>
          <div class="report-meta">
            <div>{{ votesByNameData.electionName }}</div>
            <div>{{ formatDate(votesByNameData.dateOfElection) }}</div>
          </div>
          <div class="votes-list">
            <template v-for="(p, i) in votesByNameData.people" :key="i">
              <div v-if="p.showBreak" class="section-break"></div>
              <div class="vote-person" :class="{ elected: p.section === 'E' }">
                <span class="vote-name">{{ p.personName }}</span>
                <span class="vote-count">
                  - {{ p.voteCount
                  }}{{
                    p.tieBreakRequired ? " / " + p.tieBreakCount : ""
                  }}</span
                >
              </div>
            </template>
          </div>
        </div>

        <!-- Ballots (All, Online, Imported, Tied) -->
        <div v-if="isBallotReport && ballotsData" class="report-ballots">
          <h2>{{ selectedReportName }}</h2>
          <div class="report-meta">
            <div>{{ ballotsData.electionName }}</div>
            <div>{{ formatDate(ballotsData.dateOfElection) }}</div>
          </div>
          <div v-if="ballotsData.ballots.length === 0" class="empty-msg">
            {{ $t("reporting.noBallots") }}
          </div>
          <table v-else class="ballots-table">
            <tbody>
              <tr
                v-for="b in ballotsData.ballots"
                :key="b.ballotId"
                :class="{ spoiled: b.spoiled }"
              >
                <td class="ballot-id">
                  <div class="ballot-code">{{ b.ballotCode }}</div>
                  <div v-if="b.location" class="ballot-loc">
                    {{ b.location }}
                  </div>
                  <div v-if="b.spoiled" class="ballot-status">
                    {{ b.statusCode }}
                  </div>
                </td>
                <td class="ballot-votes">
                  <span
                    v-for="(v, vi) in b.votes"
                    :key="vi"
                    class="vote-entry"
                    :class="{ 'vote-spoiled': v.spoiled }"
                  >
                    <template v-if="ballotsData.isSingleNameElection">
                      <span class="sne-count">{{
                        v.singleNameElectionCount
                      }}</span>
                    </template>
                    {{ v.personName }}
                    <span v-if="v.invalidReasonDesc" class="invalid-reason">{{
                      v.invalidReasonDesc
                    }}</span>
                  </span>
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <!-- Spoiled Votes -->
        <div
          v-if="selectedReport === 'SpoiledVotes' && spoiledVotesData"
          class="report-generic"
        >
          <h2>{{ selectedReportName }}</h2>
          <div class="report-meta">
            <div>{{ spoiledVotesData.electionName }}</div>
            <div>{{ formatDate(spoiledVotesData.dateOfElection) }}</div>
          </div>
          <div v-if="spoiledVotesData.people.length === 0" class="empty-msg">
            {{ $t("reporting.noSpoiledVotes") }}
          </div>
          <table v-else class="data-table">
            <thead>
              <tr>
                <th>{{ $t("reporting.name") }}</th>
                <th>{{ $t("reporting.votes") }}</th>
                <th>{{ $t("reporting.reason") }}</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="(p, i) in spoiledVotesData.people" :key="i">
                <td>{{ p.personName }}</td>
                <td class="num">{{ p.voteCount }}</td>
                <td>{{ p.invalidReasonDesc }}</td>
              </tr>
            </tbody>
          </table>
        </div>

        <!-- Ballot Alignment -->
        <div
          v-if="selectedReport === 'BallotAlignment' && alignmentData"
          class="report-generic"
        >
          <h2>{{ selectedReportName }}</h2>
          <div class="report-meta">
            <div>{{ alignmentData.electionName }}</div>
            <div>{{ formatDate(alignmentData.dateOfElection) }}</div>
          </div>
          <table class="data-table">
            <thead>
              <tr>
                <th>{{ $t("reporting.matchingNames") }}</th>
                <th>{{ $t("reporting.numBallots") }}</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="(r, i) in alignmentData.rows" :key="i">
                <td class="num">
                  {{ r.matchingNames }} / {{ alignmentData.numToElect }}
                </td>
                <td class="num">{{ r.ballotCount }}</td>
              </tr>
            </tbody>
          </table>
        </div>

        <!-- Duplicate Ballots -->
        <div
          v-if="selectedReport === 'BallotsSame' && ballotsSameData"
          class="report-ballots"
        >
          <h2>{{ selectedReportName }}</h2>
          <div class="report-meta">
            <div>{{ ballotsSameData.electionName }}</div>
            <div>{{ formatDate(ballotsSameData.dateOfElection) }}</div>
          </div>
          <div v-if="ballotsSameData.groups.length === 0" class="empty-msg">
            {{ $t("reporting.noDuplicates") }}
          </div>
          <div
            v-for="g in ballotsSameData.groups"
            :key="g.groupNumber"
            class="dup-group"
          >
            <h4>{{ $t("reporting.groupN", { n: g.groupNumber }) }}</h4>
            <table class="ballots-table">
              <tbody>
                <tr
                  v-for="b in g.ballots"
                  :key="b.ballotId"
                  :class="{ spoiled: b.spoiled }"
                >
                  <td class="ballot-id">
                    <div class="ballot-code">{{ b.ballotCode }}</div>
                    <div v-if="b.location" class="ballot-loc">
                      {{ b.location }}
                    </div>
                  </td>
                  <td class="ballot-votes">
                    <span
                      v-for="(v, vi) in b.votes"
                      :key="vi"
                      class="vote-entry"
                      :class="{ 'vote-spoiled': v.spoiled }"
                    >
                      {{ v.personName }}
                    </span>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>

        <!-- Ballots Summary -->
        <div
          v-if="selectedReport === 'BallotsSummary' && ballotsSummaryData"
          class="report-generic"
        >
          <h2>{{ selectedReportName }}</h2>
          <div class="report-meta">
            <div>{{ ballotsSummaryData.electionName }}</div>
            <div>{{ formatDate(ballotsSummaryData.dateOfElection) }}</div>
          </div>
          <table class="data-table">
            <thead>
              <tr>
                <th>{{ $t("reporting.ballotCode") }}</th>
                <th>{{ $t("reporting.location") }}</th>
                <th>{{ $t("reporting.status") }}</th>
                <th>{{ $t("reporting.spoiledVotes") }}</th>
                <th>{{ $t("reporting.teller1") }}</th>
                <th>{{ $t("reporting.teller2") }}</th>
              </tr>
            </thead>
            <tbody>
              <tr
                v-for="b in ballotsSummaryData.ballots"
                :key="b.ballotId"
                :class="{ spoiled: b.spoiled }"
              >
                <td>{{ b.ballotCode }}</td>
                <td>{{ b.location }}</td>
                <td>{{ b.statusCode }}</td>
                <td class="num">{{ b.spoiledVotes }}</td>
                <td>{{ b.teller1 }}</td>
                <td>{{ b.teller2 }}</td>
              </tr>
            </tbody>
          </table>
        </div>

        <!-- All Can Receive -->
        <div
          v-if="selectedReport === 'AllCanReceive' && allCanReceiveData"
          class="report-generic"
        >
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
        <div
          v-if="selectedReport === 'Voters' && votersData"
          class="report-generic"
        >
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
        <div
          v-if="selectedReport === 'Flags' && flagsData"
          class="report-generic"
        >
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
                    votersByAreaData.total.online +
                    votersByAreaData.total.onlineKiosk
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
              <tr
                v-for="l in votersByLocationData.locations"
                :key="l.locationName"
              >
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
          v-if="
            selectedReport === 'VotersByLocationArea' &&
            votersByLocationAreaData
          "
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
                  {{
                    p.canReceiveVotes ? $t("reporting.yes") : $t("reporting.no")
                  }}
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
                  {{
                    p.canReceiveVotes ? $t("reporting.yes") : $t("reporting.no")
                  }}
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
      </div>
    </div>
  </div>
</template>

<style lang="less">
.reports-page {
  display: flex;
  gap: 20px;
  min-height: calc(100vh - 120px);

  .reports-chooser {
    width: 220px;
    flex-shrink: 0;

    .chooser-inner {
      position: sticky;
      top: 70px;
    }

    h3 {
      margin: 16px 0 6px;
      font-size: 14px;
      font-weight: 600;
      color: #333;
    }

    ul {
      list-style: none;
      padding: 0;
      margin: 0;

      li {
        padding: 0;
        margin: 0;

        a {
          display: block;
          padding: 4px 8px;
          font-size: 13px;
          color: #409eff;
          text-decoration: none;
          border-radius: 4px;

          &:hover {
            background: #ecf5ff;
          }
        }

        &.active a {
          background: #409eff;
          color: #fff;
        }
      }
    }

    .chooser-actions {
      margin-top: 16px;
    }

    .print-hint {
      margin-top: 6px;
      font-size: 12px;
      color: #999;
    }
  }

  .reports-panel {
    flex: 1;
    min-width: 0;

    .placeholder {
      padding: 40px;
      text-align: center;
      color: #999;
      font-size: 16px;
    }

    .error-text {
      color: #f56c6c;
    }
  }

  .report-content {
    padding: 0 8px;

    h2 {
      margin: 0 0 4px;
      font-size: 20px;
    }

    h3 {
      margin: 20px 0 8px;
      font-size: 16px;
    }

    h4 {
      margin: 16px 0 6px;
      font-size: 14px;
      font-weight: 600;
    }

    .report-meta {
      margin-bottom: 16px;
      font-size: 14px;
      color: #666;
    }

    .empty-msg {
      padding: 20px;
      color: #999;
      font-style: italic;
    }
  }

  .info-table {
    margin-left: 10px;
    border-collapse: collapse;

    td {
      padding: 2px 8px;
      vertical-align: top;
    }

    .num {
      text-align: right;
      font-variant-numeric: tabular-nums;
    }

    .warn-row td {
      color: #e6a23c;
    }

    .spacer td {
      padding: 6px 0;
    }

    .divider td {
      padding: 4px 0;

      div {
        border-top: 1px solid #ddd;
      }
    }

    .sub-row td {
      font-size: 85%;
      color: #666;
      padding: 1px 0 0 16px;
    }
  }

  .data-table {
    width: 100%;
    border-collapse: collapse;
    font-size: 13px;

    &.compact {
      width: auto;
    }

    thead {
      background: #f5f7fa;
    }

    th,
    td {
      padding: 6px 10px;
      border: 1px solid #e4e7ed;
      text-align: left;
    }

    th {
      font-weight: 600;
      white-space: nowrap;
    }

    .num {
      text-align: right;
      font-variant-numeric: tabular-nums;
    }

    .flag-cell {
      text-align: center;
    }

    tbody tr:nth-child(even) {
      background: #fafafa;
    }

    .total-row {
      font-weight: 600;
      background: #f0f0ff;
    }

    .section-E td {
      border-top: 3px double #bbb;
    }

    .section-E + .section-E td {
      border-top: 1px solid #e4e7ed;
    }

    .section-X td {
      color: #888;
    }

    .section-O td {
      color: #aaa;
    }

    .spoiled td {
      color: #f56c6c;
      text-decoration: line-through;
    }
  }

  .ballots-table {
    width: 100%;
    border-collapse: collapse;

    td {
      padding: 4px 8px;
      border: 1px solid #e4e7ed;
      vertical-align: top;
    }

    .ballot-id {
      white-space: nowrap;
      width: 1%;
    }

    .ballot-code {
      font-weight: 600;
    }

    .ballot-loc {
      font-size: 12px;
      color: #888;
    }

    .ballot-status {
      font-size: 12px;
      color: #f56c6c;
    }

    .ballot-votes {
      .vote-entry {
        display: inline-block;
        margin-right: 10px;
        white-space: nowrap;
      }

      .vote-spoiled {
        color: #f56c6c;
        text-decoration: line-through;
      }

      .sne-count {
        font-weight: 600;
        padding-right: 4px;
      }

      .invalid-reason {
        font-size: 11px;
        color: #999;
      }
    }

    .spoiled td {
      color: #f56c6c;
    }
  }

  .votes-list {
    column-width: 14em;
    column-gap: 1em;
    min-height: 200px;

    .vote-person {
      text-indent: -2em;
      margin-left: 2em;
      line-height: 1.6;

      &.elected {
        font-weight: 600;
      }
    }

    .vote-count {
      font-size: 12px;
      color: #666;
    }

    .section-break {
      border-top: 1px solid #999;
      margin: 4px 0;
      break-inside: avoid;
    }
  }

  .name-columns {
    column-width: 16em;
    column-gap: 1em;

    .name-entry {
      padding: 2px 0;
      line-height: 1.4;
    }
  }

  .dup-group {
    margin-bottom: 16px;
  }

  .loc-area-group {
    margin-bottom: 16px;
  }

  .page-break {
    page-break-before: always;
  }
}

@media print {
  .no-print {
    display: none !important;
  }

  .reports-page {
    display: block;

    .reports-panel {
      width: 100%;
    }

    .report-content {
      padding: 0;
    }
  }

  .data-table {
    th,
    td {
      padding: 3px 6px;
      font-size: 11px;
    }
  }

  .ballots-table td {
    padding: 2px 4px;
    font-size: 11px;
  }

  .votes-list .vote-person {
    font-size: 11px;
    line-height: 1.4;
  }

  table {
    page-break-inside: auto;
  }

  tr {
    page-break-inside: avoid;
  }

  thead {
    display: table-header-group;
  }
}
</style>
