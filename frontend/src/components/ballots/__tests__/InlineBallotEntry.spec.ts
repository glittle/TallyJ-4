import type { BallotDto } from "@/types/Ballot";
import type { SearchablePersonDto } from "@/types/Person";
import type { VoteDto } from "@/types/Vote";
import { flushPromises, mount } from "@vue/test-utils";
import { ElAlert, ElButton, ElIcon, ElInput } from "element-plus";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { nextTick } from "vue";
import InlineBallotEntry from "../InlineBallotEntry.vue";

const mockT = (key: string, values?: Record<string, string | number>) => {
  const translations: Record<string, string> = {
    "ballots.cacheLoadError": "Failed to load names",
    "ballots.searchPlaceholder": "Search",
    "ballots.searchHelp": "Use arrow keys",
    "ballots.searchHelpRaw": "Find copied the name",
    "ballots.findRawName": "Find",
    "ballots.findRawNameHint": "Click again to widen the search",
    "ballots.changeRawName": "Change",
    "ballots.searchPerson": "Add a name",
    "ballots.namesOnBallot": "Names on the ballot",
    "ballots.ballotNum": "{code}",
    "ballots.noMatchesFound": "No matches found",
    "ballots.ballotFull": "Ballot is full",
    "ballots.keyboardTellerRequired":
      "Select the teller at keyboard before adding votes",
    "ballots.dragToReorder": "Drag votes to change their order",
    "ballots.addBallot": "Add Ballot",
    "ballots.addNextBallot": "Start another ballot",
    "ballots.addName": "Add a missing name or spoiled vote",
    "ballots.addNameDrawerTitle": "Add a name to this ballot",
    "ballots.setSpoiledOrNewName": "Set as spoiled vote or new name",
    "ballots.rawVoteNameReference": "Name entered on the ballot",
    "ballots.voteEntryType": "Vote entry type",
    "ballots.voteEntryNormal": "Normal vote",
    "ballots.voteEntryUnidentifiable": "Unidentifiable (U01)",
    "ballots.voteEntryUnreadable": "Unreadable (U02)",
    "ballots.personLessVoteHint":
      "No person record will be created. A spoiled vote will be recorded.",
    "common.save": "Save",
    "ballots.deleteBallot": "Delete Ballot",
    "ballots.deleteConfirm":
      "Delete ballot {code}? All votes on it will be permanently removed.",
    "ballots.deleteSuccess": "Ballot deleted successfully",
    "ballots.createSuccess": "Ballot created successfully",
    "common.warning": "Warning",
    "common.cancel": "Cancel",
    "ballots.computerCodeRequired": "Computer code required",
    "ballots.locationRequired": "Location is required",
    "ballots.onlineLocationNotAllowed":
      "Cannot start a ballot at the Online location",
    "ballots.tellerRequired": "Teller is required",
    "ballots.markNeedsReview": "Mark as Needs Review",
    "ballots.clearNeedsReview": "Clear Needs Review",
    "ballots.needsReviewUpdated": "Needs Review status updated",
    "ballots.needsReviewError": "Failed to update Needs Review status",
    "ballots.duplicateWarning": "Duplicate warning",
    "ballots.ineligible": "Ineligible",
    "common.delete": "Delete",
    "eligibility.X01": "Deceased",
    "eligibility.V04": "Rights removed (cannot be voted for)",
  };

  let result = translations[key] || key;
  if (values) {
    Object.entries(values).forEach(([name, value]) => {
      result = result.replace(`{${name}}`, String(value));
    });
  }
  return result;
};

vi.mock("vue-i18n", () => ({
  createI18n: vi.fn(),
  useI18n: () => ({
    t: mockT,
  }),
}));

const { mockShowErrorMessage, mockShowWarningMessage, mockShowSuccessMessage } =
  vi.hoisted(() => ({
    mockShowErrorMessage: vi.fn(),
    mockShowWarningMessage: vi.fn(),
    mockShowSuccessMessage: vi.fn(),
  }));

vi.mock("@/composables/useNotifications", () => ({
  useNotifications: () => ({
    showWarningMessage: mockShowWarningMessage,
    showErrorMessage: mockShowErrorMessage,
    showSuccessMessage: mockShowSuccessMessage,
    showInfoMessage: vi.fn(),
  }),
}));

const mockPeopleStore = {
  peopleCache: [] as SearchablePersonDto[],
  initializePeopleCache: vi.fn(),
};

vi.mock("@/stores/peopleStore", () => ({
  usePeopleStore: () => mockPeopleStore,
}));

const mockUpdateBallot = vi.fn();
const mockCreateBallot = vi.fn();
const mockDeleteBallot = vi.fn();

vi.mock("@/stores/ballotStore", () => ({
  useBallotStore: () => ({
    updateBallot: mockUpdateBallot,
    createBallot: mockCreateBallot,
    deleteBallot: mockDeleteBallot,
  }),
}));

