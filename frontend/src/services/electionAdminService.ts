import {
  deleteApiElectionsByGuidAdminsByUserId,
  getApiElectionsByGuidAdmins,
  postApiElectionsByGuidAdmins,
} from "../api/gen/configService/sdk.gen";
import type { ElectionsElectionAdminDto } from "../api/gen/configService/types.gen";

export interface ElectionAdmin {
  userId: string;
  email: string;
  displayName: string | null;
  role: string;
  canRemove: boolean;
}

export const alreadyElectionAdminMessage = "elections.admins.alreadyAdmin";

function mapAdmin(dto: ElectionsElectionAdminDto): ElectionAdmin {
  return {
    userId: dto.userId ?? "",
    email: dto.email ?? "",
    displayName: dto.displayName ?? null,
    role: dto.role ?? "",
    canRemove: dto.canRemove === true,
  };
}

export const electionAdminService = {
  async list(electionGuid: string): Promise<ElectionAdmin[]> {
    const response = await getApiElectionsByGuidAdmins({
      path: { guid: electionGuid },
      throwOnError: true,
    });
    return (response.data?.data ?? []).map(mapAdmin);
  },

  async add(
    electionGuid: string,
    email: string,
  ): Promise<{ admin: ElectionAdmin; alreadyAdmin: boolean }> {
    const response = await postApiElectionsByGuidAdmins({
      path: { guid: electionGuid },
      body: { email: email.trim() },
      throwOnError: true,
    });
    const data = response.data?.data;
    if (!data?.userId) {
      throw new Error(response.data?.message || "elections.admins.loadError");
    }
    return {
      admin: mapAdmin(data),
      alreadyAdmin: response.data?.message === alreadyElectionAdminMessage,
    };
  },

  async remove(electionGuid: string, userId: string): Promise<void> {
    await deleteApiElectionsByGuidAdminsByUserId({
      path: { guid: electionGuid, userId },
      throwOnError: true,
    });
  },
};
