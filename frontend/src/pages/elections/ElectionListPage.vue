<template>
  <div class="election-list-page">
    <el-card>
      <template #header>
        <div class="card-header">
          <h2>{{ $t('elections.list') }}</h2>
          <el-button type="primary" @click="createElection">
            <el-icon><Plus /></el-icon>
            {{ $t('elections.createNew') }}
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
                <el-icon><Search /></el-icon>
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
              <el-option label="STV" value="STV" />
              <el-option label="Condorcet" value="Cond" />
              <el-option label="Multi-Winner" value="Multi" />
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
              <el-button @click="clearFilters" :disabled="!hasActiveFilters">
                {{ $t('common.clearFilters') }}
              </el-button>
              <el-button
                v-if="selectedElections.length > 0"
                type="danger"
                @click="showBulkDeleteDialog"
              >
                <el-icon><Delete /></el-icon>
                {{ $t('common.deleteSelected', { count: selectedElections.length }) }}
              </el-button>
            </el-space>
          </el-col>
        </el-row>
      </div>

      <div class="table-container">
        <el-table
          :data="filteredElections"
          v-loading="loading"
          style="width: 100%"
          @selection-change="handleSelectionChange"
          @sort-change="handleSortChange"
          :default-sort="{prop: 'dateOfElection', order: 'descending'}"
        >
          <el-table-column type="selection" width="55" />
          <el-table-column prop="name" :label="$t('elections.name')" min-width="250" sortable="custom" />
          <el-table-column prop="electionType" :label="$t('elections.type')" width="120" sortable="custom">
            <template #default="scope">
              <el-tag size="small">{{ scope.row.electionType }}</el-tag>
            </template>
          </el-table-column>
          <el-table-column prop="dateOfElection" :label="$t('elections.date')" width="140" sortable="custom">
            <template #default="scope">
              {{ formatDate(scope.row.dateOfElection) }}
            </template>
          </el-table-column>
          <el-table-column prop="numberToElect" :label="$t('elections.toElect')" width="100" sortable="custom" />
          <el-table-column prop="voterCount" :label="$t('elections.voters')" width="100" sortable="custom" />
          <el-table-column prop="tallyStatus" :label="$t('elections.status')" width="120" sortable="custom">
            <template #default="scope">
              <el-tag :type="getStatusType(scope.row.tallyStatus)">
                {{ scope.row.tallyStatus || 'Draft' }}
              </el-tag>
            </template>
          </el-table-column>
          <el-table-column :label="$t('common.actions')" width="200" fixed="right">
            <template #default="scope">
              <el-button-group>
                <el-button size="small" @click="viewElection(scope.row.electionGuid)">
                  <el-icon><View /></el-icon>
                  {{ $t('common.view') }}
                </el-button>
                <el-button size="small" @click="editElection(scope.row.electionGuid)">
                  <el-icon><Edit /></el-icon>
                  {{ $t('common.edit') }}
                </el-button>
                <el-button
                  size="small"
                  type="danger"
                  @click="deleteElection(scope.row)"
                >
                  <el-icon><Delete /></el-icon>
                  {{ $t('common.delete') }}
                </el-button>
              </el-button-group>
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

    <!-- Bulk Delete Confirmation Dialog -->
    <el-dialog
      v-model="showBulkDeleteConfirm"
      :title="$t('elections.confirmBulkDelete')"
      width="500px"
    >
      <p>{{ $t('elections.bulkDeleteMessage', { count: selectedElections.length }) }}</p>
      <p class="warning-text">{{ $t('common.actionIrreversible') }}</p>

      <template #footer>
        <el-button @click="showBulkDeleteConfirm = false">
          {{ $t('common.cancel') }}
        </el-button>
        <el-button type="danger" @click="confirmBulkDelete" :loading="bulkDeleting">
          {{ $t('common.delete') }}
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { useI18n } from 'vue-i18n';
import { Plus, Search, Delete, View, Edit } from '@element-plus/icons-vue';
import { useElectionStore } from '../../stores/electionStore';
import { ElMessage, ElMessageBox } from 'element-plus';
import type { ElectionDto } from '../../types';

const router = useRouter();
const { t } = useI18n();
const electionStore = useElectionStore();

const loading = computed(() => electionStore.loading);
const allElections = computed(() => electionStore.elections);

// Filters
const filters = ref({
  search: '',
  status: '',
  type: '',
  dateRange: [] as Date[]
});

// Sorting
const sort = ref({
  prop: 'dateOfElection',
  order: 'descending' as 'ascending' | 'descending'
});

// Selection for bulk operations
const selectedElections = ref<ElectionDto[]>([]);
const showBulkDeleteConfirm = ref(false);
const bulkDeleting = ref(false);

// Pagination
const pagination = ref({
  page: 1,
  pageSize: 20,
  total: 0
});

