export interface AuditLog {
  id: number;
  timestamp: string;
  eventType: number | string;
  userId?: string | null;
  onlineVoterId?: string | null;
  electionGuid?: string | null;
  email?: string | null;
  ipAddress?: string | null;
  userAgent?: string | null;
  details?: string | null;
  isSuspicious: boolean;
  severity: number | string;
  metadata?: Record<string, string> | null;
}

export interface AuditLogFilter {
  eventType?: number | string;
  userId?: string;
  onlineVoterId?: string;
  electionGuid?: string;
  email?: string;
  ipAddress?: string;
  isSuspicious?: boolean;
  severity?: number | string;
  startDate?: string;
  endDate?: string;
  searchTerm?: string;
}
