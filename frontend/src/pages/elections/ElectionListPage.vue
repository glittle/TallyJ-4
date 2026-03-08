<template>
  <div class="election-list-page">
    <el-card>
      <template #header>
        <div class="card-header">
          <h2>{{ $t("elections.list") }}</h2>
          <el-button type="primary" @click="createElection">
            <el-icon>
              <Plus />
            </el-icon>
            {{ $t("elections.createNew") }}
          </el-button>
        </div>
      </template>

      <!-- Filters and Search -->
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
        <el-table
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
          />
          <el-table-column
            prop="electionType"
            :label="$t('elections.type')"
            width="120"
            sortable="custom"
          >
            <template #default="scope">
              <el-tag size="small">{{
                scope.row.electionType
                  ? $t(`elections.electionTypes.${scope.row.electionType}`)
                  : ""
              }}</el-tag>
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
          <el-table-column
            prop="tallyStatus"
            :label="$t('elections.status')"
            width="120"
            sortable="custom"
          >
            <template #default="scope">
              <el-tag :type="getStatusType(scope.row.tallyStatus)">
                {{ scope.row.tallyStatus || "Draft" }}
              </el-tag>
            </template>
          </el-table-column>
          <el-table-column
            :label="$t('common.actions')"
            width="120"
            fixed="right"
          >
            <template #default="scope">
              <el-button
                type="primary"
                size="small"
                @click="openElection(scope.row.electionGuid)"
              >
                {{ $t("common.open") }}
              </el-button>
            </template>
          </el-table-column>
        </el-table>

        <div class="pagination-container">
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
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch } from "vue";
import { useRouter } from "vue-router";
import { Plus, Search } from "@element-plus/icons-vue";
import { useElectionStore } from "../../stores/electionStore";
import { useApiErrorHandler } from "@/composables/useApiErrorHandler";

const router = useRouter();
const electionStore = useElectionStore();
const { handleApiError } = useApiErrorHandler();

const loading = computed(() => electionStore.loading);
const allElections = computed(() => electionStore.elections);

// Filters
const filters = ref({
  search: "",
  status: "",
  type: "",
  dateRange: [] as Date[],
});

// Sorting
const sort = ref({
  prop: "dateOfElection",
  order: "descending" as "ascending" | "descending",
});

// Pagination
const pagination = ref({
  page: 1,
  pageSize: 20,
  total: 0,
});

// Filtered and sorted elections (without pagination)
const filteredElectionsUnpaginated = computed(() => {
  let filtered = [...allElections.value];

  // Apply filters
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
      if (!election.dateOfElection) return false;
      const electionDate = new Date(election.dateOfElection);
      return electionDate >= startDate && electionDate <= endDate;
    });
  }

  // Apply sorting
  if (sort.value.prop) {
    filtered.sort((a, b) => {
      let aVal = (a as any)[sort.value.prop];
      let bVal = (b as any)[sort.value.prop];

      // Handle date sorting
      if (sort.value.prop === "dateOfElection") {
        aVal = aVal ? new Date(aVal).getTime() : 0;
        bVal = bVal ? new Date(bVal).getTime() : 0;
      }

      // Handle string sorting
      if (typeof aVal === "string") aVal = aVal.toLowerCase();
      if (typeof bVal === "string") bVal = bVal.toLowerCase();

      if (aVal < bVal) return sort.value.order === "ascending" ? -1 : 1;
      if (aVal > bVal) return sort.value.order === "ascending" ? 1 : -1;
      return 0;
    });
  }

  return filtered;
});

// Update pagination total when filtered elections change
watch(
  filteredElectionsUnpaginated,
  (filtered) => {
    pagination.value.total = filtered.length;
  },
  { immediate: true },
);

// Filtered and sorted elections with pagination
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
  await loadElections();
});

async function loadElections() {
  try {
    await electionStore.fetchElections();
  } catch (error) {
    handleApiError(error);
  }
}

function createElection() {
  router.push("/elections/create");
}

function openElection(guid: string) {
  router.push(`/elections/${guid}`);
}

function handleSearch() {
  pagination.value.page = 1; // Reset to first page when searching
}

function handleFilterChange() {
  pagination.value.page = 1; // Reset to first page when filtering
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
  pagination.value.page = 1; // Reset to first page when changing page size
}

function handlePageChange() {
  // Page change handled by computed property
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
.election-list-page {
  max-width: 1400px;
  margin: 0 auto;

  .card-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
  }

  .card-header h2 {
    margin: 0;
    color: var(--color-text-primary);
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
}
</style>
