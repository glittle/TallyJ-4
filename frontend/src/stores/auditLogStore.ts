import { defineStore } from "pinia";
import { ref } from "vue";
import { auditLogService } from "@/services/auditLogService";
import type { AuditLog, AuditLogFilter } from "@/types/AuditLog";

export const useAuditLogStore = defineStore("auditLog", () => {
  const auditLogs = ref<AuditLog[]>([]);
  const currentAuditLog = ref<AuditLog | null>(null);
  const loading = ref(false);
  const error = ref<string | null>(null);
  const totalCount = ref(0);
  const currentPage = ref(1);
  const pageSize = ref(50);

  async function fetchAuditLogs(filter?: AuditLogFilter, page = 1, size = 50) {
    loading.value = true;
    error.value = null;
    try {
      const response = await auditLogService.getAuditLogs(filter, page, size);
      auditLogs.value = response.items;
      totalCount.value = response.totalCount;
      currentPage.value = response.page;
      pageSize.value = response.pageSize;
    } catch (err: unknown) {
      error.value =
        err instanceof Error ? err.message : "Failed to fetch audit logs";
      throw err;
    } finally {
      loading.value = false;
    }
  }

  async function fetchAuditLogById(id: number) {
    loading.value = true;
    error.value = null;
    try {
      const auditLog = await auditLogService.getAuditLogById(id);
      currentAuditLog.value = auditLog;
      return auditLog;
    } catch (err: unknown) {
      error.value =
        err instanceof Error ? err.message : "Failed to fetch audit log";
      throw err;
    } finally {
      loading.value = false;
    }
  }

  function clearError() {
    error.value = null;
  }

  function $reset() {
    auditLogs.value = [];
    currentAuditLog.value = null;
    loading.value = false;
    error.value = null;
    totalCount.value = 0;
    currentPage.value = 1;
    pageSize.value = 50;
  }

  return {
    auditLogs,
    currentAuditLog,
    loading,
    error,
    totalCount,
    currentPage,
    pageSize,
    fetchAuditLogs,
    fetchAuditLogById,
    clearError,
    $reset,
  };
});
