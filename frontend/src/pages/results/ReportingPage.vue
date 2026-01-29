<template>
  <div class="reporting-page">
    <el-card>
      <template #header>
        <div class="card-header">
          <el-page-header @back="goBack" :content="$t('reporting.title')" />
        </div>
      </template>

      <div class="reporting-content">
        <!-- Report Selection -->
        <el-card class="report-selection-card" shadow="never">
          <template #header>
            <span>{{ $t('reporting.selectReport') }}</span>
          </template>

          <el-row :gutter="20">
            <el-col :span="12">
              <el-select
                v-model="selectedReportType"
                :placeholder="$t('reporting.selectReportType')"
                style="width: 100%;"
                @change="onReportTypeChange"
              >
                <el-option
                  v-for="report in availableReports"
                  :key="report.code"
                  :label="report.name"
                  :value="report.code"
                />
              </el-select>
            </el-col>
            <el-col :span="12">
              <el-button
                type="primary"
                :loading="loading"
                :disabled="!selectedReportType"
                @click="generateReport"
                icon="Document"
              >
                {{ $t('reporting.generateReport') }}
              </el-button>
            </el-col>
          </el-row>
        </el-card>

        <!-- Report Display -->
        <div v-if="currentReport" class="report-display">
          <el-card class="report-content-card">
            <template #header>
              <div class="report-header">
                <span>{{ currentReport.title }}</span>
                <div class="report-actions">
                  <el-button
                    type="success"
                    @click="exportReport('pdf')"
                    icon="Download"
                  >
                    {{ $t('reporting.exportPDF') }}
                  </el-button>
                  <el-button
                    type="info"
                    @click="exportReport('excel')"
                    icon="Download"
                  >
                    {{ $t('reporting.exportExcel') }}
                  </el-button>
                  <el-button
                    type="warning"
                    @click="printReport"
                    icon="Printer"
                  >
                    {{ $t('reporting.print') }}
                  </el-button>
                </div>
              </div>
            </template>

            <!-- Election Summary Report -->
            <div v-if="selectedReportType === 'summary'" class="report-section">
              <h3>{{ $t('reporting.electionSummary') }}</h3>

              <!-- Key Statistics Cards -->
              <el-row :gutter="20" class="stats-row">
                <el-col :span="6">
                  <el-card class="stat-card">
                    <div class="stat-value">{{ electionReport?.totalBallots || 0 }}</div>
                    <div class="stat-label">{{ $t('reporting.totalBallots') }}</div>
                    <el-progress
                      :percentage="100"
                      :show-text="false"
                      color="#409eff"
                      class="stat-progress"
                    />
                  </el-card>
                </el-col>
                <el-col :span="6">
                  <el-card class="stat-card">
                    <div class="stat-value">{{ electionReport?.totalVotes || 0 }}</div>
                    <div class="stat-label">{{ $t('reporting.totalVotes') }}</div>
                    <el-progress
                      :percentage="electionReport?.totalBallots ? (electionReport.totalVotes / electionReport.totalBallots) * 100 : 0"
                      :show-text="false"
                      color="#67c23a"
                      class="stat-progress"
                    />
                  </el-card>
                </el-col>
                <el-col :span="6">
                  <el-card class="stat-card">
                    <div class="stat-value">{{ electionReport?.spoiledBallots || 0 }}</div>
                    <div class="stat-label">{{ $t('reporting.spoiledBallots') }}</div>
                    <el-progress
                      :percentage="electionReport?.totalBallots ? (electionReport.spoiledBallots / electionReport.totalBallots) * 100 : 0"
                      :show-text="false"
                      color="#f56c6c"
                      class="stat-progress"
                    />
                  </el-card>
                </el-col>
                <el-col :span="6">
                  <el-card class="stat-card">
                    <div class="stat-value">{{ electionReport?.numToElect || 0 }}</div>
                    <div class="stat-label">{{ $t('reporting.positionsToElect') }}</div>
                    <el-progress
                      :percentage="100"
                      :show-text="false"
                      color="#e6a23c"
                      class="stat-progress"
                    />
                  </el-card>
                </el-col>
              </el-row>

              <!-- Election Details -->
              <el-card class="details-card" style="margin-top: 20px;">
                <template #header>
                  <span>{{ $t('reporting.electionDetails') }}</span>
                </template>
                <el-descriptions :column="2" border>
                  <el-descriptions-item :label="$t('reporting.electionName')">
                    {{ electionReport?.electionName }}
                  </el-descriptions-item>
                  <el-descriptions-item :label="$t('reporting.electionDate')" v-if="electionReport?.electionDate">
                    {{ formatDate(electionReport.electionDate) }}
                  </el-descriptions-item>
                  <el-descriptions-item :label="$t('reporting.electionType')">
                    {{ electionReport?.electionType }}
                  </el-descriptions-item>
                  <el-descriptions-item :label="$t('reporting.voterTurnout')">
                    {{ calculateTurnout() }}%
                  </el-descriptions-item>
                </el-descriptions>
              </el-card>

              <!-- Elected Candidates -->
              <el-card class="candidates-card" style="margin-top: 20px;">
                <template #header>
                  <span>{{ $t('reporting.electedCandidates') }}</span>
                </template>
                <div class="candidates-list">
                  <div
                    v-for="(candidate, index) in electionReport?.elected || []"
                    :key="candidate.personGuid"
                    class="candidate-item"
                    :class="{ 'winner': index < (electionReport?.numToElect || 0) }"
                  >
                    <div class="candidate-rank">
                      <el-tag :type="index < (electionReport?.numToElect || 0) ? 'success' : 'info'" size="small">
                        #{{ candidate.rank }}
                      </el-tag>
                    </div>
                    <div class="candidate-info">
                      <div class="candidate-name">{{ candidate.fullName }}</div>
                      <div class="candidate-votes">
                        <el-progress
                          :percentage="getVotePercentage(candidate.voteCount)"
                          :show-text="false"
                          :stroke-width="8"
                          color="#409eff"
                        />
                        <span class="vote-count">{{ candidate.voteCount }} {{ $t('reporting.votes') }}</span>
                      </div>
                    </div>
                  </div>
                </div>
              </el-card>

              <h4 v-if="electionReport?.extra?.length">{{ $t('reporting.extraCandidates') }}</h4>
              <el-table v-if="electionReport?.extra?.length" :data="electionReport.extra" stripe style="width: 100%; margin-bottom: 20px;">
                <el-table-column prop="rank" :label="$t('reporting.rank')" width="80" align="center" />
                <el-table-column prop="fullName" :label="$t('reporting.candidate')" width="300" />
                <el-table-column prop="voteCount" :label="$t('reporting.votes')" width="120" align="center" />
              </el-table>

              <h4 v-if="electionReport?.ties?.length">{{ $t('reporting.ties') }}</h4>
              <div v-if="electionReport?.ties?.length" class="ties-list">
                <el-card
                  v-for="tie in electionReport.ties"
                  :key="tie.tieBreakGroup"
                  class="tie-card"
                  size="small"
                >
                  <template #header>
                    <span>{{ $t('reporting.tieGroup', { group: tie.tieBreakGroup }) }} - {{ getSectionLabel(tie.section) }}</span>
                  </template>
                  <ul>
                    <li v-for="name in tie.candidateNames" :key="name">{{ name }}</li>
                  </ul>
                </el-card>
              </div>
            </div>

            <!-- Detailed Statistics Report -->
            <div v-else-if="selectedReportType === 'detailed-statistics'" class="report-section">
              <h3>{{ $t('reporting.detailedStatistics') }}</h3>

              <!-- Election Overview -->
              <el-card class="stats-card" size="small">
                <template #header>
                  <span>{{ $t('reporting.electionOverview') }}</span>
                </template>
                <el-descriptions :column="2" border>
                  <el-descriptions-item :label="$t('reporting.electionName')">
                    {{ detailedStatistics?.overview.electionName }}
                  </el-descriptions-item>
                  <el-descriptions-item :label="$t('reporting.electionDate')" v-if="detailedStatistics?.overview.electionDate">
                    {{ formatDate(detailedStatistics.overview.electionDate) }}
                  </el-descriptions-item>
                  <el-descriptions-item :label="$t('reporting.registeredVoters')">
                    {{ detailedStatistics?.overview.totalRegisteredVoters }}
                  </el-descriptions-item>
                  <el-descriptions-item :label="$t('reporting.totalBallotsCast')">
                    {{ detailedStatistics?.overview.totalBallotsCast }}
                  </el-descriptions-item>
                  <el-descriptions-item :label="$t('reporting.validBallots')">
                    {{ detailedStatistics?.overview.validBallots }}
                  </el-descriptions-item>
                  <el-descriptions-item :label="$t('reporting.spoiledBallots')">
                    {{ detailedStatistics?.overview.spoiledBallots }}
                  </el-descriptions-item>
                  <el-descriptions-item :label="$t('reporting.totalVotes')">
                    {{ detailedStatistics?.overview.totalVotes }}
                  </el-descriptions-item>
                  <el-descriptions-item :label="$t('reporting.overallTurnout')">
                    {{ detailedStatistics?.overview.overallTurnoutPercentage.toFixed(1) }}%
                  </el-descriptions-item>
                </el-descriptions>
              </el-card>

              <!-- Vote Distribution -->
              <el-card class="stats-card" size="small">
                <template #header>
                  <span>{{ $t('reporting.voteDistribution') }}</span>
                </template>
                <el-row :gutter="20">
                  <el-col :span="12">
                    <h4>{{ $t('reporting.ballotLengthDistribution') }}</h4>
                    <el-table :data="getBallotLengthData()" stripe size="small" style="width: 100%">
                      <el-table-column prop="length" :label="$t('reporting.votesPerBallot')" width="120" />
                      <el-table-column prop="count" :label="$t('reporting.ballotCount')" width="100" />
                      <el-table-column :label="$t('reporting.percentage')" width="100">
                        <template #default="scope">
                          {{ ((scope.row.count / (detailedStatistics?.overview.totalBallotsCast || 1)) * 100).toFixed(1) }}%
                        </template>
                      </el-table-column>
                    </el-table>
                  </el-col>
                  <el-col :span="12">
                    <h4>{{ $t('reporting.summaryStats') }}</h4>
                    <el-descriptions :column="1" size="small">
                      <el-descriptions-item :label="$t('reporting.averageVotesPerBallot')">
                        {{ detailedStatistics?.voteDistribution.averageVotesPerBallot.toFixed(1) }}
                      </el-descriptions-item>
                      <el-descriptions-item :label="$t('reporting.maxVotesOnBallot')">
                        {{ detailedStatistics?.voteDistribution.maxVotesOnSingleBallot }}
                      </el-descriptions-item>
                      <el-descriptions-item :label="$t('reporting.minVotesOnBallot')">
                        {{ detailedStatistics?.voteDistribution.minVotesOnSingleBallot }}
                      </el-descriptions-item>
                    </el-descriptions>
                  </el-col>
                </el-row>
              </el-card>

              <!-- Candidate Performance -->
              <el-card class="stats-card" size="small">
                <template #header>
                  <span>{{ $t('reporting.candidatePerformance') }}</span>
                </template>
                <el-table :data="detailedStatistics?.candidatePerformance || []" stripe style="width: 100%">
                  <el-table-column prop="rank" :label="$t('reporting.rank')" width="80" align="center" />
                  <el-table-column prop="fullName" :label="$t('reporting.candidate')" width="200" />
                  <el-table-column prop="totalVotes" :label="$t('reporting.totalVotes')" width="120" align="center" />
                  <el-table-column prop="votePercentage" :label="$t('reporting.votePercentage')" width="120" align="center">
                    <template #default="scope">
                      {{ scope.row.votePercentage.toFixed(1) }}%
                    </template>
                  </el-table-column>
                  <el-table-column :label="$t('reporting.status')" width="120" align="center">
                    <template #default="scope">
                      <el-tag :type="getCandidateStatusType(scope.row)">
                        {{ getCandidateStatusText(scope.row) }}
                      </el-tag>
                    </template>
                  </el-table-column>
                  <el-table-column prop="firstChoicePercentage" :label="$t('reporting.firstChoice')" width="120" align="center">
                    <template #default="scope">
                      {{ scope.row.firstChoicePercentage.toFixed(1) }}%
                    </template>
                  </el-table-column>
                </el-table>
              </el-card>

              <!-- Location Statistics -->
              <el-card class="stats-card" size="small">
                <template #header>
                  <span>{{ $t('reporting.locationStatistics') }}</span>
                </template>
                <el-table :data="detailedStatistics?.locationStatistics || []" stripe style="width: 100%">
                  <el-table-column prop="locationName" :label="$t('reporting.location')" width="200" />
                  <el-table-column prop="registeredVoters" :label="$t('reporting.registeredVoters')" width="150" align="center" />
                  <el-table-column prop="ballotsCast" :label="$t('reporting.ballotsCast')" width="120" align="center" />
                  <el-table-column prop="turnoutPercentage" :label="$t('reporting.turnout')" width="100" align="center">
                    <template #default="scope">
                      {{ scope.row.turnoutPercentage.toFixed(1) }}%
                    </template>
                  </el-table-column>
                  <el-table-column prop="totalVotes" :label="$t('reporting.totalVotes')" width="120" align="center" />
                  <el-table-column :label="$t('reporting.topCandidates')" min-width="200">
                    <template #default="scope">
                      <div v-for="(votes, name) in scope.row.topCandidates" :key="name" class="top-candidate">
                        {{ name }}: {{ votes }}
                      </div>
                    </template>
                  </el-table-column>
                </el-table>
              </el-card>
            </div>

            <!-- Turnout Analysis Report -->
            <div v-else-if="selectedReportType === 'turnout-analysis'" class="report-section">
              <h3>{{ $t('reporting.turnoutAnalysis') }}</h3>

              <!-- Overall Turnout Summary -->
              <el-card class="stats-card" size="small">
                <template #header>
                  <span>{{ $t('reporting.overallTurnoutSummary') }}</span>
                </template>
                <el-row :gutter="20">
                  <el-col :span="6">
                    <el-statistic
                      :title="$t('reporting.overallTurnout')"
                      :value="detailedStatistics?.turnoutAnalysis.overallTurnout.toFixed(1) + '%'"
                      :value-style="{ color: '#3f8600' }"
                    />
                  </el-col>
                  <el-col :span="6">
                    <el-statistic
                      :title="$t('reporting.electionDayVoting')"
                      :value="detailedStatistics?.turnoutAnalysis.electionDayVotingCount"
                    />
                  </el-col>
                  <el-col :span="6">
                    <el-statistic
                      :title="$t('reporting.earlyVoting')"
                      :value="detailedStatistics?.turnoutAnalysis.earlyVotingCount"
                    />
                  </el-col>
                  <el-col :span="6">
                    <el-statistic
                      :title="$t('reporting.earlyVotingPercentage')"
                      :value="detailedStatistics?.turnoutAnalysis.earlyVotingPercentage.toFixed(1) + '%'"
                    />
                  </el-col>
                </el-row>
              </el-card>

              <!-- Turnout by Location -->
              <el-card class="stats-card" size="small">
                <template #header>
                  <span>{{ $t('reporting.turnoutByLocation') }}</span>
                </template>
                <el-table :data="getLocationTurnoutData()" stripe style="width: 100%">
                  <el-table-column prop="location" :label="$t('reporting.location')" width="200" />
                  <el-table-column prop="turnout" :label="$t('reporting.turnoutPercentage')" width="150" align="center">
                    <template #default="scope">
                      {{ scope.row.turnout.toFixed(1) }}%
                    </template>
                  </el-table-column>
                  <el-table-column :label="$t('reporting.performance')" width="150" align="center">
                    <template #default="scope">
                      <el-tag :type="getTurnoutPerformanceType(scope.row.turnout)">
                        {{ getTurnoutPerformanceText(scope.row.turnout) }}
                      </el-tag>
                    </template>
                  </el-table-column>
                </el-table>
              </el-card>

              <!-- Demographic Breakdown -->
              <el-card class="stats-card" size="small">
                <template #header>
                  <span>{{ $t('reporting.demographicBreakdown') }}</span>
                </template>
                <el-table :data="detailedStatistics?.turnoutAnalysis.demographicBreakdown || []" stripe style="width: 100%">
                  <el-table-column prop="demographicCategory" :label="$t('reporting.category')" width="150" />
                  <el-table-column prop="demographicValue" :label="$t('reporting.value')" width="150" />
                  <el-table-column prop="totalVoters" :label="$t('reporting.totalVoters')" width="120" align="center" />
                  <el-table-column prop="voted" :label="$t('reporting.voted')" width="100" align="center" />
                  <el-table-column prop="turnoutPercentage" :label="$t('reporting.turnoutPercentage')" width="150" align="center">
                    <template #default="scope">
                      {{ scope.row.turnoutPercentage.toFixed(1) }}%
                    </template>
                  </el-table-column>
                </el-table>
              </el-card>

              <!-- Time-Based Turnout -->
              <el-card class="stats-card" size="small">
                <template #header>
                  <span>{{ $t('reporting.timeBasedTurnout') }}</span>
                </template>
                <el-table :data="detailedStatistics?.turnoutAnalysis.timeBasedTurnout || []" stripe style="width: 100%">
                  <el-table-column prop="timePeriod" :label="$t('reporting.timePeriod')" width="180">
                    <template #default="scope">
                      {{ formatTimePeriod(scope.row.timePeriod, scope.row.periodType) }}
                    </template>
                  </el-table-column>
                  <el-table-column prop="ballotsCast" :label="$t('reporting.ballotsCast')" width="120" align="center" />
                  <el-table-column prop="cumulativeTurnout" :label="$t('reporting.cumulativeTurnout')" width="150" align="center">
                    <template #default="scope">
                      {{ scope.row.cumulativeTurnout.toFixed(1) }}%
                    </template>
                  </el-table-column>
                </el-table>
              </el-card>

              <!-- Participation Rates -->
              <el-card class="stats-card" size="small">
                <template #header>
                  <span>{{ $t('reporting.participationRates') }}</span>
                </template>
                <el-row :gutter="20">
                  <el-col :span="6">
                    <el-statistic
                      :title="$t('reporting.onlineVoters')"
                      :value="detailedStatistics?.turnoutAnalysis.participationRates.onlineVoters.toFixed(1) + '%'"
                      :value-style="{ color: '#1890ff' }"
                    />
                  </el-col>
                  <el-col :span="6">
                    <el-statistic
                      :title="$t('reporting.inPersonVoters')"
                      :value="detailedStatistics?.turnoutAnalysis.participationRates.inPersonVoters.toFixed(1) + '%'"
                      :value-style="{ color: '#52c41a' }"
                    />
                  </el-col>
                  <el-col :span="6">
                    <el-statistic
                      :title="$t('reporting.firstTimeVoters')"
                      :value="detailedStatistics?.turnoutAnalysis.participationRates.firstTimeVoters.toFixed(1) + '%'"
                      :value-style="{ color: '#faad14' }"
                    />
                  </el-col>
                  <el-col :span="6">
                    <el-statistic
                      :title="$t('reporting.returningVoters')"
                      :value="detailedStatistics?.turnoutAnalysis.participationRates.returningVoters.toFixed(1) + '%'"
                      :value-style="{ color: '#722ed1' }"
                    />
                  </el-col>
                </el-row>
              </el-card>
            </div>

            <!-- Ballot Report -->
            <div v-else-if="selectedReportType === 'ballots'" class="report-section">
              <h3>{{ $t('reporting.ballotReport') }}</h3>
              <el-table :data="reportData?.data || []" stripe style="width: 100%">
                <el-table-column prop="ballotGuid" :label="$t('reporting.ballotId')" width="200" />
                <el-table-column prop="locationName" :label="$t('reporting.location')" width="200" />
                <el-table-column prop="status" :label="$t('reporting.status')" width="120" />
                <el-table-column :label="$t('reporting.votes')" min-width="300">
                  <template #default="scope">
                    <div v-for="vote in scope.row.votes" :key="vote.position" class="vote-item">
                      {{ vote.position }}. {{ vote.fullName }}
                    </div>
                  </template>
                </el-table-column>
              </el-table>
            </div>

            <!-- Voter Report -->
            <div v-else-if="selectedReportType === 'voters'" class="report-section">
              <h3>{{ $t('reporting.voterReport') }}</h3>
              <el-table :data="reportData?.data || []" stripe style="width: 100%">
                <el-table-column prop="fullName" :label="$t('reporting.voterName')" width="250" />
                <el-table-column prop="locationName" :label="$t('reporting.location')" width="200" />
                <el-table-column prop="voted" :label="$t('reporting.voted')" width="100" align="center">
                  <template #default="scope">
                    <el-tag :type="scope.row.voted ? 'success' : 'danger'">
                      {{ scope.row.voted ? $t('common.yes') : $t('common.no') }}
                    </el-tag>
                  </template>
                </el-table-column>
                <el-table-column prop="voteTime" :label="$t('reporting.voteTime')" width="180">
                  <template #default="scope">
                    {{ scope.row.voteTime ? formatDateTime(scope.row.voteTime) : '-' }}
                  </template>
                </el-table-column>
              </el-table>
            </div>

            <!-- Location Report -->
            <div v-else-if="selectedReportType === 'locations'" class="report-section">
              <h3>{{ $t('reporting.locationReport') }}</h3>
              <el-table :data="reportData?.data || []" stripe style="width: 100%">
                <el-table-column prop="locationName" :label="$t('reporting.location')" width="250" />
                <el-table-column prop="totalVoters" :label="$t('reporting.registeredVoters')" width="150" align="center" />
                <el-table-column prop="voted" :label="$t('reporting.voted')" width="120" align="center" />
                <el-table-column prop="ballotsEntered" :label="$t('reporting.ballotsEntered')" width="150" align="center" />
                <el-table-column prop="totalVotes" :label="$t('reporting.totalVotes')" width="120" align="center" />
                <el-table-column :label="$t('reporting.turnout')" width="100" align="center">
                  <template #default="scope">
                    {{ calculateTurnout(scope.row.totalVoters, scope.row.voted) }}%
                  </template>
                </el-table-column>
              </el-table>
            </div>

            <!-- Generic Report Display -->
            <div v-else class="report-section">
              <pre class="report-raw-data">{{ JSON.stringify(reportData?.data, null, 2) }}</pre>
            </div>
          </el-card>
        </div>
      </div>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import { useI18n } from 'vue-i18n';
