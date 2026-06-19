import {
  postApiResultsElectionByElectionGuidCalculate,
  getApiResultsElectionByElectionGuid,
  getApiResultsElectionByElectionGuidSummary,
  getApiResultsElectionByElectionGuidMonitor,
  getApiResultsElectionByElectionGuidReport,
  getApiResultsElectionByElectionGuidReportByReportCode,
  getApiResultsByElectionGuidByTieBreakGroupTies,
  postApiResultsElectionByElectionGuidTiesSave,
  getApiResultsElectionByElectionGuidPresentation,
  getApiResultsElectionByElectionGuidDetailedStatistics,
} from "@/api/gen/configService";
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
    const response = await postApiResultsElectionByElectionGuidCalculate({
      path: { electionGuid },
      query: { electionType },
    });
    return response.data as TallyResultDto;
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
    const response = await getApiResultsElectionByElectionGuid({
      path: { electionGuid },
    });
    return response.data as TallyResultDto;
  },

  async getStatistics(electionGuid: string): Promise<TallyStatisticsDto> {
    const response = await getApiResultsElectionByElectionGuidSummary({
      path: { electionGuid },
    });
    return response.data as TallyStatisticsDto;
  },

  async getMonitorInfo(electionGuid: string): Promise<MonitorInfoDto> {
    const response = await getApiResultsElectionByElectionGuidMonitor({
      path: { electionGuid },
    });
    return response.data as MonitorInfoDto;
  },

  async getElectionReport(electionGuid: string): Promise<ElectionReportDto> {
    const response = await getApiResultsElectionByElectionGuidReport({
      path: { electionGuid },
    });
    return response.data as ElectionReportDto;
  },

  async getReportData(
    electionGuid: string,
    reportCode: string,
  ): Promise<ReportDataResponseDto> {
    const response =
      await getApiResultsElectionByElectionGuidReportByReportCode({
        path: { electionGuid, reportCode },
      });
    return response.data as ReportDataResponseDto;
  },

  async getTieDetails(electionGuid: string): Promise<TieDetailsDto[]> {
    const resultsResponse = await getApiResultsElectionByElectionGuid({
      path: { electionGuid },
    });
    const ties = resultsResponse.data?.ties ?? [];

    if (ties.length === 0) {
      return [];
    }

    const details = await Promise.all(
      ties.map(async (tie) => {
        const response = await getApiResultsByElectionGuidByTieBreakGroupTies({
          path: {
            electionGuid,
            tieBreakGroup: tie.tieBreakGroup ?? 0,
          },
        });
        return response.data as TieDetailsDto;
      }),
    );

    return details.filter((detail) => detail.tieBreakGroup > 0);
  },

  async saveTieCounts(
    electionGuid: string,
    request: SaveTieCountsRequestDto,
  ): Promise<SaveTieCountsResponseDto> {
    const response = await postApiResultsElectionByElectionGuidTiesSave({
      path: { electionGuid },
      body: request,
    });
    return response.data as SaveTieCountsResponseDto;
  },

  async getPresentationData(electionGuid: string): Promise<PresentationDto> {
    const response = await getApiResultsElectionByElectionGuidPresentation({
      path: { electionGuid },
    });
    return response.data as PresentationDto;
  },

  async getDetailedStatistics(
    electionGuid: string,
  ): Promise<DetailedStatisticsDto> {
    const response =
      await getApiResultsElectionByElectionGuidDetailedStatistics({
        path: { electionGuid },
      });
    return response.data as DetailedStatisticsDto;
  },
};
