import api from '../services/api';

export interface ExportRequest {
  format: 'pdf' | 'excel';
  filters?: Record<string, any>;
}

export const reportsApi = {
  async exportReport(electionId: string, request: ExportRequest): Promise<Blob> {
    const response = await api.post<Blob>(`/reports/export/${electionId}`, request, {
      responseType: 'blob'
    });
    return response.data;
  }
};