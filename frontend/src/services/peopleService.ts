import api from './api';
import type { PersonDto, CreatePersonDto, UpdatePersonDto } from '../types';

export const peopleService = {
  async getAll(electionGuid: string): Promise<PersonDto[]> {
    const response = await api.get<PersonDto[]>(`/people/${electionGuid}`);
    return response.data;
  },

  async getById(personGuid: string): Promise<PersonDto> {
    const response = await api.get<PersonDto>(`/people/person/${personGuid}`);
    return response.data;
  },

  async create(dto: CreatePersonDto): Promise<PersonDto> {
    const response = await api.post<PersonDto>('/people', dto);
    return response.data;
  },

  async update(personGuid: string, dto: UpdatePersonDto): Promise<PersonDto> {
    const response = await api.put<PersonDto>(`/people/${personGuid}`, dto);
    return response.data;
  },

  async delete(personGuid: string): Promise<void> {
    await api.delete(`/people/${personGuid}`);
  },

  async search(electionGuid: string, query: string): Promise<PersonDto[]> {
    const response = await api.get<PersonDto[]>(`/people/${electionGuid}/search`, {
      params: { query }
    });
    return response.data;
  },

  async getVoters(electionGuid: string): Promise<PersonDto[]> {
    const response = await api.get<PersonDto[]>(`/people/${electionGuid}/voters`);
    return response.data;
  },

  async getCandidates(electionGuid: string): Promise<PersonDto[]> {
    const response = await api.get<PersonDto[]>(`/people/${electionGuid}/candidates`);
    return response.data;
  }
};
