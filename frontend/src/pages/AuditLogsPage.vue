<template>
  <div class="audit-logs-page">
    <el-card>
      <template #header>
        <div class="card-header">
          <h2>Audit Logs</h2>
        </div>
      </template>

      <div class="filters-container">
        <el-form :inline="true" :model="filters" class="filter-form">
          <el-form-item label="Election GUID">
            <el-input
              v-model="filters.electionGuid"
              placeholder="Filter by Election GUID"
              clearable
              @change="applyFilters"
            />
          </el-form-item>
          <el-form-item label="Location GUID">
            <el-input
              v-model="filters.locationGuid"
              placeholder="Filter by Location GUID"
              clearable
              @change="applyFilters"
            />
          </el-form-item>
          <el-form-item label="Voter ID">
            <el-input
              v-model="filters.voterId"
              placeholder="Filter by Voter ID"
              clearable
              @change="applyFilters"
            />
          </el-form-item>
          <el-form-item label="Computer Code">
            <el-input
              v-model="filters.computerCode"
              placeholder="Filter by Computer Code"
              clearable
              @change="applyFilters"
            />
          </el-form-item>
          <el-form-item label="Start Date">
            <el-date-picker
              v-model="filters.startDate"
              type="datetime"
              placeholder="Start Date"
              clearable
              @change="applyFilters"
            />
          </el-form-item>
          <el-form-item label="End Date">
            <el-date-picker
              v-model="filters.endDate"
              type="datetime"
              placeholder="End Date"
              clearable
              @change="applyFilters"
            />
          </el-form-item>
          <el-form-item label="Search">
            <el-input
              v-model="filters.searchTerm"
              placeholder="Search all fields"
              clearable
              @change="applyFilters"
            />
          </el-form-item>
          <el-form-item>
            <el-button type="primary" @click="applyFilters">Apply Filters</el-button>
            <el-button @click="clearFilters">Clear Filters</el-button>
          </el-form-item>
        </el-form>
      </div>

      <div class="table-container">
        <el-table :data="auditLogs" v-loading="loading" style="width: 100%">
          <el-table-column prop="asOf" label="Date/Time" width="180">
            <template #default="scope">
              {{ formatDate(scope.row.asOf) }}
            </template>
          </el-table-column>
          <el-table-column prop="details" label="Details" min-width="300" />
          <el-table-column prop="voterId" label="User ID" width="150">
            <template #default="scope">
              <span v-if="scope.row.voterId">{{ scope.row.voterId }}</span>
              <span v-else class="text-muted">-</span>
            </template>
          </el-table-column>
          <el-table-column prop="computerCode" label="Computer" width="120" align="center">
            <template #default="scope">
              <el-tag v-if="scope.row.computerCode" type="info">
                {{ scope.row.computerCode }}
              </el-tag>
              <span v-else class="text-muted">-</span>
            </template>
          </el-table-column>
          <el-table-column prop="electionGuid" label="Election" width="100" align="center">
            <template #default="scope">
              <el-tag v-if="scope.row.electionGuid" type="success" size="small">
                Yes
              </el-tag>
              <span v-else class="text-muted">-</span>
            </template>
          </el-table-column>
          <el-table-column label="Actions" width="100" fixed="right">
            <template #default="scope">
              <el-button size="small" @click="viewDetails(scope.row)">
                <el-icon><View /></el-icon>
                View
              </el-button>
            </template>
          </el-table-column>
        </el-table>

        <div class="pagination-container" v-if="totalPages > 1">
          <el-pagination
            v-model:current-page="currentPage"
            v-model:page-size="pageSize"
            :page-sizes="[10, 20, 50, 100, 200]"
            :total="totalCount"
            layout="total, sizes, prev, pager, next"
            @size-change="handleSizeChange"
            @current-change="handlePageChange"
          />
        </div>
      </div>
    </el-card>

    <el-dialog v-model="detailsDialogVisible" title="Audit Log Details" width="600px">
      <div v-if="selectedLog" class="log-details">
        <el-descriptions :column="1" border>
          <el-descriptions-item label="Row ID">
            {{ selectedLog.rowId }}
          </el-descriptions-item>
          <el-descriptions-item label="Date/Time">
            {{ formatDate(selectedLog.asOf) }}
          </el-descriptions-item>
          <el-descriptions-item label="Details">
            {{ selectedLog.details || '-' }}
          </el-descriptions-item>
          <el-descriptions-item label="User ID">
            {{ selectedLog.voterId || '-' }}
          </el-descriptions-item>
          <el-descriptions-item label="Computer Code">
            {{ selectedLog.computerCode || '-' }}
          </el-descriptions-item>
          <el-descriptions-item label="Election GUID">
            {{ selectedLog.electionGuid || '-' }}
          </el-descriptions-item>
          <el-descriptions-item label="Location GUID">
            {{ selectedLog.locationGuid || '-' }}
          </el-descriptions-item>
          <el-descriptions-item label="Host and Version">
            {{ selectedLog.hostAndVersion || '-' }}
          </el-descriptions-item>
        </el-descriptions>
      </div>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { View } from '@element-plus/icons-vue'
