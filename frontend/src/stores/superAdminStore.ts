import { defineStore } from "pinia";
import { ref, computed } from "vue";
import { superAdminService } from "@/services/superAdminService";
import type { SuperAdminSummary } from "@/services/superAdminService";
import { extractApiErrorMessage } from "@/utils/errorHandler";
import { useAuthStore } from "./authStore";

export const useSuperAdminStore = defineStore("superAdmin", () => {
  const authStore = useAuthStore();
  const isSuperAdmin = computed(() => authStore.isSuperAdmin);
  const summary = ref<SuperAdminSummary | null>(null);
  const loading = ref(false);
  const error = ref<string | null>(null);

  async function checkSuperAdminStatus() {
    // Ensure auth store has fetched user info
    // Only fetch if we're authenticated, email hasn't been populated yet, and not a teller
    if (
      authStore.isAuthenticated &&
      authStore.email === null &&
      authStore.authMethod !== "AccessCode"
    ) {
      await authStore.fetchUserInfo();
    }
    return authStore.isSuperAdmin;
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
    summary.value = null;
    loading.value = false;
    error.value = null;
  }

  return {
    isSuperAdmin,
    summary,
    loading,
    error,
    checkSuperAdminStatus,
    fetchSummary,
    clearError,
    $reset,
  };
});
