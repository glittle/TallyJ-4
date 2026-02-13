<script setup lang="ts">
import { ref, computed, onMounted, watch } from "vue";
import { useRouter } from "vue-router";
import {
  Document,
  CircleCheck,
  Clock,
  Finished,
  Search,
} from "@element-plus/icons-vue";
import { useSuperAdminStore } from "../stores/superAdminStore";
import type {
  SuperAdminElectionFilter,
  SuperAdminElection,
} from "../services/superAdminService";
import { useDebounceFn } from '@vueuse/core';

const router = useRouter();
const superAdminStore = useSuperAdminStore();

const searchText = ref("");
const statusFilter = ref("");
const typeFilter = ref("");
const sortBy = ref("dateOfElection");
const sortDirection = ref("desc");
const drawerVisible = ref(false);

const loading = computed(() => superAdminStore.loading);
const summary = computed(() => superAdminStore.summary);
const elections = computed(() => superAdminStore.elections);
const selectedElection = computed(() => superAdminStore.selectedElection);
const totalCount = computed(() => superAdminStore.totalCount);
const currentPage = computed(() => superAdminStore.currentPage);
const pageSize = computed(() => superAdminStore.pageSize);
const error = computed(() => superAdminStore.error);

const statusOptions = [
  { label: "All", value: "" },
  { label: "Draft", value: "Draft" },
  { label: "Voting", value: "Voting" },
  { label: "Tallying", value: "Tallying" },
  { label: "Finalized", value: "Finalized" },
];

const typeOptions = [
  { label: "All", value: "" },
  { label: "LSA", value: "LSA" },
  { label: "National", value: "National" },
  { label: "Unit", value: "Unit" },
  { label: "Ridvan", value: "Ridvan" },
  { label: "By-election", value: "By-election" },
];

function buildFilter(): SuperAdminElectionFilter {
  return {
    search: searchText.value || undefined,
    status: statusFilter.value || undefined,
    electionType: typeFilter.value || undefined,
    sortBy: sortBy.value,
    sortDirection: sortDirection.value,
    page: currentPage.value,
    pageSize: pageSize.value,
  };
}

async function loadData() {
  try {
    await Promise.all([
      superAdminStore.fetchSummary(),
      superAdminStore.fetchElections(buildFilter()),
    ]);
  } catch {
    // error stored in store
  }
}

const applyFilters = useDebounceFn(async () => {

  superAdminStore.currentPage = 1;
  try {
    await superAdminStore.fetchElections(buildFilter());
  } catch {
    // error stored in store
  }
}, 150);

async function handlePageChange(page: number) {
  superAdminStore.currentPage = page;
  try {
    await superAdminStore.fetchElections(buildFilter());
  } catch {
    // error stored in store
  }
}

const handleSortChange = useDebounceFn(async ({
  prop,
  order,
}: {
  prop: string;
  order: string | null;
}) => {
  console.log("Sorting by", prop, "order", order);
  sortBy.value = prop || "dateOfElection";
  sortDirection.value = order === "ascending" ? "asc" : "desc";
  try {
    await superAdminStore.fetchElections(buildFilter());
  } catch {
    // error stored in store
  }
}, 150);

const showElectionDetail = async (row: SuperAdminElection) => {
  drawerVisible.value = true;
  try {
    await superAdminStore.fetchElectionDetail(row.electionGuid);
  } catch {
    // error stored in store
  }
};

function formatDate(date?: string) {
  if (!date) return "-";
  return new Date(date).toLocaleDateString();
}

function getStatusType(status?: string) {
  const typeMap: Record<string, string> = {
    Draft: "info",
    Voting: "success",
    Tallying: "warning",
    Finalized: "",
  };
  return typeMap[status || ""] || "info";
}

watch([searchText, statusFilter, typeFilter], () => {
  applyFilters();
});