// Filtered and sorted elections
const filteredElections = computed(() => {
  let filtered = [...allElections.value];

  // Apply filters
  if (filters.value.search) {
    const search = filters.value.search.toLowerCase();
    filtered = filtered.filter(election =>
      election.name.toLowerCase().includes(search) ||
      election.convenor?.toLowerCase().includes(search)
    );
  }

  if (filters.value.status) {
    filtered = filtered.filter(election =>
      (election.tallyStatus || 'Draft') === filters.value.status
    );
  }

  if (filters.value.type) {
    filtered = filtered.filter(election =>
      election.electionType === filters.value.type
    );
  }

  if (filters.value.dateRange && filters.value.dateRange.length === 2) {
    const [startDate, endDate] = filters.value.dateRange;
    filtered = filtered.filter(election => {
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
      if (sort.value.prop === 'dateOfElection') {
        aVal = aVal ? new Date(aVal).getTime() : 0;
        bVal = bVal ? new Date(bVal).getTime() : 0;
      }

      // Handle string sorting
      if (typeof aVal === 'string') aVal = aVal.toLowerCase();
      if (typeof bVal === 'string') bVal = bVal.toLowerCase();

      if (aVal < bVal) return sort.value.order === 'ascending' ? -1 : 1;
      if (aVal > bVal) return sort.value.order === 'ascending' ? 1 : -1;
      return 0;
    });
  }

  // Apply pagination
  pagination.value.total = filtered.length;
  const start = (pagination.value.page - 1) * pagination.value.pageSize;
  const end = start + pagination.value.pageSize;
  return filtered.slice(start, end);
});

const hasActiveFilters = computed(() => {
  return filters.value.search ||
         filters.value.status ||
         filters.value.type ||
         (filters.value.dateRange && filters.value.dateRange.length > 0);
});

onMounted(async () => {
  await loadElections();
});

async function loadElections() {
  try {
    await electionStore.fetchElections();
  } catch (error) {
    ElMessage.error(t('elections.loadError'));
  }
}

function createElection() {
  router.push('/elections/create');
}

function viewElection(guid: string) {
  router.push(`/elections/${guid}`);
}

function editElection(guid: string) {
  router.push(`/elections/${guid}/edit`);
}

async function deleteElection(election: ElectionDto) {
  try {
    await ElMessageBox.confirm(
      t('elections.deleteConfirm', { name: election.name }),
      t('common.warning'),
      {
        confirmButtonText: t('common.delete'),
        cancelButtonText: t('common.cancel'),
        type: 'warning'
      }
    );

    await electionStore.deleteElection(election.electionGuid);
    ElMessage.success(t('elections.deleteSuccess'));
  } catch (error: any) {
    if (error !== 'cancel') {
      ElMessage.error(error.message || t('elections.deleteError'));
    }
  }
}

function handleSearch() {
  pagination.value.page = 1; // Reset to first page when searching
}

function handleFilterChange() {
  pagination.value.page = 1; // Reset to first page when filtering
}

function clearFilters() {
  filters.value = {
    search: '',
    status: '',
    type: '',
    dateRange: []
  };
  pagination.value.page = 1;
}

function handleSortChange({ prop, order }: any) {
  sort.value.prop = prop;
  sort.value.order = order;
}

function handleSelectionChange(selection: ElectionDto[]) {
  selectedElections.value = selection;
}

function showBulkDeleteDialog() {
  showBulkDeleteConfirm.value = true;
}

async function confirmBulkDelete() {
  if (selectedElections.value.length === 0) return;

  bulkDeleting.value = true;
  try {
    const deletePromises = selectedElections.value.map(election =>
      electionStore.deleteElection(election.electionGuid)
    );

    await Promise.all(deletePromises);
    ElMessage.success(t('elections.bulkDeleteSuccess', { count: selectedElections.value.length }));
    selectedElections.value = [];
    showBulkDeleteConfirm.value = false;
  } catch (error: any) {
    ElMessage.error(error.message || t('elections.bulkDeleteError'));
  } finally {
    bulkDeleting.value = false;
  }
}

function handleSizeChange() {
  pagination.value.page = 1; // Reset to first page when changing page size
}

function handlePageChange() {
  // Page change handled by computed property
}

function formatDate(date: string) {
  if (!date) return '-';
  return new Date(date).toLocaleDateString();
}

function getStatusType(status: string) {
  const typeMap: Record<string, any> = {
    'Draft': '',
    'Voting': 'success',
    'Tallying': 'warning',
    'Finalized': 'info'
  };
  return typeMap[status] || '';
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

  .warning-text {
    color: var(--color-error-600);
    font-weight: var(--font-weight-medium);
    margin: var(--spacing-2) 0 0 0;
  }
}
</style>
