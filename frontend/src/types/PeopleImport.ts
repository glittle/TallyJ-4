export interface ImportFileInfo {
  rowId: number;
  electionGuid: string;
  uploadTime: string | null;
  importTime: string | null;
  fileSize: number | null;
  hasContent: boolean | null;
  firstDataRow: number | null;
  columnsToRead: string | null;
  originalFileName: string | null;
  processingStatus: string | null;
  fileType: string | null;
  codePage: number | null;
  messages: string | null;
}

export interface ParseFileResult {
  headers: string[];
  previewRows: string[][];
  totalDataRows: number;
  autoMappings: ColumnMapping[];
}

export interface ColumnMapping {
  fileColumn: string;
  targetField: string | null;
}

export interface UpdateFileSettingsDto {
  firstDataRow?: number;
  codePage?: number;
}

export interface ImportErrorDto {
  key: string;
  parameters: Record<string, string>;
}

export interface ImportWarningDto {
  key: string;
  parameters: Record<string, string>;
}

export interface ImportPeopleResult {
  success: boolean;
  peopleAdded: number;
  peopleSkipped: number;
  totalRows: number;
  warnings: ImportWarningDto[];
  errors: ImportErrorDto[];
  timeElapsedSeconds: number;
}

export interface DeleteAllPeopleResult {
  deletedCount: number;
}

export const PEOPLE_TARGET_FIELDS = [
  { value: "FirstName", label: "First Name", required: true },
  { value: "LastName", label: "Last Name", required: true },
  { value: "BahaiId", label: "Baha'i ID", required: false },
  {
    value: "IneligibleReasonDescription",
    label: "Eligibility Status",
    required: false,
  },
  { value: "Area", label: "Area", required: false },
  { value: "Email", label: "Email", required: false },
  { value: "Phone", label: "Phone", required: false },
  { value: "OtherNames", label: "Other Names", required: false },
  { value: "OtherLastNames", label: "Other Last Names", required: false },
  { value: "OtherInfo", label: "Other Info", required: false },
] as const;
