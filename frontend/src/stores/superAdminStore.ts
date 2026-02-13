import { defineStore } from 'pinia'
import { ref } from 'vue'
import { superAdminService } from '@/services/superAdminService'
import type {
  SuperAdminSummary,
  SuperAdminElection,
  SuperAdminElectionDetail,
  SuperAdminElectionFilter
} from '@/services/superAdminService'
import { extractApiErrorMessage } from '@/utils/errorHandler'

export const useSuperAdminStore = defineStore('superAdmin', () => {
  const isSuperAdmin = ref(false)
  const checkedStatus = ref(false)
  const summary = ref<SuperAdminSummary | null>(null)
  const elections = ref<SuperAdminElection[]>([])
  const selectedElection = ref<SuperAdminElectionDetail | null>(null)
  const loading = ref(false)
  const error = ref<string | null>(null)
  const totalCount = ref(0)
  const currentPage = ref(1)
  const pageSize = ref(25)
  const totalPages = ref(0)

  async function checkSuperAdminStatus() {
    if (checkedStatus.value) return isSuperAdmin.value
    try {
      const result = await superAdminService.check()
      isSuperAdmin.value = result.isSuperAdmin
      checkedStatus.value = true
      return result.isSuperAdmin
    } catch {
      isSuperAdmin.value = false
      checkedStatus.value = true
      return false
    }
  }

  async function fetchSummary() {
    loading.value = true
    error.value = null
    try {
      summary.value = await superAdminService.getSummary()
    } catch (e: any) {
      error.value = extractApiErrorMessage(e)
      throw e
    } finally {
      loading.value = false
    }
  }

  async function fetchElections(filter?: SuperAdminElectionFilter) {
    loading.value = true
    error.value = null
    try {
      const response = await superAdminService.getElections(filter)
      elections.value = response.items
      totalCount.value = response.totalCount
      currentPage.value = response.page
      pageSize.value = response.pageSize
      totalPages.value = response.totalPages
    } catch (e: any) {
      error.value = extractApiErrorMessage(e)
      throw e
    } finally {
      loading.value = false
    }
  }

  async function fetchElectionDetail(guid: string) {
    loading.value = true
    error.value = null
    try {
      selectedElection.value = await superAdminService.getElectionDetail(guid)
      return selectedElection.value
    } catch (e: any) {
      error.value = extractApiErrorMessage(e)
      throw e
    } finally {
      loading.value = false
    }
  }

  function clearError() {
    error.value = null
  }

  function $reset() {
    isSuperAdmin.value = false
    checkedStatus.value = false
    summary.value = null
    elections.value = []
    selectedElection.value = null
    loading.value = false
    error.value = null
    totalCount.value = 0
    currentPage.value = 1
    pageSize.value = 25
    totalPages.value = 0
  }

  return {
    isSuperAdmin,
    checkedStatus,
    summary,
    elections,
    selectedElection,
    loading,
    error,
    totalCount,
    currentPage,
    pageSize,
    totalPages,
    checkSuperAdminStatus,
    fetchSummary,
    fetchElections,
    fetchElectionDetail,
    clearError,
    $reset
  }
})
