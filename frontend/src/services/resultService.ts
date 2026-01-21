import api from './api';
import type { TallyResultDto, TallyStatisticsDto } from '../types';

export const resultService = {
  async calculateNormalElection(electionGuid: string): Promise<TallyResultDto> {
    const response = await api.post<TallyResultDto>(`/results/${electionGuid}/calculate-normal`);
    return response.data;
  },

  async calculateSingleNameElection(electionGuid: string): Promise<TallyResultDto> {
    const response = await api.post<TallyResultDto>(`/results/${electionGuid}/calculate-single-name`);
    return response.data;
  },

  async getResults(electionGuid: string): Promise<TallyResultDto> {
    const response = await api.get<TallyResultDto>(`/results/${electionGuid}`);
    return response.data;
  },

  async getStatistics(electionGuid: string): Promise<TallyStatisticsDto> {
    const response = await api.get<TallyStatisticsDto>(`/results/${electionGuid}/statistics`);
    return response.data;
  }
};
