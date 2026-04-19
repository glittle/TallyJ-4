import { getApiEligibilityEligibilityReasons } from "../api/gen/configService/sdk.gen";
import type { EligibilityReasonDto } from "../types";

export const eligibilityService = {
  async getAll(): Promise<EligibilityReasonDto[]> {
    const response = await getApiEligibilityEligibilityReasons({
      throwOnError: true,
    });
    return (
      (response.data as unknown as { data: EligibilityReasonDto[] }).data ?? []
    );
  },
};
