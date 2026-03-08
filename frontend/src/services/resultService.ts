import api from "./api";
import type {
  TallyResultDto,
  TallyStatisticsDto,
  ElectionReportDto,
  ReportDataResponseDto,
  TieDetailsDto,
  SaveTieCountsRequestDto,
  SaveTieCountsResponseDto,
  PresentationDto,
  MonitorInfoDto,
  DetailedStatisticsDto,
} from "../types";

export const resultService = {
  // Tally Calculation
  async calculateTally(
    electionGuid: string,
    electionType: "normal" | "singlename" = "normal",
  ): Promise<TallyResultDto> {
    const response = await api.post<TallyResultDto>(
      `/results/election/${electionGuid}/calculate`,
      null,
      {
        params: { electionType },
      },
    );
    return response.data;
  },

  // Legacy methods for backward compatibility
  async calculateNormalElection(electionGuid: string): Promise<TallyResultDto> {
    return this.calculateTally(electionGuid, "normal");
  },

  async calculateSingleNameElection(
    electionGuid: string,
  ): Promise<TallyResultDto> {
    return this.calculateTally(electionGuid, "singlename");
  },

  // Results retrieval
  async getResults(electionGuid: string): Promise<TallyResultDto> {
    const response = await api.get<TallyResultDto>(`/results/${electionGuid}`);
    return response.data;
  },

  async getStatistics(electionGuid: string): Promise<TallyStatisticsDto> {
    const response = await api.get<TallyStatisticsDto>(
      `/results/${electionGuid}/statistics`,
    );
    return response.data;
  },

  // Monitoring
  async getMonitorInfo(electionGuid: string): Promise<MonitorInfoDto> {
    const response = await api.get<MonitorInfoDto>(
      `/results/election/${electionGuid}/monitor`,
    );
    return response.data;
  },

  // Reports
  async getElectionReport(electionGuid: string): Promise<ElectionReportDto> {
    const response = await api.get<ElectionReportDto>(
      `/results/election/${electionGuid}/report`,
    );
    return response.data;
  },

  async getReportData(
    electionGuid: string,
    reportCode: string,
  ): Promise<ReportDataResponseDto> {
    const response = await api.get<ReportDataResponseDto>(
      `/results/election/${electionGuid}/report/${reportCode}`,
    );
    return response.data;
  },

  // Tie Management
  async getTieDetails(electionGuid: string): Promise<TieDetailsDto[]> {
    const response = await api.get<TieDetailsDto[]>(
      `/results/election/${electionGuid}/ties`,
    );
    return response.data;
  },

  async saveTieCounts(
    electionGuid: string,
    request: SaveTieCountsRequestDto,
  ): Promise<SaveTieCountsResponseDto> {
    const response = await api.post<SaveTieCountsResponseDto>(
      `/results/election/${electionGuid}/ties/save`,
      request,
    );
    return response.data;
  },

  // Presentation
  async getPresentationData(electionGuid: string): Promise<PresentationDto> {
    const response = await api.get<PresentationDto>(
      `/results/election/${electionGuid}/presentation`,
    );
    return response.data;
  },

  // Detailed Statistics
  async getDetailedStatistics(
    electionGuid: string,
  ): Promise<DetailedStatisticsDto> {
    const response = await api.get<DetailedStatisticsDto>(
      `/results/election/${electionGuid}/detailed-statistics`,
    );
    return response.data;
  },
};
