<template>
  <main class="dashboard-page">
    <section class="welcome-section" aria-labelledby="welcome-heading">
      <el-card class="welcome-card">
        <h1 id="welcome-heading">{{ $t("dashboard.welcome") }}</h1>
        <p>{{ $t("dashboard.subtitle") }}</p>
      </el-card>
    </section>

    <section class="stats-section" aria-labelledby="stats-heading">
      <h2 id="stats-heading" class="sr-only">
        {{ $t("dashboard.statistics") }}
      </h2>
      <el-row
        :gutter="4"
        class="stats-row"
        role="list"
        aria-label="Election statistics"
      >
        <el-col role="listitem">
          <el-card class="stat-card">
            <div class="stat-icon elections" aria-hidden="true">
              <el-icon>
                <Document />
              </el-icon>
            </div>
            <div class="stat-content">
              <div
                class="stat-value"
                aria-label="{{ statistics.totalElections }} total elections"
              >
                {{ statistics.totalElections }}
              </div>
              <div class="stat-label">{{ $t("dashboard.totalElections") }}</div>
            </div>
          </el-card>
        </el-col>
        <el-col role="listitem">
          <el-card class="stat-card">
            <div class="stat-icon active" aria-hidden="true">
              <el-icon>
                <CircleCheck />
              </el-icon>
            </div>
            <div class="stat-content">
              <div
                class="stat-value"
                aria-label="{{ statistics.activeElections }} active elections"
              >
                {{ statistics.activeElections }}
              </div>
              <div class="stat-label">
                {{ $t("dashboard.activeElections") }}
              </div>
            </div>
          </el-card>
        </el-col>
      </el-row>
    </section>

    <section
      class="recent-elections-section"
      aria-labelledby="recent-elections-heading"
    >
      <el-card class="recent-elections-card">
        <template #header>
          <div class="card-header">
            <h2 id="recent-elections-heading">
              {{ $t("dashboard.recentElections") }}
            </h2>
            <el-button
              type="primary"
              aria-label="Create new election"
              @click="createElection"
            >
              <el-icon aria-hidden="true">
                <Plus />
              </el-icon>
              {{ $t("elections.createNew") }}
            </el-button>
          </div>
        </template>
        <div
          v-if="loading"
          class="loading-container"
          aria-live="polite"
          aria-label="Loading recent elections"
        >
          <el-skeleton :rows="3" animated />
        </div>
        <div v-else-if="elections.length === 0" class="empty-state">
          <el-empty
            :description="$t('dashboard.noElections')"
            aria-live="polite"
          >
            <el-button
              type="primary"
              aria-label="Create your first election"
              @click="createElection"
            >
              {{ $t("elections.createFirst") }}
            </el-button>
          </el-empty>
        </div>
        <div
          v-else
          role="table"
          aria-label="Recent elections table"
          class="elections-table-container"
        >
          <el-table
            :data="elections"
            style="width: 100%"
            :expand-row-keys="expandedRowKeys"
            row-key="electionGuid"
          >
            <el-table-column type="expand">
              <template #default="{ row }">
                <div class="expanded-content">
                  <el-row :gutter="20">
                    <!-- Left Column: Election Details -->
                    <el-col :xs="24" :md="12">
                      <div class="detail-section">
                        <h4>{{ $t("dashboard.electionDetails") }}</h4>
                        <div class="detail-row">
                          <span class="detail-label"
                            >{{ $t("elections.convenor") }}:</span
                          >
                          <span class="detail-value">{{
                            row.convenor || "-"
                          }}</span>
                        </div>
                        <div class="detail-row">
                          <span class="detail-label"
                            >{{ $t("elections.type") }}:</span
                          >
                          <span class="detail-value">{{
                            row.electionType || "-"
                          }}</span>
                        </div>
                        <div class="detail-row">
                          <span class="detail-label"
                            >{{ $t("elections.mode") }}:</span
                          >
                          <span class="detail-value">{{
                            row.electionMode || "-"
                          }}</span>
                        </div>
                        <div class="detail-row">
                          <span class="detail-label"
                            >{{ $t("elections.toElect") }}:</span
                          >
                          <span class="detail-value">{{
                            row.numberToElect || 0
                          }}</span>
                        </div>
                        <div class="detail-row">
                          <span class="detail-label"
                            >{{ $t("dashboard.canVote") }}:</span
                          >
                          <span class="detail-value">{{ row.voterCount }}</span>
                        </div>
                        <div class="detail-row">
                          <span class="detail-label"
                            >{{ $t("dashboard.registered") }}:</span
                          >
                          <span class="detail-value">{{ row.voterCount }}</span>
                        </div>
                        <div class="detail-row">
                          <span class="detail-label"
                            >{{ $t("dashboard.ballotsEntered") }}:</span
                          >
                          <span class="detail-value">{{
                            row.ballotCount
                          }}</span>
                        </div>
                      </div>
                    </el-col>

                    <!-- Right Column: Online Voting & Ballots -->
                    <el-col :xs="24" :md="12">
                      <div class="detail-section">
                        <h4>{{ $t("dashboard.onlineVoting") }}</h4>
                        <div class="detail-row">
                          <span class="detail-label"
                            >{{ $t("dashboard.opens") }}:</span
                          >
                          <span class="detail-value">{{
                            formatDateTime(row.onlineWhenOpen)
                          }}</span>
                        </div>
                        <div class="detail-row">
                          <span class="detail-label"
                            >{{ $t("dashboard.closes") }}:</span
                          >
                          <span class="detail-value">{{
                            formatDateTime(row.onlineWhenClose)
                          }}</span>
                        </div>
                      </div>

                      <div class="detail-section" style="margin-top: 20px">
                        <h4>{{ $t("dashboard.tellers") }}</h4>
                        <div class="detail-row">
                          <span class="detail-label"
                            >{{ $t("dashboard.tellersStatus") }}:</span
                          >
                          <span class="detail-value">
                            <el-tag
                              :type="
                                row.isTellerAccessOpen ? 'success' : 'info'
                              "
                            >
                              {{
                                row.isTellerAccessOpen
                                  ? $t("dashboard.open")
                                  : $t("dashboard.closed")
                              }}
                            </el-tag>
                          </span>
                        </div>
                      </div>
                    </el-col>
                  </el-row>

                  <div class="expanded-actions">
                    <el-button
                      type="primary"
                      @click="viewElection(row.electionGuid)"
                    >
                      {{ $t("common.enter") }}
                    </el-button>
                    <el-button @click="viewElection(row.electionGuid)">
                      {{ $t("dashboard.otherActions") }}
                    </el-button>
                  </div>
                </div>
              </template>
            </el-table-column>
            <el-table-column
              prop="name"
              :label="$t('elections.name')"
              min-width="200"
            >
              <template #default="scope">
                <div class="election-name">
                  <el-tag
                    v-if="scope.row.showAsTest"
                    type="danger"
                    size="small"
                    class="test-badge"
                    >TEST</el-tag
                  >
                  {{ scope.row.name }}
                </div>
              </template>
            </el-table-column>
            <el-table-column
              prop="dateOfElection"
              :label="$t('elections.date')"
              width="140"
            >
              <template #default="scope">
                <time :datetime="scope.row.dateOfElection">{{
                  formatDate(scope.row.dateOfElection)
                }}</time>
              </template>
            </el-table-column>
            <el-table-column
              prop="tallyStatus"
              :label="$t('elections.status')"
              width="120"
            >
              <template #default="scope">
                <el-tag :type="getStatusType(scope.row.tallyStatus || 'Draft')">
                  {{ scope.row.tallyStatus || "Draft" }}
                </el-tag>
              </template>
            </el-table-column>
            <el-table-column
              :label="$t('dashboard.onlineVoting')"
              width="140"
              align="center"
            >
              <template #default="scope">
                <div class="status-indicator">
                  <el-switch
                    :model-value="scope.row.isOnlineVotingEnabled === true"
                    :disabled="true"
                    inline-prompt
                    :active-text="$t('dashboard.open')"
                    :inactive-text="$t('dashboard.closed')"
                  />
                </div>
              </template>
            </el-table-column>
            <el-table-column
              :label="$t('dashboard.tellersStatus')"
              width="140"
              align="center"
            >
              <template #default="scope">
                <div class="status-indicator">
                  <el-switch
                    :model-value="scope.row.isTellerAccessOpen === true"
                    :disabled="true"
                    inline-prompt
                    :active-text="$t('dashboard.open')"
                    :inactive-text="$t('dashboard.closed')"
                  />
                </div>
              </template>
            </el-table-column>
            <el-table-column
              :label="$t('common.actions')"
              width="120"
              fixed="right"
              align="center"
            >
              <template #default="scope">
                <el-button
                  type="primary"
                  size="small"
                  :aria-label="'Enter election: ' + scope.row.name"
                  @click="viewElection(scope.row.electionGuid)"
                >
                  {{ $t("common.enter") }}
                </el-button>
              </template>
            </el-table-column>
          </el-table>
        </div>
      </el-card>
    </section>
  </main>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from "vue";
