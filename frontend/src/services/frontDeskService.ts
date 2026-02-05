import { getApiElectionsByElectionGuidFrontdeskEligibleVoters, postApiElectionsByElectionGuidFrontdeskCheckin, getApiElectionsByElectionGuidFrontdeskRollcall, getApiElectionsByElectionGuidFrontdeskStats } from '../api/gen/configService/sdk.gen';
import type { FrontDeskVoterDto, CheckInVoterDto, RollCallDto, FrontDeskStatsDto } from '../types/FrontDesk';

export const frontDeskService = {
  async getEligibleVoters(electionGuid: string): Promise<FrontDeskVoterDto[]> {
    const response = await getApiElectionsByElectionGuidFrontdeskEligibleVoters({ path: { electionGuid } });
    return (response.data as any).data as FrontDeskVoterDto[];
  },

  async checkInVoter(electionGuid: string, checkInDto: CheckInVoterDto): Promise<FrontDeskVoterDto> {
    const response = await postApiElectionsByElectionGuidFrontdeskCheckin({ 
      path: { electionGuid }, 
      body: checkInDto 
    });
    return (response.data as any).data as FrontDeskVoterDto;
  },

  async getRollCall(electionGuid: string): Promise<RollCallDto> {
    const response = await getApiElectionsByElectionGuidFrontdeskRollcall({ path: { electionGuid } });
    return (response.data as any).data as RollCallDto;
  },

  async getStats(electionGuid: string): Promise<FrontDeskStatsDto> {
    const response = await getApiElectionsByElectionGuidFrontdeskStats({ path: { electionGuid } });
    return (response.data as any).data as FrontDeskStatsDto;
  }
};
