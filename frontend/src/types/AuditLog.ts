export interface AuditLog {
  rowId: number
  asOf: string
  electionGuid?: string
  locationGuid?: string
  voterId?: string
  computerCode?: string
  details?: string
  hostAndVersion?: string
}

export interface AuditLogFilter {
  electionGuid?: string
  locationGuid?: string
  voterId?: string
  computerCode?: string
  startDate?: string
  endDate?: string
  searchTerm?: string
}

export interface CreateAuditLogDto {
  electionGuid?: string
  locationGuid?: string
  voterId?: string
  computerCode?: string
  details?: string
  hostAndVersion?: string
}
