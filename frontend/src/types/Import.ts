export interface FieldMapping {
  sourceColumn: string;
  targetField: string;
}

export interface ImportConfiguration {
  firstDataRow: number;
  hasHeaderRow: boolean;
  delimiter: string;
  fieldMappings: FieldMapping[];
  skipInvalidRows: boolean;
}

export interface ImportBallotRequest {
  csvContent: string;
  electionGuid: string;
  locationGuid?: string;
  configuration: ImportConfiguration;
}

export interface ImportResult {
  success: boolean;
  totalRows: number;
  ballotsCreated: number;
  votesCreated: number;
  skippedRows: number;
  errors: string[];
  warnings: string[];
}

export interface ParseCsvHeadersRequest {
  csvContent: string;
  delimiter?: string;
}

export interface ParseCsvHeadersResponse {
  headers: string[];
  previewRows: string[][];
  totalRows: number;
}

export const IMPORT_TARGET_FIELDS = [
  { value: "BallotCode", label: "Ballot Code" },
  { value: "Votes", label: "Votes (separated by |)" },
  { value: "Teller1", label: "Teller 1" },
  { value: "Teller2", label: "Teller 2" },
] as const;
