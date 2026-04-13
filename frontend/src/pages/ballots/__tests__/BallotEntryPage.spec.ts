import { describe, it, expect, vi, beforeEach } from "vitest";
import { mount, flushPromises } from "@vue/test-utils";
import { createPinia, setActivePinia } from "pinia";
import { createI18n } from "vue-i18n";
import BallotEntryPage from "../BallotEntryPage.vue";
import { useBallotStore } from "@/stores/ballotStore";
import { useElectionStore } from "@/stores/electionStore";
import { usePeopleStore } from "@/stores/peopleStore";
import type { BallotDto, ElectionDto, VoteDto } from "@/types";

vi.mock("vue-router", () => ({
  useRouter: () => ({
    push: vi.fn(),
  }),
  useRoute: () => ({
    params: {
      id: "test-election-guid",
      ballotId: "test-ballot-guid",
    },
  }),
}));

vi.mock("@/components/ballots/InlineBallotEntry.vue", () => ({
  default: {
    name: "InlineBallotEntry",
    template:
      '<div class="inline-ballot-entry-mock" data-testid="inline-ballot-entry"></div>',
    props: {
      electionGuid: String,
      ballot: Object,
      maxVotes: Number,
    },
    emits: ["vote-added", "vote-removed", "ballot-saved"],
  },
}));

