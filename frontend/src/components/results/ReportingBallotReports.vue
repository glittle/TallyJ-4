<script setup lang="ts">
import { computed } from "vue";
import type {
  BallotAlignmentReport,
  BallotsReport,
  BallotsSameReport,
  BallotsSummaryReport,
  MainReport,
  SpoiledVotesReport,
  VotesByNameReport,
  VotesByNumReport,
} from "@/types";
import {
  formatReportDate,
  formatReportPercent,
} from "@/utils/reportFormatters";

const props = defineProps<{
  selectedReport: string;
  selectedReportName: string;
  reportData: unknown;
}>();

const formatDate = formatReportDate;
const formatPercent = formatReportPercent;

const mainData = computed(() => props.reportData as MainReport | null);
const votesByNumData = computed(
  () => props.reportData as VotesByNumReport | null,
);
const votesByNameData = computed(
  () => props.reportData as VotesByNameReport | null,
);
const ballotsData = computed(() => props.reportData as BallotsReport | null);
const spoiledVotesData = computed(
  () => props.reportData as SpoiledVotesReport | null,
);
const alignmentData = computed(
  () => props.reportData as BallotAlignmentReport | null,
);
const ballotsSameData = computed(
  () => props.reportData as BallotsSameReport | null,
);
const ballotsSummaryData = computed(
  () => props.reportData as BallotsSummaryReport | null,
);
const isBallotReport = computed(() =>
  ["Ballots", "BallotsOnline", "BallotsImported", "BallotsTied"].includes(
    props.selectedReport,
  ),
);
const selectedReport = computed(() => props.selectedReport);
const selectedReportName = computed(() => props.selectedReportName);
</script>

<template>
  <div class="reporting-ballot-reports">
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
        <tbody>
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
              mainData.sumOfEnvelopesCollected !== mainData.numBallotsWithManual
            "
            class="warn-row"
          >
            <td>{{ $t("reporting.ballotsReceivedNotVoted") }}</td>
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
            <td>{{ $t("reporting.didNotVote") }}</td>
            <td class="num">
              {{
                (
                  mainData.numEligibleToVote - mainData.sumOfEnvelopesCollected
                ).toLocaleString()
              }}
            </td>
          </tr>
          <tr class="divider">
            <td colspan="2"><div></div></td>
          </tr>
          <tr>
            <td>{{ $t("reporting.ballotsCastInPerson") }}</td>
            <td class="num">
              {{ mainData.inPersonBallots.toLocaleString() }}
            </td>
          </tr>
          <tr>
            <td>{{ $t("reporting.ballotsReceivedByMail") }}</td>
            <td class="num">
              {{ mainData.mailedInBallots.toLocaleString() }}
            </td>
          </tr>
          <tr>
            <td>{{ $t("reporting.ballotsHandDelivered") }}</td>
            <td class="num">
              {{ mainData.droppedOffBallots.toLocaleString() }}
            </td>
          </tr>
          <tr v-if="mainData.onlineBallots > 0">
            <td>{{ $t("reporting.ballotsCastOnline") }}</td>
            <td class="num">
              {{ mainData.onlineBallots.toLocaleString() }}
            </td>
          </tr>
          <tr v-if="mainData.importedBallots > 0">
            <td>{{ $t("reporting.ballotsImported") }}</td>
            <td class="num">
              {{ mainData.importedBallots.toLocaleString() }}
            </td>
          </tr>
          <tr v-if="mainData.calledInBallots > 0">
            <td>{{ $t("reporting.ballotsPhonedIn") }}</td>
            <td class="num">
              {{ mainData.calledInBallots.toLocaleString() }}
            </td>
          </tr>
          <tr v-if="mainData.custom1Ballots > 0">
            <td>
              {{
                $t("reporting.ballotsCustomPrefix", {
                  name: mainData.custom1Name,
                })
              }}
            </td>
            <td class="num">
              {{ mainData.custom1Ballots.toLocaleString() }}
            </td>
          </tr>
          <tr v-if="mainData.custom2Ballots > 0">
            <td>
              {{
                $t("reporting.ballotsCustomPrefix", {
                  name: mainData.custom2Name,
                })
              }}
            </td>
            <td class="num">
              {{ mainData.custom2Ballots.toLocaleString() }}
            </td>
          </tr>
          <tr v-if="mainData.custom3Ballots > 0">
            <td>
              {{
                $t("reporting.ballotsCustomPrefix", {
                  name: mainData.custom3Name,
                })
              }}
            </td>
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
            <td class="num">
              {{ mainData.spoiledVotes.toLocaleString() }}
            </td>
          </tr>
          <tr
            v-for="sv in mainData.spoiledVoteReasons"
            :key="sv.reason"
            class="sub-row"
          >
            <td colspan="2">{{ sv.voteCount }} - {{ sv.reason }}</td>
          </tr>
        </tbody>
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
              }}{{ p.tieBreakRequired ? " / " + p.tieBreakCount : "" }}</span
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
                  <span class="sne-count">{{ v.singleNameElectionCount }}</span>
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
  </div>
</template>
