import {
  deleteApiBallotsByGuidDeleteBallot,
  getApiBallotsByElectionGuidBallots,
  getApiBallotsByGuidBallot,
  postApiBallotsCreateBallot,
  putApiBallotsByGuidUpdateBallot,
} from "@/api/gen/configService";
import type { EnumerationsBallotStatus } from "@/api/gen/configService/types.gen";
import type { BallotDto, CreateBallotDto, UpdateBallotDto } from "../types";
import { normalizeVoteList } from "../utils/voteDtoNormalization";

export const ballotService = {
  async getAll(electionGuid: string): Promise<BallotDto[]> {
    const response = await getApiBallotsByElectionGuidBallots({
      path: { electionGuid },
    });
    return (response.data?.items ?? []) as BallotDto[];
  },

  async getById(ballotGuid: string): Promise<BallotDto> {
    const response = await getApiBallotsByGuidBallot({
      path: { guid: ballotGuid },
    });
    const ballot = response.data?.data as BallotDto;
    return {
      ...ballot,
      votes: normalizeVoteList(ballot.votes),
    };
  },

  async create(dto: CreateBallotDto): Promise<BallotDto> {
    const response = await postApiBallotsCreateBallot({ body: dto });
    return response.data?.data as BallotDto;
  },

  async update(ballotGuid: string, dto: UpdateBallotDto): Promise<BallotDto> {
    const response = await putApiBallotsByGuidUpdateBallot({
      path: { guid: ballotGuid },
      body: {
        ...dto,
        statusCode: dto.statusCode as EnumerationsBallotStatus | undefined,
      },
    });
    const ballot = response.data?.data as BallotDto;
    return {
      ...ballot,
      votes: normalizeVoteList(ballot.votes),
    };
  },

  async delete(ballotGuid: string): Promise<void> {
    await deleteApiBallotsByGuidDeleteBallot({ path: { guid: ballotGuid } });
  },
};
