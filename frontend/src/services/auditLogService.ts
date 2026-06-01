import { client } from "../api/config";
import type {
  AuditLog,
  AuditLogFilter,
  CreateAuditLogDto,
} from "@/types/AuditLog";
import type { ApiResponse, PaginatedResponse } from "@/types/ApiResponse";

export const auditLogService = {
  async getAuditLogs(filter?: AuditLogFilter, pageNumber = 1, pageSize = 50) {
    const response = await client.get<PaginatedResponse<AuditLog>>({
      url: "/api/audit-logs",
      query: {
        ...(filter ?? {}),
        pageNumber,
        pageSize,
      } as Record<string, unknown>,
    });
    return response.data;
  },

  async getAuditLogById(rowId: number) {
    const response = await client.get<ApiResponse<AuditLog>>({
      url: `/api/audit-logs/${rowId}`,
    });
    return response.data.data;
  },

  async createAuditLog(auditLog: CreateAuditLogDto) {
    const response = await client.post<ApiResponse<AuditLog>>({
      url: "/api/audit-logs",
      body: auditLog,
    });
    return response.data.data;
  },
};
