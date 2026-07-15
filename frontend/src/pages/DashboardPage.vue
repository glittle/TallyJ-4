<script setup lang="ts">
// ResumeElectionCard removed: it duplicated the first list row (same election as
// the list's top entry after the default sort below). SetupTipsCard moved to
// ElectionDetailPage beside the details block — more useful during setup.
import { useApiErrorHandler } from "@/composables/useApiErrorHandler";
import { useNotifications } from "@/composables/useNotifications";
import { getActiveElectionHubGuid } from "@/utils/activeElectionHubStorage";
import { formatNumber } from "@/utils/formatNumber";
import { Plus, RefreshRight, Search, Upload } from "@element-plus/icons-vue";
import { computed, onMounted, ref, watch } from "vue";
import { useI18n } from "vue-i18n";
import { useRouter } from "vue-router";
import { STAGES } from "../domain/electionStages";
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

const filters = ref({
  search: "",
  status: "",
  type: "",
  dateRange: [] as Date[],
});

/**
 * Default sort: pin most-recently-opened election (session hub GUID — the same
 * "continue where I left off" idea the Resume card used), then dateOfElection
 * descending. Null dates sort last. User column clicks still override via
 * handleSortChange / sort.prop.
 */
const DEFAULT_SORT_PROP = "dateOfElection";
const sort = ref({
  prop: DEFAULT_SORT_PROP,
  order: "descending" as "ascending" | "descending" | null,
  /** When true, apply the resume-style default ordering instead of a pure column sort. */
  useDefaultOrder: true,
});

const statusFilterOptions = computed(() =>
  STAGES.map((stage) => ({
    label: t(`elections.stage.${stage}`),
    value: stage,
  })),
);

const pagination = ref({
  page: 1,
  pageSize: 20,
  total: 0,
});

/** Epoch ms for dateOfElection; null/invalid → 0 so they sort last when descending. */
function electionDateMs(election: ElectionDto): number {
  if (!election.dateOfElection) {
    return 0;
  }
  const ms = new Date(election.dateOfElection).getTime();
  return Number.isFinite(ms) ? ms : 0;
}

/**
 * Default list order (replaces Resume card prominence):
 * 1. Most recently opened election first (active hub GUID from session, set when
 *    opening an election). Fallback: latest dateOfElection — same proxy the
 *    former ResumeElectionCard used.
 * 2. Remaining elections by Election Date descending (future/recent first).
 */
function applyDefaultElectionOrder(list: ElectionDto[]): ElectionDto[] {
  const byDateDesc = [...list].sort(
    (a, b) => electionDateMs(b) - electionDateMs(a),
  );

  const lastOpenedGuid = getActiveElectionHubGuid();
  let pinGuid: string | null = null;

  if (
    lastOpenedGuid &&
    byDateDesc.some((e) => e.electionGuid === lastOpenedGuid)
  ) {
    // Prefer true last-opened when the user has opened an election this session.
    pinGuid = lastOpenedGuid;
  } else if (byDateDesc[0]) {
    // Resume-card fallback: highlight the election with the latest date.
    pinGuid = byDateDesc[0].electionGuid;
  }

  if (!pinGuid) {
    return byDateDesc;
  }

  const pinned = byDateDesc.find((e) => e.electionGuid === pinGuid);
  if (!pinned) {
    return byDateDesc;
  }

  return [pinned, ...byDateDesc.filter((e) => e.electionGuid !== pinGuid)];
}

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
      (election) => election.electionStage === filters.value.status,
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

  if (sort.value.useDefaultOrder || !sort.value.prop || !sort.value.order) {
    return applyDefaultElectionOrder(filtered);
  }

  // Manual column sort (user clicked a header) — pure prop order, no pin.
  filtered.sort((a, b) => {
    let aVal = (a as any)[sort.value.prop];
    let bVal = (b as any)[sort.value.prop];

    if (sort.value.prop === "dateOfElection") {
      aVal = electionDateMs(a);
      bVal = electionDateMs(b);
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
  // Element Plus passes null order when the user clears column sort → restore default.
  if (!prop || !order) {
    sort.value = {
      prop: DEFAULT_SORT_PROP,
      order: "descending",
      useDefaultOrder: true,
    };
    return;
  }
  sort.value = {
    prop,
    order,
    useDefaultOrder: false,
  };
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
    <section class="elections-section">
      <el-card>
        <template #header>
          <div class="card-header">
            <el-button text :loading="loading" @click="loadData">
              <el-icon>
                <RefreshRight />
              </el-icon>
              {{ $t("common.refresh") }}
            </el-button>
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
                <el-option
                  v-for="option in statusFilterOptions"
                  :key="option.value"
                  :label="option.label"
                  :value="option.value"
                />
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
              min-width="350"
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
              prop="electionStage"
              :label="$t('elections.status')"
              min-width="100"
              sortable="custom"
            >
              <template #default="scope">
                {{
                  scope.row.electionStage
                    ? $t(`elections.stage.${scope.row.electionStage}`)
                    : "-"
                }}
              </template>
            </el-table-column>
            <el-table-column
              prop="electionType"
              :label="$t('elections.type')"
              min-width="150"
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
              prop="dateOfElection"
              :label="$t('elections.date')"
              width="140"
              sortable="custom"
              align="center"
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
              align="center"
            />
            <el-table-column
              prop="voterCount"
              :label="$t('elections.people')"
              min-width="100"
              sortable="custom"
              align="center"
            >
              <template #default="scope">
                {{ formatNumber(scope.row.voterCount) ?? "—" }}
              </template>
            </el-table-column>
            <el-table-column
              prop="ballotCount"
              :label="$t('elections.ballots')"
              min-width="100"
              sortable="custom"
              align="center"
            >
              <template #default="scope">
                {{ formatNumber(scope.row.ballotCount) ?? "—" }}
              </template>
            </el-table-column>
          </el-table>

          <div v-if="allElections.length > 0" class="pagination-container">
            <el-pagination
              v-model:current-page="pagination.page"
              v-model:page-size="pagination.pageSize"
              :total="pagination.total"
              :default-page-size="10"
              hide-on-single-page
              layout="total, prev, pager, next"
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
    justify-content: flex-end;
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
    justify-content: flex-start;
    margin-top: var(--spacing-6);
    padding: 0 var(--spacing-4);
  }

  .el-pagination__total {
    font-weight: var(--font-weight-medium);
    font-size: var(--font-size-base);
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

.stat-content {
  flex: 1;
  min-width: 0;
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
  }
}
</style>