import { useRouter } from "vue-router";
import { Document, CircleCheck, Plus } from "@element-plus/icons-vue";
import { useElectionStore } from "../stores/electionStore";
import type { ElectionSummaryDto } from "../types";

const router = useRouter();
const electionStore = useElectionStore();
const expandedRowKeys = ref<string[]>([]);

const elections = computed<ElectionSummaryDto[]>(() => {
  return (electionStore.elections as ElectionSummaryDto[])
    .slice()
    .sort((a, b) => {
      const dateA = a.dateOfElection ? new Date(a.dateOfElection).getTime() : 0;
      const dateB = b.dateOfElection ? new Date(b.dateOfElection).getTime() : 0;
      return dateB - dateA;
    })
    .slice(0, 5);
});

const loading = computed(() => electionStore.loading);

const statistics = computed(() => ({
  totalElections: electionStore.elections.length,
  activeElections: electionStore.activeElections.length,
}));

onMounted(async () => {
  await loadDashboardData();
});

async function loadDashboardData() {
  try {
    await electionStore.fetchElections();
    await electionStore.initializeSignalR();
  } catch (error) {
    console.error("Failed to load dashboard data:", error);
  }
}

function createElection() {
  router.push("/elections/create");
}

function viewElection(guid: string) {
  router.push(`/elections/${guid}`);
}

