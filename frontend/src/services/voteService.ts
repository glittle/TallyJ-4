import api from './api';
import type { VoteDto, CreateVoteDto } from '../types';

export const voteService = {
  async getByBallot(ballotGuid: string): Promise<VoteDto[]> {
    const response = await api.get<VoteDto[]>(`/votes/${ballotGuid}`);
    return response.data;
  },

  async create(dto: CreateVoteDto): Promise<VoteDto> {
    const response = await api.post<VoteDto>('/votes', dto);
    return response.data;
  },

  async delete(ballotGuid: string, positionOnBallot: number): Promise<void> {
    await api.delete(`/votes/${ballotGuid}/${positionOnBallot}`);
  }
};
