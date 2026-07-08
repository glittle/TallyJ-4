import { createPinia, setActivePinia } from "pinia";
import { beforeEach, describe, expect, it, vi } from "vitest";

vi.mock("../services/electionService", () => ({
  electionService: {
    getStats: vi.fn(),
  },
}));

import { useElectionStatsStore } from "./electionStatsStore";

describe("electionStatsStore", () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    vi.clearAllMocks();
  });

  it("fetches and caches stats", async () => {
    const { electionService } = await import("../services/electionService");
    electionService.getStats.mockResolvedValue({
      voterCount: 10,
      ballotCount: 5,
      locationCount: 2,
    });

    const store = useElectionStatsStore();
    const stats = await store.fetchStats("election-1");

    expect(stats).toEqual({
      voterCount: 10,
      ballotCount: 5,
      locationCount: 2,
    });
    expect(electionService.getStats).toHaveBeenCalledTimes(1);
    expect(store.getCached("election-1")).toEqual(stats);
  });

  it("returns cached stats without refetching", async () => {
    const { electionService } = await import("../services/electionService");
    electionService.getStats.mockResolvedValue({
      voterCount: 1,
      ballotCount: 2,
      locationCount: 3,
    });

    const store = useElectionStatsStore();
    await store.fetchStats("election-1");
    await store.fetchStats("election-1");

    expect(electionService.getStats).toHaveBeenCalledTimes(1);
  });

  it("refetches when force is true", async () => {
    const { electionService } = await import("../services/electionService");
    electionService.getStats
      .mockResolvedValueOnce({
        voterCount: 1,
        ballotCount: 2,
        locationCount: 3,
      })
      .mockResolvedValueOnce({
        voterCount: 4,
        ballotCount: 5,
        locationCount: 6,
      });

    const store = useElectionStatsStore();
    await store.fetchStats("election-1");
    const stats = await store.fetchStats("election-1", { force: true });

    expect(electionService.getStats).toHaveBeenCalledTimes(2);
    expect(stats.voterCount).toBe(4);
  });

  it("invalidate clears cache so next fetch hits API", async () => {
    const { electionService } = await import("../services/electionService");
    electionService.getStats.mockResolvedValue({
      voterCount: 1,
      ballotCount: 2,
      locationCount: 3,
    });

    const store = useElectionStatsStore();
    await store.fetchStats("election-1");
    store.invalidate("election-1");
    expect(store.getCached("election-1")).toBeUndefined();

    await store.fetchStats("election-1");
    expect(electionService.getStats).toHaveBeenCalledTimes(2);
  });
});