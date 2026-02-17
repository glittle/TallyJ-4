export interface ImportFileInfo {
  rowId: number
  electionGuid: string
  uploadTime: string | null
  importTime: string | null
  fileSize: number | null
  hasContent: boolean | null
  firstDataRow: number | null
  columnsToRead: string | null
  originalFileName: string | null
  processingStatus: string | null
  fileType: string | null
  codePage: number | null
  messages: string | null
}

export interface ParseFileResult {
  headers: string[]
  previewRows: string[][]
  totalDataRows: number
  autoMappings: ColumnMapping[]
}

export interface ColumnMapping {
  fileColumn: string
  targetField: string | null
}

export interface UpdateFileSettingsDto {
  firstDataRow?: number
  codePage?: number
}

export interface ImportPeopleResult {
  success: boolean
  peopleAdded: number
  peopleSkipped: number
  totalRows: number
  warnings: string[]
  errors: string[]
  timeElapsedSeconds: number
}

export interface DeleteAllPeopleResult {
  deletedCount: number
}

export const PEOPLE_TARGET_FIELDS = [
  { value: 'FirstName', label: 'First Name', required: true },
  { value: 'LastName', label: 'Last Name', required: true },
  { value: 'BahaiId', label: "Baha'i ID" },
  { value: 'IneligibleReasonDescription', label: 'Eligibility Status' },
  { value: 'Area', label: 'Area' },
  { value: 'Email', label: 'Email' },
  { value: 'Phone', label: 'Phone' },
  { value: 'OtherNames', label: 'Other Names' },
  { value: 'OtherLastNames', label: 'Other Last Names' },
  { value: 'OtherInfo', label: 'Other Info' },
] as const