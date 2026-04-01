import api from "./api";
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

    const response = await api.post<ImportFileInfo>(
      `/api/PeopleImport/${electionGuid}/upload`,
      formData,
      {
        headers: {
          "Content-Type": "multipart/form-data",
        },
      },
    );
    return response.data;
  },

  async getFiles(electionGuid: string): Promise<ImportFileInfo[]> {
    const response = await api.get<ImportFileInfo[]>(
      `/api/PeopleImport/${electionGuid}/files`,
    );
    return response.data;
  },

  async parseFile(
    electionGuid: string,
    rowId: number,
    codePage?: number,
    firstDataRow?: number,
  ): Promise<ParseFileResult> {
    const params = new URLSearchParams();
    if (codePage !== undefined) {
      params.append("codePage", codePage.toString());
    }
    if (firstDataRow !== undefined) {
      params.append("firstDataRow", firstDataRow.toString());
    }

    const queryString = params.toString();
    const url = `/api/PeopleImport/${electionGuid}/files/${rowId}/parse${queryString ? `?${queryString}` : ""}`;

    const response = await api.get<ParseFileResult>(url);
    return response.data;
  },

  async saveMapping(
    electionGuid: string,
    rowId: number,
    mappings: ColumnMapping[],
  ): Promise<ImportFileInfo> {
    const response = await api.put<ImportFileInfo>(
      `/api/PeopleImport/${electionGuid}/files/${rowId}/mapping`,
      mappings,
    );
    return response.data;
  },

  async getMapping(
    electionGuid: string,
    rowId: number,
  ): Promise<ColumnMapping[] | null> {
    const response = await api.get<ColumnMapping[]>(
      `/api/PeopleImport/${electionGuid}/files/${rowId}/mapping`,
    );
    return response.data;
  },

  async updateSettings(
    electionGuid: string,
    rowId: number,
    settings: UpdateFileSettingsDto,
  ): Promise<ImportFileInfo> {
    const response = await api.put<ImportFileInfo>(
      `/api/PeopleImport/${electionGuid}/files/${rowId}/settings`,
      settings,
    );
    return response.data;
  },

  async executeImport(
    electionGuid: string,
    rowId: number,
  ): Promise<ImportPeopleResult> {
    const response = await api.post<ImportPeopleResult>(
      `/api/PeopleImport/${electionGuid}/files/${rowId}/import`,
    );
    return response.data;
  },

  async deleteFile(electionGuid: string, rowId: number): Promise<void> {
    await api.delete(`/api/PeopleImport/${electionGuid}/files/${rowId}`);
  },

  async deleteAllPeople(electionGuid: string): Promise<DeleteAllPeopleResult> {
    const response = await api.delete<DeleteAllPeopleResult>(
      `/api/PeopleImport/${electionGuid}/people`,
    );
    return response.data;
  },

  async getPeopleCount(electionGuid: string): Promise<{ count: number }> {
    const response = await api.get<{ count: number }>(
      `/api/PeopleImport/${electionGuid}/people-count`,
    );
    return response.data;
  },
};
