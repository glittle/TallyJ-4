import { client } from "../api/config";
import type { ApiResponse, PaginatedResponse } from "@/types/ApiResponse";

export interface SuperAdminSummary {
  totalElections: number;
  openElections: number;
  upcomingElections: number;
  completedElections: number;
  archivedElections: number;
}

export interface SuperAdminElection {
  electionGuid: string;
  name: string;
  convenor?: string;
  dateOfElection?: string;
  tallyStatus?: string;
  electionType?: string;
  voterCount: number;
  ballotCount: number;
  locationCount: number;
  ownerEmail?: string;
}

export interface SuperAdminElectionOwner {
  email?: string;
  displayName?: string;
  role?: string;
}

export interface SuperAdminElectionDetail extends SuperAdminElection {
  numberToElect?: number;
  electionMode?: string;
  percentComplete: number;
  owners: SuperAdminElectionOwner[];
}

export interface SuperAdminElectionFilter {
  search?: string;
  status?: string;
  electionType?: string;
  sortBy?: string;
  sortDirection?: string;
  page?: number;
  pageSize?: number;
}

export const superAdminService = {
  async getSummary(): Promise<SuperAdminSummary> {
    const response = await client.get<ApiResponse<SuperAdminSummary>>({
      url: "/api/superadmin/dashboard/summary",
    });
    return response.data.data;
  },

  async getElections(
    filter?: SuperAdminElectionFilter,
  ): Promise<PaginatedResponse<SuperAdminElection>> {
    const response = await client.get<
      ApiResponse<PaginatedResponse<SuperAdminElection>>
    >({
      url: "/api/superadmin/dashboard/elections",
      query: filter as Record<string, unknown>,
    });
    return response.data.data;
  },

  async getElectionDetail(guid: string): Promise<SuperAdminElectionDetail> {
    const response = await client.get<ApiResponse<SuperAdminElectionDetail>>({
      url: `/api/superadmin/dashboard/elections/${guid}`,
    });
    return response.data.data;
  },
};
