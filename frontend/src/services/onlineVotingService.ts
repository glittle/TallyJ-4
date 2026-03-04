import api from './api';
import type {
  RequestCodeDto,
  VerifyCodeDto,
  OnlineVoterAuthResponse,
  OnlineElectionInfo,
  OnlineCandidate,
  SubmitOnlineBallotDto,
  OnlineVoteStatus,
  GoogleAuthForVoterDto,
  FacebookAuthForVoterDto,
  KakaoAuthForVoterDto
} from '../types';

export const onlineVotingService = {
  async requestCode(data: RequestCodeDto): Promise<{ message: string }> {
    const response = await api.post<{ message: string }>('/online-voting/request-code', data);
    return response.data;
  },

  async verifyCode(data: VerifyCodeDto): Promise<OnlineVoterAuthResponse> {
    const response = await api.post<OnlineVoterAuthResponse>('/online-voting/verify-code', data);
    return response.data;
  },

  async googleAuth(data: GoogleAuthForVoterDto): Promise<OnlineVoterAuthResponse> {
    const response = await api.post<OnlineVoterAuthResponse>('/online-voting/googleAuth', data);
    return response.data;
  },

  async facebookAuth(data: FacebookAuthForVoterDto): Promise<OnlineVoterAuthResponse> {
    const response = await api.post<OnlineVoterAuthResponse>('/online-voting/facebookAuth', data);
    return response.data;
  },

  async kakaoAuth(data: KakaoAuthForVoterDto): Promise<OnlineVoterAuthResponse> {
    const response = await api.post<OnlineVoterAuthResponse>('/online-voting/kakaoAuth', data);
    return response.data;
  },

  async getElectionInfo(electionGuid: string): Promise<OnlineElectionInfo> {
    const response = await api.get<OnlineElectionInfo>(`/online-voting/elections/${electionGuid}`);
    return response.data;
  },

  async getCandidates(electionGuid: string): Promise<OnlineCandidate[]> {
    const response = await api.get<OnlineCandidate[]>(`/online-voting/elections/${electionGuid}/candidates`);
    return response.data;
  },

  async submitBallot(electionGuid: string, data: SubmitOnlineBallotDto): Promise<{ message: string }> {
    const response = await api.post<{ message: string }>(`/online-voting/elections/${electionGuid}/submit-ballot`, data);
    return response.data;
  },

  async getVoteStatus(electionGuid: string, voterId: string): Promise<OnlineVoteStatus> {
    const response = await api.get<OnlineVoteStatus>(`/online-voting/elections/${electionGuid}/vote-status`, {
      params: { voterId }
    });
    return response.data;
  }
};
