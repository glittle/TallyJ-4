export interface Teller {
  rowId: number;
  electionGuid: string;
  name: string;
  usingComputerCode?: string;
  isHeadTeller: boolean;
}

export interface CreateTellerDto {
  electionGuid: string;
  name: string;
  usingComputerCode?: string;
  isHeadTeller: boolean;
}

export interface UpdateTellerDto {
  name: string;
  usingComputerCode?: string;
  isHeadTeller: boolean;
}
