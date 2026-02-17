import { defineStore } from "pinia";
import { ref } from "vue";
import { superAdminService } from "@/services/superAdminService";
import type { SuperAdminSummary } from "@/services/superAdminService";
import { extractApiErrorMessage } from "@/utils/errorHandler";

export const useSuperAdminStore = defineStore("superAdmin", () => {
  const isSuperAdmin = ref(false);
  const checkedStatus = ref(false);
  const summary = ref<SuperAdminSummary | null>(null);
  const loading = ref(false);
  const error = ref<string | null>(null);

  async function checkSuperAdminStatus() {
    if (checkedStatus.value) {
      return isSuperAdmin.value;
    }
    try {
      const result = await superAdminService.check();
      isSuperAdmin.value = result.isSuperAdmin;
      checkedStatus.value = true;
      return result.isSuperAdmin;
    } catch {
      isSuperAdmin.value = false;
      checkedStatus.value = true;
      return false;
    }
  }

  async function fetchSummary() {
    loading.value = true;
    error.value = null;
    try {
      summary.value = await superAdminService.getSummary();
    } catch (e: any) {
      error.value = extractApiErrorMessage(e);
      throw e;
    } finally {
      loading.value = false;
    }
  }

  function clearError() {
    error.value = null;
  }

  function $reset() {
    isSuperAdmin.value = false;
    checkedStatus.value = false;
    summary.value = null;
    loading.value = false;
    error.value = null;
  }

  return {
    isSuperAdmin,
    checkedStatus,
    summary,
    loading,
    error,
    checkSuperAdminStatus,
    fetchSummary,
    clearError,
    $reset,
  };
});
