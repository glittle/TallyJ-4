import { beforeEach, describe, expect, it, vi } from "vitest";
import { createPinia, setActivePinia } from "pinia";
import type { BallotDto, VoteDto } from "@/types";
import { toBallotSummary } from "@/utils/ballotSummary";

vi.mock("@/services/ballotService", () => ({
  ballotService: {
    getAll: vi.fn(),
    getById: vi.fn(),
    create: vi.fn(),
    update: vi.fn(),
    delete: vi.fn(),
  },
}));

vi.mock("@/services/voteService", () => ({
  voteService: {
    create: vi.fn(),
    delete: vi.fn(),
    reorder: vi.fn(),
    getByBallot: vi.fn(),
  },
}));

vi.mock("@/services/signalrService", () => ({
  signalrService: {
    connectToFrontDeskHub: vi.fn(),
    joinElection: vi.fn(),
    leaveElection: vi.fn(),
  },
}));

import { ballotService } from "@/services/ballotService";
import { voteService } from "@/services/voteService";
import { useBallotStore } from "@/stores/ballotStore";

function createVote(
  rowId: number,
  positionOnBallot: number,
  name: string,
): VoteDto {
  return {
    rowId,
    ballotGuid: "ballot-1",
    positionOnBallot,
    personGuid: `person-${rowId}`,
    personFullName: name,
    statusCode: "ok",
  };
}

function createBallot(votes: VoteDto[]): BallotDto {
  return {
    ballotGuid: "ballot-1",
    ballotCode: "A1",
    locationGuid: "location-1",
    locationName: "Main Hall",
    ballotNumAtComputer: 1,
    computerCode: "A",
    statusCode: "TooFew",
    voteCount: votes.length,
    votes,
  };
}

