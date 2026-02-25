import {
  getApiByElectionGuidFrontdeskEligibleVoters,
  postApiByElectionGuidFrontdeskCheckInVoter,
  getApiByElectionGuidFrontdeskRollCall,
  getApiByElectionGuidFrontdeskStats,
  postApiByElectionGuidFrontdeskUnregisterVoter,
} from "../api/gen/configService";
import type {
  FrontDeskVoterDto,
  CheckInVoterDto,
  RollCallDto,
  FrontDeskStatsDto,
  UnregisterVoterDto,
} from "../types/FrontDesk";

export const frontDeskService = {
  async getEligibleVoters(electionGuid: string): Promise<FrontDeskVoterDto[]> {
    console.log(`Fetching eligible voters for election ${electionGuid}`);
    const response = await getApiByElectionGuidFrontdeskEligibleVoters({
      path: { electionGuid },
    });
    return (response.data?.data ?? []) as FrontDeskVoterDto[];
  },

  async checkInVoter(
    electionGuid: string,
    checkInDto: CheckInVoterDto,
  ): Promise<FrontDeskVoterDto> {
    const response = await postApiByElectionGuidFrontdeskCheckInVoter({
      path: { electionGuid },
      body: checkInDto,
    });
    return response.data?.data as FrontDeskVoterDto;
  },

  async unregisterVoter(
    electionGuid: string,
    unregisterDto: UnregisterVoterDto,
  ): Promise<FrontDeskVoterDto> {
    const response = await postApiByElectionGuidFrontdeskUnregisterVoter({
      path: { electionGuid },
      body: unregisterDto,
    });
    return response.data?.data as FrontDeskVoterDto;
  },

  async getRollCall(electionGuid: string): Promise<RollCallDto> {
    const response = await getApiByElectionGuidFrontdeskRollCall({
      path: { electionGuid },
    });
    return response.data?.data as RollCallDto;
  },

  async getStats(electionGuid: string): Promise<FrontDeskStatsDto> {
    const response = await getApiByElectionGuidFrontdeskStats({
      path: { electionGuid },
    });
    return response.data?.data as FrontDeskStatsDto;
  },
};
