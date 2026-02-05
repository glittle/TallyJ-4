import { getApiVotesBallotByBallotGuid, postApiVotes, deleteApiVotesById } from '../api/gen/configService/sdk.gen';
import type { VoteDto, CreateVoteDto } from '../types';

export const voteService = {
  async getByBallot(ballotGuid: string): Promise<VoteDto[]> {
    const response = await getApiVotesBallotByBallotGuid({ path: { ballotGuid } });
    return response.data as VoteDto[];
  },

  async create(dto: CreateVoteDto): Promise<VoteDto> {
    const response = await postApiVotes({ body: dto });
    return response.data as VoteDto;
  },

  async delete(ballotGuid: string, positionOnBallot: number): Promise<void> {
    await deleteApiVotesById({ path: { id: `${ballotGuid}/${positionOnBallot}` } });
  }
};
