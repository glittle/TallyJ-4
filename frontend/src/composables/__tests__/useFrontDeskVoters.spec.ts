import { beforeEach, describe, expect, it, vi } from "vitest";
import { nextTick, ref } from "vue";
import type { FrontDeskVoterDto } from "@/types/FrontDesk";

const getEligibleVoters = vi.fn();

vi.mock("@/services/frontDeskService", () => ({
  frontDeskService: {
    getEligibleVoters: (...args: unknown[]) => getEligibleVoters(...args),
    checkInVoter: vi.fn(),
    unregisterVoter: vi.fn(),
    updatePersonFlags: vi.fn(),
    getStats: vi.fn(),
  },
}));

vi.mock("@/services/signalrService", () => ({
  signalrService: {
    connectToFrontDeskHub: vi.fn(),
    joinFrontDeskElection: vi.fn(),
    leaveFrontDeskElection: vi.fn(),
  },
}));

import { useFrontDeskVoters } from "../useFrontDeskVoters";

function voter(
  overrides: Partial<FrontDeskVoterDto> & { personGuid: string },
): FrontDeskVoterDto {
  return {
    fullName: overrides.personGuid,
    isCheckedIn: Boolean(overrides.votingMethod || overrides.registrationTime),
    ...overrides,
  };
}

describe("useFrontDeskVoters Ballot Not Received", () => {
  beforeEach(() => {
    localStorage.clear();
    getEligibleVoters.mockReset();
  });

  async function loadVoters(list: FrontDeskVoterDto[]) {
    getEligibleVoters.mockResolvedValue(list);
    const electionGuid = ref("elec-1");
    const api = useFrontDeskVoters({
      electionGuid,
      t: (key) => key,
    });
    api.registrationFilter.value = "all";
    await api.fetchEligibleVoters("elec-1");
    return api;
  }

  it("shows only people with no voting method when Ballot Not Received is on", async () => {
    const api = await loadVoters([
      voter({ personGuid: "received", votingMethod: "P", flags: "Mailed" }),
      voter({ personGuid: "waiting", flags: "Mailed" }),
      voter({ personGuid: "online", votingMethod: "O" }),
    ]);

    expect(api.allVoters.value.map((v) => v.personGuid)).toEqual([
      "online",
      "received",
      "waiting",
    ]);
    expect(api.ballotNotReceivedCount.value).toBe(1);

    api.toggleBallotNotReceived();
    await nextTick();

    expect(api.ballotNotReceivedOnly.value).toBe(true);
    expect(api.hasActiveFilters.value).toBe(true);
    expect(api.allVoters.value.map((v) => v.personGuid)).toEqual(["waiting"]);
  });

  it("combines Ballot Not Received with a flag filter", async () => {
    const api = await loadVoters([
      voter({ personGuid: "mailed-in", votingMethod: "M", flags: "Mailed" }),
      voter({ personGuid: "mailed-waiting", flags: "Mailed" }),
      voter({ personGuid: "other-waiting", flags: "Youth" }),
    ]);

    api.toggleFlagFilter("Mailed");
    api.toggleBallotNotReceived();
    await nextTick();

    expect(api.allVoters.value.map((v) => v.personGuid)).toEqual([
      "mailed-waiting",
    ]);
  });

  it("clears the Ballot Not Received checkbox with other filters", async () => {
    const api = await loadVoters([
      voter({ personGuid: "waiting" }),
      voter({ personGuid: "received", votingMethod: "P" }),
    ]);

    api.toggleBallotNotReceived();
    api.clearFilters();
    await nextTick();

    expect(api.ballotNotReceivedOnly.value).toBe(false);
    expect(api.allVoters.value.map((v) => v.personGuid)).toEqual([
      "received",
      "waiting",
    ]);
  });
});

describe("useFrontDeskVoters registration filter", () => {
  beforeEach(() => {
    localStorage.clear();
  });

  function openDesk(electionGuid = "elec-1") {
    return useFrontDeskVoters({
      electionGuid: ref(electionGuid),
      t: (key) => key,
    });
  }

  it("opens on All when this election has no stored choice", () => {
    expect(openDesk().registrationFilter.value).toBe("all");
  });

  it("restores a stored choice for this election", () => {
    localStorage.setItem(
      "tallyj.frontDesk.registrationFilters",
      JSON.stringify({ "elec-1": "notRegistered", "elec-2": "registered" }),
    );

    expect(openDesk("elec-1").registrationFilter.value).toBe("notRegistered");
    expect(openDesk("elec-2").registrationFilter.value).toBe("registered");
  });

  it("ignores a stored choice for a different election", () => {
    localStorage.setItem(
      "tallyj.frontDesk.registrationFilters",
      JSON.stringify({ "elec-2": "notRegistered" }),
    );

    expect(openDesk("elec-1").registrationFilter.value).toBe("all");
  });

  it("stores the radio the teller chooses", async () => {
    const api = openDesk();
    api.registrationFilter.value = "registered";
    await nextTick();

    expect(
      JSON.parse(
        localStorage.getItem("tallyj.frontDesk.registrationFilters") ?? "{}",
      ),
    ).toEqual({
      "elec-1": "registered",
    });
  });
});
