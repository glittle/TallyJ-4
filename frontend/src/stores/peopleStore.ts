import { defineStore } from "pinia";
import { computed, ref } from "vue";
import { peopleService } from "../services/peopleService";
import { signalrService } from "../services/signalrService";
import type {
  CreatePersonDto,
  PersonDto,
  PersonListDto,
  SearchablePersonDto,
  UpdatePersonDto,
} from "../types";
import type {
  PersonUpdateEvent,
  PersonVoteCountUpdateEvent,
} from "../types/SignalREvents";
import { useElectionStatsStore } from "./electionStatsStore";

export const usePeopleStore = defineStore("people", () => {
  const people = ref<PersonDto[]>([]);
  const peopleList = ref<PersonListDto[]>([]);
  const activeElectionGuid = ref<string | null>(null);
  const loading = ref(false);
  const error = ref<string | null>(null);
  const signalrInitialized = ref(false);
  const peopleCache = ref<SearchablePersonDto[]>([]);
  const isCacheInitialized = ref(false);
  const personUpdatedListeners = new Set<(data: PersonUpdateEvent) => void>();

  const voters = computed(() =>
    peopleList.value.filter((p) => p.canVote === true),
  );

  const votablePeople = computed(() =>
    peopleList.value.filter((p) => p.canReceiveVotes === true),
  );

  function invalidateElectionStats(electionGuid?: string | null) {
    const guid = electionGuid ?? activeElectionGuid.value;
    if (guid) {
      useElectionStatsStore().invalidate(guid);
    }
  }

  async function fetchPeople(electionGuid: string) {
    activeElectionGuid.value = electionGuid;
    loading.value = true;
    error.value = null;
    try {
      people.value = await peopleService.getAll(electionGuid);
    } catch (e: any) {
      error.value = e.message || "Failed to fetch people";
      throw e;
    } finally {
      loading.value = false;
    }
  }

  async function fetchPeopleList(electionGuid: string) {
    activeElectionGuid.value = electionGuid;
    loading.value = true;
    error.value = null;
    try {
      peopleList.value = await peopleService.getAllPeople(electionGuid);
    } catch (e: any) {
      error.value = e.message || "Failed to fetch people";
      throw e;
    } finally {
      loading.value = false;
    }
  }

  async function fetchPersonById(personGuid: string) {
    loading.value = true;
    error.value = null;
    try {
      const person = await peopleService.getById(personGuid);

      const index = people.value.findIndex((p) => p.personGuid === personGuid);
      if (index !== -1) {
        people.value[index] = person;
      } else {
        people.value.push(person);
      }

      return person;
    } catch (e: any) {
      error.value = e.message || "Failed to fetch person";
      throw e;
    } finally {
      loading.value = false;
    }
  }

  async function createPerson(dto: CreatePersonDto) {
    loading.value = true;
    error.value = null;
    try {
      const person = await peopleService.create(dto);
      people.value.push(person);
      upsertPersonListEntry(person);
      invalidateElectionStats(dto.electionGuid);
      return person;
    } catch (e: any) {
      error.value = e.message || "Failed to create person";
      throw e;
    } finally {
      loading.value = false;
    }
  }

  async function updatePerson(personGuid: string, dto: UpdatePersonDto) {
    loading.value = true;
    error.value = null;
    try {
      const person = await peopleService.update(personGuid, dto);

      const index = people.value.findIndex((p) => p.personGuid === personGuid);
      if (index !== -1) {
        people.value[index] = person;
      }

      upsertPersonListEntry(person);
      invalidateElectionStats();

      return person;
    } catch (e: any) {
      error.value = e.message || "Failed to update person";
      throw e;
    } finally {
      loading.value = false;
    }
  }

  async function deletePerson(personGuid: string) {
    loading.value = true;
    error.value = null;
    try {
      await peopleService.delete(personGuid);
      people.value = people.value.filter((p) => p.personGuid !== personGuid);
      peopleList.value = peopleList.value.filter(
        (p) => p.personGuid !== personGuid,
      );
      invalidateElectionStats();
    } catch (e: any) {
      error.value = e.message || "Failed to delete person";
      throw e;
    } finally {
      loading.value = false;
    }
  }

  async function searchPeople(electionGuid: string, query: string) {
    loading.value = true;
    error.value = null;
    try {
      return await peopleService.search(electionGuid, query);
    } catch (e: any) {
      error.value = e.message || "Failed to search people";
      throw e;
    } finally {
      loading.value = false;
    }
  }

  function clearError() {
    error.value = null;
  }

  function toPersonListDto(person: PersonDto): PersonListDto {
    return {
      personGuid: person.personGuid,
      fullName: person.fullName,
      email: person.email,
      phone: person.phone,
      area: person.area,
      canVote: person.canVote,
      canReceiveVotes: person.canReceiveVotes,
      ineligibleReasonCode: person.ineligibleReasonCode,
    };
  }

  function upsertPersonListEntry(person: PersonDto) {
    const listEntry = toPersonListDto(person);
    const index = peopleList.value.findIndex(
      (p) => p.personGuid === person.personGuid,
    );

    if (index !== -1) {
      peopleList.value[index] = listEntry;
    } else {
      peopleList.value.push(listEntry);
    }
  }

  function enrichPersonForSearch(person: PersonDto): SearchablePersonDto {
    // Primary searchable text: names + otherInfo.
    // Area is intentionally omitted so it cannot match on its own;
    // applyAllStrategies may still apply a small area bonus when a primary match exists.
    const searchText = [
      person.firstName || "",
      person.lastName || "",
      person.otherNames || "",
      person.otherLastNames || "",
      person.otherInfo || "",
    ]
      .filter(Boolean)
      .join(" ")
      .toLowerCase()
      .trim();

    const soundexCodes = person.combinedSoundCodes
      ? person.combinedSoundCodes
          .split(",")
          .map((code) => code.trim())
          .filter(Boolean)
      : [];

    return {
      ...person,
      _searchText: searchText,
      _soundexCodes: soundexCodes,
    };
  }

  async function initializePeopleCache(electionGuid: string) {
    if (isCacheInitialized.value) {
      return;
    }

    try {
      const allPeople = await peopleService.getAllForBallotEntry(electionGuid);
      peopleCache.value = allPeople.map(enrichPersonForSearch);
      isCacheInitialized.value = true;
    } catch (e) {
      console.error("Failed to initialize people cache:", e);
      throw e;
    }
  }

  function handlePersonVoteCountUpdated(data: PersonVoteCountUpdateEvent) {
    const index = peopleCache.value.findIndex(
      (p) => p.personGuid === data.personGuid,
    );
    if (index !== -1) {
      const newCache = [...peopleCache.value];
      newCache[index] = {
        ...peopleCache.value[index],
        voteCount: data.voteCount,
      };
      peopleCache.value = newCache;
    }
  }

  function normalizePersonUpdateEvent(data: unknown): PersonUpdateEvent | null {
    if (!data || typeof data !== "object") {
      return null;
    }
    const raw = data as Record<string, unknown>;
    const personGuid = String(raw.personGuid ?? "");
    if (!personGuid) {
      return null;
    }
    const actionRaw = String(raw.action ?? "updated");
    const action: PersonUpdateEvent["action"] =
      actionRaw === "added" || actionRaw === "deleted" ? actionRaw : "updated";
    return {
      electionGuid: String(raw.electionGuid ?? ""),
      personGuid,
      action,
      firstName: typeof raw.firstName === "string" ? raw.firstName : undefined,
      lastName: typeof raw.lastName === "string" ? raw.lastName : undefined,
      updatedAt:
        typeof raw.updatedAt === "string"
          ? raw.updatedAt
          : new Date().toISOString(),
    };
  }

  async function initializeSignalR() {
    if (signalrInitialized.value) {
      return;
    }

    try {
      const connection = await signalrService.connectToFrontDeskHub();

      // Server contract (SignalRNotificationService.SendPersonUpdateAsync):
      // PersonAdded / PersonUpdated / PersonDeleted with PersonUpdateDto payload.
      connection.on("PersonAdded", (data: unknown) => {
        const event = normalizePersonUpdateEvent(data);
        if (event) {
          void handlePersonAdded({ ...event, action: "added" });
        }
      });

      connection.on("PersonUpdated", (data: unknown) => {
        const event = normalizePersonUpdateEvent(data);
        if (event) {
          void handlePersonUpdated({ ...event, action: "updated" });
        }
      });

      connection.on("PersonDeleted", (data: unknown) => {
        const event = normalizePersonUpdateEvent(data);
        if (event) {
          handlePersonDeleted({ ...event, action: "deleted" });
        }
      });

      connection.on("reloadPage", () => {
        // Thin post-import (or bulk) signal: soft re-fetch instead of full page reload.
        void handleReloadPage();
      });

      connection.on(
        "PersonVoteCountUpdated",
        (data: PersonVoteCountUpdateEvent) => {
          handlePersonVoteCountUpdated(data);
        },
      );

      signalrInitialized.value = true;
    } catch (e) {
      console.error("Failed to initialize SignalR for people store:", e);
    }
  }

  async function handlePersonAdded(data: PersonUpdateEvent) {
    const exists = people.value.some((p) => p.personGuid === data.personGuid);
    if (!exists) {
      try {
        const person = await fetchPersonById(data.personGuid);
        if (person) {
          upsertPersonListEntry(person);
          if (isCacheInitialized.value) {
            peopleCache.value.push(enrichPersonForSearch(person));
          }
        }
      } catch (e) {
        console.error("Failed to handle person added:", e);
      }
    }
  }

  function onPersonUpdated(
    listener: (data: PersonUpdateEvent) => void,
  ): () => void {
    personUpdatedListeners.add(listener);
    return () => {
      personUpdatedListeners.delete(listener);
    };
  }

  function notifyPersonUpdatedListeners(data: PersonUpdateEvent) {
    for (const listener of personUpdatedListeners) {
      try {
        listener(data);
      } catch (e) {
        console.error("Person updated listener failed:", e);
      }
    }
  }

  async function handlePersonUpdated(data: PersonUpdateEvent) {
    try {
      const person = await fetchPersonById(data.personGuid);

      if (person) {
        upsertPersonListEntry(person);

        if (isCacheInitialized.value) {
          const index = peopleCache.value.findIndex(
            (p) => p.personGuid === data.personGuid,
          );
          const searchablePerson = enrichPersonForSearch(person);
          if (index !== -1) {
            peopleCache.value[index] = searchablePerson;
          } else {
            peopleCache.value.push(searchablePerson);
          }
        }
      }
    } catch (e) {
      console.error("Failed to handle person updated:", e);
    } finally {
      notifyPersonUpdatedListeners(data);
    }
  }

  function handlePersonDeleted(data: PersonUpdateEvent) {
    people.value = people.value.filter((p) => p.personGuid !== data.personGuid);
    peopleList.value = peopleList.value.filter(
      (p) => p.personGuid !== data.personGuid,
    );

    if (isCacheInitialized.value) {
      peopleCache.value = peopleCache.value.filter(
        (p) => p.personGuid !== data.personGuid,
      );
    }
  }

  async function handleReloadPage() {
    const guid = activeElectionGuid.value;
    if (!guid) {
      return;
    }
    try {
      await Promise.all([fetchPeopleList(guid), fetchPeople(guid)]);
      if (isCacheInitialized.value) {
        // Force cache rebuild so ballot-entry search matches post-import state.
        isCacheInitialized.value = false;
        await initializePeopleCache(guid);
      }
    } catch (e) {
      console.error("Failed to re-fetch people after reloadPage:", e);
    }
  }

  /**
   * FrontDesk hub only. Main hub membership is owned by electionStore /
   * MainLayout — do not call leaveElection (Main+FrontDesk) or stage
   * statusChanged stops after leaving people/ballots pages.
   */
  async function joinElection(electionGuid: string) {
    try {
      await signalrService.joinFrontDeskElection(electionGuid);
    } catch (e) {
      console.error("Failed to join election group for people updates:", e);
    }
  }

  async function leaveElection(electionGuid: string) {
    try {
      await signalrService.leaveFrontDeskElection(electionGuid);
    } catch (e) {
      console.error("Failed to leave election group for people updates:", e);
    }
  }

  return {
    people,
    peopleList,
    loading,
    error,
    voters,
    votablePeople,
    peopleCache,
    isCacheInitialized,
    fetchPeople,
    fetchPeopleList,
    fetchPersonById,
    createPerson,
    updatePerson,
    deletePerson,
    searchPeople,
    clearError,
    toPersonListDto,
    upsertPersonListEntry,
    enrichPersonForSearch,
    initializePeopleCache,
    initializeSignalR,
    joinElection,
    leaveElection,
    handlePersonAdded,
    handlePersonUpdated,
    onPersonUpdated,
    handlePersonDeleted,
    handlePersonVoteCountUpdated,
  };
});
