import { client } from "../api/config";
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
  async calculateTally(
    electionGuid: string,
    electionType: "normal" | "singlename" = "normal",
  ): Promise<TallyResultDto> {
    const response = await client.post<TallyResultDto>({
      url: `/api/results/election/${electionGuid}/calculate`,
      query: { electionType } as Record<string, unknown>,
    });
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
    const response = await client.get<TallyResultDto>({
      url: `/api/results/election/${electionGuid}`,
    });
    return response.data;
  },

  async getStatistics(electionGuid: string): Promise<TallyStatisticsDto> {
    const response = await client.get<TallyStatisticsDto>({
      url: `/api/results/election/${electionGuid}/summary`,
    });
    return response.data;
  },

  async getMonitorInfo(electionGuid: string): Promise<MonitorInfoDto> {
    const response = await client.get<MonitorInfoDto>({
      url: `/api/results/election/${electionGuid}/monitor`,
    });
    return response.data;
  },

  async getElectionReport(electionGuid: string): Promise<ElectionReportDto> {
    const response = await client.get<ElectionReportDto>({
      url: `/api/results/election/${electionGuid}/report`,
    });
    return response.data;
  },

  async getReportData(
    electionGuid: string,
    reportCode: string,
  ): Promise<ReportDataResponseDto> {
    const response = await client.get<ReportDataResponseDto>({
      url: `/api/results/election/${electionGuid}/report/${reportCode}`,
    });
    return response.data;
  },

  async getTieDetails(electionGuid: string): Promise<TieDetailsDto[]> {
    const response = await client.get<TieDetailsDto[]>({
      url: `/api/results/election/${electionGuid}/ties`,
    });
    return response.data;
  },

  async saveTieCounts(
    electionGuid: string,
    request: SaveTieCountsRequestDto,
  ): Promise<SaveTieCountsResponseDto> {
    const response = await client.post<SaveTieCountsResponseDto>({
      url: `/api/results/election/${electionGuid}/ties/save`,
      body: request,
    });
    return response.data;
  },

  async getPresentationData(electionGuid: string): Promise<PresentationDto> {
    const response = await client.get<PresentationDto>({
      url: `/api/results/election/${electionGuid}/presentation`,
    });
    return response.data;
  },

  async getDetailedStatistics(
    electionGuid: string,
  ): Promise<DetailedStatisticsDto> {
    const response = await client.get<DetailedStatisticsDto>({
      url: `/api/results/election/${electionGuid}/detailed-statistics`,
    });
    return response.data;
  },
};