import { ElMessage } from 'element-plus';

import { useResultStore } from '../../stores/resultStore';
import type { ElectionReportDto, ReportDataResponseDto, DetailedStatisticsDto } from '../../types';

const router = useRouter();
const route = useRoute();
const { t } = useI18n();
const resultStore = useResultStore();

const electionGuid = route.params.id as string;
const selectedReportType = ref<string>('');
const currentReport = ref<{ title: string; code: string; name?: string } | null>(null);
const electionReport = ref<ElectionReportDto | null>(null);
const reportData = ref<ReportDataResponseDto | null>(null);
const detailedStatistics = ref<DetailedStatisticsDto | null>(null);
const loading = ref(false);

const availableReports = [
  { code: 'summary', name: t('reporting.summaryReport') },
  { code: 'detailed-statistics', name: t('reporting.detailedStatistics') },
  { code: 'turnout-analysis', name: t('reporting.turnoutAnalysis') },
  { code: 'ballots', name: t('reporting.ballotReport') },
  { code: 'voters', name: t('reporting.voterReport') },
  { code: 'locations', name: t('reporting.locationReport') }
];

function onReportTypeChange() {
  const report = availableReports.find(r => r.code === selectedReportType.value);
  if (report) {
    currentReport.value = {
      title: report.name,
      code: report.code,
      name: report.name
    };
  } else {
    currentReport.value = null;
  }
}

