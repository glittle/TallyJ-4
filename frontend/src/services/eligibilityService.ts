import api from "./api";
import type { EligibilityReasonDto } from "../types";

export const eligibilityService = {
  async getAll(): Promise<EligibilityReasonDto[]> {
    const response = await api.get("/api/eligibility/eligibility-reasons");
    return response.data?.data as EligibilityReasonDto[];
  },
};
