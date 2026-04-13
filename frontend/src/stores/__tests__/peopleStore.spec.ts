import { describe, it, expect, vi, beforeEach } from "vitest";
import { setActivePinia, createPinia } from "pinia";
import { usePeopleStore } from "../peopleStore";
import type { PersonDto } from "@/types/Person";
import type { PersonVoteCountUpdateEvent } from "@/types/SignalREvents";

vi.mock("@/services/peopleService", () => ({
  peopleService: {
    getAll: vi.fn().mockResolvedValue([]),
    getById: vi.fn(),
    create: vi.fn(),
    update: vi.fn(),
    delete: vi.fn(),
    search: vi.fn(),
    getCandidates: vi.fn().mockResolvedValue([]),
    getAllForBallotEntry: vi.fn().mockResolvedValue([]),
  },
}));

vi.mock("@/services/signalrService", () => ({
  signalrService: {
    connectToFrontDeskHub: vi.fn().mockResolvedValue({
      on: vi.fn(),
    }),
    joinElection: vi.fn().mockResolvedValue(undefined),
    leaveElection: vi.fn().mockResolvedValue(undefined),
  },
}));

function createPersonDto(overrides: Partial<PersonDto> = {}): PersonDto {
  return {
    personGuid: "test-guid",
    lastName: "Doe",
    fullName: "John Doe",
    voteCount: 0,
    ...overrides,
  };
}

describe("usePeopleStore", () => {
  beforeEach(() => {
    setActivePinia(createPinia());
  });

  describe("handlePersonVoteCountUpdated", () => {
    it("should update voteCount for a matching person in the cache", () => {
      const store = usePeopleStore();

      store.candidateCache = [
        {
          ...createPersonDto({
            personGuid: "person-1",
            lastName: "Smith",
            fullName: "Alice Smith",
            voteCount: 2,
          }),
          _searchText: "alice smith",
          _soundexCodes: [],
        },
        {
          ...createPersonDto({
            personGuid: "person-2",
            lastName: "Jones",
            fullName: "Bob Jones",
            voteCount: 0,
          }),
          _searchText: "bob jones",
          _soundexCodes: [],
        },
      ] as any;

      const event: PersonVoteCountUpdateEvent = {
        electionGuid: "election-1",
        personGuid: "person-1",
        voteCount: 5,
      };

      store.handlePersonVoteCountUpdated(event);

      const updated = store.candidateCache.find(
        (p) => p.personGuid === "person-1",
      );
      expect(updated?.voteCount).toBe(5);
    });

    it("should not modify other people in the cache", () => {
      const store = usePeopleStore();

      store.candidateCache = [
        {
          ...createPersonDto({
            personGuid: "person-1",
            lastName: "Smith",
            fullName: "Alice Smith",
            voteCount: 2,
          }),
          _searchText: "alice smith",
          _soundexCodes: [],
        },
        {
          ...createPersonDto({
            personGuid: "person-2",
            lastName: "Jones",
            fullName: "Bob Jones",
            voteCount: 3,
          }),
          _searchText: "bob jones",
          _soundexCodes: [],
        },
      ] as any;

      const event: PersonVoteCountUpdateEvent = {
        electionGuid: "election-1",
        personGuid: "person-1",
        voteCount: 10,
      };

      store.handlePersonVoteCountUpdated(event);

      const unchanged = store.candidateCache.find(
        (p) => p.personGuid === "person-2",
      );
      expect(unchanged?.voteCount).toBe(3);
    });

    it("should replace the array reference to invalidate search cache", () => {
      const store = usePeopleStore();

      const initialCache = [
        {
          ...createPersonDto({
            personGuid: "person-1",
            lastName: "Smith",
            fullName: "Alice Smith",
            voteCount: 0,
          }),
          _searchText: "alice smith",
          _soundexCodes: [],
        },
      ] as any;
      store.candidateCache = initialCache;

      const event: PersonVoteCountUpdateEvent = {
        electionGuid: "election-1",
        personGuid: "person-1",
        voteCount: 7,
      };

      store.handlePersonVoteCountUpdated(event);

      expect(store.candidateCache).not.toBe(initialCache);
    });

    it("should do nothing when person is not found in cache", () => {
      const store = usePeopleStore();

      store.candidateCache = [
        {
          ...createPersonDto({
            personGuid: "person-1",
            lastName: "Smith",
            fullName: "Alice Smith",
            voteCount: 2,
          }),
          _searchText: "alice smith",
          _soundexCodes: [],
        },
      ] as any;

      const event: PersonVoteCountUpdateEvent = {
        electionGuid: "election-1",
        personGuid: "unknown-guid",
        voteCount: 5,
      };

      store.handlePersonVoteCountUpdated(event);

      expect(store.candidateCache).toHaveLength(1);
      expect(store.candidateCache[0]!.voteCount).toBe(2);
    });

    it("should call getAllForBallotEntry when initializing candidate cache", async () => {
      const { peopleService } = await import("@/services/peopleService");
      const mockPerson = createPersonDto({
        personGuid: "p1",
        lastName: "Brown",
        fullName: "Ted Brown",
        canReceiveVotes: false,
      });
      vi.mocked(peopleService.getAllForBallotEntry).mockResolvedValue([
        mockPerson,
      ]);

      const store = usePeopleStore();
      await store.initializeCandidateCache("election-1");

      expect(peopleService.getAllForBallotEntry).toHaveBeenCalledWith(
        "election-1",
      );
      expect(store.candidateCache).toHaveLength(1);
      expect(store.candidateCache[0]!.personGuid).toBe("p1");
      expect(store.isCacheInitialized).toBe(true);
    });

    it("should include ineligible people in candidate cache", async () => {
      const { peopleService } = await import("@/services/peopleService");
      const eligible = createPersonDto({
        personGuid: "p1",
        lastName: "Doe",
        fullName: "Jane Doe",
        canReceiveVotes: true,
      });
      const ineligible = createPersonDto({
        personGuid: "p2",
        lastName: "Smith",
        fullName: "Bob Smith",
        canReceiveVotes: false,
        ineligibleReasonCode: "X01",
      });
      vi.mocked(peopleService.getAllForBallotEntry).mockResolvedValue([
        eligible,
        ineligible,
      ]);

      const store = usePeopleStore();
      await store.initializeCandidateCache("election-1");

      expect(store.candidateCache).toHaveLength(2);
      const ineligibleCached = store.candidateCache.find(
        (p) => p.personGuid === "p2",
      );
      expect(ineligibleCached).toBeDefined();
      expect(ineligibleCached?.ineligibleReasonCode).toBe("X01");
    });
  });
});
