import {
  getApiSecurityAuditLogs,
  getApiSecurityAuditLogsById,
  type BackendSecurityEventSeverity,
  type BackendSecurityEventType,
} from "@/api/gen/configService";
import type { AuditLog, AuditLogFilter } from "@/types/AuditLog";
import type { PaginatedResponse } from "@/types/ApiResponse";

/**
 * Client for security/operational audit logs.
 */
export const auditLogService = {
  async getAuditLogs(
    filter?: AuditLogFilter,
    pageNumber = 1,
    pageSize = 50,
  ): Promise<PaginatedResponse<AuditLog>> {
    const response = await getApiSecurityAuditLogs({
      query: {
        eventType: filter?.eventType as unknown as BackendSecurityEventType | undefined,
        userId: filter?.userId,
        onlineVoterId: filter?.onlineVoterId,
        electionGuid: filter?.electionGuid,
        email: filter?.email,
        ipAddress: filter?.ipAddress,
        isSuspicious: filter?.isSuspicious,
        severity: filter?.severity as never,
        startDate: filter?.startDate ? new Date(filter.startDate) : undefined,
        endDate: filter?.endDate ? new Date(filter.endDate) : undefined,
        searchTerm: filter?.searchTerm,
        pageNumber,
        pageSize,
      },
      throwOnError: true,
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

  async getAuditLogById(id: number): Promise<AuditLog> {
    const response = await getApiSecurityAuditLogsById({
      path: { id },
      throwOnError: true,
    });
    return response.data?.data as AuditLog;
  },
};