async function generateReport() {
  if (!selectedReportType.value) return;

  try {
    loading.value = true;

    if (selectedReportType.value === 'summary') {
      electionReport.value = await resultStore.fetchElectionReport(electionGuid);
    } else if (selectedReportType.value === 'detailed-statistics') {
      detailedStatistics.value = await resultStore.fetchDetailedStatistics(electionGuid);
    } else {
      reportData.value = await resultStore.fetchReportData(electionGuid, selectedReportType.value);
    }

    ElMessage.success(t('reporting.reportGenerated'));
  } catch (error) {
    ElMessage.error(t('reporting.reportGenerationError'));
  } finally {
    loading.value = false;
  }
}

function exportReport(format: 'pdf' | 'excel') {
  // This would typically call an API endpoint to generate and download the file
  ElMessage.info(`${t('reporting.exportStarted')} ${format.toUpperCase()}`);
  // TODO: Implement actual export functionality
}

function printReport() {
  window.print();
}

function formatDate(date: string) {
  if (!date) return '-';
  return new Date(date).toLocaleDateString();
}

function formatDateTime(date: string) {
  if (!date) return '-';
  return new Date(date).toLocaleString();
}

function getSectionLabel(section: string) {
  const labelMap: Record<string, string> = {
    'E': t('results.elected'),
    'X': t('results.extra'),
    'O': t('results.other')
  };
  return labelMap[section] || section;
}

