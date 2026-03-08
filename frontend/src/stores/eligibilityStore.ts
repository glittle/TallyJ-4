import { defineStore } from "pinia";
import { ref, computed } from "vue";
import { eligibilityService } from "../services/eligibilityService";
import type { EligibilityReasonDto } from "../types";

export const useEligibilityStore = defineStore("eligibility", () => {
  const reasons = ref<EligibilityReasonDto[]>([]);
  const loaded = ref(false);

  const personReasons = computed(() =>
    reasons.value.filter((reason) => !reason.internalOnly),
  );

  const groupedReasons = computed(() => {
    const groups: Record<string, EligibilityReasonDto[]> = {
      X: [],
      V: [],
      R: [],
    };

    personReasons.value.forEach((reason) => {
      const prefix = reason.code.charAt(0);
      if (groups[prefix]) {
        groups[prefix].push(reason);
      }
    });

    return groups;
  });

  const getByGuid = computed(
    () => (guid: string) =>
      reasons.value.find((reason) => reason.reasonGuid === guid),
  );

  const getByCode = computed(
    () => (code: string) =>
      reasons.value.find((reason) => reason.code === code),
  );

  async function fetchReasons() {
    if (loaded.value) return;

    try {
      reasons.value = await eligibilityService.getAll();
      loaded.value = true;
    } catch (error) {
      console.error("Failed to fetch eligibility reasons:", error);
      throw error;
    }
  }

  return {
    reasons,
    loaded,
    personReasons,
    groupedReasons,
    getByGuid,
    getByCode,
    fetchReasons,
  };
});