function formatDate(date: string) {
  if (!date) return "-";
  return new Date(date).toLocaleDateString();
}

function formatDateTime(date: string | null | undefined) {
  if (!date) return "-";
  const dateObj = new Date(date);
  return dateObj.toLocaleString();
}

function getStatusType(status: string) {
  const typeMap: Record<string, any> = {
    Draft: "info",
    Voting: "success",
    Tallying: "warning",
    Finalized: "info",
  };
  return typeMap[status] || "info";
}
</script>

<style lang="less">
.dashboard-page {
  // max-width: 1400px;
  margin: 0 auto;
  padding: var(--spacing-6) var(--spacing-4);

  .election-name {
    display: flex;
    align-items: center;
    gap: var(--spacing-2);

    .test-badge {
      font-weight: bold;
    }
  }

  .status-indicator {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: var(--spacing-1);

    .status-time {
      font-size: var(--font-size-xs);
      color: var(--color-text-secondary);
    }
  }

  .expanded-content {
    padding: var(--spacing-6);
    background-color: var(--color-bg-secondary);
    border-radius: var(--radius-md);

    .detail-section {
      margin-bottom: var(--spacing-4);

      h4 {
        margin: 0 0 var(--spacing-3) 0;
        font-size: var(--font-size-lg);
        font-weight: var(--font-weight-semibold);
        color: var(--color-text-primary);
        border-bottom: 2px solid var(--color-primary-500);
        padding-bottom: var(--spacing-2);
      }

      .detail-row {
        display: flex;
        justify-content: space-between;
        padding: var(--spacing-2) 0;
        border-bottom: 1px solid var(--color-border-light);

        &:last-child {
          border-bottom: none;
        }

        .detail-label {
          font-weight: var(--font-weight-medium);
          color: var(--color-text-secondary);
        }

        .detail-value {
          color: var(--color-text-primary);
          text-align: right;
        }
      }
    }

    .expanded-actions {
      margin-top: var(--spacing-6);
      padding-top: var(--spacing-4);
      border-top: 2px solid var(--color-border);
      display: flex;
      gap: var(--spacing-3);
      justify-content: flex-end;
    }
  }
}