function calculateTurnout(registered: number, voted: number) {
  if (registered === 0) return 0;
  return Math.round((voted / registered) * 100);
}

function getBallotLengthData() {
  if (!detailedStatistics.value) return [];
  return Object.entries(detailedStatistics.value.voteDistribution.ballotLengthDistribution)
    .map(([length, count]) => ({
      length: parseInt(length),
      count
    }))
    .sort((a, b) => a.length - b.length);
}

function getCandidateStatusType(candidate: any) {
  if (candidate.isElected) return 'success';
  if (candidate.isEliminated) return 'danger';
  return 'warning';
}

function getCandidateStatusText(candidate: any) {
  if (candidate.isElected) return t('reporting.elected');
  if (candidate.isEliminated) return t('reporting.eliminated');
  return t('reporting.contender');
}

function getLocationTurnoutData() {
  if (!detailedStatistics.value) return [];
  return Object.entries(detailedStatistics.value.turnoutAnalysis.turnoutByLocation)
    .map(([location, turnout]) => ({
      location,
      turnout
    }))
    .sort((a, b) => b.turnout - a.turnout);
}

function getTurnoutPerformanceType(turnout: number) {
  if (turnout >= 80) return 'success';
  if (turnout >= 60) return 'warning';
  return 'danger';
}

