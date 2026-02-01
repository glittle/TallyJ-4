export interface ComputerDto {
  computerGuid: string;
  electionGuid: string;
  locationGuid: string;
  computerCode: string;
  browserInfo?: string;
  ipAddress?: string;
  lastActivity?: string;
  registeredAt?: string;
  isActive?: boolean;
}

export interface RegisterComputerDto {
  electionGuid: string;
  locationGuid: string;
  computerCode?: string;
  browserInfo?: string;
  ipAddress?: string;
}

export interface UpdateComputerDto {
  browserInfo?: string;
  ipAddress?: string;
  isActive?: boolean;
}