onMounted(() => {
  if (!superAdminStore.isSuperAdmin) {
    router.replace("/dashboard");
    return;
  }
  loadData();
});
</script>
<template>
  <main class="sa-dashboard-page">
    <section class="sa-header-section">
      <h1>{{ $t("superAdmin.title") }}</h1>
    </section>

    <el-alert v-if="error" type="error" :title="error" show-icon closable @close="superAdminStore.clearError()" />

    <section class="sa-stats-section">
      <el-row :gutter="20" class="sa-stats-row">
        <el-col>
          <el-card class="stat-card">
            <div class="stat-icon elections" aria-hidden="true">
              <el-icon>
                <Document />
              </el-icon>
            </div>
            <div class="stat-content">
              <div class="stat-value">{{ summary?.totalElections ?? 0 }}</div>
              <div class="stat-label">{{ $t("superAdmin.summary.totalElections") }}</div>
            </div>
          </el-card>
        </el-col>
        <el-col>
          <el-card class="stat-card">
            <div class="stat-icon active" aria-hidden="true">
              <el-icon>
                <CircleCheck />
              </el-icon>
            </div>
            <div class="stat-content">
              <div class="stat-value">{{ summary?.openElections ?? 0 }}</div>
              <div class="stat-label">{{ $t("superAdmin.summary.openElections") }}</div>
            </div>
          </el-card>
        </el-col>
        <el-col>
          <el-card class="stat-card">
            <div class="stat-icon voters" aria-hidden="true">
              <el-icon>
                <Clock />
              </el-icon>
            </div>
            <div class="stat-content">
              <div class="stat-value">{{ summary?.upcomingElections ?? 0 }}</div>
              <div class="stat-label">{{ $t("superAdmin.summary.upcomingElections") }}</div>
            </div>
          </el-card>
        </el-col>
        <el-col>
          <el-card class="stat-card">
            <div class="stat-icon ballots" aria-hidden="true">
              <el-icon>
                <Finished />
              </el-icon>
            </div>
            <div class="stat-content">
              <div class="stat-value">{{ summary?.completedElections ?? 0 }}</div>
              <div class="stat-label">{{ $t("superAdmin.summary.completedElections") }}</div>
            </div>
          </el-card>
        </el-col>
      </el-row>
    </section>

    <section class="sa-elections-section">
      <el-card>
        <template #header>
          <div class="sa-filter-bar">
            <el-input v-model="searchText" :placeholder="$t('superAdmin.elections.search')" :prefix-icon="Search"
              clearable class="sa-search-input" />
            <el-select v-model="statusFilter" :placeholder="$t('superAdmin.elections.filterStatus')" clearable
              class="sa-filter-select">
              <el-option v-for="opt in statusOptions" :key="opt.value" :label="opt.label" :value="opt.value" />
            </el-select>
            <el-select v-model="typeFilter" :placeholder="$t('superAdmin.elections.filterType')" clearable
              class="sa-filter-select">
              <el-option v-for="opt in typeOptions" :key="opt.value" :label="opt.label" :value="opt.value" />
            </el-select>
          </div>
        </template>

        <div v-if="loading" class="sa-loading">
          <el-skeleton :rows="5" animated />
        </div>
        <div v-else-if="elections.length === 0" class="sa-empty">
          <el-empty :description="$t('superAdmin.elections.noResults')" />
        </div>
        <template v-else>
          <el-table :data="elections" style="width: 100%" @sort-change="handleSortChange"
            @row-click="showElectionDetail" class="sa-elections-table">
            <el-table-column prop="name" :label="$t('superAdmin.elections.name')" min-width="200" sortable="custom" />
            <el-table-column prop="convenor" :label="$t('superAdmin.elections.convenor')" min-width="150"
              sortable="custom" />
            <el-table-column prop="dateOfElection" :label="$t('superAdmin.elections.date')" width="140"
              sortable="custom">
              <template #default="scope">
                {{ formatDate(scope.row.dateOfElection) }}
              </template>
            </el-table-column>
            <el-table-column prop="tallyStatus" :label="$t('superAdmin.elections.status')" width="120"
              sortable="custom">
              <template #default="scope">
                <el-tag :type="getStatusType(scope.row.tallyStatus)">
                  {{ scope.row.tallyStatus || "Draft" }}
                </el-tag>
              </template>
            </el-table-column>
            <el-table-column prop="electionType" :label="$t('superAdmin.elections.type')" width="120"
              sortable="custom" />
            <el-table-column prop="voterCount" :label="$t('superAdmin.elections.voters')" width="100"
              sortable="custom" />
            <el-table-column prop="ballotCount" :label="$t('superAdmin.elections.ballots')" width="100"
              sortable="custom" />
            <el-table-column prop="ownerEmail" :label="$t('superAdmin.elections.owner')" min-width="180" />
          </el-table>

          <div class="sa-pagination">
            <el-pagination v-model:current-page="superAdminStore.currentPage" :page-size="pageSize" :total="totalCount"
              layout="total, prev, pager, next" @current-change="handlePageChange" />
          </div>
        </template>
      </el-card>
    </section>

    <el-drawer v-model="drawerVisible" :title="$t('superAdmin.detail.title')" direction="rtl" size="500px">
      <div v-if="loading && !selectedElection" class="sa-loading">
        <el-skeleton :rows="6" animated />
      </div>
      <div v-else-if="selectedElection" class="sa-detail">
        <el-descriptions :column="1" border>
          <el-descriptions-item :label="$t('superAdmin.elections.name')">
            {{ selectedElection.name }}
          </el-descriptions-item>
          <el-descriptions-item :label="$t('superAdmin.elections.convenor')">
            {{ selectedElection.convenor || "-" }}
          </el-descriptions-item>
          <el-descriptions-item :label="$t('superAdmin.elections.date')">
            {{ formatDate(selectedElection.dateOfElection) }}
          </el-descriptions-item>
          <el-descriptions-item :label="$t('superAdmin.elections.status')">
            <el-tag :type="getStatusType(selectedElection.tallyStatus)">
              {{ selectedElection.tallyStatus || "Draft" }}
            </el-tag>
          </el-descriptions-item>
          <el-descriptions-item :label="$t('superAdmin.elections.type')">
            {{ selectedElection.electionType || "-" }}
          </el-descriptions-item>
          <el-descriptions-item :label="$t('superAdmin.detail.electionMode')">
            {{ selectedElection.electionMode || "-" }}
          </el-descriptions-item>
          <el-descriptions-item :label="$t('superAdmin.detail.numberToElect')">
            {{ selectedElection.numberToElect ?? "-" }}
          </el-descriptions-item>
          <el-descriptions-item :label="$t('superAdmin.elections.voters')">
            {{ selectedElection.voterCount }}
          </el-descriptions-item>
          <el-descriptions-item :label="$t('superAdmin.elections.ballots')">
            {{ selectedElection.ballotCount }}
          </el-descriptions-item>
          <el-descriptions-item :label="$t('superAdmin.detail.locations')">
            {{ selectedElection.locationCount }}
          </el-descriptions-item>
          <el-descriptions-item :label="$t('superAdmin.detail.percentComplete')">
            <el-progress :percentage="selectedElection.percentComplete" :stroke-width="16" />
          </el-descriptions-item>
        </el-descriptions>

        <div class="sa-owners-section">
          <h3>{{ $t("superAdmin.detail.owners") }}</h3>
          <el-table :data="selectedElection.owners" style="width: 100%" size="small">
            <el-table-column prop="displayName" label="Name" />
            <el-table-column prop="email" label="Email" />
            <el-table-column prop="role" label="Role" width="100" />
          </el-table>
        </div>
      </div>
    </el-drawer>
  </main>
