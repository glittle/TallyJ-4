import { getApiBallotsElectionByElectionGuid, getApiBallotsByGuid, postApiBallots, putApiBallotsByGuid, deleteApiBallotsByGuid } from '../api/gen/configService/sdk.gen';
import type { BallotDto, CreateBallotDto, UpdateBallotDto } from '../types';

export const ballotService = {
  async getAll(electionGuid: string): Promise<BallotDto[]> {
    const response = await getApiBallotsElectionByElectionGuid({ path: { electionGuid } });
    return response.data as BallotDto[];
  },

  async getById(ballotGuid: string): Promise<BallotDto> {
    const response = await getApiBallotsByGuid({ path: { guid: ballotGuid } });
    return response.data as BallotDto;
  },

  async create(dto: CreateBallotDto): Promise<BallotDto> {
    const response = await postApiBallots({ body: dto });
    return response.data as BallotDto;
  },

  async update(ballotGuid: string, dto: UpdateBallotDto): Promise<BallotDto> {
    const response = await putApiBallotsByGuid({ path: { guid: ballotGuid }, body: dto });
    return response.data as BallotDto;
  },

  async delete(ballotGuid: string): Promise<void> {
    await deleteApiBallotsByGuid({ path: { guid: ballotGuid } });
  },

  async getByLocation(locationGuid: string): Promise<BallotDto[]> {
    const response = await getApiBallotsElectionByElectionGuid({ path: { electionGuid: locationGuid } });
    return response.data as BallotDto[];
  }
};
