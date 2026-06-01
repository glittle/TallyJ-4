import { client } from "../api/config";
import type { Teller, CreateTellerDto, UpdateTellerDto } from "@/types/teller";
import type { ApiResponse, PaginatedResponse } from "@/types/ApiResponse";

export const tellerService = {
  async getTellersByElection(
    electionGuid: string,
    pageNumber = 1,
    pageSize = 50,
  ) {
    const response = await client.get<PaginatedResponse<Teller>>({
      url: `/api/elections/${electionGuid}/tellers`,
      query: { pageNumber, pageSize } as Record<string, unknown>,
    });
    return response.data;
  },

  async getTellerById(electionGuid: string, rowId: number) {
    const response = await client.get<ApiResponse<Teller>>({
      url: `/api/elections/${electionGuid}/tellers/${rowId}`,
    });
    return response.data.data;
  },

  async createTeller(electionGuid: string, teller: CreateTellerDto) {
    const response = await client.post<ApiResponse<Teller>>({
      url: `/api/elections/${electionGuid}/tellers`,
      body: teller,
    });
    return response.data.data;
  },

  async updateTeller(
    electionGuid: string,
    rowId: number,
    teller: UpdateTellerDto,
  ) {
    const response = await client.put<ApiResponse<Teller>>({
      url: `/api/elections/${electionGuid}/tellers/${rowId}`,
      body: teller,
    });
    return response.data.data;
  },

  async deleteTeller(electionGuid: string, rowId: number) {
    const response = await client.delete<ApiResponse<boolean>>({
      url: `/api/elections/${electionGuid}/tellers/${rowId}`,
    });
    return response.data.data;
  },

  async toggleTellerAccess(electionGuid: string, isOpen: boolean) {
    const response = await client.put<ApiResponse<boolean>>({
      url: `/api/elections/${electionGuid}/teller-access`,
      body: { isOpen },
    });
    return response.data.data;
  },
};
