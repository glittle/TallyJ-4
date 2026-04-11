<script setup lang="ts">
import { useApiErrorHandler } from "@/composables/useApiErrorHandler";
import { useNotifications } from "@/composables/useNotifications";
import {
  CircleCheck,
  Document,
  Plus,
  Search,
  Upload,
} from "@element-plus/icons-vue";
import { computed, onMounted, ref, watch } from "vue";
import { useI18n } from "vue-i18n";
import { useRouter } from "vue-router";
import { electionService } from "../services/electionService";
import { useElectionStore } from "../stores/electionStore";
import type { ElectionDto } from "../types";
import { extractApiErrorMessage } from "../utils/errorHandler";

const router = useRouter();
const { t } = useI18n();
const electionStore = useElectionStore();
const { handleApiError } = useApiErrorHandler();
const { showSuccessMessage, showErrorMessage } = useNotifications();

const loading = computed(() => electionStore.loading);
const allElections = computed(() => electionStore.elections);

const statistics = computed(() => ({
  totalElections: electionStore.elections.length,
  activeElections: electionStore.activeElections.length,
}));

const filters = ref({
  search: "",
  status: "",
  type: "",
  dateRange: [] as Date[],
});

const sort = ref({
  prop: "dateOfElection",
  order: "descending" as "ascending" | "descending",
});

const pagination = ref({
  page: 1,
  pageSize: 20,
  total: 0,
});

const filteredElectionsUnpaginated = computed(() => {
  let filtered = [...allElections.value];

  if (filters.value.search) {
    const search = filters.value.search.toLowerCase();
    filtered = filtered.filter(
      (election) =>
        election.name.toLowerCase().includes(search) ||
        election.convenor?.toLowerCase().includes(search),
    );
  }

  if (filters.value.status) {
    filtered = filtered.filter(
      (election) => (election.tallyStatus || "Draft") === filters.value.status,
    );
  }

  if (filters.value.type) {
    filtered = filtered.filter(
      (election) => election.electionType === filters.value.type,
    );
  }

  if (filters.value.dateRange && filters.value.dateRange.length === 2) {
    const [startDate, endDate] = filters.value.dateRange;
    filtered = filtered.filter((election) => {
      if (!election.dateOfElection) {
        return false;
      }
      const electionDate = new Date(election.dateOfElection);
      return (
        electionDate >= (startDate ?? 0) &&
        electionDate <= (endDate ?? Infinity)
      );
    });
  }

  if (sort.value.prop) {
    filtered.sort((a, b) => {
      let aVal = (a as any)[sort.value.prop];
      let bVal = (b as any)[sort.value.prop];

      if (sort.value.prop === "dateOfElection") {
        aVal = aVal ? new Date(aVal).getTime() : 0;
        bVal = bVal ? new Date(bVal).getTime() : 0;
      }

      if (typeof aVal === "string") {
        aVal = aVal.toLowerCase();
      }
      if (typeof bVal === "string") {
        bVal = bVal.toLowerCase();
      }

      if (aVal < bVal) {
        return sort.value.order === "ascending" ? -1 : 1;
      }
      if (aVal > bVal) {
        return sort.value.order === "ascending" ? 1 : -1;
      }
      return 0;
    });
  }

  return filtered;
});

watch(
  filteredElectionsUnpaginated,
  (filtered) => {
    pagination.value.total = filtered.length;
  },
  { immediate: true },
);

const filteredElections = computed(() => {
  const start = (pagination.value.page - 1) * pagination.value.pageSize;
  const end = start + pagination.value.pageSize;
  return filteredElectionsUnpaginated.value.slice(start, end);
});

const hasActiveFilters = computed(() => {
  return (
    filters.value.search ||
    filters.value.status ||
    filters.value.type ||
    (filters.value.dateRange && filters.value.dateRange.length > 0)
  );
});

onMounted(async () => {
  await loadData();
});

async function loadData() {
  try {
    await electionStore.fetchElections();
    await electionStore.initializeSignalR();
  } catch (error) {
    handleApiError(error);
  }
}

function createElection() {
  router.push("/elections/create");
}

