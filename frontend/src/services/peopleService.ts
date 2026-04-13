import {
  getApiPeopleByElectionGuidGetPeople,
  getApiPeopleByGuidGetPerson,
  postApiPeopleCreatePerson,
  putApiPeopleByGuidUpdatePerson,
  deleteApiPeopleByGuidDeletePerson,
  getApiPeopleByElectionGuidSearchPeople,
  getApiPeopleByElectionGuidGetCandidates,
  getApiPeopleByElectionGuidGetAllPeople,
  getApiPeopleByGuidGetPersonDetails,
  getApiPeopleByElectionGuidGetAllForBallotEntry,
} from "../api/gen/configService/sdk.gen";
import type {
  PersonDto,
  PersonListDto,
  PersonDetailDto,
  CreatePersonDto,
  UpdatePersonDto,
} from "../types";

export const peopleService = {
  async getAll(electionGuid: string): Promise<PersonDto[]> {
    const response = await getApiPeopleByElectionGuidGetPeople({
      path: { electionGuid },
    });
    return (response.data?.items ?? []) as PersonDto[];
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
    return (response.data?.data ?? []) as PersonDto[];
  },

  async getVoters(electionGuid: string): Promise<PersonDto[]> {
    const response = await getApiPeopleByElectionGuidGetPeople({
      path: { electionGuid },
    });
    return (response.data?.items ?? []) as PersonDto[];
  },

  async getCandidates(electionGuid: string): Promise<PersonDto[]> {
    const response = await getApiPeopleByElectionGuidGetCandidates({
      path: { electionGuid },
    });
    return (response.data?.data ?? []) as PersonDto[];
  },

  async getAllForBallotEntry(electionGuid: string): Promise<PersonDto[]> {
    const response = await getApiPeopleByElectionGuidGetAllForBallotEntry({
      path: { electionGuid },
    });
    return (response.data?.data ?? []) as PersonDto[];
  },
};
