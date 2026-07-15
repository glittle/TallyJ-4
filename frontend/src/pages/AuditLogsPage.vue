<script setup lang="ts">
import { useAuditLogStore } from "@/stores/auditLogStore";
import type { AuditLog, AuditLogFilter } from "@/types/AuditLog";
import { View } from "@element-plus/icons-vue";
import { computed, onMounted, ref } from "vue";

const auditLogStore = useAuditLogStore();

const filters = ref<AuditLogFilter>({});
const detailsDialogVisible = ref(false);
const selectedLog = ref<AuditLog | null>(null);

const auditLogs = computed(() => auditLogStore.auditLogs);
const loading = computed(() => auditLogStore.loading);
const totalCount = computed(() => auditLogStore.totalCount);
const currentPage = computed({
  get: () => auditLogStore.currentPage,
  set: (val) => (auditLogStore.currentPage = val),
});
const pageSize = computed({
  get: () => auditLogStore.pageSize,
  set: (val) => (auditLogStore.pageSize = val),
});
const totalPages = computed(() => Math.ceil(totalCount.value / pageSize.value));

onMounted(() => {
  loadAuditLogs();
});

async function loadAuditLogs() {
  const filterParams: AuditLogFilter = {};
  if (filters.value.electionGuid) {
    filterParams.electionGuid = filters.value.electionGuid;
  }
  if (filters.value.userId) {
    filterParams.userId = filters.value.userId;
  }
  if (filters.value.onlineVoterId) {
    filterParams.onlineVoterId = filters.value.onlineVoterId;
  }
  if (filters.value.email) {
    filterParams.email = filters.value.email;
  }
  if (filters.value.ipAddress) {
    filterParams.ipAddress = filters.value.ipAddress;
  }
  if (
    filters.value.isSuspicious !== undefined &&
    filters.value.isSuspicious !== null
  ) {
    filterParams.isSuspicious = filters.value.isSuspicious;
  }
  if (filters.value.startDate) {
    filterParams.startDate = new Date(filters.value.startDate).toISOString();
  }
  if (filters.value.endDate) {
    filterParams.endDate = new Date(filters.value.endDate).toISOString();
  }
  if (filters.value.searchTerm) {
    filterParams.searchTerm = filters.value.searchTerm;
  }

  await auditLogStore.fetchAuditLogs(
    filterParams,
    currentPage.value,
    pageSize.value,
  );
}

function applyFilters() {
  currentPage.value = 1;
  loadAuditLogs();
}

function clearFilters() {
  filters.value = {};
  currentPage.value = 1;
  loadAuditLogs();
}

function handlePageChange(_page: number) {
  loadAuditLogs();
}

function handleSizeChange(_size: number) {
  currentPage.value = 1;
  loadAuditLogs();
}

function viewDetails(log: AuditLog) {
  selectedLog.value = log;
  detailsDialogVisible.value = true;
}

function formatDate(dateString: string) {
  if (!dateString) {
    return "-";
  }
  const date = new Date(dateString);
  return date.toLocaleString("en-US", {
    year: "numeric",
    month: "short",
    day: "numeric",
    hour: "2-digit",
    minute: "2-digit",
    second: "2-digit",
  });
}

function formatValue(value: unknown) {
  if (value === null || value === undefined || value === "") {
    return "-";
  }
  return String(value);
}
</script>

