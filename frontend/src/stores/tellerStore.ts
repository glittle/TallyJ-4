import { defineStore } from 'pinia'
import { ref } from 'vue'
import { tellerService } from '@/services/tellerService'
import type { Teller, CreateTellerDto, UpdateTellerDto } from '@/types/teller'
import { ElMessage } from 'element-plus'

export const useTellerStore = defineStore('teller', () => {
  const tellers = ref<Teller[]>([])
  const currentTeller = ref<Teller | null>(null)
  const loading = ref(false)
  const error = ref<string | null>(null)
  const totalCount = ref(0)
  const currentPage = ref(1)
  const pageSize = ref(50)

  async function fetchTellers(electionGuid: string, page = 1, size = 50) {
    loading.value = true
    error.value = null
    try {
      const response = await tellerService.getTellersByElection(electionGuid, page, size)
      tellers.value = response.data
      totalCount.value = response.totalCount
      currentPage.value = response.pageNumber
      pageSize.value = response.pageSize
    } catch (err: any) {
      error.value = err.message || 'Failed to fetch tellers'
      ElMessage.error(error.value)
      throw err
    } finally {
      loading.value = false
    }
  }

  async function fetchTellerById(electionGuid: string, rowId: number) {
    loading.value = true
    error.value = null
    try {
      const teller = await tellerService.getTellerById(electionGuid, rowId)
      currentTeller.value = teller
      return teller
    } catch (err: any) {
      error.value = err.message || 'Failed to fetch teller'
      ElMessage.error(error.value)
      throw err
    } finally {
      loading.value = false
    }
  }

  async function createTeller(electionGuid: string, tellerData: CreateTellerDto) {
    loading.value = true
    error.value = null
    try {
      const newTeller = await tellerService.createTeller(electionGuid, tellerData)
      tellers.value.push(newTeller)
      totalCount.value++
      ElMessage.success('Teller created successfully')
      return newTeller
    } catch (err: any) {
      error.value = err.message || 'Failed to create teller'
      ElMessage.error(error.value)
      throw err
    } finally {
      loading.value = false
    }
  }

  async function updateTeller(electionGuid: string, rowId: number, tellerData: UpdateTellerDto) {
    loading.value = true
    error.value = null
    try {
      const updatedTeller = await tellerService.updateTeller(electionGuid, rowId, tellerData)
      const index = tellers.value.findIndex((t) => t.rowId === rowId)
      if (index !== -1) {
        tellers.value[index] = updatedTeller
      }
      if (currentTeller.value?.rowId === rowId) {
        currentTeller.value = updatedTeller
      }
      ElMessage.success('Teller updated successfully')
      return updatedTeller
    } catch (err: any) {
      error.value = err.message || 'Failed to update teller'
      ElMessage.error(error.value)
      throw err
    } finally {
      loading.value = false
    }
  }

  async function deleteTeller(electionGuid: string, rowId: number) {
    loading.value = true
    error.value = null
    try {
      await tellerService.deleteTeller(electionGuid, rowId)
      tellers.value = tellers.value.filter((t) => t.rowId !== rowId)
      totalCount.value--
      if (currentTeller.value?.rowId === rowId) {
        currentTeller.value = null
      }
      ElMessage.success('Teller deleted successfully')
    } catch (err: any) {
      error.value = err.message || 'Failed to delete teller'
      ElMessage.error(error.value)
      throw err
    } finally {
      loading.value = false
    }
  }

  function clearError() {
    error.value = null
  }

  function $reset() {
    tellers.value = []
    currentTeller.value = null
    loading.value = false
    error.value = null
    totalCount.value = 0
    currentPage.value = 1
    pageSize.value = 50
  }

  return {
    tellers,
    currentTeller,
    loading,
    error,
    totalCount,
    currentPage,
    pageSize,
    fetchTellers,
    fetchTellerById,
    createTeller,
    updateTeller,
    deleteTeller,
    clearError,
    $reset
  }
})
