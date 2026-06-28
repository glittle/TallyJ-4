import {
  getApiVotesByBallotGuidGetVotesByBallot,
  postApiVotesCreateVote,
  deleteApiVotesByIdDeleteVote,
} from "../api/gen/configService/sdk.gen";
import type { CreateVoteDto, VoteWithBallotStatusDto } from "../types";
import {
  normalizeVoteDto,
  normalizeVoteList,
} from "../utils/voteDtoNormalization";

type ApiVoteDto = Parameters<typeof normalizeVoteDto>[0];

function normalizeVoteResult(
  result: VoteWithBallotStatusDto,
): VoteWithBallotStatusDto {
  const votes = result.votes
    ? normalizeVoteList(result.votes as ApiVoteDto[])
    : undefined;

  return {
    ...result,
    vote: result.vote
      ? normalizeVoteDto(result.vote as ApiVoteDto)
      : undefined,
    votes,
    ballotStatusCode: String(result.ballotStatusCode ?? ""),
  };
}

export const voteService = {
  async getByBallot(ballotGuid: string): Promise<VoteDto[]> {
    const response = await getApiVotesByBallotGuidGetVotesByBallot({
      path: { ballotGuid },
    });
    return (response.data?.data?.items ?? []) as VoteDto[];
  },

  async create(dto: CreateVoteDto): Promise<VoteWithBallotStatusDto> {
    const response = await postApiVotesCreateVote({ body: dto });
    return normalizeVoteResult(response.data?.data as VoteWithBallotStatusDto);
  },

  async delete(voteId: number): Promise<VoteWithBallotStatusDto> {
    const response = await deleteApiVotesByIdDeleteVote({ path: { id: voteId } });
    return normalizeVoteResult(response.data?.data as VoteWithBallotStatusDto);
  },
};
