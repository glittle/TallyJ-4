import { getApiElectionsByElectionGuidLocationsByLocationGuidComputers, postApiElectionsByElectionGuidLocationsByLocationGuidComputers, deleteApiElectionsByElectionGuidLocationsByLocationGuidComputersByComputerGuid } from '../api/gen/configService/sdk.gen';
import type { ComputerDto, RegisterComputerDto, UpdateComputerDto } from '../types';

export const computerService = {
  async getByLocation(electionGuid: string, locationGuid: string): Promise<ComputerDto[]> {
    const response = await getApiElectionsByElectionGuidLocationsByLocationGuidComputers({ 
      path: { electionGuid, locationGuid } 
    });
    return (response.data as any).data as ComputerDto[];
  },

  async register(electionGuid: string, locationGuid: string, dto: RegisterComputerDto): Promise<ComputerDto> {
    const response = await postApiElectionsByElectionGuidLocationsByLocationGuidComputers({ 
      path: { electionGuid, locationGuid }, 
      body: dto 
    });
    return (response.data as any).data as ComputerDto;
  },

  async delete(electionGuid: string, locationGuid: string, computerGuid: string): Promise<void> {
    await deleteApiElectionsByElectionGuidLocationsByLocationGuidComputersByComputerGuid({ 
      path: { electionGuid, locationGuid, computerGuid } 
    });
  }
};
