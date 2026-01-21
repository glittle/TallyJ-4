import api from './api';
import type { ElectionDto, CreateElectionDto, UpdateElectionDto, ElectionSummaryDto } from '../types';

export const electionService = {
  async getAll(): Promise<ElectionDto[]> {
    const response = await api.get<ElectionDto[]>('/elections');
    return response.data;
  },

  async getById(electionGuid: string): Promise<ElectionDto> {
    const response = await api.get<ElectionDto>(`/elections/${electionGuid}`);
    return response.data;
  },

  async getSummaries(): Promise<ElectionSummaryDto[]> {
    const response = await api.get<ElectionSummaryDto[]>('/elections/summaries');
    return response.data;
  },

  async create(dto: CreateElectionDto): Promise<ElectionDto> {
    const response = await api.post<ElectionDto>('/elections', dto);
    return response.data;
  },

  async update(electionGuid: string, dto: UpdateElectionDto): Promise<ElectionDto> {
    const response = await api.put<ElectionDto>(`/elections/${electionGuid}`, dto);
    return response.data;
  },

  async delete(electionGuid: string): Promise<void> {
    await api.delete(`/elections/${electionGuid}`);
  },

  async getCurrentElection(): Promise<ElectionDto | null> {
    try {
      const response = await api.get<ElectionDto>('/elections/current');
      return response.data;
    } catch {
      return null;
    }
  }
};
