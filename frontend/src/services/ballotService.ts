import api from './api';
import type { BallotDto, CreateBallotDto, UpdateBallotDto } from '../types';

export const ballotService = {
  async getAll(electionGuid: string): Promise<BallotDto[]> {
    const response = await api.get<BallotDto[]>(`/ballots/${electionGuid}`);
    return response.data;
  },

  async getById(ballotGuid: string): Promise<BallotDto> {
    const response = await api.get<BallotDto>(`/ballots/ballot/${ballotGuid}`);
    return response.data;
  },

  async create(dto: CreateBallotDto): Promise<BallotDto> {
    const response = await api.post<BallotDto>('/ballots', dto);
    return response.data;
  },

  async update(ballotGuid: string, dto: UpdateBallotDto): Promise<BallotDto> {
    const response = await api.put<BallotDto>(`/ballots/${ballotGuid}`, dto);
    return response.data;
  },

  async delete(ballotGuid: string): Promise<void> {
    await api.delete(`/ballots/${ballotGuid}`);
  },

  async getByLocation(locationGuid: string): Promise<BallotDto[]> {
    const response = await api.get<BallotDto[]>(`/ballots/location/${locationGuid}`);
    return response.data;
  }
};
