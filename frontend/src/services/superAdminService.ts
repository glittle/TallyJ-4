import {
  getApiSuperadminDashboardSummary,
  getApiSuperadminDashboardElections,
  getApiSuperadminDashboardElectionsByGuid,
  getApiSuperadminUsers,
  getApiSuperadminUsersByUserId,
  putApiSuperadminUsersByUserId,
} from "@/api/gen/configService";
import type { PaginatedResponse } from "@/types/ApiResponse";

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
    const response = await getApiSuperadminDashboardSummary();
    return response.data?.data as SuperAdminSummary;
  },

  async getElections(
    filter?: SuperAdminElectionFilter,
  ): Promise<PaginatedResponse<SuperAdminElection>> {
    const response = await getApiSuperadminDashboardElections({
      query: {
        Search: filter?.search,
        Status: filter?.status,
        ElectionType: filter?.electionType as never,
        SortBy: filter?.sortBy,
        SortDirection: filter?.sortDirection,
        Page: filter?.page,
        PageSize: filter?.pageSize,
      },
    });

    const data = response.data?.data;
    return {
      items: (data?.items ?? []) as SuperAdminElection[],
      totalCount: data?.totalCount ?? 0,
      page: data?.pageNumber ?? 1,
      pageSize: data?.pageSize ?? 50,
      totalPages: data?.totalPages ?? 0,
    };
  },

  async getElectionDetail(guid: string): Promise<SuperAdminElectionDetail> {
    const response = await getApiSuperadminDashboardElectionsByGuid({
      path: { guid },
    });
    return response.data?.data as SuperAdminElectionDetail;
  },

  async getUsers(filter?: {
    search?: string;
    page?: number;
    pageSize?: number;
  }): Promise<PaginatedResponse<SuperAdminUser>> {
    const response = await getApiSuperadminUsers({
      query: {
        Search: filter?.search,
        Page: filter?.page,
        PageSize: filter?.pageSize,
      },
      throwOnError: true,
    });
    const data = response.data?.data;
    return {
      items: (data?.items ?? []) as SuperAdminUser[],
      totalCount: data?.totalCount ?? 0,
      page: data?.pageNumber ?? 1,
      pageSize: data?.pageSize ?? 25,
      totalPages: data?.totalPages ?? 0,
    };
  },

  async getUserDetail(userId: string): Promise<SuperAdminUserDetail> {
    const response = await getApiSuperadminUsersByUserId({
      path: { userId },
      throwOnError: true,
    });
    return response.data?.data as SuperAdminUserDetail;
  },

  async updateUser(
    userId: string,
    body: { displayName?: string; email?: string },
  ): Promise<SuperAdminUserDetail> {
    const response = await putApiSuperadminUsersByUserId({
      path: { userId },
      body,
      throwOnError: true,
    });
    return response.data?.data as SuperAdminUserDetail;
  },
};

export interface SuperAdminUser {
  id: string;
  email?: string;
  displayName?: string;
  authMethod?: string;
  emailConfirmed?: boolean;
  pendingEmail?: string | null;
  lockoutEnd?: string | null;
}

export interface SuperAdminEmailChangeEntry {
  oldEmail: string;
  newEmail: string;
  changedAt: string;
  source: string;
  changedByUserId?: string | null;
}

export interface SuperAdminUserDetail extends SuperAdminUser {
  emailHistory: SuperAdminEmailChangeEntry[];
}
