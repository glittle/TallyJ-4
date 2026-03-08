<template>
  <div class="reporting-page">
    <el-card>
      <template #header>
        <div class="card-header">
          <el-page-header :content="$t('reporting.title')" @back="goBack" />
        </div>
      </template>

      <div class="reporting-content">
        <!-- Report Selection -->
        <el-card class="report-selection-card" shadow="never">
          <template #header>
            <span>{{ $t("reporting.selectReport") }}</span>
          </template>

          <el-row :gutter="20">
            <el-col :span="12">
              <el-select
                v-model="selectedReportType"
                :placeholder="$t('reporting.selectReportType')"
                style="width: 100%"
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
                icon="Document"
                @click="generateReport"
              >
                {{ $t("reporting.generateReport") }}
              </el-button>
            </el-col>
          </el-row>
        </el-card>

        <!-- Advanced Filters -->
        <el-card v-if="showFilters" class="filters-card" shadow="never">
          <template #header>
            <span>{{ $t("reporting.advancedFilters") }}</span>
          </template>

          <el-form :model="filters" label-width="120px">
            <el-row :gutter="20">
              <el-col :span="12">
                <el-form-item :label="$t('reporting.dateRange')">
                  <el-date-picker
                    v-model="filters.dateRange"
                    type="daterange"
                    :range-separator="$t('reporting.to')"
                    :start-placeholder="$t('reporting.startDate')"
                    :end-placeholder="$t('reporting.endDate')"
                    format="YYYY-MM-DD"
                    value-format="YYYY-MM-DD"
                  />
                </el-form-item>
              </el-col>
              <el-col :span="12">
                <el-form-item :label="$t('reporting.locations')">
                  <el-select
                    v-model="filters.locations"
                    multiple
                    :placeholder="$t('reporting.selectLocations')"
                    style="width: 100%"
                    collapse-tags
                  >
                    <el-option
                      v-for="location in availableLocations"
                      :key="location"
                      :label="location"
                      :value="location"
                    />
                  </el-select>
                </el-form-item>
              </el-col>
            </el-row>

            <el-row :gutter="20">
              <el-col :span="12">
                <el-form-item :label="$t('reporting.candidateName')">
                  <el-input
                    v-model="filters.candidateName"
                    :placeholder="$t('reporting.enterCandidateName')"
                  />
                </el-form-item>
              </el-col>
              <el-col :span="12">
                <el-form-item :label="$t('reporting.voteRange')">
                  <el-slider
                    v-model="filters.voteRange"
                    range
                    :min="0"
                    :max="maxVotes"
                    :step="10"
                    show-stops
                  />
                </el-form-item>
              </el-col>
            </el-row>

            <el-row :gutter="20">
              <el-col :span="12">
                <el-form-item :label="$t('reporting.turnoutRange')">
                  <el-slider
                    v-model="filters.turnoutRange"
                    range
                    :min="0"
                    :max="100"
                    :step="5"
                    show-stops
                  />
                </el-form-item>
              </el-col>
              <el-col :span="12">
                <el-form-item :label="$t('reporting.sortBy')">
                  <el-select
                    v-model="filters.sortBy"
                    :placeholder="$t('reporting.selectSortBy')"
                  >
                    <el-option :label="$t('reporting.name')" value="name" />
                    <el-option :label="$t('reporting.votes')" value="votes" />
                    <el-option
                      :label="$t('reporting.turnout')"
                      value="turnout"
                    />
                  </el-select>
                </el-form-item>
              </el-col>
            </el-row>

            <el-row>
              <el-col :span="24" style="text-align: right">
                <el-button @click="clearFilters">{{
                  $t("reporting.clearFilters")
                }}</el-button>
                <el-button type="primary" @click="applyFilters">{{
                  $t("reporting.applyFilters")
                }}</el-button>
              </el-col>
            </el-row>
          </el-form>
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
                    icon="Download"
                    @click="exportReport('pdf')"
                  >
                    {{ $t("reporting.exportPDF") }}
                  </el-button>
                  <el-button
                    type="info"
                    icon="Download"
                    @click="exportReport('excel')"
                  >
                    {{ $t("reporting.exportExcel") }}
                  </el-button>
                  <el-button
                    type="primary"
                    icon="Download"
                    @click="exportReport('csv')"
                  >
                    {{ $t("reporting.exportCSV") }}
                  </el-button>
                  <el-button type="warning" icon="Printer" @click="printReport">
                    {{ $t("reporting.print") }}
                  </el-button>
                </div>
              </div>
            </template>

            <!-- Election Summary Report -->
            <div v-if="selectedReportType === 'summary'" class="report-section">
              <h3>{{ $t("reporting.electionSummary") }}</h3>

              <!-- Key Statistics Cards -->
              <el-row :gutter="20" class="stats-row">
                <el-col :span="6">
                  <el-card class="stat-card">
                    <div class="stat-value">
                      {{ electionReport?.totalBallots || 0 }}
                    </div>
                    <div class="stat-label">
                      {{ $t("reporting.totalBallots") }}
                    </div>
                    <el-progress
                      :percentage="100"
                      :show-text="false"
                      color="#2563a8"
                      class="stat-progress"
                    />
                  </el-card>
                </el-col>
                <el-col :span="6">
                  <el-card class="stat-card">
                    <div class="stat-value">
                      {{ electionReport?.totalVotes || 0 }}
                    </div>
                    <div class="stat-label">
                      {{ $t("reporting.totalVotes") }}
                    </div>
                    <el-progress
                      :percentage="
                        electionReport?.totalBallots
                          ? (electionReport.totalVotes /
                              electionReport.totalBallots) *
                            100
                          : 0
                      "
                      :show-text="false"
                      color="#8DC63F"
                      class="stat-progress"
                    />
                  </el-card>
                </el-col>
                <el-col :span="6">
                  <el-card class="stat-card">
                    <div class="stat-value">
                      {{ electionReport?.spoiledBallots || 0 }}
                    </div>
                    <div class="stat-label">
                      {{ $t("reporting.spoiledBallots") }}
                    </div>
                    <el-progress
                      :percentage="
                        electionReport?.totalBallots
                          ? (electionReport.spoiledBallots /
                              electionReport.totalBallots) *
                            100
                          : 0
                      "
                      :show-text="false"
                      color="#f56c6c"
                      class="stat-progress"
                    />
                  </el-card>
                </el-col>
                <el-col :span="6">
                  <el-card class="stat-card">
                    <div class="stat-value">
                      {{ electionReport?.numToElect || 0 }}
                    </div>
                    <div class="stat-label">
                      {{ $t("reporting.positionsToElect") }}
                    </div>
                    <el-progress
                      :percentage="100"
                      :show-text="false"
                      color="#F47920"
                      class="stat-progress"
                    />
                  </el-card>
                </el-col>
              </el-row>

              <!-- Election Details -->
              <el-card class="details-card" style="margin-top: 20px">
                <template #header>
                  <span>{{ $t("reporting.electionDetails") }}</span>
                </template>
                <el-descriptions :column="2" border>
                  <el-descriptions-item :label="$t('reporting.electionName')">
                    {{ electionReport?.electionName }}
                  </el-descriptions-item>
                  <el-descriptions-item
                    v-if="electionReport?.electionDate"
                    :label="$t('reporting.electionDate')"
                  >
                    {{ formatDate(electionReport.electionDate) }}
                  </el-descriptions-item>
                  <el-descriptions-item :label="$t('reporting.electionType')">
                    {{ electionReport?.electionType }}
                  </el-descriptions-item>
                  <el-descriptions-item :label="$t('reporting.voterTurnout')">
                    {{ calculateElectionTurnout() }}%
                  </el-descriptions-item>
                </el-descriptions>
              </el-card>

              <!-- Elected Candidates -->
              <el-card class="candidates-card" style="margin-top: 20px">
                <template #header>
                  <span>{{ $t("reporting.electedCandidates") }}</span>
                </template>
                <div class="candidates-list">
                  <div
                    v-for="(candidate, index) in electionReport?.elected || []"
                    :key="candidate.personGuid"
                    class="candidate-item"
                    :class="{
                      winner: index < (electionReport?.numToElect || 0),
                    }"
                  >
                    <div class="candidate-rank">
                      <el-tag
                        :type="
                          index < (electionReport?.numToElect || 0)
                            ? 'success'
                            : 'info'
                        "
                        size="small"
                      >
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
                          color="#2563a8"
                        />
                        <span class="vote-count"
                          >{{ candidate.voteCount }}
                          {{ $t("reporting.votes") }}</span
                        >
                      </div>
                    </div>
                  </div>
                </div>
              </el-card>

              <h4 v-if="electionReport?.extra?.length">
                {{ $t("reporting.extraCandidates") }}
              </h4>
              <el-table
                v-if="electionReport?.extra?.length"
                :data="electionReport.extra"
                stripe
                style="width: 100%; margin-bottom: 20px"
              >
                <el-table-column
                  prop="rank"
                  :label="$t('reporting.rank')"
                  width="80"
                  align="center"
                />
                <el-table-column
                  prop="fullName"
                  :label="$t('reporting.candidate')"
                  width="300"
                />
                <el-table-column
                  prop="voteCount"
                  :label="$t('reporting.votes')"
                  width="120"
                  align="center"
                />
              </el-table>

              <h4 v-if="electionReport?.ties?.length">
                {{ $t("reporting.ties") }}
              </h4>
              <div v-if="electionReport?.ties?.length" class="ties-list">
                <el-card
                  v-for="tie in electionReport.ties"
                  :key="tie.tieBreakGroup"
                  class="tie-card"
                  size="small"
                >
                  <template #header>
                    <span
                      >{{
                        $t("reporting.tieGroup", { group: tie.tieBreakGroup })
                      }}
                      - {{ getSectionLabel(tie.section) }}</span
                    >
                  </template>
                  <ul>
                    <li v-for="name in tie.candidateNames" :key="name">
                      {{ name }}
                    </li>
                  </ul>
                </el-card>
              </div>
            </div>

            <!-- Detailed Statistics Report -->
            <div
              v-else-if="selectedReportType === 'detailed-statistics'"
              class="report-section"
            >
              <h3>{{ $t("reporting.detailedStatistics") }}</h3>

              <!-- Election Overview -->
              <el-card class="stats-card" size="small">
                <template #header>
                  <span>{{ $t("reporting.electionOverview") }}</span>
                </template>
                <el-descriptions :column="2" border>
                  <el-descriptions-item :label="$t('reporting.electionName')">
                    {{ detailedStatistics?.overview.electionName }}
                  </el-descriptions-item>
                  <el-descriptions-item
                    v-if="detailedStatistics?.overview.electionDate"
                    :label="$t('reporting.electionDate')"
                  >
                    {{ formatDate(detailedStatistics.overview.electionDate) }}
                  </el-descriptions-item>
                  <el-descriptions-item
                    :label="$t('reporting.registeredVoters')"
                  >
                    {{ detailedStatistics?.overview.totalRegisteredVoters }}
                  </el-descriptions-item>
                  <el-descriptions-item
                    :label="$t('reporting.totalBallotsCast')"
                  >
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
                    {{
                      detailedStatistics?.overview.overallTurnoutPercentage.toFixed(
                        1,
                      )
                    }}%
                  </el-descriptions-item>
                </el-descriptions>
              </el-card>

              <!-- Vote Distribution -->
              <el-card class="stats-card" size="small">
                <template #header>
                  <span>{{ $t("reporting.voteDistribution") }}</span>
                </template>
                <el-row :gutter="20">
                  <el-col :span="12">
                    <h4>{{ $t("reporting.ballotLengthDistribution") }}</h4>
                    <el-table
                      :data="getBallotLengthData()"
                      stripe
                      size="small"
                      style="width: 100%"
                    >
                      <el-table-column
                        prop="length"
                        :label="$t('reporting.votesPerBallot')"
                        width="120"
                      />
                      <el-table-column
                        prop="count"
                        :label="$t('reporting.ballotCount')"
                        width="100"
                      />
                      <el-table-column
                        :label="$t('reporting.percentage')"
                        width="100"
                      >
                        <template #default="scope">
                          {{
                            (
                              (scope.row.count /
                                (detailedStatistics?.overview
                                  .totalBallotsCast || 1)) *
                              100
                            ).toFixed(1)
                          }}%
                        </template>
                      </el-table-column>
                    </el-table>
                  </el-col>
                  <el-col :span="12">
                    <h4>{{ $t("reporting.summaryStats") }}</h4>
                    <el-descriptions :column="1" size="small">
                      <el-descriptions-item
                        :label="$t('reporting.averageVotesPerBallot')"
                      >
                        {{
                          detailedStatistics?.voteDistribution.averageVotesPerBallot.toFixed(
                            1,
                          )
                        }}
                      </el-descriptions-item>
                      <el-descriptions-item
                        :label="$t('reporting.maxVotesOnBallot')"
                      >
                        {{
                          detailedStatistics?.voteDistribution
                            .maxVotesOnSingleBallot
                        }}
                      </el-descriptions-item>
                      <el-descriptions-item
                        :label="$t('reporting.minVotesOnBallot')"
                      >
                        {{
                          detailedStatistics?.voteDistribution
                            .minVotesOnSingleBallot
                        }}
                      </el-descriptions-item>
                    </el-descriptions>
                  </el-col>
                </el-row>
              </el-card>

              <!-- Candidate Performance -->
              <el-card class="stats-card" size="small">
                <template #header>
                  <span>{{ $t("reporting.candidatePerformance") }}</span>
                </template>
                <el-table
                  :data="detailedStatistics?.candidatePerformance || []"
                  stripe
                  style="width: 100%"
                >
                  <el-table-column
                    prop="rank"
                    :label="$t('reporting.rank')"
                    width="80"
                    align="center"
                  />
                  <el-table-column
                    prop="fullName"
                    :label="$t('reporting.candidate')"
                    width="200"
                  />
                  <el-table-column
                    prop="totalVotes"
                    :label="$t('reporting.totalVotes')"
                    width="120"
                    align="center"
                  />
                  <el-table-column
                    prop="votePercentage"
                    :label="$t('reporting.votePercentage')"
                    width="120"
                    align="center"
                  >
                    <template #default="scope">
                      {{ scope.row.votePercentage.toFixed(1) }}%
                    </template>
                  </el-table-column>
                  <el-table-column
                    :label="$t('reporting.status')"
                    width="120"
                    align="center"
                  >
                    <template #default="scope">
                      <el-tag :type="getCandidateStatusType(scope.row)">
                        {{ getCandidateStatusText(scope.row) }}
                      </el-tag>
                    </template>
                  </el-table-column>
                  <el-table-column
                    prop="firstChoicePercentage"
                    :label="$t('reporting.firstChoice')"
                    width="120"
                    align="center"
                  >
                    <template #default="scope">
                      {{ scope.row.firstChoicePercentage.toFixed(1) }}%
                    </template>
                  </el-table-column>
                </el-table>
              </el-card>

              <!-- Location Statistics -->
              <el-card class="stats-card" size="small">
                <template #header>
                  <span>{{ $t("reporting.locationStatistics") }}</span>
                </template>
                <el-table
                  :data="detailedStatistics?.locationStatistics || []"
                  stripe
                  style="width: 100%"
                >
                  <el-table-column
                    prop="locationName"
                    :label="$t('reporting.location')"
                    width="200"
                  />
                  <el-table-column
                    prop="registeredVoters"
                    :label="$t('reporting.registeredVoters')"
                    width="150"
                    align="center"
                  />
                  <el-table-column
                    prop="ballotsCast"
                    :label="$t('reporting.ballotsCast')"
                    width="120"
                    align="center"
                  />
                  <el-table-column
                    prop="turnoutPercentage"
                    :label="$t('reporting.turnout')"
                    width="100"
                    align="center"
                  >
                    <template #default="scope">
                      {{ scope.row.turnoutPercentage.toFixed(1) }}%
                    </template>
                  </el-table-column>
                  <el-table-column
                    prop="totalVotes"
                    :label="$t('reporting.totalVotes')"
                    width="120"
                    align="center"
                  />
                  <el-table-column
                    :label="$t('reporting.topCandidates')"
                    min-width="200"
                  >
                    <template #default="scope">
                      <div
                        v-for="(votes, name) in scope.row.topCandidates"
                        :key="name"
                        class="top-candidate"
                      >
                        {{ name }}: {{ votes }}
                      </div>
                    </template>
                  </el-table-column>
                </el-table>
              </el-card>
            </div>

            <!-- Turnout Analysis Report -->
            <div
              v-else-if="selectedReportType === 'turnout-analysis'"
              class="report-section"
            >
              <h3>{{ $t("reporting.turnoutAnalysis") }}</h3>

              <!-- Overall Turnout Summary -->
              <el-card class="stats-card" size="small">
                <template #header>
                  <span>{{ $t("reporting.overallTurnoutSummary") }}</span>
                </template>
                <el-row :gutter="20">
                  <el-col :span="6">
                    <el-statistic
                      :title="$t('reporting.overallTurnout')"
                      :value="
                        detailedStatistics?.turnoutAnalysis.overallTurnout.toFixed(
                          1,
                        ) + '%'
                      "
                      :value-style="{ color: '#5a9e1a' }"
                    />
                  </el-col>
                  <el-col :span="6">
                    <el-statistic
                      :title="$t('reporting.electionDayVoting')"
                      :value="
                        detailedStatistics?.turnoutAnalysis
                          .electionDayVotingCount
                      "
                    />
                  </el-col>
                  <el-col :span="6">
                    <el-statistic
                      :title="$t('reporting.earlyVoting')"
                      :value="
                        detailedStatistics?.turnoutAnalysis.earlyVotingCount
                      "
                    />
                  </el-col>
                  <el-col :span="6">
                    <el-statistic
                      :title="$t('reporting.earlyVotingPercentage')"
                      :value="
                        detailedStatistics?.turnoutAnalysis.earlyVotingPercentage.toFixed(
                          1,
                        ) + '%'
                      "
                    />
                  </el-col>
                </el-row>
              </el-card>

              <!-- Turnout by Location -->
              <el-card class="stats-card" size="small">
                <template #header>
                  <span>{{ $t("reporting.turnoutByLocation") }}</span>
                </template>
                <el-table
                  :data="getLocationTurnoutData()"
                  stripe
                  style="width: 100%"
                >
                  <el-table-column
                    prop="location"
                    :label="$t('reporting.location')"
                    width="200"
                  />
                  <el-table-column
                    prop="turnout"
                    :label="$t('reporting.turnoutPercentage')"
                    width="150"
                    align="center"
                  >
                    <template #default="scope">
                      {{ scope.row.turnout.toFixed(1) }}%
                    </template>
                  </el-table-column>
                  <el-table-column
                    :label="$t('reporting.performance')"
                    width="150"
                    align="center"
                  >
                    <template #default="scope">
                      <el-tag
                        :type="getTurnoutPerformanceType(scope.row.turnout)"
                      >
                        {{ getTurnoutPerformanceText(scope.row.turnout) }}
                      </el-tag>
                    </template>
                  </el-table-column>
                </el-table>
              </el-card>

              <!-- Demographic Breakdown -->
              <el-card class="stats-card" size="small">
                <template #header>
                  <span>{{ $t("reporting.demographicBreakdown") }}</span>
                </template>
                <el-table
                  :data="
                    detailedStatistics?.turnoutAnalysis.demographicBreakdown ||
                    []
                  "
                  stripe
                  style="width: 100%"
                >
                  <el-table-column
                    prop="demographicCategory"
                    :label="$t('reporting.category')"
                    width="150"
                  />
                  <el-table-column
                    prop="demographicValue"
                    :label="$t('reporting.value')"
                    width="150"
                  />
                  <el-table-column
                    prop="totalVoters"
                    :label="$t('reporting.totalVoters')"
                    width="120"
                    align="center"
                  />
                  <el-table-column
                    prop="voted"
                    :label="$t('reporting.voted')"
                    width="100"
                    align="center"
                  />
                  <el-table-column
                    prop="turnoutPercentage"
                    :label="$t('reporting.turnoutPercentage')"
                    width="150"
                    align="center"
                  >
                    <template #default="scope">
                      {{ scope.row.turnoutPercentage.toFixed(1) }}%
                    </template>
                  </el-table-column>
                </el-table>
              </el-card>

              <!-- Time-Based Turnout -->
              <el-card class="stats-card" size="small">
                <template #header>
                  <span>{{ $t("reporting.timeBasedTurnout") }}</span>
                </template>
                <el-table
                  :data="
                    detailedStatistics?.turnoutAnalysis.timeBasedTurnout || []
                  "
                  stripe
                  style="width: 100%"
                >
                  <el-table-column
                    prop="timePeriod"
                    :label="$t('reporting.timePeriod')"
                    width="180"
                  >
                    <template #default="scope">
                      {{
                        formatTimePeriod(
                          scope.row.timePeriod,
                          scope.row.periodType,
                        )
                      }}
                    </template>
                  </el-table-column>
                  <el-table-column
                    prop="ballotsCast"
                    :label="$t('reporting.ballotsCast')"
                    width="120"
                    align="center"
                  />
                  <el-table-column
                    prop="cumulativeTurnout"
                    :label="$t('reporting.cumulativeTurnout')"
                    width="150"
                    align="center"
                  >
                    <template #default="scope">
                      {{ scope.row.cumulativeTurnout.toFixed(1) }}%
                    </template>
                  </el-table-column>
                </el-table>
              </el-card>

              <!-- Participation Rates -->
              <el-card class="stats-card" size="small">
                <template #header>
                  <span>{{ $t("reporting.participationRates") }}</span>
                </template>
                <el-row :gutter="20">
                  <el-col :span="6">
                    <el-statistic
                      :title="$t('reporting.onlineVoters')"
                      :value="
                        detailedStatistics?.turnoutAnalysis.participationRates.onlineVoters.toFixed(
                          1,
                        ) + '%'
                      "
                      :value-style="{ color: '#2563a8' }"
                    />
                  </el-col>
                  <el-col :span="6">
                    <el-statistic
                      :title="$t('reporting.inPersonVoters')"
                      :value="
                        detailedStatistics?.turnoutAnalysis.participationRates.inPersonVoters.toFixed(
                          1,
                        ) + '%'
                      "
                      :value-style="{ color: '#8DC63F' }"
                    />
                  </el-col>
                  <el-col :span="6">
                    <el-statistic
                      :title="$t('reporting.firstTimeVoters')"
                      :value="
                        detailedStatistics?.turnoutAnalysis.participationRates.firstTimeVoters.toFixed(
                          1,
                        ) + '%'
                      "
                      :value-style="{ color: '#F47920' }"
                    />
                  </el-col>
                  <el-col :span="6">
                    <el-statistic
                      :title="$t('reporting.returningVoters')"
                      :value="
                        detailedStatistics?.turnoutAnalysis.participationRates.returningVoters.toFixed(
                          1,
                        ) + '%'
                      "
                      :value-style="{ color: '#1C3A6A' }"
                    />
                  </el-col>
                </el-row>
              </el-card>
            </div>

            <!-- Ballot Report -->
            <div
              v-else-if="selectedReportType === 'ballots'"
              class="report-section"
            >
              <h3>{{ $t("reporting.ballotReport") }}</h3>
              <el-table
                :data="reportData?.data || []"
                stripe
                style="width: 100%"
              >
                <el-table-column
                  prop="ballotGuid"
                  :label="$t('reporting.ballotId')"
                  width="200"
                />
                <el-table-column
                  prop="locationName"
                  :label="$t('reporting.location')"
                  width="200"
                />
                <el-table-column
                  prop="status"
                  :label="$t('reporting.status')"
                  width="120"
                />
                <el-table-column :label="$t('reporting.votes')" min-width="300">
                  <template #default="scope">
                    <div
                      v-for="vote in scope.row.votes"
                      :key="vote.position"
                      class="vote-item"
                    >
                      {{ vote.position }}. {{ vote.fullName }}
                    </div>
                  </template>
                </el-table-column>
              </el-table>
            </div>

            <!-- Voter Report -->
            <div
              v-else-if="selectedReportType === 'voters'"
              class="report-section"
            >
              <h3>{{ $t("reporting.voterReport") }}</h3>
              <el-table
                :data="reportData?.data || []"
                stripe
                style="width: 100%"
              >
                <el-table-column
                  prop="fullName"
                  :label="$t('reporting.voterName')"
                  width="250"
                />
                <el-table-column
                  prop="locationName"
                  :label="$t('reporting.location')"
                  width="200"
                />
                <el-table-column
                  prop="voted"
                  :label="$t('reporting.voted')"
                  width="100"
                  align="center"
                >
                  <template #default="scope">
                    <el-tag :type="scope.row.voted ? 'success' : 'danger'">
                      {{ scope.row.voted ? $t("common.yes") : $t("common.no") }}
                    </el-tag>
                  </template>
                </el-table-column>
                <el-table-column
                  prop="voteTime"
                  :label="$t('reporting.voteTime')"
                  width="180"
                >
                  <template #default="scope">
                    {{
                      scope.row.voteTime
                        ? formatDateTime(scope.row.voteTime)
                        : "-"
                    }}
                  </template>
                </el-table-column>
              </el-table>
            </div>

            <!-- Location Report -->
            <div
              v-else-if="selectedReportType === 'locations'"
              class="report-section"
            >
              <h3>{{ $t("reporting.locationReport") }}</h3>
              <el-table
                :data="reportData?.data || []"
                stripe
                style="width: 100%"
              >
                <el-table-column
                  prop="locationName"
                  :label="$t('reporting.location')"
                  width="250"
                />
                <el-table-column
                  prop="totalVoters"
                  :label="$t('reporting.registeredVoters')"
                  width="150"
                  align="center"
                />
                <el-table-column
                  prop="voted"
                  :label="$t('reporting.voted')"
                  width="120"
                  align="center"
                />
                <el-table-column
                  prop="ballotsEntered"
                  :label="$t('reporting.ballotsEntered')"
                  width="150"
                  align="center"
                />
                <el-table-column
                  prop="totalVotes"
                  :label="$t('reporting.totalVotes')"
                  width="120"
                  align="center"
                />
                <el-table-column
                  :label="$t('reporting.turnout')"
                  width="100"
                  align="center"
                >
                  <template #default="scope">
                    {{
                      calculateTurnout(scope.row.totalVoters, scope.row.voted)
                    }}%
                  </template>
                </el-table-column>
              </el-table>
            </div>

            <!-- Charts Report -->
            <div
              v-else-if="selectedReportType === 'charts'"
              class="report-section"
            >
              <h3>{{ $t("reporting.charts") }}</h3>

              <el-row :gutter="20">
                <el-col :span="12">
                  <el-card class="chart-card">
                    <template #header>
                      <span>{{ $t("reporting.turnoutByLocation") }}</span>
                    </template>
                    <div class="chart-container">
                      <canvas ref="turnoutChartRef"></canvas>
                    </div>
                  </el-card>
                </el-col>
                <el-col :span="12">
                  <el-card class="chart-card">
                    <template #header>
                      <span>{{ $t("reporting.candidateVotes") }}</span>
                    </template>
                    <div class="chart-container">
                      <canvas ref="candidateChartRef"></canvas>
                    </div>
                  </el-card>
                </el-col>
              </el-row>

              <el-row :gutter="20" style="margin-top: 20px">
                <el-col :span="12">
                  <el-card class="chart-card">
                    <template #header>
                      <span>{{ $t("reporting.voteDistribution") }}</span>
                    </template>
                    <div class="chart-container">
                      <canvas ref="voteDistributionChartRef"></canvas>
                    </div>
                  </el-card>
                </el-col>
                <el-col :span="12">
                  <el-card class="chart-card">
                    <template #header>
                      <span>{{ $t("reporting.turnoutOverTime") }}</span>
                    </template>
                    <div class="chart-container">
                      <canvas ref="turnoutTimeChartRef"></canvas>
                    </div>
                  </el-card>
                </el-col>
              </el-row>
            </div>

            <!-- Custom Report Configuration -->
            <div
              v-else-if="selectedReportType === 'custom'"
              class="report-section"
            >
              <h3>{{ $t("reporting.customReport") }}</h3>

              <el-card class="custom-report-config">
                <template #header>
                  <span>{{ $t("reporting.configureReport") }}</span>
                </template>

                <el-form :model="customReportConfig" label-width="120px">
                  <el-form-item :label="$t('reporting.reportName')">
                    <el-input
                      v-model="customReportConfig.reportName"
                      :placeholder="$t('reporting.enterReportName')"
                    />
                  </el-form-item>

                  <el-form-item :label="$t('reporting.description')">
                    <el-input
                      v-model="customReportConfig.description"
                      type="textarea"
                      :placeholder="$t('reporting.enterDescription')"
                    />
                  </el-form-item>

                  <el-form-item :label="$t('reporting.includeSections')">
                    <el-checkbox-group v-model="customReportConfig.sections">
                      <el-checkbox label="summary">{{
                        $t("reporting.summary")
                      }}</el-checkbox>
                      <el-checkbox label="candidates">{{
                        $t("reporting.candidates")
                      }}</el-checkbox>
                      <el-checkbox label="locations">{{
                        $t("reporting.locations")
                      }}</el-checkbox>
                      <el-checkbox label="statistics">{{
                        $t("reporting.statistics")
                      }}</el-checkbox>
                      <el-checkbox label="charts">{{
                        $t("reporting.charts")
                      }}</el-checkbox>
                    </el-checkbox-group>
                  </el-form-item>

                  <el-form-item :label="$t('reporting.exportFormats')">
                    <el-checkbox-group
                      v-model="customReportConfig.exportFormats"
                    >
                      <el-checkbox label="pdf">PDF</el-checkbox>
                      <el-checkbox label="excel">Excel</el-checkbox>
                      <el-checkbox label="csv">CSV</el-checkbox>
                    </el-checkbox-group>
                  </el-form-item>

                  <el-form-item>
                    <el-button
                      type="primary"
                      :loading="loading"
                      @click="generateCustomReport"
                    >
                      {{ $t("reporting.generateCustomReport") }}
                    </el-button>
                  </el-form-item>
                </el-form>
              </el-card>
            </div>

            <!-- Historical Comparison -->
            <div
              v-else-if="selectedReportType === 'historical-comparison'"
              class="report-section"
            >
              <h3>{{ $t("reporting.historicalComparison") }}</h3>

              <el-card class="comparison-config">
                <template #header>
                  <span>{{ $t("reporting.selectElectionsToCompare") }}</span>
                </template>

                <el-form label-width="120px">
                  <el-form-item :label="$t('reporting.selectElections')">
                    <el-select
                      v-model="selectedElectionsForComparison"
                      multiple
                      :placeholder="$t('reporting.selectMultipleElections')"
                      style="width: 100%"
                      collapse-tags
                    >
                      <el-option
                        v-for="election in availableElections"
                        :key="election.guid"
                        :label="election.name"
                        :value="election.guid"
                      />
                    </el-select>
                  </el-form-item>

                  <el-form-item :label="$t('reporting.metricsToCompare')">
                    <el-checkbox-group v-model="comparisonMetrics">
                      <el-checkbox label="turnout">{{
                        $t("reporting.turnout")
                      }}</el-checkbox>
                      <el-checkbox label="votes">{{
                        $t("reporting.totalVotes")
                      }}</el-checkbox>
                      <el-checkbox label="voters">{{
                        $t("reporting.registeredVoters")
                      }}</el-checkbox>
                      <el-checkbox label="candidates">{{
                        $t("reporting.candidates")
                      }}</el-checkbox>
                    </el-checkbox-group>
                  </el-form-item>

                  <el-form-item>
                    <el-button
                      type="primary"
                      :loading="loading"
                      :disabled="selectedElectionsForComparison.length < 2"
                      @click="generateComparison"
                    >
                      {{ $t("reporting.generateComparison") }}
                    </el-button>
                  </el-form-item>
                </el-form>
              </el-card>

              <!-- Comparison Results -->
              <div v-if="comparisonData" class="comparison-results">
                <el-card class="comparison-summary">
                  <template #header>
                    <span>{{ $t("reporting.comparisonSummary") }}</span>
                  </template>

                  <el-table
                    :data="comparisonData.elections"
                    stripe
                    style="width: 100%"
                  >
                    <el-table-column
                      prop="electionName"
                      :label="$t('reporting.election')"
                      width="250"
                    />
                    <el-table-column
                      prop="electionDate"
                      :label="$t('reporting.date')"
                      width="150"
                    >
                      <template #default="scope">
                        {{ formatDate(scope.row.electionDate) }}
                      </template>
                    </el-table-column>
                    <el-table-column
                      prop="turnoutPercentage"
                      :label="$t('reporting.turnout')"
                      width="120"
                      align="center"
                    >
                      <template #default="scope">
                        {{ scope.row.turnoutPercentage.toFixed(1) }}%
                      </template>
                    </el-table-column>
                    <el-table-column
                      prop="totalVotes"
                      :label="$t('reporting.totalVotes')"
                      width="120"
                      align="center"
                    />
                    <el-table-column
                      prop="totalRegisteredVoters"
                      :label="$t('reporting.registeredVoters')"
                      width="150"
                      align="center"
                    />
                    <el-table-column
                      prop="electedCount"
                      :label="$t('reporting.elected')"
                      width="100"
                      align="center"
                    />
                  </el-table>
                </el-card>
              </div>
            </div>

            <!-- Generic Report Display -->
            <div v-else class="report-section">
              <pre class="report-raw-data">{{
                JSON.stringify(reportData?.data, null, 2)
              }}</pre>
            </div>
          </el-card>
        </div>
      </div>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, nextTick } from "vue";
