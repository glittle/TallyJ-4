import { getApiElections, getApiElectionsByGuid, postApiElections, putApiElectionsByGuid, deleteApiElectionsByGuid, getApiElectionsByGuidSummary } from '../api/gen/configService/sdk.gen';
import { cacheService } from './cacheService';
import type { ElectionDto, CreateElectionDto, UpdateElectionDto, ElectionSummaryDto } from '../types';

export const electionService = {
  async getAll(): Promise<ElectionDto[]> {
    const response = await getApiElections();
    return response.data.items || [];
  },

  async getById(electionGuid: string): Promise<ElectionDto> {
    const response = await getApiElectionsByGuid({ path: { guid: electionGuid } });
    return response.data as ElectionDto;
  },

  async getSummaries(): Promise<ElectionSummaryDto[]> {
    const response = await getApiElectionsByGuidSummary({ path: { guid: '' } });
    return response.data as ElectionSummaryDto[];
  },

  async create(dto: CreateElectionDto): Promise<ElectionDto> {
    const response = await postApiElections({ body: dto });
    await cacheService.remove(cacheService.generateKey({ url: '/api/elections', method: 'GET' }));
    await cacheService.remove(cacheService.generateKey({ url: '/api/elections/summaries', method: 'GET' }));
    return response.data as ElectionDto;
  },

  async update(electionGuid: string, dto: UpdateElectionDto): Promise<ElectionDto> {
    const response = await putApiElectionsByGuid({ path: { guid: electionGuid }, body: dto });
    await cacheService.remove(cacheService.generateKey({ url: `/api/elections/${electionGuid}`, method: 'GET' }));
    await cacheService.remove(cacheService.generateKey({ url: '/api/elections', method: 'GET' }));
    await cacheService.remove(cacheService.generateKey({ url: '/api/elections/summaries', method: 'GET' }));
    return response.data as ElectionDto;
  },

  async delete(electionGuid: string): Promise<void> {
    await deleteApiElectionsByGuid({ path: { guid: electionGuid } });
    await cacheService.remove(cacheService.generateKey({ url: '/api/elections', method: 'GET' }));
    await cacheService.remove(cacheService.generateKey({ url: '/api/elections/summaries', method: 'GET' }));
  },

  async getCurrentElection(): Promise<ElectionDto | null> {
    try {
      const response = await getApiElections();
      return response.data.items?.[0] || null;
    } catch {
      return null;
    }
  }
};
