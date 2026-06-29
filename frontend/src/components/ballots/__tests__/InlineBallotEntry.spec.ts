import { describe, it, expect, beforeEach, vi } from "vitest";
import { mount, flushPromises } from "@vue/test-utils";
import { nextTick } from "vue";
import InlineBallotEntry from "../InlineBallotEntry.vue";
import type { BallotDto } from "@/types/Ballot";
import type { VoteDto } from "@/types/Vote";
import type { SearchablePersonDto } from "@/types/Person";
import { ElAlert, ElButton, ElIcon, ElInput } from "element-plus";

const mockT = (key: string, values?: Record<string, string | number>) => {
  const translations: Record<string, string> = {
    "ballots.cacheLoadError": "Failed to load candidates",
    "ballots.searchPlaceholder": "Search",
    "ballots.searchHelp": "Use arrow keys",
    "ballots.searchPerson": "Add a name",
    "ballots.namesOnBallot": "Names on the ballot",
    "ballots.ballotNum": "{code}",
    "ballots.noMatchesFound": "No matches found",
    "ballots.ballotFull": "Ballot is full",
    "ballots.keyboardTellerRequired":
      "Select the teller at keyboard before adding votes",
    "ballots.dragToReorder": "Drag votes to change their order",
    "ballots.addBallot": "Add Ballot",
    "ballots.deleteBallot": "Delete Ballot",
    "ballots.deleteConfirm": "Delete ballot {code}? All votes on it will be permanently removed.",
    "ballots.deleteSuccess": "Ballot deleted successfully",
    "ballots.createSuccess": "Ballot created successfully",
    "common.warning": "Warning",
    "common.cancel": "Cancel",
    "ballots.computerCodeRequired": "Computer code required",
    "ballots.locationRequired": "Location required",
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

vi.mock("@/composables/useNotifications", () => ({
  useNotifications: () => ({
    showWarningMessage: vi.fn(),
    showErrorMessage: vi.fn(),
    showSuccessMessage: vi.fn(),
    showInfoMessage: vi.fn(),
  }),
}));

const mockPeopleStore = {
  candidateCache: [] as SearchablePersonDto[],
  initializeCandidateCache: vi.fn(),
};

vi.mock("@/stores/peopleStore", () => ({
  usePeopleStore: () => mockPeopleStore,
}));

const mockUpdateBallot = vi.fn();
const mockCreateBallot = vi.fn();

vi.mock("@/stores/ballotStore", () => ({
  useBallotStore: () => ({
    updateBallot: mockUpdateBallot,
    createBallot: mockCreateBallot,
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

vi.mock("@/stores/locationStore", () => ({
  useLocationStore: () => ({
    selectedLocationGuid: "location-1",
  }),
}));

vi.mock("@/composables/useApiErrorHandler", () => ({
  useApiErrorHandler: () => ({
    handleApiError: vi.fn(),
  }),
}));

vi.mock("@/utils/activeTellerStorage", () => ({
  getActiveTellerPayload: () => ({
    teller1: "Alice",
    teller2: "Bob",
  }),
}));

vi.mock("@/composables/usePersonSearch", async () => {
  const { computed } = await import("vue");
  return {
    usePersonSearch: (
      searchQuery: { value: string },
      candidates: { value: SearchablePersonDto[] },
    ) => ({
      searchResults: computed(() => {
        const query = searchQuery.value?.toLowerCase() || "";
        if (!query) {
          return [];
        }
        return candidates.value.filter((candidate) =>
          candidate.fullName.toLowerCase().includes(query),
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
  let mockCandidates: SearchablePersonDto[];

  beforeEach(() => {
    vi.clearAllMocks();
    mockCandidates = [
      createMockPerson("John", "Doe"),
      createMockPerson("Jane", "Smith"),
      createMockPerson("Bob", "Johnson", {
        canReceiveVotes: false,
        ineligibleReasonCode: "X01",
      }),
    ];
    mockPeopleStore.candidateCache = mockCandidates;
    mockPeopleStore.initializeCandidateCache.mockResolvedValue(undefined);
  });

  it("initializes candidate cache on mount", async () => {
    mount(InlineBallotEntry, {
      props: {
        electionGuid: "election-123",
        ballot: createMockBallot(),
        requiredVotes: 9,
      },
      ...mountOptions,
    });

    await flushPromises();
    expect(mockPeopleStore.initializeCandidateCache).toHaveBeenCalledWith(
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
      .find((button) => button.text().includes("Add Ballot"));
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

  it("emits delete-ballot after confirmation", async () => {
    mockMessageBoxConfirm.mockResolvedValue(undefined);

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
    expect(wrapper.emitted("delete-ballot")?.[0]).toEqual(["ballot-123"]);
  });

  it("does not emit delete-ballot when confirmation is cancelled", async () => {
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

    expect(wrapper.emitted("delete-ballot")).toBeUndefined();
  });

  it("renders vote rows for required votes and existing votes", async () => {
    const votes: VoteDto[] = [
      {
        rowId: 1,
        ballotGuid: "ballot-123",
        positionOnBallot: 1,
        personGuid: mockCandidates[0].personGuid,
        personFullName: mockCandidates[0].fullName,
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
        personGuid: mockCandidates[2].personGuid,
        personFullName: mockCandidates[2].fullName,
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
        personGuid: mockCandidates[0].personGuid,
        personFullName: mockCandidates[0].fullName,
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
    expect(wrapper.text()).not.toContain(mockCandidates[0].fullName);
  });

  it("rebuilds vote rows when the ballot prop updates after a delete", async () => {
    const initialVotes: VoteDto[] = [
      {
        rowId: 1,
        ballotGuid: "ballot-123",
        positionOnBallot: 1,
        personGuid: mockCandidates[0].personGuid,
        personFullName: mockCandidates[0].fullName,
        statusCode: "ok",
      },
      {
        rowId: 2,
        ballotGuid: "ballot-123",
        positionOnBallot: 2,
        personGuid: mockCandidates[1].personGuid,
        personFullName: mockCandidates[1].fullName,
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
    expect(wrapper.text()).toContain(mockCandidates[1].fullName);

    await wrapper.setProps({
      ballot: createMockBallot([initialVotes[0]]),
    });
    await nextTick();

    expect(wrapper.text()).toContain(mockCandidates[0].fullName);
    expect(wrapper.text()).not.toContain(mockCandidates[1].fullName);
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
      personGuid: mockCandidates[0].personGuid,
      personFullName: mockCandidates[0].fullName,
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
        personGuid: mockCandidates[0].personGuid,
        personFullName: mockCandidates[0].fullName,
        statusCode: "ok",
      },
      {
        rowId: 11,
        ballotGuid: "ballot-123",
        positionOnBallot: 2,
        personGuid: mockCandidates[1].personGuid,
        personFullName: mockCandidates[1].fullName,
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

    expect(wrapper.find(".drag-handle").exists()).toBe(false);
    expect(wrapper.find(".votes-drag-hint").exists()).toBe(false);
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
        personGuid: mockCandidates[0].personGuid,
        personFullName: mockCandidates[0].fullName,
        statusCode: "ok",
      },
      {
        rowId: 11,
        ballotGuid: "ballot-123",
        positionOnBallot: 2,
        personGuid: mockCandidates[1].personGuid,
        personFullName: mockCandidates[1].fullName,
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
        personGuid: mockCandidates[0].personGuid,
        personFullName: mockCandidates[0].fullName,
        statusCode: "ok",
      },
      {
        rowId: 11,
        ballotGuid: "ballot-123",
        positionOnBallot: 2,
        personGuid: mockCandidates[1].personGuid,
        personFullName: mockCandidates[1].fullName,
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
        personGuid: mockCandidates[1].personGuid,
        personFullName: mockCandidates[1].fullName,
        statusCode: "ok",
      },
      {
        rowId: 10,
        ballotGuid: "ballot-123",
        positionOnBallot: 2,
        personGuid: mockCandidates[0].personGuid,
        personFullName: mockCandidates[0].fullName,
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
        personGuid: mockCandidates[0].personGuid,
        personFullName: mockCandidates[0].fullName,
        statusCode: "ok",
      },
      {
        rowId: 11,
        ballotGuid: "ballot-123",
        positionOnBallot: 2,
        personGuid: mockCandidates[1].personGuid,
        personFullName: mockCandidates[1].fullName,
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
});