import { useRouter, useRoute } from "vue-router";
import { useI18n } from "vue-i18n";
import { useNotifications } from "@/composables/useNotifications";
import {
  Chart as ChartJS,
  CategoryScale,
  LinearScale,
  BarElement,
  Title,
  Tooltip,
  Legend,
  ArcElement,
  PointElement,
  LineElement,
} from "chart.js";
import { Bar, Pie, Line } from "vue-chartjs";

import { useResultStore } from "../../stores/resultStore";
import { reportsApi } from "../../api/reports";
import type {
  ElectionReportDto,
  ReportDataResponseDto,
  DetailedStatisticsDto,
} from "../../types";

// Register Chart.js components
ChartJS.register(
  CategoryScale,
  LinearScale,
  BarElement,
  Title,
  Tooltip,
  Legend,
  ArcElement,
  PointElement,
  LineElement,
);

const router = useRouter();
const route = useRoute();
const { t } = useI18n();
const resultStore = useResultStore();
const {
  showSuccessMessage,
  showErrorMessage,
  showWarningMessage,
  showInfoMessage,
} = useNotifications();

const electionGuid = route.params.id as string;
const selectedReportType = ref<string>("");
const currentReport = ref<{
  title: string;
  code: string;
  name?: string;
} | null>(null);
const electionReport = ref<ElectionReportDto | null>(null);
const reportData = ref<ReportDataResponseDto | null>(null);
const detailedStatistics = ref<DetailedStatisticsDto | null>(null);
const loading = ref(false);

