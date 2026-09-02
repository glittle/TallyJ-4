import type { ElectionStage } from "../domain/electionStages";
import type {
  CreateElectionDto,
  DuplicateElectionDto,
  ElectionDto,
  ElectionStats,
  ElectionStatus,
  ElectionSummaryDto,
  ImportResultDto,
  UpdateElectionDto,
} from "../types";
import { client } from "../api/gen/configService/client.gen";
import {
  deleteApiElectionsByGuidDeleteElection,
  getApiElectionsByGuidElection,
  getApiElectionsByGuidStats,
  getApiElectionsByGuidStatus,
  getApiElectionsGetElections,
  getApiImportExportElectionToJsonByElectionGuid,
  postApiElectionsByGuidDuplicateElection,
  postApiElectionsByGuidResetElection,
  postApiElectionsCreateElection,
  postApiImportImportCdnBallotsByElectionGuid,
  postApiImportImportElectionFromJson,
  postApiImportImportTallyJv3Election,
  getApiElectionsByGuidOnlineBallotsAcceptAllSummary,
  postApiElectionsByGuidOnlineBallotsAcceptAll,
  putApiElectionsByGuidStage,
  putApiElectionsByGuidTellerAccess,
  putApiElectionsByGuidUpdateElection,
} from "../api/gen/configService/sdk.gen";
import type {
  OnlineVotingAcceptAllOnlineBallotsResultDto,
  OnlineVotingAcceptAllOnlineBallotsSummaryDto,
} from "../api/gen/configService/types.gen";
const convertStringToDate = (dateString?: string): Date | null => {
  return dateString ? new Date(dateString) : null;
};

/** Normalize API date fields that may already be Date or ISO string. */
const convertDateToString = (
  date?: Date | string | null,
): string | undefined => {
  if (date === null || date === undefined || date === "") {
    return undefined;
  }
  if (typeof date === "string") {
    const parsed = new Date(date);
    return Number.isNaN(parsed.getTime()) ? date : parsed.toISOString();
  }
  if (date instanceof Date) {
    return Number.isNaN(date.getTime()) ? undefined : date.toISOString();
  }
  // Fallback for unexpected shapes (e.g. plain object) — avoid crashing UI.
  try {
    const parsed = new Date(date as string | number);
    return Number.isNaN(parsed.getTime()) ? undefined : parsed.toISOString();
  } catch {
    return undefined;
  }
};

function mapElectionDto(data: any): ElectionDto {
  return {
    ...(data as object),
    dateOfElection: convertDateToString(data.dateOfElection),
    onlineWhenOpen: convertDateToString(data.onlineWhenOpen),
    onlineWhenClose: convertDateToString(data.onlineWhenClose),
    tellerAccessOpenedAt: convertDateToString(data.tellerAccessOpenedAt),
  } as ElectionDto;
}

