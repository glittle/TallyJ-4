<template>
  <main class="dashboard-page">
    <section class="welcome-section" aria-labelledby="welcome-heading">
      <el-card class="welcome-card">
        <h1 id="welcome-heading">{{ $t("dashboard.welcome") }}</h1>
        <p>{{ $t("dashboard.subtitle") }}</p>
      </el-card>
    </section>

    <section class="stats-section" aria-labelledby="stats-heading">
      <h2 id="stats-heading" class="sr-only">{{ $t("dashboard.statistics") }}</h2>
      <el-row :gutter="20" class="stats-row" role="list" aria-label="Election statistics">
        <el-col :xs="24" :sm="12" role="listitem">
          <el-card class="stat-card">
            <div class="stat-icon elections" aria-hidden="true">
              <el-icon>
                <Document />
              </el-icon>
            </div>
            <div class="stat-content">
              <div class="stat-value" aria-label="{{ statistics.totalElections }} total elections">
                {{ statistics.totalElections }}
              </div>
              <div class="stat-label">{{ $t("dashboard.totalElections") }}</div>
            </div>
          </el-card>
        </el-col>
        <el-col :xs="24" :sm="12" role="listitem">
          <el-card class="stat-card">
            <div class="stat-icon active" aria-hidden="true">
              <el-icon>
                <CircleCheck />
              </el-icon>
            </div>
            <div class="stat-content">
              <div class="stat-value" aria-label="{{ statistics.activeElections }} active elections">
                {{ statistics.activeElections }}
              </div>
              <div class="stat-label">{{ $t("dashboard.activeElections") }}</div>
            </div>
          </el-card>
        </el-col>
      </el-row>
    </section>

    <section class="recent-elections-section" aria-labelledby="recent-elections-heading">
      <el-card class="recent-elections-card">
        <template #header>
          <div class="card-header">
            <h2 id="recent-elections-heading">{{ $t("dashboard.recentElections") }}</h2>
            <el-button type="primary" @click="createElection" aria-label="Create new election">
              <el-icon aria-hidden="true">
                <Plus />
              </el-icon>
              {{ $t("elections.createNew") }}
            </el-button>
          </div>
        </template>
        <div v-if="loading" class="loading-container" aria-live="polite" aria-label="Loading recent elections">
          <el-skeleton :rows="3" animated />
        </div>
        <div v-else-if="elections.length === 0" class="empty-state">
          <el-empty :description="$t('dashboard.noElections')" aria-live="polite">
            <el-button type="primary" @click="createElection" aria-label="Create your first election">
              {{ $t("elections.createFirst") }}
            </el-button>
          </el-empty>
        </div>
        <div v-else role="table" aria-label="Recent elections table" class="elections-table-container">
          <el-table :data="elections" style="width: 100%">
            <el-table-column prop="name" :label="$t('elections.name')" min-width="200" />
            <el-table-column prop="electionType" :label="$t('elections.type')" width="120" />
            <el-table-column prop="dateOfElection" :label="$t('elections.date')" width="140">
              <template #default="scope">
                <time :datetime="scope.row.dateOfElection">{{
                  formatDate(scope.row.dateOfElection)
                }}</time>
              </template>
            </el-table-column>
            <el-table-column prop="voterCount" :label="$t('elections.people')" width="100" align="center" />
            <el-table-column prop="ballotCount" :label="$t('elections.ballots')" width="100" align="center" />
            <el-table-column prop="tallyStatus" :label="$t('elections.status')" width="120">
              <template #default="scope">
                {{ scope.row.tallyStatus || "Draft" }}
              </template>
            </el-table-column>
            <el-table-column :label="$t('common.actions')" width="150" fixed="right">
              <template #default="scope">
                <el-button size="small" @click="viewElection(scope.row.electionGuid)"
                  :aria-label="'View election: ' + scope.row.name">
                  {{ $t("common.view") }}
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
import { computed, onMounted } from "vue";
import { useRouter } from "vue-router";
import {
  Document,
  CircleCheck,
  Plus,
} from "@element-plus/icons-vue";
import { useElectionStore } from "../stores/electionStore";

const router = useRouter();
const electionStore = useElectionStore();

const elections = computed(() => {
  // Sort by dateOfElection descending (most recent first), then take first 5
  return electionStore.elections
    .slice()
    .sort((a, b) => {
      const dateA = a.dateOfElection ? new Date(a.dateOfElection).getTime() : 0;
      const dateB = b.dateOfElection ? new Date(b.dateOfElection).getTime() : 0;
      return dateB - dateA; // Descending order
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
  background: linear-gradient(135deg,
      rgba(255, 255, 255, 0.1) 0%,
      rgba(255, 255, 255, 0) 100%);
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
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  box-shadow: 0 4px 12px rgba(102, 126, 234, 0.3);
}

.stat-icon.active {
  background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%);
  box-shadow: 0 4px 12px rgba(240, 147, 251, 0.3);
}

.stat-icon.voters {
  background: linear-gradient(135deg, #4facfe 0%, #00f2fe 100%);
  box-shadow: 0 4px 12px rgba(79, 172, 254, 0.3);
}

.stat-icon.ballots {
  background: linear-gradient(135deg, #43e97b 0%, #38f9d7 100%);
  box-shadow: 0 4px 12px rgba(67, 233, 123, 0.3);
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

    .el-table tbody tr:hover>td {
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