describe("useBallotStore deleteVote", () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    vi.clearAllMocks();
  });

  it("stores ballot summaries without vote rows in the list", async () => {
    const store = useBallotStore();
    const ballot = createBallot([createVote(1, 1, "Alice")]);

    vi.mocked(ballotService.getAll).mockResolvedValue([ballot]);

    await store.fetchBallots("election-1");

    expect(store.ballots).toHaveLength(1);
    expect(store.ballots[0].voteCount).toBe(1);
    expect("votes" in store.ballots[0]).toBe(false);
  });

  it("removes the deleted vote and renumbers remaining votes in currentBallot", async () => {
    const store = useBallotStore();
    const initialVotes = [
      createVote(1, 1, "Alice"),
      createVote(2, 2, "Bob"),
      createVote(3, 3, "Carol"),
    ];
    store.currentBallot = createBallot(initialVotes);
    store.ballots = [toBallotSummary(store.currentBallot)];

    vi.mocked(voteService.delete).mockResolvedValue({
      ballotStatusCode: "TooFew",
      votePositions: [
        { rowId: 1, positionOnBallot: 1 },
        { rowId: 3, positionOnBallot: 2 },
      ],
    });

    await store.deleteVote("ballot-1", 2);

    expect(store.currentBallot?.votes).toHaveLength(2);
    expect(
      store.currentBallot?.votes.map((vote) => vote.personFullName),
    ).toEqual(["Alice", "Carol"]);
    expect(store.currentBallot?.voteCount).toBe(2);
    expect(store.ballots[0].voteCount).toBe(2);
    expect("votes" in store.ballots[0]).toBe(false);
  });

  it("compacts vote positions when the API omits vote positions", async () => {
    const store = useBallotStore();
    const initialVotes = [
      createVote(1, 1, "Alice"),
      createVote(2, 2, "Bob"),
      createVote(3, 3, "Carol"),
    ];
    store.currentBallot = createBallot(initialVotes);
    store.ballots = [toBallotSummary(store.currentBallot)];

    vi.mocked(voteService.delete).mockResolvedValue({
      ballotStatusCode: "TooFew",
    });

    await store.deleteVote("ballot-1", 2);

    expect(store.currentBallot?.votes).toHaveLength(2);
    expect(
      store.currentBallot?.votes.map((vote) => vote.positionOnBallot),
    ).toEqual([1, 2]);
    expect(
      store.currentBallot?.votes.map((vote) => vote.personFullName),
    ).toEqual(["Alice", "Carol"]);
  });

  it("adopts a fetched ballot as currentBallot when opening from the list", async () => {
    const store = useBallotStore();
    const ballot = createBallot([createVote(1, 1, "Alice")]);

    vi.mocked(ballotService.getById).mockResolvedValue(ballot);

    await store.fetchBallotById("ballot-1");

    expect(store.currentBallot?.ballotGuid).toBe("ballot-1");
    expect(store.currentBallot?.votes).toHaveLength(1);
  });

  it("does not patch list voteCount from a single vote when the ballot is not current", async () => {
    const store = useBallotStore();
    const otherBallot = createBallot([createVote(1, 1, "Alice")]);
    otherBallot.ballotGuid = "ballot-2";
    otherBallot.voteCount = 3;

    store.currentBallot = createBallot([
      createVote(10, 1, "Dave"),
      createVote(11, 2, "Eve"),
    ]);
    store.ballots = [
      toBallotSummary(store.currentBallot),
      toBallotSummary(otherBallot),
    ];

    vi.mocked(voteService.create).mockResolvedValue({
      ballotStatusCode: "TooFew",
      vote: {
        ...createVote(20, 4, "Frank"),
        ballotGuid: "ballot-2",
        positionOnBallot: 4,
      },
    });

    await store.createVote({
      ballotGuid: "ballot-2",
      personGuid: "person-20",
      positionOnBallot: 4,
    });

    expect(
      store.ballots.find((b) => b.ballotGuid === "ballot-2")?.voteCount,
    ).toBe(3);
    expect(store.currentBallot?.ballotGuid).toBe("ballot-1");
    expect(store.currentBallot?.votes).toHaveLength(2);
  });

  it("adopts authoritative votes from updateBallot response", async () => {
    const store = useBallotStore();
    const staleVotes = [createVote(1, 1, "Alice"), createVote(2, 2, "Bob")];
    store.currentBallot = createBallot(staleVotes);
    store.ballots = [toBallotSummary(store.currentBallot)];

    const authoritativeVotes = [
      createVote(1, 1, "Alice"),
      { ...createVote(3, 2, "Carol"), statusCode: "ok" },
    ];
    const updatedBallot = {
      ...createBallot(authoritativeVotes),
      statusCode: "Ok",
    };

    vi.mocked(ballotService.update).mockResolvedValue(updatedBallot);

    await store.updateBallot("ballot-1", { ballotCode: "A2" });

    expect(store.currentBallot?.votes).toHaveLength(2);
    expect(
      store.currentBallot?.votes.map((vote) => vote.personFullName),
    ).toEqual(["Alice", "Carol"]);
    expect(store.currentBallot?.votes[1].positionOnBallot).toBe(2);
    expect(store.currentBallot?.statusCode).toBe("Ok");
    expect(store.ballots[0].voteCount).toBe(2);
  });

  it("does not let a stale fetchBallotById overwrite a newer delete mutation", async () => {
    const store = useBallotStore();
    const initialVotes = [createVote(1, 1, "Alice"), createVote(2, 2, "Bob")];
    store.currentBallot = createBallot(initialVotes);
    store.ballots = [toBallotSummary(store.currentBallot)];

    let resolveFetch: ((ballot: BallotDto) => void) | undefined;
    const fetchPromise = new Promise<BallotDto>((resolve) => {
      resolveFetch = resolve;
    });

    vi.mocked(ballotService.getById).mockReturnValue(fetchPromise);
    vi.mocked(voteService.delete).mockResolvedValue({
      ballotStatusCode: "TooFew",
      votePositions: [{ rowId: 1, positionOnBallot: 1 }],
    });

    const inFlightFetch = store.fetchBallotById("ballot-1");
    await store.deleteVote("ballot-1", 2);

    resolveFetch?.(createBallot(initialVotes));
    await inFlightFetch;

    expect(store.currentBallot?.votes).toHaveLength(1);
    expect(store.currentBallot?.votes[0].personFullName).toBe("Alice");
    expect(store.currentBallot?.voteCount).toBe(1);
  });
});
