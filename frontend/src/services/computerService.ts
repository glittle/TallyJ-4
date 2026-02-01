import api from './api';
import type { ComputerDto, RegisterComputerDto, UpdateComputerDto } from '../types';

export const computerService = {
  async getByLocation(electionGuid: string, locationGuid: string): Promise<ComputerDto[]> {
    const response = await api.get<{ data: ComputerDto[] }>(`/elections/${electionGuid}/locations/${locationGuid}/computers`);
    return response.data.data;
  },

  async register(electionGuid: string, locationGuid: string, dto: RegisterComputerDto): Promise<ComputerDto> {
    const response = await api.post<{ data: ComputerDto }>(`/elections/${electionGuid}/locations/${locationGuid}/computers`, dto);
    return response.data.data;
  },

  async delete(electionGuid: string, locationGuid: string, computerGuid: string): Promise<void> {
    await api.delete(`/elections/${electionGuid}/locations/${locationGuid}/computers/${computerGuid}`);
  }
};