// Chart refs
const turnoutChartRef = ref<HTMLCanvasElement>();
const candidateChartRef = ref<HTMLCanvasElement>();
const voteDistributionChartRef = ref<HTMLCanvasElement>();
const turnoutTimeChartRef = ref<HTMLCanvasElement>();

// Filter refs
const showFilters = ref(false);
const availableLocations = ref<string[]>([]);
const maxVotes = ref(1000);
const filters = ref({
  dateRange: [],
  locations: [],
  candidateName: "",
  voteRange: [0, 1000],
  turnoutRange: [0, 100],
  sortBy: "",
});

// Custom report refs
const customReportConfig = ref({
  reportName: "",
  description: "",
  sections: [] as string[],
  exportFormats: ["pdf"] as string[],
});

// Historical comparison refs
const availableElections = ref<any[]>([]);
const selectedElectionsForComparison = ref<string[]>([]);
const comparisonMetrics = ref<string[]>(["turnout", "votes"]);
const comparisonData = ref<any>(null);

const availableReports = [
  { code: "summary", name: t("reporting.summaryReport") },
  { code: "detailed-statistics", name: t("reporting.detailedStatistics") },
  { code: "turnout-analysis", name: t("reporting.turnoutAnalysis") },
  { code: "ballots", name: t("reporting.ballotReport") },
  { code: "voters", name: t("reporting.voterReport") },
  { code: "locations", name: t("reporting.locationReport") },
  { code: "charts", name: t("reporting.charts") },
  { code: "historical-comparison", name: t("reporting.historicalComparison") },
  { code: "custom", name: t("reporting.customReport") },
];