async function importElection() {
  try {
    const input = document.createElement("input");
    input.type = "file";
    input.accept = ".json,.xml";
    input.onchange = async (event) => {
      const file = (event.target as HTMLInputElement).files?.[0];
      if (!file) {
        return;
      }

      try {
        let election: ElectionDto;
        if (file.name.toLowerCase().endsWith(".json")) {
          election = await electionService.importElectionFromFile(file);
        } else if (file.name.toLowerCase().endsWith(".xml")) {
          election = await electionService.importTallyJv3ElectionFromFile(file);
        } else {
          showErrorMessage(t("elections.importElectionError"));
          return;
        }

        showSuccessMessage(t("elections.importElectionSuccess"));
        await loadData();
        router.push(`/elections/${election.electionGuid}`);
      } catch (error: any) {
        showErrorMessage(
          extractApiErrorMessage(error) || t("elections.importElectionError"),
        );
      }
    };
    input.click();
  } catch (error: any) {
    showErrorMessage(
      extractApiErrorMessage(error) || t("elections.importElectionError"),
    );
  }
}

function openElection(guid: string) {
  router.push(`/elections/${guid}`);
}

function handleSearch() {
  pagination.value.page = 1;
}

function handleFilterChange() {
  pagination.value.page = 1;
}

function clearFilters() {
  filters.value = {
    search: "",
    status: "",
    type: "",
    dateRange: [],
  };
  pagination.value.page = 1;
}

function handleSortChange({ prop, order }: any) {
  sort.value.prop = prop;
  sort.value.order = order;
}

function handleSizeChange() {
  pagination.value.page = 1;
}

function handlePageChange() {}

function formatDate(date: string) {
  if (!date) {
    return "-";
  }
  return new Date(date).toLocaleDateString();
}
</script>