export const electionService = {
  async getAll(): Promise<ElectionSummaryDto[]> {
    const response = await getApiElectionsGetElections();
    return (
      response.data?.items?.map((item) => ({
        ...item,
        electionGuid: item.electionGuid || "",
        name: item.name || "",
        dateOfElection: convertDateToString(item.dateOfElection),
        voterCount: item.voterCount ?? 0,
        ballotCount: item.ballotCount ?? 0,
        isTellerAccessOpen: item.isTellerAccessOpen ?? false,
        isOnlineVotingEnabled: item.isOnlineVotingEnabled ?? false,
        showAsTest: item.showAsTest ?? false,
        // Summary API exposes `toElect`; rest of the app uses `numberToElect`.
        numberToElect: item.toElect ?? undefined,
      })) || []
    );
  },

  async getStats(electionGuid: string): Promise<ElectionStats> {
    const response = await getApiElectionsByGuidStats({
      path: { guid: electionGuid },
    });
    const data = response.data?.data;
    if (!data) {
      throw new Error("Election stats not found");
    }
    return {
      voterCount: data.voterCount ?? 0,
      ballotCount: data.ballotCount ?? 0,
      locationCount: data.locationCount ?? 0,
    };
  },

  async getStatus(electionGuid: string): Promise<ElectionStatus> {
    const response = await getApiElectionsByGuidStatus({
      path: { guid: electionGuid },
    });
    const data = response.data?.data;
    if (!data) {
      throw new Error("Election status not found");
    }
    return {
      electionGuid: data.electionGuid || electionGuid,
      name: data.name || "",
      dateOfElection: convertDateToString(data.dateOfElection),
      electionType: data.electionType,
      electionStage: data.electionStage as ElectionStage | undefined,
      isActive: data.isActive ?? false,
      registeredVoters: data.registeredVoters ?? 0,
      ballotsSubmitted: data.ballotsSubmitted ?? 0,
    };
  },

  async getById(electionGuid: string): Promise<ElectionDto> {
    const response = await getApiElectionsByGuidElection({
      path: { guid: electionGuid },
    });
    const data = response.data?.data;
    if (!data) {
      throw new Error("Election not found");
    }
    return mapElectionDto(data);
  },

  async duplicate(
    electionGuid: string,
    dto: DuplicateElectionDto = {},
  ): Promise<ElectionDto> {
    const response = await postApiElectionsByGuidDuplicateElection({
      path: { guid: electionGuid },
      body: { name: dto.name },
    });
    const data = response.data?.data;
    if (!data) {
      throw new Error(response.data?.message || "Failed to duplicate election");
    }
    return mapElectionDto(data);
  },

  async reset(electionGuid: string): Promise<ElectionDto> {
    const response = await postApiElectionsByGuidResetElection({
      path: { guid: electionGuid },
    });
    const data = response.data?.data;
    if (!data) {
      throw new Error(response.data?.message || "Failed to reset election");
    }
    return mapElectionDto(data);
  },

  async create(dto: CreateElectionDto): Promise<ElectionDto> {
    const response = await postApiElectionsCreateElection({
      body: {
        ...dto,
        electionType: dto.electionType as any,
        electionMode: dto.electionMode as any,
        dateOfElection: convertStringToDate(dto.dateOfElection),
      },
    });
    return response.data?.data as unknown as ElectionDto;
  },

  async update(
    electionGuid: string,
    dto: UpdateElectionDto,
  ): Promise<ElectionDto> {
    const response = await putApiElectionsByGuidUpdateElection({
      path: { guid: electionGuid },
      body: {
        ...dto,
        electionType: dto.electionType as any,
        electionMode: dto.electionMode as any,
        dateOfElection: convertStringToDate(dto.dateOfElection),
        onlineWhenOpen: convertStringToDate(dto.onlineWhenOpen),
        onlineWhenClose: convertStringToDate(dto.onlineWhenClose),
      },
    });
    return response.data?.data as unknown as ElectionDto;
  },

  async updateOnlineVotingWindow(
    electionGuid: string,
    options: {
      onlineWhenOpen?: string | null;
      onlineWhenClose?: string | null;
      onlineCloseIsEstimate: boolean;
    },
  ): Promise<ElectionDto> {
    const response = await client.put({
      url: "/api/Elections/{guid}/online-voting-window",
      path: { guid: electionGuid },
      // Send null dates explicitly so the server can clear a bound.
      body: {
        onlineWhenOpen: options.onlineWhenOpen
          ? new Date(options.onlineWhenOpen)
          : null,
        onlineWhenClose: options.onlineWhenClose
          ? new Date(options.onlineWhenClose)
          : null,
        onlineCloseIsEstimate: options.onlineCloseIsEstimate,
      },
      headers: {
        "Content-Type": "application/json",
      },
    });
    const data = (response.data as any)?.data;
    if (!data) {
      throw new Error(
        (response.data as any)?.message ||
          "Failed to update online voting window",
      );
    }
    return mapElectionDto(data);
  },

  async getAcceptAllOnlineBallotsSummary(
    electionGuid: string,
  ): Promise<OnlineVotingAcceptAllOnlineBallotsSummaryDto> {
    const response = await getApiElectionsByGuidOnlineBallotsAcceptAllSummary({
      path: { guid: electionGuid },
    });
    return (response.data ?? {
      pendingCount: 0,
      processedCount: 0,
    }) as OnlineVotingAcceptAllOnlineBallotsSummaryDto;
  },

  async acceptAllOnlineBallots(
    electionGuid: string,
  ): Promise<OnlineVotingAcceptAllOnlineBallotsResultDto> {
    const response = await postApiElectionsByGuidOnlineBallotsAcceptAll({
      path: { guid: electionGuid },
    });
    return (response.data ?? {}) as OnlineVotingAcceptAllOnlineBallotsResultDto;
  },

  async delete(electionGuid: string): Promise<void> {
    await deleteApiElectionsByGuidDeleteElection({
      path: { guid: electionGuid },
    });
  },

  async exportElectionToJson(electionGuid: string): Promise<Blob> {
    const response = await getApiImportExportElectionToJsonByElectionGuid({
      path: { electionGuid },
      parseAs: "blob",
    });

    return response.data as Blob;
  },

  async importElectionFromFile(file: File): Promise<ElectionDto> {
    const response = await postApiImportImportElectionFromJson({
      body: { file },
    });

    return (response.data as any).election;
  },

  async importTallyJv3ElectionFromFile(file: File): Promise<ElectionDto> {
    const response = await postApiImportImportTallyJv3Election({
      body: { file },
    });

    return (response.data as any).election;
  },

  async importCdnBallots(
    electionGuid: string,
    file: File,
  ): Promise<ImportResultDto> {
    const response = await postApiImportImportCdnBallotsByElectionGuid({
      path: { electionGuid },
      body: { file },
    });

    return response.data as ImportResultDto;
  },

  async changeStage(
    electionGuid: string,
    stage: ElectionStage,
  ): Promise<ElectionDto> {
    const response = await putApiElectionsByGuidStage({
      path: { guid: electionGuid },
      body: { electionStage: stage },
    });
    const envelope = response.data;
    const data = envelope?.data;
    if (!envelope?.success || !data) {
      throw new Error(envelope?.message || "Failed to change election stage");
    }
    return mapElectionDto(data);
  },

  async toggleTellerAccess(
    electionGuid: string,
    isOpen: boolean,
  ): Promise<ElectionDto> {
    const response = await putApiElectionsByGuidTellerAccess({
      path: { guid: electionGuid },
      body: { isOpen },
    });
    const data = response.data?.data;
    if (!data) {
      throw new Error("Failed to toggle teller access");
    }
    return mapElectionDto(data);
  },

  // async getCurrentElection(): Promise<ElectionDto | null> {
  //   try {
  //     const response = await getApiElectionsByGuidElection();
  //     return response.data.items?.[0] || null;
  //   } catch {
  //     return null;
  //   }
  // },
};
