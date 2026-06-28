import { getApiElectionsGetElections } from "../api/gen/configService/sdk.gen";
import type {
  CreateElectionDto,
  ElectionDto,
  ElectionSummaryDto,
  ImportResultDto,
  UpdateElectionDto,
} from "../types";
import type { ElectionStage } from "../domain/electionStages";
import {
  deleteApiElectionsByGuidDeleteElection,
  getApiElectionsByGuidElection,
  getApiImportExportElectionToJsonByElectionGuid,
  postApiElectionsCreateElection,
  postApiImportImportCdnBallotsByElectionGuid,
  postApiImportImportElectionFromJson,
  postApiImportImportTallyJv3Election,
  putApiElectionsByGuidStage,
  putApiElectionsByGuidTellerAccess,
  putApiElectionsByGuidUpdateElection,
} from "./../api/gen/configService/sdk.gen";
const convertStringToDate = (dateString?: string): Date | null => {
  return dateString ? new Date(dateString) : null;
};

const convertDateToString = (date?: Date | null): string | undefined => {
  return date ? date.toISOString() : undefined;
};

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
      })) || []
    );
  },

  async getById(electionGuid: string): Promise<ElectionDto> {
    const response = await getApiElectionsByGuidElection({
      path: { guid: electionGuid },
    });
    const data = response.data?.data;
    if (!data) {
      throw new Error("Election not found");
    }
    return {
      ...(data as any),
      dateOfElection: convertDateToString(data.dateOfElection),
      onlineWhenOpen: convertDateToString(data.onlineWhenOpen),
      onlineWhenClose: convertDateToString(data.onlineWhenClose),
      onlineAnnounced: convertDateToString(data.onlineAnnounced),
      tellerAccessOpenedAt: convertDateToString(data.tellerAccessOpenedAt),
    } as ElectionDto;
  },

  async create(dto: CreateElectionDto): Promise<ElectionDto> {
    const response = await postApiElectionsCreateElection({
      body: {
        ...dto,
        electionType: dto.electionType as any,
        electionMode: dto.electionMode as any,
        dateOfElection: convertStringToDate(dto.dateOfElection),
        onlineAnnounced: convertStringToDate(dto.onlineAnnounced),
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
        onlineAnnounced: convertStringToDate(dto.onlineAnnounced),
      },
    });
    return response.data?.data as unknown as ElectionDto;
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
      throw {
        message: envelope?.message || "Failed to change election stage",
      };
    }
    return {
      ...(data as any),
      dateOfElection: convertDateToString(data.dateOfElection),
      onlineWhenOpen: convertDateToString(data.onlineWhenOpen),
      onlineWhenClose: convertDateToString(data.onlineWhenClose),
      onlineAnnounced: convertDateToString(data.onlineAnnounced),
      tellerAccessOpenedAt: convertDateToString(data.tellerAccessOpenedAt),
    } as ElectionDto;
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
    return {
      ...(data as any),
      dateOfElection: convertDateToString(data.dateOfElection),
      onlineWhenOpen: convertDateToString(data.onlineWhenOpen),
      onlineWhenClose: convertDateToString(data.onlineWhenClose),
      onlineAnnounced: convertDateToString(data.onlineAnnounced),
      tellerAccessOpenedAt: convertDateToString(data.tellerAccessOpenedAt),
    } as ElectionDto;
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