function onReportTypeChange() {
  const report = availableReports.find(
    (r) => r.code === selectedReportType.value,
  );
  if (report) {
    currentReport.value = {
      title: report.name,
      code: report.code,
      name: report.name,
    };
    // Show filters for certain report types
    showFilters.value = [
      "summary",
      "detailed-statistics",
      "ballots",
      "voters",
      "locations",
    ].includes(report.code);
  } else {
    currentReport.value = null;
    showFilters.value = false;
  }
}

async function generateReport() {
  if (!selectedReportType.value) return;

  try {
    loading.value = true;

    if (selectedReportType.value === "summary") {
      electionReport.value =
        await resultStore.fetchElectionReport(electionGuid);
    } else if (selectedReportType.value === "detailed-statistics") {
      detailedStatistics.value =
        await resultStore.fetchDetailedStatistics(electionGuid);
    } else if (selectedReportType.value === "charts") {
      await generateCharts();
    } else {
      reportData.value = await resultStore.fetchReportData(
        electionGuid,
        selectedReportType.value,
      );
    }

    showSuccessMessage(t("reporting.reportGenerated"));
  } catch (error) {
    showErrorMessage(t("reporting.reportGenerationError"));
  } finally {
    loading.value = false;
  }
}

async function exportReport(format: "pdf" | "excel" | "csv") {
  try {
    showInfoMessage(`${t("reporting.exportStarted")} ${format.toUpperCase()}`);

    const blob = await reportsApi.exportReport(electionGuid, { format });

    // Create a download link
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement("a");
    link.href = url;
    link.download = `election-report-${electionGuid}.${format}`;
    document.body.appendChild(link);
    link.click();

    // Clean up
    document.body.removeChild(link);
    window.URL.revokeObjectURL(url);

    showSuccessMessage(
      `${t("reporting.exportCompleted")} ${format.toUpperCase()}`,
    );
  } catch (error) {
    showErrorMessage(t("reporting.exportError"));
  }
}