function getTurnoutPerformanceText(turnout: number) {
  if (turnout >= 80) return 'High';
  if (turnout >= 60) return 'Medium';
  return 'Low';
}

function formatTimePeriod(timePeriod: string, periodType: string) {
  if (periodType === 'Hour') {
    const date = new Date(timePeriod);
    return date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
  }
  return timePeriod;
}

function calculateTurnout() {
  // Calculate voter turnout based on available data
  // This is a simplified calculation - in a real scenario you'd have registered voter count
  const totalBallots = electionReport.value?.totalBallots || 0;
  const spoiledBallots = electionReport.value?.spoiledBallots || 0;
  const validBallots = totalBallots - spoiledBallots;

  if (totalBallots === 0) return 0;
  return Math.round((validBallots / totalBallots) * 100);
}

function getVotePercentage(voteCount: number) {
  if (!electionReport.value?.totalVotes || electionReport.value.totalVotes === 0) return 0;
  return Math.round((voteCount / electionReport.value.totalVotes) * 100);
}

function goBack() {
  router.push(`/elections/${electionGuid}/results`);
}
</script>

<style scoped>
.reporting-page {
  max-width: 1400px;
  margin: 0 auto;
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.reporting-content {
  display: flex;
  flex-direction: column;
  gap: 20px;
}

.report-selection-card {
  margin-bottom: 20px;
}

.report-display {
  margin-top: 20px;
}

.report-content-card {
  min-height: 400px;
}

.report-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  width: 100%;
}