import { useAuditLogStore } from '@/stores/auditLogStore'
import type { AuditLog, AuditLogFilter } from '@/types/AuditLog'

const auditLogStore = useAuditLogStore()

const filters = ref<AuditLogFilter>({})
const detailsDialogVisible = ref(false)
const selectedLog = ref<AuditLog | null>(null)

const auditLogs = computed(() => auditLogStore.auditLogs)
const loading = computed(() => auditLogStore.loading)
const totalCount = computed(() => auditLogStore.totalCount)
const currentPage = computed({
  get: () => auditLogStore.currentPage,
  set: (val) => (auditLogStore.currentPage = val)
})
const pageSize = computed({
  get: () => auditLogStore.pageSize,
  set: (val) => (auditLogStore.pageSize = val)
})
const totalPages = computed(() => Math.ceil(totalCount.value / pageSize.value))

onMounted(() => {
  loadAuditLogs()
})

async function loadAuditLogs() {
  const filterParams: AuditLogFilter = {}
  if (filters.value.electionGuid) filterParams.electionGuid = filters.value.electionGuid
  if (filters.value.locationGuid) filterParams.locationGuid = filters.value.locationGuid
  if (filters.value.voterId) filterParams.voterId = filters.value.voterId
  if (filters.value.computerCode) filterParams.computerCode = filters.value.computerCode
  if (filters.value.startDate)
    filterParams.startDate = new Date(filters.value.startDate).toISOString()
  if (filters.value.endDate) filterParams.endDate = new Date(filters.value.endDate).toISOString()
  if (filters.value.searchTerm) filterParams.searchTerm = filters.value.searchTerm

  await auditLogStore.fetchAuditLogs(filterParams, currentPage.value, pageSize.value)
}

function applyFilters() {
  currentPage.value = 1
  loadAuditLogs()
}

function clearFilters() {
  filters.value = {}
  currentPage.value = 1
  loadAuditLogs()
}

function handlePageChange(page: number) {
  loadAuditLogs()
}

function handleSizeChange(size: number) {
  currentPage.value = 1
  loadAuditLogs()
}

function viewDetails(log: AuditLog) {
  selectedLog.value = log
  detailsDialogVisible.value = true
}

function formatDate(dateString: string) {
  if (!dateString) return '-'
  const date = new Date(dateString)
  return date.toLocaleString('en-US', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit'
  })
}
</script>

<style scoped>
.audit-logs-page {
  padding: 20px;
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.card-header h2 {
  margin: 0;
  font-size: 20px;
  font-weight: 600;
}

.filters-container {
  margin-bottom: 20px;
  padding: 20px;
  background-color: #f5f7fa;
  border-radius: 4px;
}

.filter-form {
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
}

.table-container {
  margin-top: 20px;
}

.pagination-container {
  margin-top: 20px;
  display: flex;
  justify-content: center;
}

.text-muted {
  color: #909399;
}

.log-details {
  padding: 10px 0;
}
</style>
