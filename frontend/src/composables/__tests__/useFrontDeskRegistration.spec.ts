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

function setup(
  selected: FrontDeskVoterDto | null,
  extras: { isElectionFinalized?: boolean } = {},
) {
  const unregisterVoter = vi.fn();
  const checkInVoter = vi.fn();
  const api = useFrontDeskRegistration({
    electionGuid: ref("elec-1"),
    hasActiveTeller: ref(true),
    isElectionFinalized: ref(extras.isElectionFinalized ?? false),
    electionFlags: ref([]),
    registrationTypes: computed(() => [
      { value: "P", label: "In Person", key: "", isVotingMethod: true },
    ]),
    selectedVoter: ref(selected),
    searchInputRef: ref(null),
    registrationOverlayRef: ref(null),
    checkInVoter,
    unregisterVoter,
    savePersonFlags: vi.fn(),
    t: (key) => key,
    showSuccessMessage: vi.fn(),
    showErrorMessage: vi.fn(),
  });
  return { api, unregisterVoter, checkInVoter };
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
    expect(api.dialogButtons.value.some((b) => b.isVotingMethod)).toBe(false);
  });

  it("offers method buttons after Unregister so the teller can change method", () => {
    const unregistered = voter({
      isCheckedIn: false,
      votingMethod: undefined,
      registrationTime: undefined,
    });
    const { api } = setup(unregistered);
    expect(api.dialogButtons.value.some((b) => b.isUnregister)).toBe(false);
    expect(api.dialogButtons.value.some((b) => b.isVotingMethod)).toBe(true);
  });

  it("does not call Unregister or check-in when the election is Finalized", async () => {
    const desk = voter({
      isCheckedIn: true,
      votingMethod: "P",
      registrationTime: "2026-09-13T12:00:00Z",
    });
    const { api, unregisterVoter, checkInVoter } = setup(desk, {
      isElectionFinalized: true,
    });

    expect(api.registrationWritesAllowed.value).toBe(false);
    await api.handleUnregisterSelected();
    expect(confirm).not.toHaveBeenCalled();
    expect(unregisterVoter).not.toHaveBeenCalled();

    api.clickDialogButton("P");
    expect(checkInVoter).not.toHaveBeenCalled();
  });
});