<template>
  <div class="audit-logs-page">
    <el-card>
      <div class="filters-container">
        <el-form :inline="true" :model="filters" class="filter-form">
          <el-form-item :label="$t('audit.filters.electionGuid')">
            <el-input
              v-model="filters.electionGuid"
              :placeholder="$t('audit.filters.electionGuidPlaceholder')"
              clearable
              @change="applyFilters"
            />
          </el-form-item>
          <el-form-item :label="$t('audit.filters.userId')">
            <el-input
              v-model="filters.userId"
              :placeholder="$t('audit.filters.userIdPlaceholder')"
              clearable
              @change="applyFilters"
            />
          </el-form-item>
          <el-form-item :label="$t('audit.filters.onlineVoterId')">
            <el-input
              v-model="filters.onlineVoterId"
              :placeholder="$t('audit.filters.onlineVoterIdPlaceholder')"
              clearable
              @change="applyFilters"
            />
          </el-form-item>
          <el-form-item :label="$t('audit.filters.email')">
            <el-input
              v-model="filters.email"
              :placeholder="$t('audit.filters.emailPlaceholder')"
              clearable
              @change="applyFilters"
            />
          </el-form-item>
          <el-form-item :label="$t('audit.filters.ipAddress')">
            <el-input
              v-model="filters.ipAddress"
              :placeholder="$t('audit.filters.ipAddressPlaceholder')"
              clearable
              @change="applyFilters"
            />
          </el-form-item>
          <el-form-item :label="$t('audit.filters.suspicious')">
            <el-select
              v-model="filters.isSuspicious"
              :placeholder="$t('audit.filters.suspiciousPlaceholder')"
              clearable
              @change="applyFilters"
            >
              <el-option
                :label="$t('audit.filters.suspiciousYes')"
                :value="true"
              />
              <el-option
                :label="$t('audit.filters.suspiciousNo')"
                :value="false"
              />
            </el-select>
          </el-form-item>
          <el-form-item :label="$t('audit.filters.startDate')">
            <el-date-picker
              v-model="filters.startDate"
              type="datetime"
              :placeholder="$t('audit.filters.startDatePlaceholder')"
              clearable
              @change="applyFilters"
            />
          </el-form-item>
          <el-form-item :label="$t('audit.filters.endDate')">
            <el-date-picker
              v-model="filters.endDate"
              type="datetime"
              :placeholder="$t('audit.filters.endDatePlaceholder')"
              clearable
              @change="applyFilters"
            />
          </el-form-item>
          <el-form-item :label="$t('audit.filters.search')">
            <el-input
              v-model="filters.searchTerm"
              :placeholder="$t('audit.filters.searchPlaceholder')"
              clearable
              @change="applyFilters"
            />
          </el-form-item>
          <el-form-item>
            <el-button type="primary" @click="applyFilters">{{
              $t("audit.filters.apply")
            }}</el-button>
            <el-button @click="clearFilters">{{
              $t("audit.filters.clear")
            }}</el-button>
          </el-form-item>
        </el-form>
      </div>

      <div class="table-container">
        <el-table v-loading="loading" :data="auditLogs" style="width: 100%">
          <el-table-column
            prop="timestamp"
            :label="$t('audit.table.dateTime')"
            width="180"
          >
            <template #default="scope">
              {{ formatDate(scope.row.timestamp) }}
            </template>
          </el-table-column>
          <el-table-column
            prop="eventType"
            :label="$t('audit.table.eventType')"
            width="160"
          >
            <template #default="scope">
              {{ formatValue(scope.row.eventType) }}
            </template>
          </el-table-column>
          <el-table-column
            prop="severity"
            :label="$t('audit.table.severity')"
            width="100"
            align="center"
          >
            <template #default="scope">
              {{ formatValue(scope.row.severity) }}
            </template>
          </el-table-column>
          <el-table-column
            prop="details"
            :label="$t('audit.table.details')"
            min-width="280"
          />
          <el-table-column
            prop="userId"
            :label="$t('audit.table.userId')"
            width="140"
          >
            <template #default="scope">
              <span v-if="scope.row.userId">{{ scope.row.userId }}</span>
              <span v-else class="text-muted">-</span>
            </template>
          </el-table-column>
          <el-table-column
            prop="email"
            :label="$t('audit.table.email')"
            width="180"
          >
            <template #default="scope">
              <span v-if="scope.row.email">{{ scope.row.email }}</span>
              <span v-else class="text-muted">-</span>
            </template>
          </el-table-column>
          <el-table-column
            prop="isSuspicious"
            :label="$t('audit.table.suspicious')"
            width="110"
            align="center"
          >
            <template #default="scope">
              <el-tag v-if="scope.row.isSuspicious" type="danger" size="small">
                {{ $t("audit.table.yes") }}
              </el-tag>
              <span v-else class="text-muted">-</span>
            </template>
          </el-table-column>
          <el-table-column
            prop="electionGuid"
            :label="$t('audit.table.election')"
            width="100"
            align="center"
          >
            <template #default="scope">
              <el-tag v-if="scope.row.electionGuid" type="success" size="small">
                {{ $t("audit.table.yes") }}
              </el-tag>
              <span v-else class="text-muted">-</span>
            </template>
          </el-table-column>
          <el-table-column
            :label="$t('audit.table.actions')"
            width="100"
            fixed="right"
          >
            <template #default="scope">
              <el-button size="small" @click="viewDetails(scope.row)">
                <el-icon><View /></el-icon>
                {{ $t("audit.table.view") }}
              </el-button>
            </template>
          </el-table-column>
        </el-table>

        <div v-if="totalPages > 1" class="pagination-container">
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

    <el-dialog
      v-model="detailsDialogVisible"
      :title="$t('audit.dialog.title')"
      width="640px"
    >
      <div v-if="selectedLog" class="log-details">
        <el-descriptions :column="1" border>
          <el-descriptions-item :label="$t('audit.dialog.id')">
            {{ selectedLog.id }}
          </el-descriptions-item>
          <el-descriptions-item :label="$t('audit.dialog.dateTime')">
            {{ formatDate(selectedLog.timestamp) }}
          </el-descriptions-item>
          <el-descriptions-item :label="$t('audit.dialog.eventType')">
            {{ formatValue(selectedLog.eventType) }}
          </el-descriptions-item>
          <el-descriptions-item :label="$t('audit.dialog.severity')">
            {{ formatValue(selectedLog.severity) }}
          </el-descriptions-item>
          <el-descriptions-item :label="$t('audit.dialog.details')">
            {{ selectedLog.details || "-" }}
          </el-descriptions-item>
          <el-descriptions-item :label="$t('audit.dialog.userId')">
            {{ selectedLog.userId || "-" }}
          </el-descriptions-item>
          <el-descriptions-item :label="$t('audit.dialog.onlineVoterId')">
            {{ selectedLog.onlineVoterId || "-" }}
          </el-descriptions-item>
          <el-descriptions-item :label="$t('audit.dialog.email')">
            {{ selectedLog.email || "-" }}
          </el-descriptions-item>
          <el-descriptions-item :label="$t('audit.dialog.ipAddress')">
            {{ selectedLog.ipAddress || "-" }}
          </el-descriptions-item>
          <el-descriptions-item :label="$t('audit.dialog.userAgent')">
            {{ selectedLog.userAgent || "-" }}
          </el-descriptions-item>
          <el-descriptions-item :label="$t('audit.dialog.electionGuid')">
            {{ selectedLog.electionGuid || "-" }}
          </el-descriptions-item>
          <el-descriptions-item :label="$t('audit.dialog.suspicious')">
            {{
              selectedLog.isSuspicious
                ? $t("audit.table.yes")
                : $t("audit.filters.suspiciousNo")
            }}
          </el-descriptions-item>
        </el-descriptions>
      </div>
    </el-dialog>
  </div>
</template>

<style lang="less">
.audit-logs-page {
  padding: 20px;

  .card-header {
    display: flex;
    justify-content: space-between;
    align-items: center;

    h2 {
      margin: 0;
      font-size: 20px;
      font-weight: 600;
    }
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
}
</style>
