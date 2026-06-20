import {
  postApiPeopleImportByElectionGuidUpload,
  getApiPeopleImportByElectionGuidFiles,
  getApiPeopleImportByElectionGuidFilesByRowIdParse,
  putApiPeopleImportByElectionGuidFilesByRowIdMapping,
  getApiPeopleImportByElectionGuidFilesByRowIdMapping,
  putApiPeopleImportByElectionGuidFilesByRowIdSettings,
  postApiPeopleImportByElectionGuidFilesByRowIdImport,
  deleteApiPeopleImportByElectionGuidFilesByRowId,
  deleteApiPeopleImportByElectionGuidPeople,
  getApiPeopleImportByElectionGuidPeopleCount,
} from "@/api/gen/configService";
import type {
  ImportFileInfo,
  ParseFileResult,
  ColumnMapping,
  UpdateFileSettingsDto,
  ImportPeopleResult,
  DeleteAllPeopleResult,
} from "../types";

export const peopleImportService = {
  async uploadFile(electionGuid: string, file: File): Promise<ImportFileInfo> {
    const response = await postApiPeopleImportByElectionGuidUpload({
      path: { electionGuid },
      body: { file },
    });
    return response.data as ImportFileInfo;
  },

  async getFiles(electionGuid: string): Promise<ImportFileInfo[]> {
    const response = await getApiPeopleImportByElectionGuidFiles({
      path: { electionGuid },
    });
    return (response.data ?? []) as ImportFileInfo[];
  },

  async parseFile(
    electionGuid: string,
    rowId: number,
    codePage?: number,
    firstDataRow?: number,
  ): Promise<ParseFileResult> {
    const response = await getApiPeopleImportByElectionGuidFilesByRowIdParse({
      path: { electionGuid, rowId },
      query: {
        codePage,
        firstDataRow,
      },
    });
    return response.data as ParseFileResult;
  },

  async saveMapping(
    electionGuid: string,
    rowId: number,
    mappings: ColumnMapping[],
  ): Promise<ImportFileInfo> {
    const response = await putApiPeopleImportByElectionGuidFilesByRowIdMapping({
      path: { electionGuid, rowId },
      body: mappings,
    });
    return response.data as ImportFileInfo;
  },

  async getMapping(
    electionGuid: string,
    rowId: number,
  ): Promise<ColumnMapping[] | null> {
    const response = await getApiPeopleImportByElectionGuidFilesByRowIdMapping({
      path: { electionGuid, rowId },
    });
    return (response.data ?? null) as ColumnMapping[] | null;
  },

  async updateSettings(
    electionGuid: string,
    rowId: number,
    settings: UpdateFileSettingsDto,
  ): Promise<ImportFileInfo> {
    const response = await putApiPeopleImportByElectionGuidFilesByRowIdSettings(
      {
        path: { electionGuid, rowId },
        body: settings,
      },
    );
    return response.data as ImportFileInfo;
  },

  async executeImport(
    electionGuid: string,
    rowId: number,
  ): Promise<ImportPeopleResult> {
    const response = await postApiPeopleImportByElectionGuidFilesByRowIdImport({
      path: { electionGuid, rowId },
    });
    return response.data as ImportPeopleResult;
  },

  async deleteFile(electionGuid: string, rowId: number): Promise<void> {
    await deleteApiPeopleImportByElectionGuidFilesByRowId({
      path: { electionGuid, rowId },
    });
  },

  async deleteAllPeople(electionGuid: string): Promise<DeleteAllPeopleResult> {
    const response = await deleteApiPeopleImportByElectionGuidPeople({
      path: { electionGuid },
    });
    return response.data as DeleteAllPeopleResult;
  },

  async getPeopleCount(electionGuid: string): Promise<{ count: number }> {
    const response = await getApiPeopleImportByElectionGuidPeopleCount({
      path: { electionGuid },
    });
    return response.data as { count: number };
  },
};
