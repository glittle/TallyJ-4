import { beforeEach, describe, expect, it, vi } from "vitest";
import { computed, ref } from "vue";
import type { FrontDeskVoterDto } from "@/types/FrontDesk";
import {
  hasDeskRegistration,
  useFrontDeskRegistration,
} from "../useFrontDeskRegistration";

const confirm = vi.fn();

vi.mock("element-plus", () => ({
  ElMessageBox: {
    confirm: (...args: unknown[]) => confirm(...args),
  },
}));

function voter(overrides: Partial<FrontDeskVoterDto> = {}): FrontDeskVoterDto {
  return {
    personGuid: "p-1",
    fullName: "Lee, Pat",
    isCheckedIn: false,
    ...overrides,
  };
}

function setup(selected: FrontDeskVoterDto | null) {
  const unregisterVoter = vi.fn();
  const api = useFrontDeskRegistration({
    electionGuid: ref("elec-1"),
    hasActiveTeller: ref(true),
    electionFlags: ref([]),
    registrationTypes: computed(() => [
      { value: "P", label: "In Person", key: "", isVotingMethod: true },
    ]),
    selectedVoter: ref(selected),
    searchInputRef: ref(null),
    registrationOverlayRef: ref(null),
    checkInVoter: vi.fn(),
    unregisterVoter,
    savePersonFlags: vi.fn(),
    t: (key) => key,
    showSuccessMessage: vi.fn(),
    showErrorMessage: vi.fn(),
  });
  return { api, unregisterVoter };
}

describe("useFrontDeskRegistration Unregister gate", () => {
  beforeEach(() => {
    confirm.mockReset();
  });

  it("does not offer or call Unregister for Processed online without RegistrationTime", async () => {
    const processed = voter({
      isCheckedIn: true,
      onlineBallotStatus: "Processed",
    });
    expect(hasDeskRegistration(processed)).toBe(false);

    const { api, unregisterVoter } = setup(processed);

    expect(api.dialogButtons.value.some((b) => b.isUnregister)).toBe(false);
    await api.handleUnregisterSelected();
    expect(confirm).not.toHaveBeenCalled();
    expect(unregisterVoter).not.toHaveBeenCalled();
  });

  it("offers Unregister when Front Desk set RegistrationTime", () => {
    const desk = voter({
      isCheckedIn: true,
      votingMethod: "P",
      registrationTime: "2026-09-13T12:00:00Z",
    });
    expect(hasDeskRegistration(desk)).toBe(true);

    const { api } = setup(desk);
    expect(api.dialogButtons.value.some((b) => b.isUnregister)).toBe(true);
  });
});
