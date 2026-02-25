import {
  getApiByElectionGuidLocationsGetLocations,
  getApiByElectionGuidLocationsByLocationGuidGetLocation,
  postApiByElectionGuidLocationsCreateLocation,
  putApiByElectionGuidLocationsByLocationGuidUpdateLocation,
  deleteApiByElectionGuidLocationsByLocationGuidDeleteLocation,
} from "../api/gen/configService/sdk.gen";
import type {
  LocationDto,
  CreateLocationDto,
  UpdateLocationDto,
} from "../types";

export const locationService = {
  async getAll(
    electionGuid: string,
    pageNumber = 1,
    pageSize = 50,
  ): Promise<{
    data: LocationDto[];
    pageNumber: number;
    pageSize: number;
    totalCount: number;
    totalPages: number;
  }> {
    const response = await getApiByElectionGuidLocationsGetLocations({
      path: { electionGuid },
      query: { pageNumber, pageSize },
    });
    return response.data as {
      data: LocationDto[];
      pageNumber: number;
      pageSize: number;
      totalCount: number;
      totalPages: number;
    };
  },

  async getById(
    electionGuid: string,
    locationGuid: string,
  ): Promise<LocationDto> {
    const response =
      await getApiByElectionGuidLocationsByLocationGuidGetLocation({
        path: { electionGuid, locationGuid },
      });
    return response.data?.data as LocationDto;
  },

  async create(
    electionGuid: string,
    dto: CreateLocationDto,
  ): Promise<LocationDto> {
    const response = await postApiByElectionGuidLocationsCreateLocation({
      path: { electionGuid },
      body: dto,
    });
    return response.data?.data as LocationDto;
  },

  async update(
    electionGuid: string,
    locationGuid: string,
    dto: UpdateLocationDto,
  ): Promise<LocationDto> {
    const response =
      await putApiByElectionGuidLocationsByLocationGuidUpdateLocation({
        path: { electionGuid, locationGuid },
        body: dto,
      });
    return response.data?.data as LocationDto;
  },

  async delete(electionGuid: string, locationGuid: string): Promise<void> {
    await deleteApiByElectionGuidLocationsByLocationGuidDeleteLocation({
      path: { electionGuid, locationGuid },
    });
  },
};
