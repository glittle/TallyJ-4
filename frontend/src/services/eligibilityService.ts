import { client } from "../api/config";
import type { EligibilityReasonDto } from "../types";

export const eligibilityService = {
  async getAll(): Promise<EligibilityReasonDto[]> {
    const response = await client.get<{ data: EligibilityReasonDto[] }>({
      url: "/api/eligibility/eligibility-reasons",
    });
    return response.data?.data ?? [];
  },
};