</template>

<style lang="less">
.sa-dashboard-page {
  margin: 0 auto;
  padding: var(--spacing-6) var(--spacing-4);

  .sa-header-section {
    margin-bottom: var(--spacing-6);

    h1 {
      margin: 0;
      color: var(--color-text-primary);
      font-size: var(--font-size-3xl);
      font-weight: var(--font-weight-bold);
    }
  }

  .sa-stats-section {
    margin-bottom: var(--spacing-8);
  }

  .sa-stats-row {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
    gap: var(--spacing-6);
  }

  .stat-card {
    transition: var(--transition-normal);
    cursor: default;
    position: relative;
    overflow: hidden;
  }

  .stat-card:hover {
    transform: translateY(-4px);
    box-shadow: var(--shadow-xl);
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
  }

  .stat-label {
    font-size: var(--font-size-sm);
    color: var(--color-text-secondary);
    font-weight: var(--font-weight-medium);
    text-transform: uppercase;
    letter-spacing: 0.025em;
  }

  .sa-elections-section {
    margin-bottom: var(--spacing-8);
  }

  .sa-filter-bar {
    display: flex;
    gap: var(--spacing-4);
    flex-wrap: wrap;
    align-items: center;
  }

  .sa-search-input {
    flex: 1;
    min-width: 200px;
  }

  .sa-filter-select {
    width: 180px;
  }

  .sa-elections-table {
    cursor: pointer;
  }

  .sa-loading {
    padding: var(--spacing-8);
  }

  .sa-empty {
    padding: var(--spacing-12) var(--spacing-6);
    text-align: center;
  }

  .sa-pagination {
    display: flex;
    justify-content: flex-end;
    margin-top: var(--spacing-4);
  }

  .sa-detail {
    .sa-owners-section {
      margin-top: var(--spacing-6);

      h3 {
        margin: 0 0 var(--spacing-3) 0;
        font-size: var(--font-size-lg);
        font-weight: var(--font-weight-semibold);
        color: var(--color-text-primary);
      }
    }
  }

  @media (max-width: 768px) {
    padding: var(--spacing-4) var(--spacing-3);

    .sa-stats-row {
      grid-template-columns: 1fr;
      gap: var(--spacing-4);
    }

    .sa-filter-bar {
      flex-direction: column;
    }

    .sa-search-input {
      width: 100%;
    }

    .sa-filter-select {
      width: 100%;
    }
  }
}
</style>
