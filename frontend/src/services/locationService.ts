import api from './api';
import type { LocationDto, CreateLocationDto, UpdateLocationDto } from '../types';

export const locationService = {
  async getAll(electionGuid: string, pageNumber = 1, pageSize = 50): Promise<{ data: LocationDto[]; pageNumber: number; pageSize: number; totalCount: number; totalPages: number }> {
    const response = await api.get<{ data: LocationDto[]; pageNumber: number; pageSize: number; totalCount: number; totalPages: number }>(`/elections/${electionGuid}/locations`, {
      params: { pageNumber, pageSize }
    });
    return response.data;
  },

  async getById(electionGuid: string, locationGuid: string): Promise<LocationDto> {
    const response = await api.get<{ data: LocationDto }>(`/elections/${electionGuid}/locations/${locationGuid}`);
    return response.data.data;
  },

  async create(electionGuid: string, dto: CreateLocationDto): Promise<LocationDto> {
    const response = await api.post<{ data: LocationDto }>(`/elections/${electionGuid}/locations`, dto);
    return response.data.data;
  },

  async update(electionGuid: string, locationGuid: string, dto: UpdateLocationDto): Promise<LocationDto> {
    const response = await api.put<{ data: LocationDto }>(`/elections/${electionGuid}/locations/${locationGuid}`, dto);
    return response.data.data;
  },

  async delete(electionGuid: string, locationGuid: string): Promise<void> {
    await api.delete(`/elections/${electionGuid}/locations/${locationGuid}`);
  }
};