describe("BallotEntryPage", () => {
  let wrapper: any;
  let ballotStore: any;
  let electionStore: any;
  let peopleStore: any;

  const mockBallot: BallotDto = {
    ballotGuid: "test-ballot-guid",
    locationGuid: "test-location-guid",
    ballotCode: "B001",
    locationName: "Test Location",
    ballotNumAtComputer: 1,
    computerCode: "C001",
    statusCode: "Ok",
    teller1: "John Doe",
    teller2: "Jane Smith",
    voteCount: 2,
    votes: [
      {
        rowId: 1,
        ballotGuid: "test-ballot-guid",
        positionOnBallot: 1,
        personGuid: "person-1",
        personFullName: "Alice Johnson",
        statusCode: "Ok",
      },
      {
        rowId: 2,
        ballotGuid: "test-ballot-guid",
        positionOnBallot: 2,
        personGuid: "person-2",
        personFullName: "Bob Williams",
        statusCode: "Ok",
      },
    ],
  };

  const mockElection: ElectionDto = {
    electionGuid: "test-election-guid",
    name: "Test Election",
    numberToElect: 9,
    tallyStatus: "InProgress",
    voterCount: 100,
    ballotCount: 50,
    locationCount: 5,
  };

  beforeEach(() => {
    setActivePinia(createPinia());

    ballotStore = useBallotStore();
    electionStore = useElectionStore();
    peopleStore = usePeopleStore();

    vi.spyOn(ballotStore, "initializeSignalR").mockResolvedValue(undefined);
    vi.spyOn(ballotStore, "joinElection").mockResolvedValue(undefined);
    vi.spyOn(ballotStore, "leaveElection").mockResolvedValue(undefined);
    vi.spyOn(ballotStore, "fetchBallotById").mockImplementation(async () => {
      ballotStore.currentBallot = mockBallot;
      return mockBallot;
    });
    vi.spyOn(ballotStore, "createVote").mockResolvedValue({} as VoteDto);
    vi.spyOn(ballotStore, "deleteVote").mockResolvedValue(undefined);

    vi.spyOn(electionStore, "fetchElectionById").mockImplementation(
      async () => {
        electionStore.currentElection = mockElection;
        return mockElection;
      },
    );

    vi.spyOn(peopleStore, "initializeSignalR").mockResolvedValue(undefined);
    vi.spyOn(peopleStore, "joinElection").mockResolvedValue(undefined);
    vi.spyOn(peopleStore, "leaveElection").mockResolvedValue(undefined);

    ballotStore.loading = false;
    electionStore.loading = false;

    const i18n = createI18n({
      legacy: false,
      locale: "en",
      messages: {
        en: {
          ballots: {
            entry: "Ballot Entry - {code}",
            location: "Location",
            status: "Status",
            teller1: "Teller 1",
            teller2: "Teller 2",
            voteCount: "Vote Count",
            loadError: "Failed to load ballots",
            voteAddedSuccess: "Vote added successfully",
            voteAddedError: "Failed to add vote",
            voteRemovedSuccess: "Vote removed successfully",
            voteRemovedError: "Failed to remove vote",
            personUpdated: "{name} was updated",
          },
        },
      },
    });

    wrapper = mount(BallotEntryPage, {
      global: {
        plugins: [i18n],
        stubs: {
          ElCard: {
            template:
              '<div class="el-card"><slot name="header"></slot><slot></slot></div>',
          },
          ElPageHeader: {
            template: '<div class="el-page-header"></div>',
          },
          ElSkeleton: {
            template: '<div class="el-skeleton"></div>',
          },
          ElDescriptions: {
            template: '<div class="el-descriptions"><slot></slot></div>',
          },
          ElDescriptionsItem: {
            template: '<div class="el-descriptions-item"><slot></slot></div>',
            props: ["label"],
          },
          ElTag: {
            template: '<span class="el-tag"><slot></slot></span>',
            props: ["type"],
          },
        },
      },
    });
  });

  it("renders InlineBallotEntry component", async () => {
    await flushPromises();

    const inlineEntry = wrapper.find('[data-testid="inline-ballot-entry"]');
    expect(inlineEntry.exists()).toBe(true);
  });

  it("passes correct props to InlineBallotEntry", async () => {
    await flushPromises();

    const inlineEntry = wrapper.findComponent({ name: "InlineBallotEntry" });
    expect(inlineEntry.exists()).toBe(true);
    expect(inlineEntry.props("electionGuid")).toBe("test-election-guid");
    expect(inlineEntry.props("ballot")).toEqual(mockBallot);
    expect(inlineEntry.props("maxVotes")).toBe(9);
  });

  it("initializes stores on mount", async () => {
    await flushPromises();

    expect(ballotStore.initializeSignalR).toHaveBeenCalled();
    expect(peopleStore.initializeSignalR).toHaveBeenCalled();
    expect(ballotStore.joinElection).toHaveBeenCalledWith("test-election-guid");
    expect(peopleStore.joinElection).toHaveBeenCalledWith("test-election-guid");
    expect(ballotStore.fetchBallotById).toHaveBeenCalledWith(
      "test-ballot-guid",
    );
    expect(electionStore.fetchElectionById).toHaveBeenCalledWith(
      "test-election-guid",
    );
  });

  it("handles vote-added event", async () => {
    await flushPromises();

    const inlineEntry = wrapper.findComponent({ name: "InlineBallotEntry" });
    expect(inlineEntry.exists()).toBe(true);

    const testVote: VoteDto = {
      rowId: 3,
      ballotGuid: "test-ballot-guid",
      positionOnBallot: 3,
      personGuid: "person-3",
      personFullName: "Charlie Brown",
      statusCode: "Ok",
    };

    await inlineEntry.trigger("vote-added", testVote);
    await wrapper.vm.$emit("vote-added", testVote);

    const handler = wrapper.vm.handleVoteAdded;
    if (handler) {
      await handler(testVote);
    }
    await flushPromises();

    expect(ballotStore.createVote).toHaveBeenCalledWith({
      ballotGuid: "test-ballot-guid",
      positionOnBallot: 3,
      personGuid: "person-3",
      personName: "Charlie Brown",
    });
  });

  it("handles vote-removed event", async () => {
    await flushPromises();

    const inlineEntry = wrapper.findComponent({ name: "InlineBallotEntry" });
    expect(inlineEntry.exists()).toBe(true);

    const handler = wrapper.vm.handleVoteRemoved;
    if (handler) {
      await handler(2);
    }
    await flushPromises();

    expect(ballotStore.deleteVote).toHaveBeenCalledWith("test-ballot-guid", 2);
  });

  it("leaves election groups on unmount", async () => {
    await flushPromises();

    wrapper.unmount();
    await flushPromises();

    expect(ballotStore.leaveElection).toHaveBeenCalledWith(
      "test-election-guid",
    );
    expect(peopleStore.leaveElection).toHaveBeenCalledWith(
      "test-election-guid",
    );
  });

  it("displays ballot information", async () => {
    await flushPromises();

    expect(wrapper.text()).toContain("Test Location");
    expect(wrapper.text()).toContain("John Doe");
    expect(wrapper.text()).toContain("Jane Smith");
  });
});
