import {
  deleteApiByElectionGuidTellersByRowIdDeleteTeller,
  getApiByElectionGuidTellers,
  getApiByElectionGuidTellersByRowIdGetTeller,
  postApiByElectionGuidTellersCreateTeller,
  putApiByElectionGuidTellersByRowIdUpdateTeller,
  putApiElectionsByGuidTellerAccess,
} from "@/api/gen/configService";
import type { CreateTellerDto, Teller, UpdateTellerDto } from "@/types/teller";

export interface TellerListResult {
  items: Teller[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
}

export const tellerService = {
  async getTellersByElection(
    electionGuid: string,
    pageNumber = 1,
    pageSize = 50,
  ): Promise<TellerListResult> {
    const response = await getApiByElectionGuidTellers({
      path: { electionGuid },
      query: { pageNumber, pageSize },
    });

    return {
      items: (response.data?.items ?? []) as Teller[],
      totalCount: response.data?.totalCount ?? 0,
      pageNumber: response.data?.pageNumber ?? pageNumber,
      pageSize: response.data?.pageSize ?? pageSize,
    };
  },

  async getTellerById(electionGuid: string, rowId: number): Promise<Teller> {
    const response = await getApiByElectionGuidTellersByRowIdGetTeller({
      path: { electionGuid, rowId },
    });
    return response.data?.data as Teller;
  },

  async createTeller(
    electionGuid: string,
    teller: CreateTellerDto,
  ): Promise<Teller> {
    const response = await postApiByElectionGuidTellersCreateTeller({
      path: { electionGuid },
      body: teller,
    });
    return response.data?.data as Teller;
  },

  async updateTeller(
    electionGuid: string,
    rowId: number,
    teller: UpdateTellerDto,
  ): Promise<Teller> {
    const response = await putApiByElectionGuidTellersByRowIdUpdateTeller({
      path: { electionGuid, rowId },
      body: teller,
    });
    return response.data?.data as Teller;
  },

  async deleteTeller(electionGuid: string, rowId: number): Promise<boolean> {
    const response = await deleteApiByElectionGuidTellersByRowIdDeleteTeller({
      path: { electionGuid, rowId },
    });
    return response.data?.data ?? false;
  },

  async toggleTellerAccess(electionGuid: string, isOpen: boolean) {
    const response = await putApiElectionsByGuidTellerAccess({
      path: { guid: electionGuid },
      body: { isOpen },
    });
    return response.data?.data;
  },
};