<template>
  <main class="dashboard-page">
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
              <div class="stat-value">
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
              <div class="stat-value">
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

    <section class="elections-section">
      <el-card>
        <template #header>
          <div class="card-header">
            <el-button
              :type="allElections.length ? 'info' : 'primary'"
              @click="createElection"
            >
              <el-icon>
                <Plus />
              </el-icon>
              {{ $t("elections.createNew") }}
            </el-button>
            <el-button type="info" @click="importElection">
              <el-icon>
                <Upload />
              </el-icon>
              {{ $t("elections.importElection") }}
            </el-button>
          </div>
        </template>

        <div class="filters-section">
          <el-row :gutter="20" align="middle">
            <el-col :span="6">
              <el-input
                v-model="filters.search"
                :placeholder="$t('elections.searchPlaceholder')"
                clearable
                @input="handleSearch"
              >
                <template #prefix>
                  <el-icon>
                    <Search />
                  </el-icon>
                </template>
              </el-input>
            </el-col>
            <el-col :span="4">
              <el-select
                v-model="filters.status"
                :placeholder="$t('elections.filterByStatus')"
                clearable
                @change="handleFilterChange"
              >
                <el-option label="Draft" value="Draft" />
                <el-option label="Voting" value="Voting" />
                <el-option label="Tallying" value="Tallying" />
                <el-option label="Finalized" value="Finalized" />
              </el-select>
            </el-col>
            <el-col :span="4">
              <el-select
                v-model="filters.type"
                :placeholder="$t('elections.filterByType')"
                clearable
                @change="handleFilterChange"
              >
                <el-option
                  :label="$t('elections.electionTypes.LSA')"
                  value="LSA"
                />
                <el-option
                  :label="$t('elections.electionTypes.LSA1')"
                  value="LSA1"
                />
                <el-option
                  :label="$t('elections.electionTypes.LSA2')"
                  value="LSA2"
                />
                <el-option
                  :label="$t('elections.electionTypes.NSA')"
                  value="NSA"
                />
                <el-option
                  :label="$t('elections.electionTypes.Con')"
                  value="Con"
                />
                <el-option
                  :label="$t('elections.electionTypes.Reg')"
                  value="Reg"
                />
                <el-option
                  :label="$t('elections.electionTypes.Oth')"
                  value="Oth"
                />
              </el-select>
            </el-col>
            <el-col :span="4">
              <el-date-picker
                v-model="filters.dateRange"
                type="daterange"
                :range-separator="$t('common.to')"
                :start-placeholder="$t('common.startDate')"
                :end-placeholder="$t('common.endDate')"
                @change="handleFilterChange"
              />
            </el-col>
            <el-col :span="6" class="text-right">
              <el-space>
                <el-button :disabled="!hasActiveFilters" @click="clearFilters">
                  {{ $t("common.clearFilters") }}
                </el-button>
              </el-space>
            </el-col>
          </el-row>
        </div>

        <div class="table-container">
          <div v-if="loading" class="loading-container" aria-live="polite">
            <el-skeleton :rows="3" animated />
          </div>
          <div v-else-if="allElections.length === 0" class="empty-state">
            <el-empty
              :description="$t('dashboard.noElections')"
              aria-live="polite"
            >
              <el-button type="primary" @click="createElection">
                {{ $t("elections.createFirst") }}
              </el-button>
            </el-empty>
          </div>
          <el-table
            v-else
            v-loading="loading"
            :data="filteredElections"
            style="width: 100%"
            :default-sort="{ prop: 'dateOfElection', order: 'descending' }"
            @sort-change="handleSortChange"
          >
            <el-table-column
              prop="name"
              :label="$t('elections.name')"
              min-width="250"
              sortable="custom"
            >
              <template #default="scope">
                <div
                  class="election-name clickable"
                  @click="openElection(scope.row.electionGuid)"
                >
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
              prop="electionType"
              :label="$t('elections.type')"
              min-width="120"
              sortable="custom"
            >
              <template #default="scope">
                {{
                  scope.row.electionType
                    ? $t(`elections.electionTypes.${scope.row.electionType}`)
                    : ""
                }}
              </template>
            </el-table-column>
            <el-table-column
              prop="tallyStatus"
              :label="$t('elections.status')"
              min-width="120"
              sortable="custom"
            >
              <template #default="scope">
                {{ scope.row.tallyStatus || "-" }}
              </template>
            </el-table-column>
            <el-table-column
              prop="dateOfElection"
              :label="$t('elections.date')"
              width="140"
              sortable="custom"
            >
              <template #default="scope">
                {{ formatDate(scope.row.dateOfElection) }}
              </template>
            </el-table-column>
            <el-table-column
              prop="numberToElect"
              :label="$t('elections.toElect')"
              width="100"
              sortable="custom"
            />
            <el-table-column
              prop="voterCount"
              :label="$t('elections.people')"
              min-width="100"
              sortable="custom"
            />
            <el-table-column
              prop="ballotCount"
              :label="$t('elections.ballots')"
              min-width="100"
              sortable="custom"
            />
          </el-table>

          <div v-if="allElections.length > 0" class="pagination-container">
            <el-pagination
              v-model:current-page="pagination.page"
              v-model:page-size="pagination.pageSize"
              :page-sizes="[10, 20, 50, 100]"
              :total="pagination.total"
              layout="total, sizes, prev, pager, next"
              @size-change="handleSizeChange"
              @current-change="handlePageChange"
            />
          </div>
        </div>
      </el-card>
    </section>
  </main>
</template>

<style lang="less">
.dashboard-page {
  margin: 0 auto;
  padding: var(--spacing-6) var(--spacing-4);

  .card-header {
    display: flex;
    justify-content: center;
    gap: 20px;
    align-items: center;
    margin: 0;
  }

  .election-name {
    display: flex;
    align-items: center;
    gap: var(--spacing-2);

    .test-badge {
      font-weight: bold;
    }
  }

  .clickable {
    cursor: pointer;
    color: var(--el-color-primary);
    transition: color 0.2s ease;

    &:hover {
      color: var(--el-color-primary-dark-2);
      text-decoration: underline;
    }
  }

  .filters-section {
    margin-bottom: var(--spacing-6);
    padding: var(--spacing-4);
    background-color: var(--color-bg-secondary);
    border-radius: var(--radius-lg);
  }

  .text-right {
    text-align: right;
  }

  .table-container {
    margin-top: var(--spacing-4);
  }

  .pagination-container {
    display: flex;
    justify-content: flex-end;
    margin-top: var(--spacing-6);
  }

  .loading-container {
    padding: var(--spacing-8);
    text-align: center;
  }

  .empty-state {
    padding: var(--spacing-12) var(--spacing-6);
    text-align: center;
  }
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

.elections-section {
  margin-bottom: var(--spacing-8);
}

@media (max-width: 768px) {
  .dashboard-page {
    padding: var(--spacing-4) var(--spacing-3);

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
  }
}
</style>
