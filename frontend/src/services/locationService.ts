import { getApiElectionsByElectionGuidLocations, getApiElectionsByElectionGuidLocationsByLocationGuid, postApiElectionsByElectionGuidLocations, putApiElectionsByElectionGuidLocationsByLocationGuid, deleteApiElectionsByElectionGuidLocationsByLocationGuid } from '../api/gen/configService/sdk.gen';
import type { LocationDto, CreateLocationDto, UpdateLocationDto } from '../types';

export const locationService = {
  async getAll(electionGuid: string, pageNumber = 1, pageSize = 50): Promise<{ data: LocationDto[]; pageNumber: number; pageSize: number; totalCount: number; totalPages: number }> {
    const response = await getApiElectionsByElectionGuidLocations({ 
      path: { electionGuid }, 
      query: { pageNumber, pageSize } 
    });
    return response.data?.data as { data: LocationDto[]; pageNumber: number; pageSize: number; totalCount: number; totalPages: number };
  },

  async getById(electionGuid: string, locationGuid: string): Promise<LocationDto> {
    const response = await getApiElectionsByElectionGuidLocationsByLocationGuid({ 
      path: { electionGuid, locationGuid } 
    });
    return response.data?.data as LocationDto;
  },

  async create(electionGuid: string, dto: CreateLocationDto): Promise<LocationDto> {
    const response = await postApiElectionsByElectionGuidLocations({ 
      path: { electionGuid }, 
      body: dto 
    });
    return response.data?.data as LocationDto;
  },

  async update(electionGuid: string, locationGuid: string, dto: UpdateLocationDto): Promise<LocationDto> {
    const response = await putApiElectionsByElectionGuidLocationsByLocationGuid({ 
      path: { electionGuid, locationGuid }, 
      body: dto 
    });
    return response.data?.data as LocationDto;
  },

  async delete(electionGuid: string, locationGuid: string): Promise<void> {
    await deleteApiElectionsByElectionGuidLocationsByLocationGuid({ 
      path: { electionGuid, locationGuid } 
    });
  }
};
