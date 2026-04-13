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
import {
  getApiResultsByElectionGuidResults,
  getApiResultsByElectionGuidSummary,
  getApiResultsByElectionGuidMonitor,
  getApiResultsByElectionGuidReport,
  getApiResultsByElectionGuidReportByReportCode,
  getApiResultsByElectionGuidPresentation,
  getApiResultsByElectionGuidDetailedStatistics,
  postApiResultsByElectionGuidCalculate,
  postApiResultsByElectionGuidTiesSave,
} from "../api/gen/configService/sdk.gen";

export const resultService = {
  async calculateTally(
    electionGuid: string,
    electionType: "normal" | "singlename" = "normal",
  ): Promise<TallyResultDto> {
    const response = await postApiResultsByElectionGuidCalculate({
      path: { electionGuid },
      query: { electionType },
      throwOnError: true,
    });
    return response.data as unknown as TallyResultDto;
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
    const response = await getApiResultsByElectionGuidResults({
      path: { electionGuid },
      throwOnError: true,
    });
    return response.data as unknown as TallyResultDto;
  },

  async getStatistics(electionGuid: string): Promise<TallyStatisticsDto> {
    const response = await getApiResultsByElectionGuidSummary({
      path: { electionGuid },
      throwOnError: true,
    });
    return response.data as unknown as TallyStatisticsDto;
  },

  async getMonitorInfo(electionGuid: string): Promise<MonitorInfoDto> {
    const response = await getApiResultsByElectionGuidMonitor({
      path: { electionGuid },
      throwOnError: true,
    });
    return response.data as unknown as MonitorInfoDto;
  },

  async getElectionReport(electionGuid: string): Promise<ElectionReportDto> {
    const response = await getApiResultsByElectionGuidReport({
      path: { electionGuid },
      throwOnError: true,
    });
    return response.data as unknown as ElectionReportDto;
  },

  async getReportData(
    electionGuid: string,
    reportCode: string,
  ): Promise<ReportDataResponseDto> {
    const response = await getApiResultsByElectionGuidReportByReportCode({
      path: { electionGuid, reportCode },
      throwOnError: true,
    });
    return response.data as unknown as ReportDataResponseDto;
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
    const response = await postApiResultsByElectionGuidTiesSave({
      path: { electionGuid },
      body: request,
      throwOnError: true,
    });
    return response.data as unknown as SaveTieCountsResponseDto;
  },

  async getPresentationData(electionGuid: string): Promise<PresentationDto> {
    const response = await getApiResultsByElectionGuidPresentation({
      path: { electionGuid },
      throwOnError: true,
    });
    return response.data as unknown as PresentationDto;
  },

  async getDetailedStatistics(
    electionGuid: string,
  ): Promise<DetailedStatisticsDto> {
    const response = await getApiResultsByElectionGuidDetailedStatistics({
      path: { electionGuid },
      throwOnError: true,
    });
    return response.data as unknown as DetailedStatisticsDto;
  },
};
