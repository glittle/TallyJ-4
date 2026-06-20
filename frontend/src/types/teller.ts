export interface Teller {
  rowId: number;
  electionGuid: string;
  name: string;
}

export interface CreateTellerDto {
  electionGuid: string;
  name: string;
}

export interface UpdateTellerDto {
  name: string;
}
