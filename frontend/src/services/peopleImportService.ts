import { client } from "../api/config";
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
    const formData = new FormData();
    formData.append("file", file);

    const response = await client.post<ImportFileInfo>({
      url: `/api/PeopleImport/${electionGuid}/upload`,
      body: formData,
    });
    return response.data;
  },

  async getFiles(electionGuid: string): Promise<ImportFileInfo[]> {
    const response = await client.get<ImportFileInfo[]>({
      url: `/api/PeopleImport/${electionGuid}/files`,
    });
    return response.data;
  },

  async parseFile(
    electionGuid: string,
    rowId: number,
    codePage?: number,
    firstDataRow?: number,
  ): Promise<ParseFileResult> {
    const query: Record<string, unknown> = {};
    if (codePage !== undefined) {
      query.codePage = codePage;
    }
    if (firstDataRow !== undefined) {
      query.firstDataRow = firstDataRow;
    }

    const response = await client.get<ParseFileResult>({
      url: `/api/PeopleImport/${electionGuid}/files/${rowId}/parse`,
      query,
    });
    return response.data;
  },

  async saveMapping(
    electionGuid: string,
    rowId: number,
    mappings: ColumnMapping[],
  ): Promise<ImportFileInfo> {
    const response = await client.put<ImportFileInfo>({
      url: `/api/PeopleImport/${electionGuid}/files/${rowId}/mapping`,
      body: mappings,
    });
    return response.data;
  },

  async getMapping(
    electionGuid: string,
    rowId: number,
  ): Promise<ColumnMapping[] | null> {
    const response = await client.get<ColumnMapping[]>({
      url: `/api/PeopleImport/${electionGuid}/files/${rowId}/mapping`,
    });
    return response.data;
  },

  async updateSettings(
    electionGuid: string,
    rowId: number,
    settings: UpdateFileSettingsDto,
  ): Promise<ImportFileInfo> {
    const response = await client.put<ImportFileInfo>({
      url: `/api/PeopleImport/${electionGuid}/files/${rowId}/settings`,
      body: settings,
    });
    return response.data;
  },

  async executeImport(
    electionGuid: string,
    rowId: number,
  ): Promise<ImportPeopleResult> {
    const response = await client.post<ImportPeopleResult>({
      url: `/api/PeopleImport/${electionGuid}/files/${rowId}/import`,
    });
    return response.data;
  },

  async deleteFile(electionGuid: string, rowId: number): Promise<void> {
    await client.delete({
      url: `/api/PeopleImport/${electionGuid}/files/${rowId}`,
    });
  },

  async deleteAllPeople(electionGuid: string): Promise<DeleteAllPeopleResult> {
    const response = await client.delete<DeleteAllPeopleResult>({
      url: `/api/PeopleImport/${electionGuid}/people`,
    });
    return response.data;
  },

  async getPeopleCount(electionGuid: string): Promise<{ count: number }> {
    const response = await client.get<{ count: number }>({
      url: `/api/PeopleImport/${electionGuid}/people-count`,
    });
    return response.data;
  },
};
