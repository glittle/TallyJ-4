import { client } from "../api/config";
import type { PublicDisplayDto } from "../types";

export const publicService = {
  async getPublicDisplay(electionGuid: string): Promise<PublicDisplayDto> {
    const response = await client.get<PublicDisplayDto>({
      url: `/public/elections/${electionGuid}/display`,
    });
    return response.data;
  },
};