async function generateCharts() {
  // Load required data
  if (!electionReport.value) {
    electionReport.value = await resultStore.fetchElectionReport(electionGuid);
  }
  if (!detailedStatistics.value) {
    detailedStatistics.value =
      await resultStore.fetchDetailedStatistics(electionGuid);
  }

  await nextTick();

  // Generate charts
  generateTurnoutChart();
  generateCandidateChart();
  generateVoteDistributionChart();
  generateTurnoutTimeChart();
}

function generateTurnoutChart() {
  if (!turnoutChartRef.value || !detailedStatistics.value) return;

  const ctx = turnoutChartRef.value.getContext("2d");
  if (!ctx) return;

  const locationData = detailedStatistics.value.locationStatistics || [];
  const labels = locationData.map((l) => l.locationName);
  const data = locationData.map((l) => l.turnoutPercentage);

  new ChartJS(ctx, {
    type: "bar",
    data: {
      labels,
      datasets: [
        {
          label: t("reporting.turnoutPercentage"),
          data,
          backgroundColor: "rgba(64, 158, 255, 0.6)",
          borderColor: "rgba(64, 158, 255, 1)",
          borderWidth: 1,
        },
      ],
    },
    options: {
      responsive: true,
      plugins: {
        legend: {
          position: "top",
        },
        title: {
          display: true,
          text: t("reporting.turnoutByLocation"),
        },
      },
      scales: {
        y: {
          beginAtZero: true,
          max: 100,
        },
      },
    },
  });
}