const { mockMessageBoxConfirm } = vi.hoisted(() => ({
  mockMessageBoxConfirm: vi.fn(),
}));

vi.mock("element-plus", async (importOriginal) => {
  const actual = await importOriginal<typeof import("element-plus")>();
  return {
    ...actual,
    ElMessageBox: {
      confirm: mockMessageBoxConfirm,
    },
  };
});

vi.mock("@/composables/useComputerCode", () => ({
  useComputerCode: () => ({
    computerCode: { value: "WS01" },
  }),
}));

const { mockLocationStore } = vi.hoisted(() => ({
  mockLocationStore: {
    selectedLocationGuid: "location-1" as string | null,
    locations: [
      { locationGuid: "location-1", locationType: "Manual" as const },
      { locationGuid: "location-online", locationType: "Online" as const },
    ],
  },
}));

vi.mock("@/stores/locationStore", () => ({
  useLocationStore: () => mockLocationStore,
}));

vi.mock("@/composables/useApiErrorHandler", () => ({
  useApiErrorHandler: () => ({
    handleApiError: vi.fn(),
  }),
}));

const { mockActiveTellers } = vi.hoisted(() => ({
  mockActiveTellers: {
    teller1: "Alice",
    teller2: "Bob",
  },
}));

vi.mock("@/utils/activeTellerStorage", () => ({
  getActiveTellerPayload: () => ({
    teller1: mockActiveTellers.teller1 || undefined,
    teller2: mockActiveTellers.teller2 || undefined,
  }),
  getActiveTellers: () => ({ ...mockActiveTellers }),
}));

vi.mock("@/composables/usePersonSearch", async () => {
  const { computed } = await import("vue");
  return {
    usePersonSearch: (
      searchQuery: { value: string },
      searchablePeople: { value: SearchablePersonDto[] },
    ) => ({
      searchResults: computed(() => {
        const query = searchQuery.value?.toLowerCase() || "";
        if (!query) {
          return [];
        }
        return searchablePeople.value.filter((person) =>
          person.fullName.toLowerCase().includes(query),
        );
      }),
    }),
  };
});

function createMockPerson(
  firstName: string,
  lastName: string,
  options: Partial<SearchablePersonDto> = {},
): SearchablePersonDto {
  const fullName = `${firstName} ${lastName}`;
  return {
    personGuid: `guid-${firstName}-${lastName}`,
    firstName,
    lastName,
    fullName,
    _searchText: fullName.toLowerCase(),
    _soundexCodes: [],
    voteCount: 0,
    canReceiveVotes: true,
    ...options,
  };
}

function createMockBallot(votes: VoteDto[] = []): BallotDto {
  return {
    ballotGuid: "ballot-123",
    ballotCode: "B001",
    locationGuid: "location-1",
    locationName: "Main Hall",
    ballotNumAtComputer: 1,
    computerCode: "C01",
    statusCode: "Ok",
    voteCount: votes.length,
    votes,
  };
}

const mountOptions = {
  global: {
    components: {
      ElButton,
      ElAlert,
      ElInput,
      ElIcon,
      ElDrawer: {
        template:
          '<div v-if="modelValue" class="el-drawer"><slot></slot></div>',
        props: ["modelValue"],
      },
    },
    mocks: {
      $t: mockT,
    },
  },
};

