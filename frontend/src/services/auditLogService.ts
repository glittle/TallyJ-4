import api from "./api";
import type {
  AuditLog,
  AuditLogFilter,
  CreateAuditLogDto,
} from "@/types/AuditLog";
import type { ApiResponse, PaginatedResponse } from "@/types/ApiResponse";

export const auditLogService = {
  async getAuditLogs(filter?: AuditLogFilter, pageNumber = 1, pageSize = 50) {
    const response = await api.get<PaginatedResponse<AuditLog>>(
      "/api/audit-logs",
      {
        params: {
          ...filter,
          pageNumber,
          pageSize,
        },
      },
    );
    return response.data;
  },

  async getAuditLogById(rowId: number) {
    const response = await api.get<ApiResponse<AuditLog>>(
      `/api/audit-logs/${rowId}`,
    );
    return response.data.data;
  },

  async createAuditLog(auditLog: CreateAuditLogDto) {
    const response = await api.post<ApiResponse<AuditLog>>(
      "/api/audit-logs",
      auditLog,
    );
    return response.data.data;
  },
};
