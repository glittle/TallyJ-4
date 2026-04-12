import type {
  DetailedStatisticsDto,
  ElectionReportDto,
  MonitorInfoDto,
  PresentationDto,
  ReportDataResponseDto,
  SaveTieCountsRequestDto,
  SaveTieCountsResponseDto,
  TallyResultDto,
  TallyStatisticsDto,
  TieDetailsDto,
} from "../types";
import api from "./api";

export const resultService = {
  async calculateTally(
    electionGuid: string,
    electionType: "normal" | "singlename" = "normal",
  ): Promise<TallyResultDto> {
    const response = await api.post<TallyResultDto>(
      `/api/results/${electionGuid}/calculate`,
      null,
      {
        params: { electionType },
      },
    );
    return response.data;
  },

  async calculateNormalElection(electionGuid: string): Promise<TallyResultDto> {
    return this.calculateTally(electionGuid, "normal");
  },

  async calculateSingleNameElection(
    electionGuid: string,
  ): Promise<TallyResultDto> {
    return this.calculateTally(electionGuid, "singlename");
  },

  async getResults(electionGuid: string): Promise<TallyResultDto> {
    const response = await api.get<TallyResultDto>(
      `/api/results/${electionGuid}`,
    );
    return response.data;
  },

  async getStatistics(electionGuid: string): Promise<TallyStatisticsDto> {
    const response = await api.get<TallyStatisticsDto>(
      `/api/results/${electionGuid}/summary`,
    );
    return response.data;
  },

  async getMonitorInfo(electionGuid: string): Promise<MonitorInfoDto> {
    const response = await api.get<MonitorInfoDto>(
      `/api/results/${electionGuid}/monitor`,
    );
    return response.data;
  },

  async getElectionReport(electionGuid: string): Promise<ElectionReportDto> {
    const response = await api.get<ElectionReportDto>(
      `/api/results/${electionGuid}/report`,
    );
    return response.data;
  },

  async getReportData(
    electionGuid: string,
    reportCode: string,
  ): Promise<ReportDataResponseDto> {
    const response = await api.get<ReportDataResponseDto>(
      `/api/results/${electionGuid}/report/${reportCode}`,
    );
    return response.data;
  },

  async getTieDetails(electionGuid: string): Promise<TieDetailsDto[]> {
    const response = await api.get<TieDetailsDto[]>(
      `/api/results/${electionGuid}/ties`,
    );
    return response.data;
  },

  async saveTieCounts(
    electionGuid: string,
    request: SaveTieCountsRequestDto,
  ): Promise<SaveTieCountsResponseDto> {
    const response = await api.post<SaveTieCountsResponseDto>(
      `/api/results/${electionGuid}/ties/save`,
      request,
    );
    return response.data;
  },

  async getPresentationData(electionGuid: string): Promise<PresentationDto> {
    const response = await api.get<PresentationDto>(
      `/api/results/${electionGuid}/presentation`,
    );
    return response.data;
  },

  async getDetailedStatistics(
    electionGuid: string,
  ): Promise<DetailedStatisticsDto> {
    const response = await api.get<DetailedStatisticsDto>(
      `/api/results/${electionGuid}/detailed-statistics`,
    );
    return response.data;
  },
};