describe("InlineBallotEntry", () => {
  let mockSearchablePeople: SearchablePersonDto[];

  beforeEach(() => {
    vi.clearAllMocks();
    mockLocationStore.selectedLocationGuid = "location-1";
    mockLocationStore.locations = [
      { locationGuid: "location-1", locationType: "Manual" },
      { locationGuid: "location-online", locationType: "Online" },
    ];
    mockActiveTellers.teller1 = "Alice";
    mockActiveTellers.teller2 = "Bob";
    mockSearchablePeople = [
      createMockPerson("John", "Doe"),
      createMockPerson("Jane", "Smith"),
      createMockPerson("Bob", "Johnson", {
        canReceiveVotes: false,
        ineligibleReasonCode: "X01",
      }),
    ];
    mockPeopleStore.peopleCache = mockSearchablePeople;
    mockPeopleStore.initializePeopleCache.mockResolvedValue(undefined);
  });

  it("initializes people cache on mount", async () => {
    mount(InlineBallotEntry, {
      props: {
        electionGuid: "election-123",
        ballot: createMockBallot(),
        requiredVotes: 9,
      },
      ...mountOptions,
    });

    await flushPromises();
    expect(mockPeopleStore.initializePeopleCache).toHaveBeenCalledWith(
      "election-123",
    );
  });

  it("creates a new ballot and emits ballot-created", async () => {
    mockCreateBallot.mockResolvedValue({
      ballotGuid: "ballot-new",
      ballotCode: "B002",
    });

    const wrapper = mount(InlineBallotEntry, {
      props: {
        electionGuid: "election-123",
        ballot: createMockBallot(),
        requiredVotes: 9,
      },
      ...mountOptions,
    });

    await flushPromises();

    const addBallotButton = wrapper
      .findAllComponents(ElButton)
      .find((button) => button.text().includes("Start another ballot"));
    expect(addBallotButton).toBeDefined();
    await addBallotButton!.trigger("click");
    await flushPromises();

    expect(mockCreateBallot).toHaveBeenCalledWith({
      electionGuid: "election-123",
      computerCode: "WS01",
      locationGuid: "location-1",
      teller1: "Alice",
      teller2: "Bob",
    });
    expect(wrapper.emitted("ballot-created")?.[0]).toEqual(["ballot-new"]);
  });

  it("does not start another ballot when location is unset", async () => {
    mockLocationStore.selectedLocationGuid = null;

    const wrapper = mount(InlineBallotEntry, {
      props: {
        electionGuid: "election-123",
        ballot: createMockBallot(),
        requiredVotes: 9,
      },
      ...mountOptions,
    });

    await flushPromises();

    const addBallotButton = wrapper
      .findAllComponents(ElButton)
      .find((button) => button.text().includes("Start another ballot"));
    await addBallotButton!.trigger("click");
    await flushPromises();

    expect(mockCreateBallot).not.toHaveBeenCalled();
    expect(mockShowErrorMessage).toHaveBeenCalledWith("Location is required");
    expect(wrapper.emitted("ballot-start-blocked")?.[0]).toEqual(["location"]);
  });

  it("disables Start another ballot when the selected location is Online", async () => {
    mockLocationStore.selectedLocationGuid = "location-online";

    const wrapper = mount(InlineBallotEntry, {
      props: {
        electionGuid: "election-123",
        ballot: createMockBallot(),
        requiredVotes: 9,
      },
      ...mountOptions,
    });

    await flushPromises();

    const addBallotButton = wrapper
      .findAllComponents(ElButton)
      .find((button) => button.text().includes("Start another ballot"));
    expect(addBallotButton).toBeDefined();
    expect(addBallotButton!.props("disabled")).toBe(true);
    expect(addBallotButton!.attributes("title")).toBe(
      "Cannot start a ballot at the Online location",
    );

    await addBallotButton!.trigger("click");
    await flushPromises();
    expect(mockCreateBallot).not.toHaveBeenCalled();
  });

  it("does not start another ballot when the main teller is unset", async () => {
    mockActiveTellers.teller1 = "";

    const wrapper = mount(InlineBallotEntry, {
      props: {
        electionGuid: "election-123",
        ballot: createMockBallot(),
        requiredVotes: 9,
        hasKeyboardTeller: false,
      },
      ...mountOptions,
    });

    await flushPromises();

    const addBallotButton = wrapper
      .findAllComponents(ElButton)
      .find((button) => button.text().includes("Start another ballot"));
    await addBallotButton!.trigger("click");
    await flushPromises();

    expect(mockCreateBallot).not.toHaveBeenCalled();
    expect(mockShowErrorMessage).toHaveBeenCalledWith("Teller is required");
    expect(wrapper.emitted("ballot-start-blocked")?.[0]).toEqual(["teller"]);
  });

  it("deletes ballot and emits ballot-deleted after confirmation", async () => {
    mockMessageBoxConfirm.mockResolvedValue(undefined);
    mockDeleteBallot.mockResolvedValue(undefined);

    const wrapper = mount(InlineBallotEntry, {
      props: {
        electionGuid: "election-123",
        ballot: createMockBallot(),
        requiredVotes: 9,
      },
      ...mountOptions,
    });

    await flushPromises();

    const deleteButton = wrapper
      .findAllComponents(ElButton)
      .find((button) => button.text().includes("Delete Ballot"));
    expect(deleteButton).toBeDefined();
    await deleteButton!.trigger("click");
    await flushPromises();

    expect(mockMessageBoxConfirm).toHaveBeenCalledWith(
      "Delete ballot B001? All votes on it will be permanently removed.",
      "Warning",
      expect.objectContaining({
        confirmButtonText: "Delete",
        cancelButtonText: "Cancel",
        type: "warning",
      }),
    );
    expect(mockDeleteBallot).toHaveBeenCalledWith("ballot-123");
    expect(wrapper.emitted("ballot-deleted")?.[0]).toEqual(["ballot-123"]);
  });

  it("does not delete ballot when confirmation is cancelled", async () => {
    mockMessageBoxConfirm.mockRejectedValue("cancel");

    const wrapper = mount(InlineBallotEntry, {
      props: {
        electionGuid: "election-123",
        ballot: createMockBallot(),
        requiredVotes: 9,
      },
      ...mountOptions,
    });

    await flushPromises();

    const deleteButton = wrapper
      .findAllComponents(ElButton)
      .find((button) => button.text().includes("Delete Ballot"));
    await deleteButton!.trigger("click");
    await flushPromises();

    expect(mockDeleteBallot).not.toHaveBeenCalled();
    expect(wrapper.emitted("ballot-deleted")).toBeUndefined();
  });

  it("renders vote rows for required votes and existing votes", async () => {
    const votes: VoteDto[] = [
      {
        rowId: 1,
        ballotGuid: "ballot-123",
        positionOnBallot: 1,
        personGuid: mockSearchablePeople[0].personGuid,
        personFullName: mockSearchablePeople[0].fullName,
        statusCode: "ok",
      },
    ];

    const wrapper = mount(InlineBallotEntry, {
      props: {
        electionGuid: "election-123",
        ballot: createMockBallot(votes),
        requiredVotes: 9,
      },
      ...mountOptions,
    });

    await flushPromises();
    expect(wrapper.findAll(".vote-row").length).toBe(9);
    expect(wrapper.text()).toContain("John Doe");
  });

  it("emits vote-added when a person is selected from search", async () => {
    const wrapper = mount(InlineBallotEntry, {
      props: {
        electionGuid: "election-123",
        ballot: createMockBallot(),
        requiredVotes: 9,
        hasKeyboardTeller: true,
      },
      ...mountOptions,
    });

    await flushPromises();
    await wrapper.find(".search-input input").setValue("John");
    await nextTick();
    await wrapper.find(".search-result-item").trigger("click");
    await nextTick();

    expect(wrapper.emitted("vote-added")).toBeTruthy();
    const emitted = wrapper.emitted("vote-added") as VoteDto[][];
    expect(emitted[0][0].personFullName).toBe("John Doe");
    expect(emitted[0][0].positionOnBallot).toBe(1);
  });

  it("blocks vote additions when teller at keyboard is not set", async () => {
    const wrapper = mount(InlineBallotEntry, {
      props: {
        electionGuid: "election-123",
        ballot: createMockBallot(),
        requiredVotes: 9,
        hasKeyboardTeller: false,
      },
      ...mountOptions,
    });

    await flushPromises();
    expect(wrapper.find(".keyboard-teller-alert").exists()).toBe(true);
    expect(
      wrapper.find(".search-input input").attributes("disabled"),
    ).toBeDefined();
    expect(wrapper.findAll(".search-result-item").length).toBe(0);
    expect(wrapper.emitted("vote-added")).toBeFalsy();
  });

  it("shows spoiled vote icon and eligibility label under the name", async () => {
    const votes: VoteDto[] = [
      {
        rowId: 2,
        ballotGuid: "ballot-123",
        positionOnBallot: 1,
        personGuid: mockSearchablePeople[2].personGuid,
        personFullName: mockSearchablePeople[2].fullName,
        statusCode: "Spoiled",
        ineligibleReasonCode: "X01",
      },
    ];

    const wrapper = mount(InlineBallotEntry, {
      props: {
        electionGuid: "election-123",
        ballot: createMockBallot(votes),
        requiredVotes: 9,
      },
      ...mountOptions,
    });

    await flushPromises();
    expect(wrapper.find(".vote-name.is-spoiled").exists()).toBe(true);
    expect(wrapper.find(".vote-ineligible-reason").text()).toBe("Deceased");
  });

  it("adds ineligible search results as spoiled votes", async () => {
    const wrapper = mount(InlineBallotEntry, {
      props: {
        electionGuid: "election-123",
        ballot: createMockBallot(),
        requiredVotes: 9,
        hasKeyboardTeller: true,
      },
      ...mountOptions,
    });

    await flushPromises();
    await wrapper.find(".search-input input").setValue("Bob");
    await nextTick();
    await wrapper.find(".search-result-item").trigger("click");
    await nextTick();

    const emitted = wrapper.emitted("vote-added") as VoteDto[][];
    expect(emitted[0][0].statusCode).toBe("Spoiled");
    expect(emitted[0][0].ineligibleReasonCode).toBe("X01");
    expect(emitted[0][0].personFullName).toBe("Bob Johnson");
  });

  it("emits vote-removed when delete is clicked", async () => {
    const votes: VoteDto[] = [
      {
        rowId: 3,
        ballotGuid: "ballot-123",
        positionOnBallot: 1,
        personGuid: mockSearchablePeople[0].personGuid,
        personFullName: mockSearchablePeople[0].fullName,
        statusCode: "ok",
      },
    ];

    const wrapper = mount(InlineBallotEntry, {
      props: {
        electionGuid: "election-123",
        ballot: createMockBallot(votes),
        requiredVotes: 9,
      },
      ...mountOptions,
    });

    await flushPromises();
    await wrapper.find(".vote-actions .el-button").trigger("click");
    expect(wrapper.emitted("vote-removed")).toEqual([[1]]);
    expect(wrapper.text()).not.toContain(mockSearchablePeople[0].fullName);
  });

  it("rebuilds vote rows when the ballot prop updates after a delete", async () => {
    const initialVotes: VoteDto[] = [
      {
        rowId: 1,
        ballotGuid: "ballot-123",
        positionOnBallot: 1,
        personGuid: mockSearchablePeople[0].personGuid,
        personFullName: mockSearchablePeople[0].fullName,
        statusCode: "ok",
      },
      {
        rowId: 2,
        ballotGuid: "ballot-123",
        positionOnBallot: 2,
        personGuid: mockSearchablePeople[1].personGuid,
        personFullName: mockSearchablePeople[1].fullName,
        statusCode: "ok",
      },
    ];

    const wrapper = mount(InlineBallotEntry, {
      props: {
        electionGuid: "election-123",
        ballot: createMockBallot(initialVotes),
        requiredVotes: 9,
      },
      ...mountOptions,
    });

    await flushPromises();
    expect(wrapper.text()).toContain(mockSearchablePeople[1].fullName);

    await wrapper.setProps({
      ballot: createMockBallot([initialVotes[0]]),
    });
    await nextTick();

    expect(wrapper.text()).toContain(mockSearchablePeople[0].fullName);
    expect(wrapper.text()).not.toContain(mockSearchablePeople[1].fullName);
  });

  it("shows drag handle after an optimistic vote is saved on the ballot prop", async () => {
    const wrapper = mount(InlineBallotEntry, {
      props: {
        electionGuid: "election-123",
        ballot: createMockBallot(),
        requiredVotes: 9,
        hasKeyboardTeller: true,
      },
      ...mountOptions,
    });

    await flushPromises();
    await wrapper.find(".search-input input").setValue("John");
    await nextTick();
    await wrapper.find(".search-result-item").trigger("click");
    await nextTick();

    expect(wrapper.find(".drag-handle").exists()).toBe(false);

    const optimisticVote: VoteDto = {
      rowId: 42,
      ballotGuid: "ballot-123",
      positionOnBallot: 1,
      personGuid: mockSearchablePeople[0].personGuid,
      personFullName: mockSearchablePeople[0].fullName,
      statusCode: "ok",
    };

    await wrapper.setProps({
      ballot: createMockBallot([optimisticVote]),
    });
    await nextTick();

    expect(wrapper.find(".drag-handle").exists()).toBe(true);
    expect(wrapper.find(".vote-row").classes()).toContain("is-draggable");
  });

  it("disables reordering while an optimistic vote is still saving", async () => {
    const persistedVotes: VoteDto[] = [
      {
        rowId: 10,
        ballotGuid: "ballot-123",
        positionOnBallot: 1,
        personGuid: mockSearchablePeople[0].personGuid,
        personFullName: mockSearchablePeople[0].fullName,
        statusCode: "ok",
      },
      {
        rowId: 11,
        ballotGuid: "ballot-123",
        positionOnBallot: 2,
        personGuid: mockSearchablePeople[1].personGuid,
        personFullName: mockSearchablePeople[1].fullName,
        statusCode: "ok",
      },
    ];

    const wrapper = mount(InlineBallotEntry, {
      props: {
        electionGuid: "election-123",
        ballot: createMockBallot(persistedVotes),
        requiredVotes: 9,
        hasKeyboardTeller: true,
      },
      ...mountOptions,
    });

    await flushPromises();
    await wrapper.find(".search-input input").setValue("Bob");
    await nextTick();
    await wrapper.find(".search-result-item").trigger("click");
    await nextTick();

    // Handles stay visible on already-saved votes so the list does not jump;
    // interaction is disabled until the optimistic vote is persisted.
    const handles = wrapper.findAll(".drag-handle");
    expect(handles.length).toBe(2);
    expect(handles.every((h) => h.classes().includes("is-inactive"))).toBe(
      true,
    );
    expect(wrapper.find(".votes-drag-hint").exists()).toBe(true);
    expect(wrapper.findAll(".vote-row.is-draggable")).toHaveLength(0);

    const rows = wrapper.findAll(".vote-row");
    await rows[1].trigger("dragstart");
    await rows[0].trigger("dragover");
    await rows[0].trigger("drop");

    expect(wrapper.emitted("votes-reordered")).toBeUndefined();
  });

  it("emits votes-reordered when a persisted vote is dropped on another vote", async () => {
    const votes: VoteDto[] = [
      {
        rowId: 10,
        ballotGuid: "ballot-123",
        positionOnBallot: 1,
        personGuid: mockSearchablePeople[0].personGuid,
        personFullName: mockSearchablePeople[0].fullName,
        statusCode: "ok",
      },
      {
        rowId: 11,
        ballotGuid: "ballot-123",
        positionOnBallot: 2,
        personGuid: mockSearchablePeople[1].personGuid,
        personFullName: mockSearchablePeople[1].fullName,
        statusCode: "ok",
      },
    ];

    const wrapper = mount(InlineBallotEntry, {
      props: {
        electionGuid: "election-123",
        ballot: createMockBallot(votes),
        requiredVotes: 9,
      },
      ...mountOptions,
    });

    await flushPromises();

    const rows = wrapper.findAll(".vote-row");
    await rows[1].trigger("dragstart");
    await rows[0].trigger("dragover");
    await rows[0].trigger("drop");

    expect(wrapper.emitted("votes-reordered")).toEqual([[[11, 10]]]);
  });

  it("keeps reordering disabled after drop until ballot updates", async () => {
    const initialVotes: VoteDto[] = [
      {
        rowId: 10,
        ballotGuid: "ballot-123",
        positionOnBallot: 1,
        personGuid: mockSearchablePeople[0].personGuid,
        personFullName: mockSearchablePeople[0].fullName,
        statusCode: "ok",
      },
      {
        rowId: 11,
        ballotGuid: "ballot-123",
        positionOnBallot: 2,
        personGuid: mockSearchablePeople[1].personGuid,
        personFullName: mockSearchablePeople[1].fullName,
        statusCode: "ok",
      },
    ];

    const wrapper = mount(InlineBallotEntry, {
      props: {
        electionGuid: "election-123",
        ballot: createMockBallot(initialVotes),
        requiredVotes: 9,
      },
      ...mountOptions,
    });

    await flushPromises();

    const rows = wrapper.findAll(".vote-row");
    await rows[1].trigger("dragstart");
    await rows[0].trigger("dragover");
    await rows[0].trigger("drop");
    await rows[1].trigger("dragend");
    await nextTick();

    expect(wrapper.emitted("votes-reordered")).toEqual([[[11, 10]]]);
    expect((wrapper.vm as { reorderingVotes: boolean }).reorderingVotes).toBe(
      true,
    );

    const reorderedVotes: VoteDto[] = [
      {
        rowId: 11,
        ballotGuid: "ballot-123",
        positionOnBallot: 1,
        personGuid: mockSearchablePeople[1].personGuid,
        personFullName: mockSearchablePeople[1].fullName,
        statusCode: "ok",
      },
      {
        rowId: 10,
        ballotGuid: "ballot-123",
        positionOnBallot: 2,
        personGuid: mockSearchablePeople[0].personGuid,
        personFullName: mockSearchablePeople[0].fullName,
        statusCode: "ok",
      },
    ];

    await wrapper.setProps({ ballot: createMockBallot(reorderedVotes) });
    await nextTick();

    expect((wrapper.vm as { reorderingVotes: boolean }).reorderingVotes).toBe(
      false,
    );
  });

  it("re-enables reordering when resyncKey bumps after a failed reorder", async () => {
    const votes: VoteDto[] = [
      {
        rowId: 10,
        ballotGuid: "ballot-123",
        positionOnBallot: 1,
        personGuid: mockSearchablePeople[0].personGuid,
        personFullName: mockSearchablePeople[0].fullName,
        statusCode: "ok",
      },
      {
        rowId: 11,
        ballotGuid: "ballot-123",
        positionOnBallot: 2,
        personGuid: mockSearchablePeople[1].personGuid,
        personFullName: mockSearchablePeople[1].fullName,
        statusCode: "ok",
      },
    ];

    const wrapper = mount(InlineBallotEntry, {
      props: {
        electionGuid: "election-123",
        ballot: createMockBallot(votes),
        requiredVotes: 9,
        resyncKey: 0,
      },
      ...mountOptions,
    });

    await flushPromises();

    const rows = wrapper.findAll(".vote-row");
    await rows[1].trigger("dragstart");
    await rows[0].trigger("dragover");
    await rows[0].trigger("drop");
    await rows[1].trigger("dragend");
    await nextTick();

    expect((wrapper.vm as { reorderingVotes: boolean }).reorderingVotes).toBe(
      true,
    );

    await wrapper.setProps({ resyncKey: 1 });
    await nextTick();

    expect((wrapper.vm as { reorderingVotes: boolean }).reorderingVotes).toBe(
      false,
    );
  });

  it("shows the voter-entered name and copies it into search on Find", async () => {
    const votes: VoteDto[] = [
      {
        rowId: 11,
        ballotGuid: "ballot-123",
        positionOnBallot: 1,
        statusCode: "Raw",
        onlineVoteRaw:
          '{"First":"Jonathan","Last":"Smythe","OtherInfo":"Jonathan Smythe"}',
      },
    ];

    const wrapper = mount(InlineBallotEntry, {
      props: {
        electionGuid: "election-123",
        ballot: {
          ...createMockBallot(votes),
          computerCode: "OL",
        },
        requiredVotes: 9,
        hasKeyboardTeller: true,
      },
      ...mountOptions,
    });

    await flushPromises();
    expect(wrapper.find(".raw-name").text()).toBe("Jonathan Smythe");
    expect(wrapper.find(".vote-name").exists()).toBe(false);
    expect(wrapper.find(".vote-row").classes()).toContain("is-raw-unresolved");
    expect(wrapper.find(".needs-resolution").exists()).toBe(false);
    expect(wrapper.find(".raw-vote .raw-find-btn").text()).toContain("Find");

    const findButton = wrapper
      .findAllComponents(ElButton)
      .find((button) => button.text().includes("Find"));
    expect(findButton).toBeDefined();
    await findButton!.trigger("click");
    await nextTick();

    const searchInput = wrapper.find(".search-input input")
      .element as HTMLInputElement;
    expect(searchInput.value).toBe("Jonathan Smythe");

    await findButton!.trigger("click");
    await nextTick();
    expect(searchInput.value).toBe("Jonatha Smyth");
  });

  it("assigns a search match to the targeted raw vote instead of adding a line", async () => {
    const votes: VoteDto[] = [
      {
        rowId: 11,
        ballotGuid: "ballot-123",
        positionOnBallot: 1,
        statusCode: "Raw",
        onlineVoteRaw: '{"First":"John","Last":"Doe","OtherInfo":"John Doe"}',
      },
    ];

    const wrapper = mount(InlineBallotEntry, {
      props: {
        electionGuid: "election-123",
        ballot: {
          ...createMockBallot(votes),
          computerCode: "OL",
        },
        requiredVotes: 9,
        hasKeyboardTeller: true,
      },
      ...mountOptions,
    });

    await flushPromises();
    await wrapper.find(".search-input input").setValue("John");
    await nextTick();
    await wrapper.find(".search-result-item").trigger("click");
    await nextTick();

    expect(wrapper.emitted("vote-added")).toBeFalsy();
    const updated = wrapper.emitted("vote-updated") as VoteDto[][];
    expect(updated[0][0].rowId).toBe(11);
    expect(updated[0][0].personFullName).toBe("John Doe");
    expect(updated[0][0].onlineVoteRaw).toContain("John");
  });

  it("keeps the raw name in normal weight and shows the matched name after entry", async () => {
    const votes: VoteDto[] = [
      {
        rowId: 12,
        ballotGuid: "ballot-123",
        positionOnBallot: 1,
        statusCode: "ok",
        personGuid: mockSearchablePeople[0].personGuid,
        personFullName: "Abbas, Cyrus",
        onlineVoteRaw: '{"First":"cyrus","Last":"rus","OtherInfo":"cyrus rus"}',
      },
    ];

    const wrapper = mount(InlineBallotEntry, {
      props: {
        electionGuid: "election-123",
        ballot: {
          ...createMockBallot(votes),
          computerCode: "OL",
        },
        requiredVotes: 9,
        hasKeyboardTeller: true,
      },
      ...mountOptions,
    });

    await flushPromises();
    expect(wrapper.find(".raw-name").text()).toBe("cyrus rus");
    expect(wrapper.find(".vote-name").text()).toBe("Abbas, Cyrus");
    expect(wrapper.find(".vote-row").classes()).not.toContain(
      "is-raw-unresolved",
    );
    expect(wrapper.find(".needs-resolution").exists()).toBe(false);
    const findButton = wrapper.find(".vote-actions .raw-find-btn");
    expect(findButton.exists()).toBe(true);
    expect(wrapper.find(".raw-vote .raw-find-btn").exists()).toBe(false);
  });

  it("hides delete and reorder controls on online ballots", async () => {
    const votes: VoteDto[] = [
      {
        rowId: 12,
        ballotGuid: "ballot-123",
        positionOnBallot: 1,
        statusCode: "ok",
        personGuid: mockSearchablePeople[0].personGuid,
        personFullName: "Abbas, Cyrus",
        onlineVoteRaw: '{"First":"cyrus","Last":"rus","OtherInfo":"cyrus rus"}',
      },
    ];

    const wrapper = mount(InlineBallotEntry, {
      props: {
        electionGuid: "election-123",
        ballot: {
          ...createMockBallot(votes),
          computerCode: "OL",
        },
        requiredVotes: 9,
        hasKeyboardTeller: true,
      },
      ...mountOptions,
    });

    await flushPromises();
    expect(wrapper.find(".delete-ballot-action").exists()).toBe(false);
    expect(wrapper.find(".vote-actions [aria-label='Delete']").exists()).toBe(
      false,
    );
    expect(wrapper.find(".vote-actions .raw-find-btn").exists()).toBe(true);
    expect(wrapper.find(".vote-actions .raw-find-btn").text()).toBe("Change");
    expect(wrapper.find(".drag-handle").exists()).toBe(false);
    expect(wrapper.find(".votes-drag-hint").exists()).toBe(false);
    expect(wrapper.find(".add-name-action").exists()).toBe(false);
  });

  it("hides Add a missing name on online ballots until Find is used", async () => {
    const votes: VoteDto[] = [
      {
        rowId: 20,
        ballotGuid: "ballot-123",
        positionOnBallot: 1,
        statusCode: "ok",
        personGuid: mockSearchablePeople[0].personGuid,
        personFullName: "John Doe",
        onlineVoteRaw: '{"First":"John","Last":"Doe","OtherInfo":"John Doe"}',
      },
    ];

    const wrapper = mount(InlineBallotEntry, {
      props: {
        electionGuid: "election-123",
        ballot: {
          ...createMockBallot(votes),
          computerCode: "OL",
        },
        requiredVotes: 9,
        hasKeyboardTeller: true,
      },
      ...mountOptions,
    });

    await flushPromises();
    expect(wrapper.find(".add-name-action").exists()).toBe(false);

    await wrapper.find(".vote-row.has-vote").trigger("click");
    await nextTick();
    expect(wrapper.find(".add-name-action").exists()).toBe(false);

    await wrapper.find(".vote-actions .raw-find-btn").trigger("click");
    await nextTick();

    expect(wrapper.find(".add-name-action").exists()).toBe(true);
    expect(wrapper.find(".add-name-action").text()).toBe(
      "Set as spoiled vote or new name",
    );
    expect(wrapper.find(".add-name-action .el-icon").exists()).toBe(false);
    const actionBlocks = wrapper
      .findAll(".add-name-action, .new-ballot-action")
      .map((block) => block.classes());
    expect(actionBlocks[0]).toContain("add-name-action");
    expect(actionBlocks[1]).toContain("new-ballot-action");
  });

  it("applies a spoiled vote to the selected online vote instead of adding a line", async () => {
    const votes: VoteDto[] = [
      {
        rowId: 21,
        ballotGuid: "ballot-123",
        positionOnBallot: 1,
        statusCode: "Raw",
        onlineVoteRaw: '{"First":"Jon","Last":"Smyth","OtherInfo":"Jon Smyth"}',
      },
    ];

    const wrapper = mount(InlineBallotEntry, {
      props: {
        electionGuid: "election-123",
        ballot: {
          ...createMockBallot(votes),
          computerCode: "IM",
        },
        requiredVotes: 9,
        hasKeyboardTeller: true,
      },
      ...mountOptions,
    });

    await flushPromises();
    expect(wrapper.find(".add-name-action").exists()).toBe(false);

    await wrapper.find(".raw-find-btn").trigger("click");
    await nextTick();
    expect(wrapper.find(".add-name-action").exists()).toBe(true);

    await wrapper.find(".add-name-action button").trigger("click");
    await nextTick();

    expect(wrapper.find(".ballot-add-person-panel__raw-value").text()).toBe(
      "Jon Smyth",
    );

    const saveButton = wrapper
      .findAllComponents(ElButton)
      .find((button) => button.text().includes("Save"));
    expect(saveButton).toBeDefined();
    await saveButton!.trigger("click");
    await flushPromises();

    expect(wrapper.emitted("vote-added")).toBeFalsy();
    const updated = wrapper.emitted("vote-updated") as VoteDto[][];
    expect(updated[0][0].rowId).toBe(21);
    expect(updated[0][0].statusCode).toBe("Spoiled");
    expect(updated[0][0].ineligibleReasonCode).toBe("U01");
    expect(updated[0][0].personGuid).toBeUndefined();
    expect(updated[0][0].onlineVoteRaw).toContain("Jon");
  });

  it("places Clear Needs Review above Start another ballot", async () => {
    const reviewBallot = {
      ...createMockBallot(),
      statusCode: "Review",
    };

    const wrapper = mount(InlineBallotEntry, {
      props: {
        electionGuid: "election-123",
        ballot: reviewBallot,
        requiredVotes: 9,
        hasKeyboardTeller: true,
      },
      ...mountOptions,
    });

    await flushPromises();

    const actionBlocks = wrapper
      .findAll(
        ".needs-review-toggle, .new-ballot-action, .add-name-action, .delete-ballot-action",
      )
      .map((block) => block.classes());

    expect(actionBlocks[0]).toContain("needs-review-toggle");
    expect(actionBlocks[1]).toContain("new-ballot-action");
    expect(wrapper.find(".needs-review-toggle").text()).toContain(
      "Clear Needs Review",
    );
  });

  it("keeps Mark as Needs Review below Start another ballot", async () => {
    const wrapper = mount(InlineBallotEntry, {
      props: {
        electionGuid: "election-123",
        ballot: createMockBallot(),
        requiredVotes: 9,
        hasKeyboardTeller: true,
      },
      ...mountOptions,
    });

    await flushPromises();

    const actionBlocks = wrapper
      .findAll(
        ".needs-review-toggle, .new-ballot-action, .add-name-action, .delete-ballot-action",
      )
      .map((block) => block.classes());

    expect(actionBlocks[0]).toContain("new-ballot-action");
    expect(actionBlocks).toContainEqual(
      expect.arrayContaining(["needs-review-toggle"]),
    );
    const reviewIndex = actionBlocks.findIndex((classes) =>
      classes.includes("needs-review-toggle"),
    );
    expect(reviewIndex).toBeGreaterThan(0);
    expect(wrapper.find(".needs-review-toggle").text()).toContain(
      "Mark as Needs Review",
    );
    expect(wrapper.find(".add-name-action").exists()).toBe(true);
  });
});
