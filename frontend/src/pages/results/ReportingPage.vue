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
              <el-descriptions :column="2" border>
                <el-descriptions-item :label="$t('reporting.electionName')">
                  {{ electionReport?.electionName }}
                </el-descriptions-item>
                <el-descriptions-item :label="$t('reporting.electionDate')" v-if="electionReport?.electionDate">
                  {{ formatDate(electionReport.electionDate) }}
                </el-descriptions-item>
                <el-descriptions-item :label="$t('reporting.positionsToElect')">
                  {{ electionReport?.numToElect }}
                </el-descriptions-item>
                <el-descriptions-item :label="$t('reporting.totalBallots')">
                  {{ electionReport?.totalBallots }}
                </el-descriptions-item>
                <el-descriptions-item :label="$t('reporting.spoiledBallots')">
                  {{ electionReport?.spoiledBallots }}
                </el-descriptions-item>
                <el-descriptions-item :label="$t('reporting.totalVotes')">
                  {{ electionReport?.totalVotes }}
                </el-descriptions-item>
              </el-descriptions>

              <h4>{{ $t('reporting.electedCandidates') }}</h4>
              <el-table :data="electionReport?.elected || []" stripe style="width: 100%; margin-bottom: 20px;">
                <el-table-column prop="rank" :label="$t('reporting.rank')" width="80" align="center" />
                <el-table-column prop="fullName" :label="$t('reporting.candidate')" width="300" />
                <el-table-column prop="voteCount" :label="$t('reporting.votes')" width="120" align="center" />
              </el-table>

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
import { ref, computed, onMounted } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import { useI18n } from 'vue-i18n';
import { ElMessage } from 'element-plus';
import { Document, Download, Printer } from '@element-plus/icons-vue';
import { useResultStore } from '../../stores/resultStore';
import type { ElectionReportDto, ReportDataResponseDto } from '../../types';

const router = useRouter();
const route = useRoute();
const { t } = useI18n();
const resultStore = useResultStore();

const electionGuid = route.params.id as string;
const selectedReportType = ref<string>('');
const currentReport = ref<{ title: string; code: string } | null>(null);
const electionReport = ref<ElectionReportDto | null>(null);
const reportData = ref<ReportDataResponseDto | null>(null);
const loading = ref(false);

const availableReports = [
  { code: 'summary', name: t('reporting.summaryReport') },
  { code: 'ballots', name: t('reporting.ballotReport') },
  { code: 'voters', name: t('reporting.voterReport') },
  { code: 'locations', name: t('reporting.locationReport') }
];

function onReportTypeChange() {
  const report = availableReports.find(r => r.code === selectedReportType.value);
  currentReport.value = report || null;
}

async function generateReport() {
  if (!selectedReportType.value) return;

  try {
    loading.value = true;

    if (selectedReportType.value === 'summary') {
      electionReport.value = await resultStore.fetchElectionReport(electionGuid);
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