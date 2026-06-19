import {
  postApiImportParseCsvHeaders,
  postApiImportImportBallots,
} from "@/api/gen/configService";
import type {
  ParseCsvHeadersRequest,
  ParseCsvHeadersResponse,
  ImportBallotRequest,
  ImportResult,
} from "../types";

export const importService = {
  async parseCsvHeaders(
    request: ParseCsvHeadersRequest,
  ): Promise<ParseCsvHeadersResponse> {
    const response = await postApiImportParseCsvHeaders({ body: request });
    return response.data as ParseCsvHeadersResponse;
  },

  async importBallots(request: ImportBallotRequest): Promise<ImportResult> {
    const response = await postApiImportImportBallots({ body: request });
    return response.data as ImportResult;
  },
};