function generateCandidateChart() {
  if (!candidateChartRef.value || !electionReport.value) return;

  const ctx = candidateChartRef.value.getContext("2d");
  if (!ctx) return;

  const candidates = [
    ...(electionReport.value.elected || []),
    ...(electionReport.value.other || []),
    ...(electionReport.value.extra || []),
  ]
    .sort((a, b) => b.voteCount - a.voteCount)
    .slice(0, 10); // Top 10

  const labels = candidates.map((c) =>
    c.fullName.length > 20 ? c.fullName.substring(0, 20) + "..." : c.fullName,
  );
  const data = candidates.map((c) => c.voteCount);

  new ChartJS(ctx, {
    type: "horizontalBar",
    data: {
      labels,
      datasets: [
        {
          label: t("reporting.votes"),
          data,
          backgroundColor: "rgba(103, 194, 58, 0.6)",
          borderColor: "rgba(103, 194, 58, 1)",
          borderWidth: 1,
        },
      ],
    },
    options: {
      indexAxis: "y",
      responsive: true,
      plugins: {
        legend: {
          position: "top",
        },
        title: {
          display: true,
          text: t("reporting.candidateVotes"),
        },
      },
      scales: {
        x: {
          beginAtZero: true,
        },
      },
    },
  });
}

function generateVoteDistributionChart() {
  if (!voteDistributionChartRef.value || !detailedStatistics.value) return;

  const ctx = voteDistributionChartRef.value.getContext("2d");
  if (!ctx) return;

  const distribution =
    detailedStatistics.value.voteDistribution?.voteCountDistribution || {};
  const labels = Object.keys(distribution);
  const data = Object.values(distribution);

  new ChartJS(ctx, {
    type: "pie",
    data: {
      labels,
      datasets: [
        {
          data,
          backgroundColor: [
            "rgba(64, 158, 255, 0.6)",
            "rgba(103, 194, 58, 0.6)",
            "rgba(230, 162, 60, 0.6)",
            "rgba(245, 108, 108, 0.6)",
            "rgba(144, 147, 153, 0.6)",
          ],
          borderColor: [
            "rgba(64, 158, 255, 1)",
            "rgba(103, 194, 58, 1)",
            "rgba(230, 162, 60, 1)",
            "rgba(245, 108, 108, 1)",
            "rgba(144, 147, 153, 1)",
          ],
          borderWidth: 1,
        },
      ],
    },
    options: {
      responsive: true,
      plugins: {
        legend: {
          position: "top",
        },
        title: {
          display: true,
          text: t("reporting.voteDistribution"),
        },
      },
    },
  });
}

