import { describe, expect, it, vi } from "vitest";

vi.mock("@/api/gen/configService/sdk.gen", () => ({
  getApiVotesByBallotGuidGetVotesByBallot: vi.fn(),
  postApiVotesCreateVote: vi.fn(),
  deleteApiVotesByIdDeleteVote: vi.fn(),
}));

import {
  deleteApiVotesByIdDeleteVote,
  postApiVotesCreateVote,
} from "@/api/gen/configService/sdk.gen";
import { voteService } from "../voteService";

describe("voteService.create", () => {
  it("maps voteStatus from the API to statusCode for the UI", async () => {
    vi.mocked(postApiVotesCreateVote).mockResolvedValue({
      data: {
        data: {
          vote: {
            rowId: 42,
            ballotGuid: "cab56d1c-c0c1-8243-25f5-f0fe09023b46",
            positionOnBallot: 6,
            personGuid: "1d99f163-5bb4-2b34-f0c4-80e413ad439e",
            personFullName: "Jane Doe",
            voteStatus: "Ok",
          },
          ballotStatusCode: "Ok",
        },
      },
    } as any);

    const result = await voteService.create({
      ballotGuid: "cab56d1c-c0c1-8243-25f5-f0fe09023b46",
      positionOnBallot: 6,
      personGuid: "1d99f163-5bb4-2b34-f0c4-80e413ad439e",
    });

    expect(result.vote?.statusCode).toBe("ok");
    expect(result.vote?.personFullName).toBe("Jane Doe");
    expect(result.ballotStatusCode).toBe("Ok");
  });
});

describe("voteService.delete", () => {
  it("returns the re-evaluated ballot status and vote list from the API", async () => {
    vi.mocked(deleteApiVotesByIdDeleteVote).mockResolvedValue({
      data: {
        data: {
          ballotStatusCode: "TooFew",
          votes: [
            {
              rowId: 1,
              ballotGuid: "cab56d1c-c0c1-8243-25f5-f0fe09023b46",
              positionOnBallot: 1,
              personGuid: "266e4637-9305-e857-f69d-cefc54b94746",
              personFullName: "Walker, Charles",
              voteStatus: "Ok",
            },
          ],
        },
      },
    } as any);

    const result = await voteService.delete(99);

    expect(result.ballotStatusCode).toBe("TooFew");
    expect(result.votes).toHaveLength(1);
    expect(result.votes?.[0].positionOnBallot).toBe(1);
    expect(result.votes?.[0].statusCode).toBe("ok");
  });
});