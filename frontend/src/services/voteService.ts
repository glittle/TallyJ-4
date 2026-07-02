import {
  deleteApiVotesByIdDeleteVote,
  getApiVotesByBallotGuidGetVotesByBallot,
  postApiVotesCreateVote,
  putApiVotesReorderVotes,
} from "../api/gen/configService/sdk.gen";
import type {
  CreateVoteDto,
  ReorderVotesDto,
  VoteDto,
  VotePositionDto,
  VoteWithBallotStatusDto,
} from "../types";
import {
  normalizeVoteDto,
  normalizeVoteList,
} from "../utils/voteDtoNormalization";

type ApiVoteDto = Parameters<typeof normalizeVoteDto>[0];

function normalizeVotePositions(
  votePositions: VotePositionDto[] | undefined,
): VotePositionDto[] | undefined {
  return votePositions?.map((position) => ({
    rowId: position.rowId,
    positionOnBallot: position.positionOnBallot,
  }));
}

function normalizeVoteResult(
  result: VoteWithBallotStatusDto,
): VoteWithBallotStatusDto {
  const normalized: VoteWithBallotStatusDto = {
    ...result,
    vote: result.vote ? normalizeVoteDto(result.vote as ApiVoteDto) : undefined,
    votePositions: normalizeVotePositions(result.votePositions),
  };

  if (
    result.ballotStatusCode !== null &&
    result.ballotStatusCode !== undefined &&
    result.ballotStatusCode !== ""
  ) {
    normalized.ballotStatusCode = String(result.ballotStatusCode);
  } else {
    delete (normalized as any).ballotStatusCode;
  }

  return normalized;
}

export const voteService = {
  async getByBallot(ballotGuid: string): Promise<VoteDto[]> {
    const response = await getApiVotesByBallotGuidGetVotesByBallot({
      path: { ballotGuid },
    });
    return normalizeVoteList(response.data?.data as ApiVoteDto[] | undefined);
  },

  async create(dto: CreateVoteDto): Promise<VoteWithBallotStatusDto> {
    const response = await postApiVotesCreateVote({ body: dto });
    return normalizeVoteResult(response.data?.data as VoteWithBallotStatusDto);
  },

  async delete(voteId: number): Promise<VoteWithBallotStatusDto> {
    const response = await deleteApiVotesByIdDeleteVote({
      path: { id: voteId },
    });
    return normalizeVoteResult(response.data?.data as VoteWithBallotStatusDto);
  },

  async reorder(dto: ReorderVotesDto): Promise<VoteWithBallotStatusDto> {
    const response = await putApiVotesReorderVotes({ body: dto });
    return normalizeVoteResult(response.data?.data as VoteWithBallotStatusDto);
  },
};