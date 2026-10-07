import {
  getApiSuperadminDashboardSummary,
  getApiSuperadminDashboardElections,
  getApiSuperadminDashboardElectionsByGuid,
  getApiSuperadminUsers,
  getApiSuperadminUsersByUserId,
  putApiSuperadminUsersByUserId,
  postApiSuperadminAccountInvites,
  getApiSuperadminPaidSends,
  postApiSuperadminPaidSendsOwnersByUserIdApprove,
  postApiSuperadminPaidSendsOwnersByUserIdFreeze,
  postApiSuperadminPaidSendsOwnersByUserIdUnfreeze,
  postApiSuperadminPaidSendsOwnersByUserIdDailyCap,
  postApiSuperadminPaidSendsElectionsByGuidFreeze,
  postApiSuperadminPaidSendsElectionsByGuidUnfreeze,
  postApiSuperadminPaidSendsElectionsByGuidAllowance,
  postApiSuperadminPaidSendsElectionsByGuidClearFlag,
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

  async createAccountInvite(): Promise<AccountInviteCreated> {
    const response = await postApiSuperadminAccountInvites({
      throwOnError: true,
    });
    return response.data?.data as AccountInviteCreated;
  },

  async getPaidSends(): Promise<PaidSendOverview> {
    const response = await getApiSuperadminPaidSends({ throwOnError: true });
    const data = response.data?.data;
    return {
      pendingOwners: (data?.pendingOwners ?? []) as PendingPaidSendOwner[],
      capHits: (data?.capHits ?? []) as PaidSendCapHit[],
      frozenElections: (data?.frozenElections ?? []) as FrozenSendElection[],
      frozenOwners: (data?.frozenOwners ?? []) as FrozenSendOwner[],
      flaggedElections: (data?.flaggedElections ?? []).map((election) => ({
        electionGuid: election.electionGuid || "",
        name: election.name || "",
        flaggedAt: election.flaggedAt,
        rows: (election.rows ?? []).map((row) => ({
          rowNumber: row.rowNumber,
          maskedValue: row.maskedValue || "",
          reason: row.reason || "",
        })),
      })),
    };
  },

  async approvePaidSends(userId: string): Promise<void> {
    await postApiSuperadminPaidSendsOwnersByUserIdApprove({
      path: { userId },
      throwOnError: true,
    });
  },

  async freezeOwner(userId: string): Promise<void> {
    await postApiSuperadminPaidSendsOwnersByUserIdFreeze({
      path: { userId },
      throwOnError: true,
    });
  },

  async unfreezeOwner(userId: string): Promise<void> {
    await postApiSuperadminPaidSendsOwnersByUserIdUnfreeze({
      path: { userId },
      throwOnError: true,
    });
  },

  async raiseOwnerDailyCap(userId: string, dailyCap: number): Promise<void> {
    await postApiSuperadminPaidSendsOwnersByUserIdDailyCap({
      path: { userId },
      body: { dailyCap },
      throwOnError: true,
    });
  },

  async freezeElection(guid: string): Promise<void> {
    await postApiSuperadminPaidSendsElectionsByGuidFreeze({
      path: { guid },
      throwOnError: true,
    });
  },

  async unfreezeElection(guid: string): Promise<void> {
    await postApiSuperadminPaidSendsElectionsByGuidUnfreeze({
      path: { guid },
      throwOnError: true,
    });
  },

  async raiseElectionAllowance(guid: string, allowance: number): Promise<void> {
    await postApiSuperadminPaidSendsElectionsByGuidAllowance({
      path: { guid },
      body: { allowance },
      throwOnError: true,
    });
  },

  async clearElectionFlag(guid: string): Promise<void> {
    await postApiSuperadminPaidSendsElectionsByGuidClearFlag({
      path: { guid },
      throwOnError: true,
    });
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
  lockoutEnd?: string | Date | null;
}

export interface SuperAdminEmailChangeEntry {
  oldEmail: string;
  newEmail: string;
  changedAt: string | Date;
  source: string;
  changedByUserId?: string | null;
}

export interface SuperAdminUserDetail extends SuperAdminUser {
  emailHistory: SuperAdminEmailChangeEntry[];
}

export interface AccountInviteCreated {
  token: string;
  inviteUrl: string;
  expiresAt: string | Date;
}

export interface PendingPaidSendOwner {
  userId: string;
  email?: string | null;
  displayName?: string | null;
  electionCount: number;
}

export interface PaidSendCapHit {
  scope: string;
  electionGuid?: string | null;
  electionName?: string | null;
  ownerUserId?: string | null;
  ownerEmail?: string | null;
  used: number;
  cap: number;
}

export interface FrozenSendElection {
  electionGuid: string;
  name: string;
}

export interface FrozenSendOwner {
  userId: string;
  email?: string | null;
  displayName?: string | null;
}

export interface FlaggedVoterContact {
  rowNumber?: number | null;
  maskedValue: string;
  reason: string;
}

export interface FlaggedElection {
  electionGuid: string;
  name: string;
  flaggedAt?: string | Date | null;
  rows: FlaggedVoterContact[];
}

export interface PaidSendOverview {
  pendingOwners: PendingPaidSendOwner[];
  capHits: PaidSendCapHit[];
  frozenElections: FrozenSendElection[];
  frozenOwners: FrozenSendOwner[];
  flaggedElections: FlaggedElection[];
}
