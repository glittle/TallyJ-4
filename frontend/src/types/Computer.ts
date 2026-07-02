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

export interface ActiveComputerDto {
  computerCode: string;
  clientId: string;
  isMainTeller: boolean;
  connectedAt: string;
}
