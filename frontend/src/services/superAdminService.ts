import api from './api'
import type { ApiResponse, PaginatedResponse } from '@/types/ApiResponse'

export interface SuperAdminCheckResult {
  isSuperAdmin: boolean
}

export interface SuperAdminSummary {
  totalElections: number
  openElections: number
  upcomingElections: number
  completedElections: number
  archivedElections: number
}

export interface SuperAdminElection {
  electionGuid: string
  name: string
  convenor?: string
  dateOfElection?: string
  tallyStatus?: string
  electionType?: string
  voterCount: number
  ballotCount: number
  locationCount: number
  ownerEmail?: string
}

export interface SuperAdminElectionOwner {
  email?: string
  displayName?: string
  role?: string
}

export interface SuperAdminElectionDetail extends SuperAdminElection {
  numberToElect?: number
  electionMode?: string
  percentComplete: number
  owners: SuperAdminElectionOwner[]
}

export interface SuperAdminElectionFilter {
  search?: string
  status?: string
  electionType?: string
  sortBy?: string
  sortDirection?: string
  page?: number
  pageSize?: number
}

export const superAdminService = {
  async check(): Promise<SuperAdminCheckResult> {
    const response = await api.get<ApiResponse<SuperAdminCheckResult>>('/api/superadmin/check')
    return response.data.data
  },

  async getSummary(): Promise<SuperAdminSummary> {
    const response = await api.get<ApiResponse<SuperAdminSummary>>('/api/superadmin/dashboard/summary')
    return response.data.data
  },

  async getElections(filter?: SuperAdminElectionFilter): Promise<PaginatedResponse<SuperAdminElection>> {
    const response = await api.get<ApiResponse<PaginatedResponse<SuperAdminElection>>>('/api/superadmin/dashboard/elections', {
      params: filter
    })
    return response.data.data
  },

  async getElectionDetail(guid: string): Promise<SuperAdminElectionDetail> {
    const response = await api.get<ApiResponse<SuperAdminElectionDetail>>(`/api/superadmin/dashboard/elections/${guid}`)
    return response.data.data
  }
}