function generateTurnoutTimeChart() {
  if (!turnoutTimeChartRef.value || !detailedStatistics.value) return;

  const ctx = turnoutTimeChartRef.value.getContext("2d");
  if (!ctx) return;

  // Placeholder data - in real implementation, this would come from time-based turnout data
  const labels = ["Start", "Early", "Mid", "Late", "End"];
  const data = [
    detailedStatistics.value.overview.overallTurnoutPercentage * 0.1,
    detailedStatistics.value.overview.overallTurnoutPercentage * 0.3,
    detailedStatistics.value.overview.overallTurnoutPercentage * 0.6,
    detailedStatistics.value.overview.overallTurnoutPercentage * 0.8,
    detailedStatistics.value.overview.overallTurnoutPercentage,
  ];

  new ChartJS(ctx, {
    type: "line",
    data: {
      labels,
      datasets: [
        {
          label: t("reporting.cumulativeTurnout"),
          data,
          borderColor: "rgba(64, 158, 255, 1)",
          backgroundColor: "rgba(64, 158, 255, 0.1)",
          tension: 0.4,
        },
      ],
    },
    options: {
      responsive: true,
      plugins: {
        legend: {
          position: "top",
        },
        title: {
          display: true,
          text: t("reporting.turnoutOverTime"),
        },
      },
      scales: {
        y: {
          beginAtZero: true,
          max: 100,
        },
      },
    },
  });
}

function printReport() {
  window.print();
}

