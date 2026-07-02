import {
  getApiPeopleByElectionGuidGetPeople,
  getApiPeopleByGuidGetPerson,
  postApiPeopleCreatePerson,
  putApiPeopleByGuidUpdatePerson,
  deleteApiPeopleByGuidDeletePerson,
  getApiPeopleByElectionGuidSearchPeople,
  getApiPeopleByElectionGuidGetAllPeople,
  getApiPeopleByGuidGetPersonDetails,
  getApiPeopleByElectionGuidGetAllForBallotEntry,
} from "@/api/gen/configService";
import type {
  PersonDto,
  PersonListDto,
  PersonDetailDto,
  CreatePersonDto,
  UpdatePersonDto,
} from "../types";

const MAX_PAGE_SIZE = 200;

type PeopleQueryFilters = {
  search?: string;
  canVote?: boolean;
  canReceiveVotes?: boolean;
};

async function fetchAllPeoplePages(
  electionGuid: string,
  filters: PeopleQueryFilters = {},
): Promise<PersonDto[]> {
  const allPeople: PersonDto[] = [];
  let pageNumber = 1;
  let hasNextPage = true;

  while (hasNextPage) {
    const response = await getApiPeopleByElectionGuidGetPeople({
      path: { electionGuid },
      query: {
        pageNumber,
        pageSize: MAX_PAGE_SIZE,
        ...filters,
      },
    });

    const page = response.data;
    allPeople.push(...((page?.items ?? []) as PersonDto[]));
    hasNextPage = page?.hasNextPage ?? false;
    pageNumber++;
  }

  return allPeople;
}

export const peopleService = {
  async getAll(electionGuid: string): Promise<PersonDto[]> {
    return fetchAllPeoplePages(electionGuid);
  },

  async getAllPeople(electionGuid: string): Promise<PersonListDto[]> {
    const response = await getApiPeopleByElectionGuidGetAllPeople({
      path: { electionGuid },
    });
    return (response.data?.data ?? []) as PersonListDto[];
  },

  async getById(personGuid: string): Promise<PersonDto> {
    const response = await getApiPeopleByGuidGetPerson({
      path: { guid: personGuid },
    });
    return response.data?.data as PersonDto;
  },

  async getDetails(personGuid: string): Promise<PersonDetailDto> {
    const response = await getApiPeopleByGuidGetPersonDetails({
      path: { guid: personGuid },
    });
    return response.data?.data as PersonDetailDto;
  },

  async create(dto: CreatePersonDto): Promise<PersonDto> {
    const response = await postApiPeopleCreatePerson({ body: dto });
    return response.data?.data as PersonDto;
  },

  async update(personGuid: string, dto: UpdatePersonDto): Promise<PersonDto> {
    const response = await putApiPeopleByGuidUpdatePerson({
      path: { guid: personGuid },
      body: dto,
    });
    return response.data?.data as PersonDto;
  },

  async delete(personGuid: string): Promise<void> {
    await deleteApiPeopleByGuidDeletePerson({ path: { guid: personGuid } });
  },

  async search(electionGuid: string, query: string): Promise<PersonDto[]> {
    const response = await getApiPeopleByElectionGuidSearchPeople({
      path: { electionGuid },
      query: { q: query },
    });
    return response.data?.data ?? [];
  },

  async getVotablePeople(electionGuid: string): Promise<PersonDto[]> {
    return fetchAllPeoplePages(electionGuid, { canReceiveVotes: true });
  },

  async getAllForBallotEntry(electionGuid: string): Promise<PersonDto[]> {
    const response = await getApiPeopleByElectionGuidGetAllForBallotEntry({
      path: { electionGuid },
    });
    return (response.data?.data ?? []) as PersonDto[];
  },
};