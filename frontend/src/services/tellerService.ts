import api from "./api";
import type { Teller, CreateTellerDto, UpdateTellerDto } from "@/types/teller";
import type { ApiResponse, PaginatedResponse } from "@/types/ApiResponse";

export const tellerService = {
  async getTellersByElection(
    electionGuid: string,
    pageNumber = 1,
    pageSize = 50,
  ) {
    const response = await api.get<PaginatedResponse<Teller>>(
      `/api/elections/${electionGuid}/tellers`,
      {
        params: { pageNumber, pageSize },
      },
    );
    return response.data;
  },

  async getTellerById(electionGuid: string, rowId: number) {
    const response = await api.get<ApiResponse<Teller>>(
      `/api/elections/${electionGuid}/tellers/${rowId}`,
    );
    return response.data.data;
  },

  async createTeller(electionGuid: string, teller: CreateTellerDto) {
    const response = await api.post<ApiResponse<Teller>>(
      `/api/elections/${electionGuid}/tellers`,
      teller,
    );
    return response.data.data;
  },

  async updateTeller(
    electionGuid: string,
    rowId: number,
    teller: UpdateTellerDto,
  ) {
    const response = await api.put<ApiResponse<Teller>>(
      `/api/elections/${electionGuid}/tellers/${rowId}`,
      teller,
    );
    return response.data.data;
  },

  async deleteTeller(electionGuid: string, rowId: number) {
    const response = await api.delete<ApiResponse<boolean>>(
      `/api/elections/${electionGuid}/tellers/${rowId}`,
    );
    return response.data.data;
  },

  async toggleTellerAccess(electionGuid: string, isOpen: boolean) {
    const response = await api.put<ApiResponse<boolean>>(
      `/api/elections/${electionGuid}/teller-access`,
      { isOpen },
    );
    return response.data.data;
  },
};