.welcome-section {
  margin-bottom: var(--spacing-8);
}

.welcome-card {
  text-align: center;
  transition: var(--transition-normal);
}

.welcome-card:hover {
  transform: translateY(-2px);
  box-shadow: var(--shadow-lg);
}

.welcome-card h1 {
  margin: 0 0 var(--spacing-3) 0;
  color: var(--color-text-primary);
  font-size: var(--font-size-4xl);
  font-weight: var(--font-weight-bold);
  line-height: var(--line-height-tight);
}

.welcome-card p {
  margin: 0;
  color: var(--color-text-secondary);
  font-size: var(--font-size-lg);
  line-height: var(--line-height-relaxed);
}

.el-col-md-6 {
  max-width: none;
}

.stats-section {
  margin-bottom: var(--spacing-8);
}

.stats-row {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
  gap: var(--spacing-6);
}

.stat-card {
  transition: var(--transition-normal);
  cursor: pointer;
  position: relative;
  overflow: hidden;
}

.stat-card::before {
  content: "";
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: linear-gradient(
    135deg,
    rgba(255, 255, 255, 0.1) 0%,
    rgba(255, 255, 255, 0) 100%
  );
  opacity: 0;
  transition: var(--transition-normal);
}

.stat-card:hover {
  transform: translateY(-4px);
  box-shadow: var(--shadow-xl);
}

.stat-card:hover::before {
  opacity: 1;
}

.stat-card .el-card__body {
  display: flex;
  align-items: center;
  gap: var(--spacing-4);
  padding: var(--spacing-6);
}

.stat-icon {
  width: 3.5rem;
  height: 3.5rem;
  border-radius: var(--radius-lg);
  display: flex;
  align-items: center;
  justify-content: center;
  margin-right: var(--spacing-4);
  flex-shrink: 0;
  transition: var(--transition-normal);
}

