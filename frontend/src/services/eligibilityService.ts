import { getApiEligibilityEligibilityReasons } from "@/api/gen/configService";
import type { EligibilityReasonDto } from "../types";

export const eligibilityService = {
  async getAll(): Promise<EligibilityReasonDto[]> {
    const response = await getApiEligibilityEligibilityReasons();
    return response.data?.data ?? [];
  },
};
