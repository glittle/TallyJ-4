import { getApiVotesByBallotGuidGetVotesByBallot, postApiVotesCreateVote, deleteApiVotesByIdDeleteVote } from '../api/gen/configService/sdk.gen';
import type { VoteDto, CreateVoteDto } from '../types';

export const voteService = {
  async getByBallot(ballotGuid: string): Promise<VoteDto[]> {
    const response = await getApiVotesByBallotGuidGetVotesByBallot({ path: { ballotGuid } });
    return response.data as VoteDto[];
  },

  async create(dto: CreateVoteDto): Promise<VoteDto> {
    const response = await postApiVotesCreateVote({ body: dto });
    return response.data as VoteDto;
  },

  async delete(voteId: number): Promise<void> {
    await deleteApiVotesByIdDeleteVote({ path: { id: voteId } });
  }
};