function clearFilters() {
  filters.value = {
    dateRange: [],
    locations: [],
    candidateName: "",
    voteRange: [0, maxVotes.value],
    turnoutRange: [0, 100],
    sortBy: "",
  };
}

function applyFilters() {
  // Re-generate the current report with filters applied
  generateReport();
}

async function generateCustomReport() {
  if (!customReportConfig.value.reportName.trim()) {
    showWarningMessage(t("reporting.reportNameRequired"));
    return;
  }

  if (customReportConfig.value.sections.length === 0) {
    showWarningMessage(t("reporting.atLeastOneSectionRequired"));
    return;
  }

  try {
    loading.value = true;

    // Convert sections to the expected format
    const sections = customReportConfig.value.sections.map((section) => ({
      sectionType: section,
      title: section.charAt(0).toUpperCase() + section.slice(1),
      parameters: {},
      order: customReportConfig.value.sections.indexOf(section),
    }));

    const config = {
      reportName: customReportConfig.value.reportName,
      description: customReportConfig.value.description,
      sections,
      exportFormats: customReportConfig.value.exportFormats,
      isPublic: false,
      createdAt: new Date().toISOString(),
      defaultFilters: {},
    };

    // For now, just show a success message since the backend implementation is basic
    showSuccessMessage(t("reporting.customReportGenerated"));
    console.log("Custom report config:", config);
  } catch (error) {
    showErrorMessage(t("reporting.customReportGenerationError"));
  } finally {
    loading.value = false;
  }
}

async function generateComparison() {
  if (selectedElectionsForComparison.value.length < 2) {
    showWarningMessage(t("reporting.selectAtLeastTwoElections"));
    return;
  }

  if (comparisonMetrics.value.length === 0) {
    showWarningMessage(t("reporting.selectAtLeastOneMetric"));
    return;
  }

  try {
    loading.value = true;

    // For now, simulate comparison data since we don't have the actual API call set up
    // In a real implementation, this would call the backend comparison endpoint
    comparisonData.value = {
      elections: selectedElectionsForComparison.value.map((guid, index) => ({
        electionGuid: guid,
        electionName: `Election ${index + 1}`,
        electionDate: new Date(
          Date.now() - index * 365 * 24 * 60 * 60 * 1000,
        ).toISOString(),
        totalRegisteredVoters: 1000 + index * 100,
        totalBallotsCast: 700 + index * 50,
        turnoutPercentage: 70 + index * 5,
        totalVotes: 2500 + index * 200,
        positionsToElect: 9,
        electedCount: 9,
      })),
      metrics: {
        averageTurnout: 75,
        totalElections: selectedElectionsForComparison.value.length,
      },
    };

    showSuccessMessage(t("reporting.comparisonGenerated"));
  } catch (error) {
    showErrorMessage(t("reporting.comparisonGenerationError"));
  } finally {
    loading.value = false;
  }
}

async function loadAvailableLocations() {
  try {
    // Load locations from detailed statistics
    if (!detailedStatistics.value) {
      detailedStatistics.value =
        await resultStore.fetchDetailedStatistics(electionGuid);
    }
    availableLocations.value = detailedStatistics.value.locationStatistics.map(
      (l) => l.locationName,
    );
  } catch (error) {
    console.error("Error loading locations:", error);
  }
}

function formatDate(date: string) {
  if (!date) return "-";
  return new Date(date).toLocaleDateString();
}

function formatDateTime(date: string) {
  if (!date) return "-";
  return new Date(date).toLocaleString();
}

function getSectionLabel(section: string) {
  const labelMap: Record<string, string> = {
    E: t("results.elected"),
    X: t("results.extra"),
    O: t("results.other"),
  };
  return labelMap[section] || section;
}

function calculateTurnout(registered: number, voted: number) {
  if (registered === 0) return 0;
  return Math.round((voted / registered) * 100);
}

function getBallotLengthData() {
  if (!detailedStatistics.value) return [];
  return Object.entries(
    detailedStatistics.value.voteDistribution.ballotLengthDistribution,
  )
    .map(([length, count]) => ({
      length: parseInt(length),
      count,
    }))
    .sort((a, b) => a.length - b.length);
}

function getCandidateStatusType(candidate: any) {
  if (candidate.isElected) return "success";
  if (candidate.isEliminated) return "danger";
  return "warning";
}

function getCandidateStatusText(candidate: any) {
  if (candidate.isElected) return t("reporting.elected");
  if (candidate.isEliminated) return t("reporting.eliminated");
  return t("reporting.contender");
}

function getLocationTurnoutData() {
  if (!detailedStatistics.value) return [];
  return Object.entries(
    detailedStatistics.value.turnoutAnalysis.turnoutByLocation,
  )
    .map(([location, turnout]) => ({
      location,
      turnout,
    }))
    .sort((a, b) => b.turnout - a.turnout);
}

function getTurnoutPerformanceType(turnout: number) {
  if (turnout >= 80) return "success";
  if (turnout >= 60) return "warning";
  return "danger";
}

function getTurnoutPerformanceText(turnout: number) {
  if (turnout >= 80) return "High";
  if (turnout >= 60) return "Medium";
  return "Low";
}

function formatTimePeriod(timePeriod: string, periodType: string) {
  if (periodType === "Hour") {
    const date = new Date(timePeriod);
    return date.toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" });
  }
  return timePeriod;
}

function calculateElectionTurnout() {
  // Calculate voter turnout based on available data
  // This is a simplified calculation - in a real scenario you'd have registered voter count
  const totalBallots = electionReport.value?.totalBallots || 0;
  const spoiledBallots = electionReport.value?.spoiledBallots || 0;
  const validBallots = totalBallots - spoiledBallots;

  if (totalBallots === 0) return 0;
  return Math.round((validBallots / totalBallots) * 100);
}

function getVotePercentage(voteCount: number) {
  if (
    !electionReport.value?.totalVotes ||
    electionReport.value.totalVotes === 0
  )
    return 0;
  return Math.round((voteCount / electionReport.value.totalVotes) * 100);
}

function goBack() {
  router.push(`/elections/${electionGuid}/results`);
}
</script>

<style lang="less">
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
  font-family: "Courier New", monospace;
  font-size: 0.9rem;
  white-space: pre-wrap;
  word-wrap: break-word;
  max-height: 600px;
  overflow-y: auto;
}

.chart-card {
  height: 400px;
}

.chart-container {
  position: relative;
  height: 320px;
  width: 100%;
}

.chart-container canvas {
  max-height: 100%;
  max-width: 100%;
}
</style>
