import api from './api';
import type { FrontDeskVoterDto, CheckInVoterDto, RollCallDto, FrontDeskStatsDto } from '../types/FrontDesk';

export const frontDeskService = {
  async getEligibleVoters(electionGuid: string): Promise<FrontDeskVoterDto[]> {
    const response = await api.get(`/elections/${electionGuid}/frontdesk/eligible-voters`);
    return response.data.data;
  },

  async checkInVoter(electionGuid: string, checkInDto: CheckInVoterDto): Promise<FrontDeskVoterDto> {
    const response = await api.post(`/elections/${electionGuid}/frontdesk/checkin`, checkInDto);
    return response.data.data;
  },

  async getRollCall(electionGuid: string): Promise<RollCallDto> {
    const response = await api.get(`/elections/${electionGuid}/frontdesk/rollcall`);
    return response.data.data;
  },

  async getStats(electionGuid: string): Promise<FrontDeskStatsDto> {
    const response = await api.get(`/elections/${electionGuid}/frontdesk/stats`);
    return response.data.data;
  }
};
