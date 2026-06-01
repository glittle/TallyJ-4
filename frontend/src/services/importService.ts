import { client } from "../api/config";
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
    const response = await client.post<ParseCsvHeadersResponse>({
      url: "/api/Import/parse-csv-headers",
      body: request,
    });
    return response.data;
  },

  async importBallots(request: ImportBallotRequest): Promise<ImportResult> {
    const response = await client.post<ImportResult>({
      url: "/api/Import/importBallots",
      body: request,
    });
    return response.data;
  },
};
