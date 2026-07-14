import { client } from "@/api/gen/configService/client.gen";
import type { AuditLog, AuditLogFilter } from "@/types/AuditLog";
import type { PaginatedResponse } from "@/types/ApiResponse";

/**
 * Client for security/operational audit logs.
 * Uses the shared OpenAPI client until generated SDK methods are regenerated.
 */
export const auditLogService = {
  async getAuditLogs(
    filter?: AuditLogFilter,
    pageNumber = 1,
    pageSize = 50,
  ): Promise<PaginatedResponse<AuditLog>> {
    const response = await client.get({
      url: "/api/security-audit-logs",
      query: {
        eventType: filter?.eventType,
        userId: filter?.userId,
        onlineVoterId: filter?.onlineVoterId,
        electionGuid: filter?.electionGuid,
        email: filter?.email,
        ipAddress: filter?.ipAddress,
        isSuspicious: filter?.isSuspicious,
        severity: filter?.severity,
        startDate: filter?.startDate
          ? new Date(filter.startDate).toISOString()
          : undefined,
        endDate: filter?.endDate
          ? new Date(filter.endDate).toISOString()
          : undefined,
        searchTerm: filter?.searchTerm,
        pageNumber,
        pageSize,
      },
      throwOnError: true,
    });

    const data = response.data as {
      items?: AuditLog[];
      totalCount?: number;
      pageNumber?: number;
      pageSize?: number;
      totalPages?: number;
    };

    return {
      items: data?.items ?? [],
      totalCount: data?.totalCount ?? 0,
      page: data?.pageNumber ?? pageNumber,
      pageSize: data?.pageSize ?? pageSize,
      totalPages: data?.totalPages ?? 0,
    };
  },

  async getAuditLogById(id: number): Promise<AuditLog> {
    const response = await client.get({
      url: `/api/security-audit-logs/${id}`,
      throwOnError: true,
    });
    const outer = response.data as { data?: AuditLog };
    return outer?.data as AuditLog;
  },
};
