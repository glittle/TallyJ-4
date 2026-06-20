import {
  getApiAuditLogsGetAuditLogs,
  getApiAuditLogsByRowIdGetAuditLog,
  postApiAuditLogsCreateAuditLog,
} from "@/api/gen/configService";
import type {
  AuditLog,
  AuditLogFilter,
  CreateAuditLogDto,
} from "@/types/AuditLog";
import type { PaginatedResponse } from "@/types/ApiResponse";

export const auditLogService = {
  async getAuditLogs(
    filter?: AuditLogFilter,
    pageNumber = 1,
    pageSize = 50,
  ): Promise<PaginatedResponse<AuditLog>> {
    const response = await getApiAuditLogsGetAuditLogs({
      query: {
        electionGuid: filter?.electionGuid,
        locationGuid: filter?.locationGuid,
        voterId: filter?.voterId,
        computerCode: filter?.computerCode,
        startDate: filter?.startDate ? new Date(filter.startDate) : undefined,
        endDate: filter?.endDate ? new Date(filter.endDate) : undefined,
        searchTerm: filter?.searchTerm,
        pageNumber,
        pageSize,
      },
    });

    const data = response.data;
    return {
      items: (data?.items ?? []) as AuditLog[],
      totalCount: data?.totalCount ?? 0,
      page: data?.pageNumber ?? pageNumber,
      pageSize: data?.pageSize ?? pageSize,
      totalPages: data?.totalPages ?? 0,
    };
  },

  async getAuditLogById(rowId: number) {
    const response = await getApiAuditLogsByRowIdGetAuditLog({
      path: { rowId },
    });
    return response.data?.data as AuditLog;
  },

  async createAuditLog(auditLog: CreateAuditLogDto) {
    const response = await postApiAuditLogsCreateAuditLog({ body: auditLog });
    return response.data?.data as AuditLog;
  },
};
