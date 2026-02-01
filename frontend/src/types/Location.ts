export interface LocationDto {
  locationGuid: string;
  electionGuid: string;
  name: string;
  contactInfo?: string;
  longitude?: string;
  latitude?: string;
  tallyStatus?: string;
  sortOrder?: number;
  ballotsCollected?: number;
}

export interface CreateLocationDto {
  electionGuid: string;
  name: string;
  contactInfo?: string;
  longitude?: string;
  latitude?: string;
  sortOrder?: number;
}

export interface UpdateLocationDto {
  name?: string;
  contactInfo?: string;
  longitude?: string;
  latitude?: string;
  sortOrder?: number;
}
