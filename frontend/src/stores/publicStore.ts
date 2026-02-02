import { defineStore } from 'pinia'
import { ref } from 'vue'
import { publicService } from '../services/publicService'
import type { PublicDisplayDto, PublicDisplayOptions } from '../types'

export const usePublicStore = defineStore('public', () => {
  const displayData = ref<PublicDisplayDto | null>(null)
  const loading = ref(false)
  const error = ref<string | null>(null)
  const displayOptions = ref<PublicDisplayOptions>({
    showVoteCounts: true,
    showStatistics: true,
    showAdditionalNames: true,
    theme: 'light',
    refreshInterval: 30000,
    autoRefresh: true
  })

  async function fetchPublicDisplay(electionGuid: string) {
    loading.value = true
    error.value = null
    try {
      displayData.value = await publicService.getPublicDisplay(electionGuid)
      return displayData.value
    } catch (e: any) {
      error.value = e.message || 'Failed to fetch public display data'
      throw e
    } finally {
      loading.value = false
    }
  }

  function updateDisplayData(data: PublicDisplayDto) {
    displayData.value = data
  }

  function setDisplayOptions(options: Partial<PublicDisplayOptions>) {
    displayOptions.value = { ...displayOptions.value, ...options }
  }

  function toggleTheme() {
    displayOptions.value.theme = displayOptions.value.theme === 'light' ? 'dark' : 'light'
  }

  function reset() {
    displayData.value = null
    error.value = null
    loading.value = false
  }

  return {
    displayData,
    loading,
    error,
    displayOptions,
    fetchPublicDisplay,
    updateDisplayData,
    setDisplayOptions,
    toggleTheme,
    reset
  }
})