.report-actions {
  display: flex;
  gap: 10px;
}

.report-section {
  margin-bottom: 30px;
}

.stats-row {
  margin-bottom: var(--spacing-4);
}

.stat-card {
  text-align: center;
  padding: var(--spacing-4);
}

.stat-value {
  font-size: 2rem;
  font-weight: var(--font-weight-bold);
  color: var(--color-text-primary);
  margin-bottom: var(--spacing-2);
}

.stat-label {
  font-size: 0.875rem;
  color: var(--color-text-secondary);
  margin-bottom: var(--spacing-2);
}

.stat-progress {
  margin: 0 auto;
  width: 80%;
}

.details-card {
  margin-top: var(--spacing-6);
}

.candidates-card {
  margin-top: var(--spacing-6);
}

.candidates-list {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-3);
}

.candidate-item {
  display: flex;
  align-items: center;
  padding: var(--spacing-3);
  border: 1px solid var(--color-gray-200);
  border-radius: var(--radius-md);
  background-color: var(--color-bg-primary);
  transition: all 0.2s ease;
}

.candidate-item:hover {
  box-shadow: var(--shadow-sm);
}

.candidate-item.winner {
  border-color: #67c23a;
  background-color: #f0f9ff;
}

.candidate-rank {
  margin-right: var(--spacing-4);
}

