import api from './api';
import type { 
  ParseCsvHeadersRequest, 
  ParseCsvHeadersResponse, 
  ImportBallotRequest, 
  ImportResult 
} from '../types';

export const importService = {
  async parseCsvHeaders(request: ParseCsvHeadersRequest): Promise<ParseCsvHeadersResponse> {
    const response = await api.post<ParseCsvHeadersResponse>('/import/parse-csv-headers', request);
    return response.data;
  },

  async importBallots(request: ImportBallotRequest): Promise<ImportResult> {
    const response = await api.post<ImportResult>('/import/ballots', request);
    return response.data;
  }
};
