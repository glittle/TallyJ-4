import { beforeEach, describe, expect, it, vi } from "vitest";
import { createPinia, setActivePinia } from "pinia";

const logoutApi = vi.fn().mockResolvedValue(undefined);
const getSession = vi.fn();
const verifyCode = vi.fn();

vi.mock("@/services/onlineVotingService", () => ({
  onlineVotingService: {
    logout: (...args: unknown[]) => logoutApi(...args),
    getSession: (...args: unknown[]) => getSession(...args),
    verifyCode: (...args: unknown[]) => verifyCode(...args),
    requestCode: vi.fn(),
    googleAuth: vi.fn(),
    facebookAuth: vi.fn(),
    kakaoAuth: vi.fn(),
    telegramAuth: vi.fn(),
    getElectionInfo: vi.fn(),
    getVotablePeople: vi.fn(),
    submitBallot: vi.fn(),
    getAvailableElections: vi.fn(),
    getVoteStatus: vi.fn(),
  },
}));

vi.mock("@/services/secureTokenService", () => ({
  secureTokenService: {
    isVoterAuthenticated: () => true,
    clearVoterSession: vi.fn(),
  },
}));

vi.mock("@/composables/useApiErrorHandler", () => ({
  useApiErrorHandler: () => ({
    handleApiError: vi.fn(),
  }),
}));

vi.mock("@/services/signalrService", () => ({
  signalrService: {
    disconnectVoterHubs: vi.fn(),
    connectVoterHubs: vi.fn(),
    joinOnlineVoterElection: vi.fn(),
    leaveOnlineVoterElection: vi.fn(),
  },
}));

import { useOnlineVotingStore } from "../onlineVotingStore";

describe("onlineVotingStore kiosk session isolation", () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    logoutApi.mockClear();
    verifyCode.mockReset();
  });

  it("clears voter identity on logout so the next kiosk login starts clean", async () => {
    const store = useOnlineVotingStore();
    verifyCode.mockResolvedValue({ voterId: "SMART", voterIdType: "C" });

    await store.verifyCode({ voterId: "SMART", verifyCode: "SMART" });
    expect(store.voterId).toBe("SMART");
    expect(store.voterIdType).toBe("C");
    expect(store.isKioskSession).toBe(true);

    await store.logout();

    expect(logoutApi).toHaveBeenCalled();
    expect(store.voterId).toBeNull();
    expect(store.voterIdType).toBeNull();
    expect(store.isKioskSession).toBe(false);
    expect(store.voteStatus).toBeNull();
    expect(store.electionInfo).toBeNull();
  });
});
