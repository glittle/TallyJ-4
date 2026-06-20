import { getApiPublicByElectionGuidPublicDisplay } from "@/api/gen/configService";
import type { PublicDisplayDto } from "../types";

export const publicService = {
  async getPublicDisplay(electionGuid: string): Promise<PublicDisplayDto> {
    const response = await getApiPublicByElectionGuidPublicDisplay({
      path: { electionGuid },
    });
    return response.data?.data as PublicDisplayDto;
  },
};