.candidate-info {
  flex: 1;
}

.candidate-name {
  font-weight: var(--font-weight-medium);
  color: var(--color-text-primary);
  margin-bottom: var(--spacing-2);
}

.candidate-votes {
  display: flex;
  align-items: center;
  gap: var(--spacing-3);
}

.vote-count {
  font-size: 0.875rem;
  color: var(--color-text-secondary);
  min-width: 80px;
}

.ties-list {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-3);
  margin-top: var(--spacing-4);
}

.tie-card {
  border-left: 4px solid #e6a23c;
}

.tie-card ul {
  margin: 0;
  padding-left: var(--spacing-4);
}

.tie-card li {
  margin-bottom: var(--spacing-1);
  color: var(--color-text-secondary);
}

.report-section h3 {
  margin-bottom: 20px;
  color: #303133;
  font-size: 1.2rem;
  font-weight: 600;
}

.report-section h4 {
  margin: 25px 0 15px 0;
  color: #606266;
  font-size: 1.1rem;
  font-weight: 500;
}

.ties-list {
  display: flex;
  flex-direction: column;
  gap: 15px;
}

.tie-card {
  border: 1px solid #ebeef5;
}

.tie-card ul {
  margin: 0;
  padding-left: 20px;
}

.tie-card li {
  margin: 5px 0;
  color: #606266;
}

.vote-item {
  margin: 2px 0;
  padding: 2px 0;
  border-bottom: 1px solid #f5f5f5;
  font-size: 0.9rem;
}

.vote-item:last-child {
  border-bottom: none;
}

.stats-card {
  margin-bottom: 20px;
}

.stats-card .el-card__header {
  background-color: #f8f9fa;
  font-weight: 600;
}

.top-candidate {
  margin: 2px 0;
  padding: 2px 0;
  font-size: 0.9rem;
  border-bottom: 1px solid #f0f0f0;
}

.top-candidate:last-child {
  border-bottom: none;
}

.report-raw-data {
  background: #f8f9fa;
  padding: 20px;
  border-radius: 4px;
  font-family: 'Courier New', monospace;
  font-size: 0.9rem;
  white-space: pre-wrap;
  word-wrap: break-word;
  max-height: 600px;
  overflow-y: auto;
}
</style>