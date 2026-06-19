import {
  getApiByElectionGuidFrontdeskEligibleVoters,
  postApiByElectionGuidFrontdeskCheckInVoter,
  getApiByElectionGuidFrontdeskRollCall,
  getApiByElectionGuidFrontdeskStats,
  postApiByElectionGuidFrontdeskUnregisterVoter,
  postApiByElectionGuidFrontdeskUpdatePersonFlags,
  postApiByElectionGuidFrontdeskUpdateEnvelopeNumber,
} from "@/api/gen/configService";
import type {
  FrontDeskVoterDto,
  CheckInVoterDto,
  RollCallDto,
  FrontDeskStatsDto,
  UnregisterVoterDto,
  UpdatePersonFlagsDto,
  UpdateEnvelopeNumberDto,
} from "../types/FrontDesk";

export const frontDeskService = {
  async getEligibleVoters(electionGuid: string): Promise<FrontDeskVoterDto[]> {
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

  async updatePersonFlags(
    electionGuid: string,
    updateFlagsDto: UpdatePersonFlagsDto,
  ): Promise<FrontDeskVoterDto> {
    const response = await postApiByElectionGuidFrontdeskUpdatePersonFlags({
      path: { electionGuid },
      body: updateFlagsDto,
    });
    return response.data?.data as FrontDeskVoterDto;
  },

  async updateEnvelopeNumber(
    electionGuid: string,
    updateDto: UpdateEnvelopeNumberDto,
  ): Promise<FrontDeskVoterDto> {
    const response = await postApiByElectionGuidFrontdeskUpdateEnvelopeNumber({
      path: { electionGuid },
      body: updateDto,
    });
    return response.data?.data as FrontDeskVoterDto;
  },
};
