import { getApiPeopleElectionByElectionGuid, getApiPeopleByGuid, postApiPeople, putApiPeopleByGuid, deleteApiPeopleByGuid, getApiPeopleElectionByElectionGuidSearch } from '../api/gen/configService/sdk.gen';
import type { PersonDto, CreatePersonDto, UpdatePersonDto } from '../types';

export const peopleService = {
  async getAll(electionGuid: string): Promise<PersonDto[]> {
    const response = await getApiPeopleElectionByElectionGuid({ path: { electionGuid } });
    return response.data as PersonDto[];
  },

  async getById(personGuid: string): Promise<PersonDto> {
    const response = await getApiPeopleByGuid({ path: { guid: personGuid } });
    return response.data as PersonDto;
  },

  async create(dto: CreatePersonDto): Promise<PersonDto> {
    const response = await postApiPeople({ body: dto });
    return response.data as PersonDto;
  },

  async update(personGuid: string, dto: UpdatePersonDto): Promise<PersonDto> {
    const response = await putApiPeopleByGuid({ path: { guid: personGuid }, body: dto });
    return response.data as PersonDto;
  },

  async delete(personGuid: string): Promise<void> {
    await deleteApiPeopleByGuid({ path: { guid: personGuid } });
  },

  async search(electionGuid: string, query: string): Promise<PersonDto[]> {
    const response = await getApiPeopleElectionByElectionGuidSearch({ 
      path: { electionGuid }, 
      query: { query } 
    });
    return response.data as PersonDto[];
  },

  async getVoters(electionGuid: string): Promise<PersonDto[]> {
    const response = await getApiPeopleElectionByElectionGuid({ path: { electionGuid } });
    return response.data as PersonDto[];
  },

  async getCandidates(electionGuid: string): Promise<PersonDto[]> {
    const response = await getApiPeopleElectionByElectionGuid({ path: { electionGuid } });
    return response.data as PersonDto[];
  }
};