.stat-icon.elections {
  background: linear-gradient(135deg, #1c3a6a 0%, #2563a8 100%);
  box-shadow: 0 4px 12px rgba(28, 58, 106, 0.3);
}

.stat-icon.active {
  background: linear-gradient(135deg, #f47920 0%, #d4661a 100%);
  box-shadow: 0 4px 12px rgba(244, 121, 32, 0.3);
}

.stat-icon.voters {
  background: linear-gradient(135deg, #8dc63f 0%, #5a9e1a 100%);
  box-shadow: 0 4px 12px rgba(141, 198, 63, 0.3);
}

.stat-icon.ballots {
  background: linear-gradient(135deg, #2563a8 0%, #1c3a6a 100%);
  box-shadow: 0 4px 12px rgba(37, 99, 168, 0.3);
}

.stat-icon .el-icon {
  font-size: 1.5rem;
  color: white;
  transition: var(--transition-normal);
}

.stat-card:hover .stat-icon {
  transform: scale(1.1);
}

.stat-content {
  flex: 1;
  min-width: 0;
}

.stat-value {
  font-size: var(--font-size-3xl);
  font-weight: var(--font-weight-bold);
  color: var(--color-text-primary);
  line-height: var(--line-height-tight);
  margin-bottom: var(--spacing-1);
  display: block;
}

.stat-label {
  font-size: var(--font-size-sm);
  color: var(--color-text-secondary);
  font-weight: var(--font-weight-medium);
  text-transform: uppercase;
  letter-spacing: 0.025em;
}

.recent-elections-section {
  margin-bottom: var(--spacing-8);
}

.recent-elections-card {
  transition: var(--transition-normal);
}

.recent-elections-card:hover {
  box-shadow: var(--shadow-lg);
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: var(--spacing-4);
}

.card-header h2 {
  margin: 0;
  color: var(--color-text-primary);
  font-size: var(--font-size-2xl);
  font-weight: var(--font-weight-semibold);
}

.loading-container {
  padding: var(--spacing-8);
  text-align: center;
}

.empty-state {
  padding: var(--spacing-12) var(--spacing-6);
  text-align: center;
}

.empty-state .el-empty__description p {
  color: var(--color-text-secondary);
  font-size: var(--font-size-base);
}

/* Mobile responsiveness */
@media (max-width: 768px) {
  .dashboard-page {
    padding: var(--spacing-4) var(--spacing-3);

    .welcome-section {
      margin-bottom: var(--spacing-6);
    }

    .welcome-card h1 {
      font-size: var(--font-size-3xl);
    }

    .stats-section {
      margin-bottom: var(--spacing-6);
    }

    .stats-row {
      grid-template-columns: 1fr;
      gap: var(--spacing-4);
    }

    .stat-card .el-card__body {
      padding: var(--spacing-5);
    }

    .stat-icon {
      width: 3.5rem;
      height: 3.5rem;
      margin-right: var(--spacing-3);
    }

    .stat-icon .el-icon {
      font-size: 1.5rem;
    }

    .stat-value {
      font-size: var(--font-size-2xl);
    }

    .stat-label {
      font-size: var(--font-size-xs);
    }

    .recent-elections-section {
      margin-bottom: var(--spacing-6);
    }

    .card-header {
      flex-direction: column;
      align-items: flex-start;
      gap: var(--spacing-3);
    }

    .card-header h2 {
      font-size: var(--font-size-xl);
    }

    .loading-container {
      padding: var(--spacing-6);
    }

    .empty-state {
      padding: var(--spacing-8) var(--spacing-4);
    }

    .expanded-content {
      padding: var(--spacing-4);

      .expanded-actions {
        flex-direction: column;

        .el-button {
          width: 100%;
        }
      }
    }
  }
}

@media (max-width: 480px) {
  .dashboard-page {
    padding: var(--spacing-3) var(--spacing-2);

    .welcome-card h1 {
      font-size: var(--font-size-2xl);
    }

    .stat-card .el-card__body {
      padding: var(--spacing-4);
    }

    .stat-icon {
      width: 3rem;
      height: 3rem;
      margin-right: var(--spacing-3);
    }

    .stat-icon .el-icon {
      font-size: 1.25rem;
    }

    .stat-value {
      font-size: var(--font-size-xl);
    }

    .welcome-section {
      margin-bottom: var(--spacing-5);
    }

    .stats-section {
      margin-bottom: var(--spacing-5);
    }

    .recent-elections-section {
      margin-bottom: var(--spacing-5);
    }

    /* Enhanced table styling */
    .el-table {
      border-radius: var(--radius-lg);
      overflow: hidden;
    }

    .el-table th {
      background: var(--color-gray-50);
      font-weight: var(--font-weight-semibold);
      color: var(--color-text-primary);
      border-bottom: 2px solid var(--color-gray-200);
    }

    .el-table td {
      border-bottom: 1px solid var(--color-gray-100);
    }

    .el-table tbody tr:hover > td {
      background-color: var(--color-gray-50);
    }

    .el-button--primary {
      background-color: var(--color-primary-500);
      border-color: var(--color-primary-500);
      transition: var(--transition-normal);
    }

    .el-button--primary:hover {
      background-color: var(--color-primary-600);
      border-color: var(--color-primary-600);
      transform: translateY(-1px);
      box-shadow: var(--shadow-md);
    }

    .el-tag {
      border-radius: var(--radius-sm);
      font-weight: var(--font-weight-medium);
    }

    .el-tag--success {
      background-color: var(--color-success-50);
      border-color: var(--color-success-200);
      color: var(--color-success-700);
    }

    .el-tag--warning {
      background-color: var(--color-warning-50);
      border-color: var(--color-warning-200);
      color: var(--color-warning-700);
    }

    .el-tag--info {
      background-color: var(--color-primary-50);
      border-color: var(--color-primary-200);
      color: var(--color-primary-700);
    }
  }
}
</style>
