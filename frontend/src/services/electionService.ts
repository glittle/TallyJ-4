import {
  deleteApiElectionsByGuidDeleteElection,
  getApiElectionsByGuidElection,
  getApiElectionsByGuidElectionSummary,
  postApiElectionsCreateElection,
  putApiElectionsByGuidUpdateElection,
} from "./../api/gen/configService/sdk.gen";
import { getApiElectionsGetElections } from "../api/gen/configService/sdk.gen";
import { cacheService } from "./cacheService";
import type {
  ElectionDto,
  CreateElectionDto,
  UpdateElectionDto,
  ElectionSummaryDto,
} from "../types";

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
        dateOfElection: convertDateToString(item.dateOfElection),
        tallyStatus: item.tallyStatus ?? undefined,
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
    return response.data?.data as ElectionDto;
  },

  async getSummaries(): Promise<ElectionSummaryDto[]> {
    const response = await getApiElectionsByGuidElectionSummary({
      path: { guid: "" },
    });
    return (response.data?.data?.items ?? []) as ElectionSummaryDto[];
  },

  async create(dto: CreateElectionDto): Promise<ElectionDto> {
    const response = await postApiElectionsCreateElection({
      body: {
        ...dto,
        dateOfElection: convertStringToDate(dto.dateOfElection),
        onlineAnnounced: convertStringToDate(dto.onlineAnnounced),
      },
    });
    await cacheService.remove(
      cacheService.generateKey({ url: "/api/elections", method: "GET" }),
    );
    await cacheService.remove(
      cacheService.generateKey({
        url: "/api/elections/summaries",
        method: "GET",
      }),
    );
    return response.data?.data as ElectionDto;
  },

  async update(
    electionGuid: string,
    dto: UpdateElectionDto,
  ): Promise<ElectionDto> {
    const response = await putApiElectionsByGuidUpdateElection({
      path: { guid: electionGuid },
      body: {
        ...dto,
        dateOfElection: convertStringToDate(dto.dateOfElection),
        onlineWhenOpen: convertStringToDate(dto.onlineWhenOpen),
        onlineWhenClose: convertStringToDate(dto.onlineWhenClose),
        onlineAnnounced: convertStringToDate(dto.onlineAnnounced),
      },
    });
    await cacheService.remove(
      cacheService.generateKey({
        url: `/api/elections/${electionGuid}`,
        method: "GET",
      }),
    );
    await cacheService.remove(
      cacheService.generateKey({ url: "/api/elections", method: "GET" }),
    );
    await cacheService.remove(
      cacheService.generateKey({
        url: "/api/elections/summaries",
        method: "GET",
      }),
    );
    return response.data?.data as ElectionDto;
  },

  async delete(electionGuid: string): Promise<void> {
    await deleteApiElectionsByGuidDeleteElection({
      path: { guid: electionGuid },
    });
    await cacheService.remove(
      cacheService.generateKey({ url: "/api/elections", method: "GET" }),
    );
    await cacheService.remove(
      cacheService.generateKey({
        url: "/api/elections/summaries",
        method: "GET",
      }),
    );
  },

  async exportElectionToJson(electionGuid: string): Promise<Blob> {
    const response = await fetch(`/api/import/exportElectionToJson/${electionGuid}`, {
      method: 'GET',
      headers: {
        'Authorization': `Bearer ${secureTokenService.getAccessToken()}`,
      },
    });

    if (!response.ok) {
      throw new Error('Export failed');
    }

    return await response.blob();
  },

  async importElectionFromFile(file: File): Promise<ElectionDto> {
    const formData = new FormData();
    formData.append('file', file);

    const response = await fetch('/api/import/importElectionFromJson', {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${secureTokenService.getAccessToken()}`,
      },
      body: formData,
    });

    if (!response.ok) {
      throw new Error('Import failed');
    }

    const result = await response.json();
    return result.election;
  },

  async importTallyJv2ElectionFromFile(file: File): Promise<ElectionDto> {
    const formData = new FormData();
    formData.append('file', file);

    const response = await fetch('/api/import/importTallyJv2Election', {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${secureTokenService.getAccessToken()}`,
      },
      body: formData,
    });

    if (!response.ok) {
      throw new Error('Import failed');
    }

    const result = await response.json();
    return result.election;
  },

  async importCdnBallots(electionGuid: string, file: File): Promise<ImportResultDto> {
    const formData = new FormData();
    formData.append('file', file);

    const response = await fetch(`/api/import/importCdnBallots/${electionGuid}`, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${secureTokenService.getAccessToken()}`,
      },
      body: formData,
    });

    if (!response.ok) {
      throw new Error('Import failed');
    }

    return await response.json();
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
