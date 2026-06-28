import { describe, it, expect, vi, beforeEach } from "vitest";
import { mount, flushPromises } from "@vue/test-utils";
import { createPinia, setActivePinia } from "pinia";
import { createI18n } from "vue-i18n";
import BallotManagementPage from "../BallotManagementPage.vue";
import { useBallotStore } from "@/stores/ballotStore";
import type { BallotDto } from "@/types";

vi.mock("vue-router", () => ({
  useRoute: () => ({
    params: { id: "test-election-guid" },
  }),
}));

vi.mock("@/components/ballots/BallotEntryPanel.vue", () => ({
  default: {
    name: "BallotEntryPanel",
    template: '<div data-testid="ballot-entry-panel"></div>',
    props: [
      "electionGuid",
      "ballotGuid",
      "showMetadata",
      "manageBallotSignalR",
      "managePeopleSignalR",
    ],
  },
}));

vi.mock("@/components/tellers/ActiveTellerSelector.vue", () => ({
  default: {
    name: "ActiveTellerSelector",
    template: "<div></div>",
    props: ["electionGuid"],
  },
}));

describe("BallotManagementPage", () => {
  let ballotStore: ReturnType<typeof useBallotStore>;

  const mockBallots: BallotDto[] = [
    {
      ballotGuid: "ballot-1",
      electionGuid: "test-election-guid",
      ballotCode: "A1",
      locationGuid: "loc-1",
      locationName: "Main Hall",
      ballotNumAtComputer: 1,
      computerCode: "A",
      statusCode: "Ok",
      teller1: "Alice",
      teller2: "Bob",
      voteCount: 3,
      votes: [],
    },
  ];

  const i18n = createI18n({
    legacy: false,
    locale: "en",
    messages: {
      en: {
        ballots: {
          management: "Enter Ballots",
          code: "Ballot Code",
          location: "Location",
          computer: "Computer",
          status: "Status",
          teller1: "Teller 1",
          teller2: "Teller 2",
          voteCount: "Votes",
          addBallot: "Add Ballot",
          createSuccess: "Ballot created successfully",
          entry: "Ballot Entry - {code}",
          entryPage: "Ballot Entry",
          loadError: "Failed to load ballots",
          "statusValue.Ok": "Ok",
        },
        common: {
          actions: "Actions",
        },
      },
    },
  });

  function mountPage() {
    return mount(BallotManagementPage, {
      global: {
        plugins: [i18n],
        stubs: {
          ElCard: {
            template:
              '<div class="el-card"><slot name="header"></slot><slot></slot></div>',
          },
          ElTable: true,
          ElTableColumn: true,
          ElButton: {
            template:
              '<button class="el-button" @click="$emit(\'click\')"><slot></slot></button>',
          },
          ElTag: {
            template: '<span class="el-tag"><slot></slot></span>',
          },
          ElSkeleton: {
            template: '<div class="el-skeleton"></div>',
          },
          ElDrawer: {
            props: ["modelValue"],
            template:
              '<div v-if="modelValue" class="el-drawer"><slot></slot></div>',
          },
          ElIcon: true,
        },
      },
    });
  }

  beforeEach(() => {
    setActivePinia(createPinia());
    ballotStore = useBallotStore();
    ballotStore.ballots = mockBallots;
    ballotStore.loading = false;

    vi.spyOn(ballotStore, "fetchBallots").mockResolvedValue(mockBallots);
    vi.spyOn(ballotStore, "initializeSignalR").mockResolvedValue(undefined);
    vi.spyOn(ballotStore, "joinElection").mockResolvedValue(undefined);
    vi.spyOn(ballotStore, "leaveElection").mockResolvedValue(undefined);
    vi.spyOn(ballotStore, "createBallot").mockImplementation(async () => {
      const created: BallotDto = {
        ...mockBallots[0],
        ballotGuid: "new-ballot-guid",
        ballotCode: "A2",
        ballotNumAtComputer: 2,
        voteCount: 0,
      };
      ballotStore.ballots.push(created);
      return created;
    });
  });

  it("loads ballots and joins signalr on mount", async () => {
    mountPage();
    await flushPromises();

    expect(ballotStore.fetchBallots).toHaveBeenCalledWith("test-election-guid");
    expect(ballotStore.initializeSignalR).toHaveBeenCalled();
    expect(ballotStore.joinElection).toHaveBeenCalledWith("test-election-guid");
  });

  it("creates a new ballot and opens drawer without metadata", async () => {
    const wrapper = mountPage();
    await flushPromises();

    const addButtons = wrapper.findAll(".el-button");
    const addButton = addButtons.find((b) => b.text().includes("Add Ballot"));
    expect(addButton).toBeDefined();
    await addButton!.trigger("click");
    await flushPromises();

    expect(ballotStore.createBallot).toHaveBeenCalled();
    const panel = wrapper.findComponent({ name: "BallotEntryPanel" });
    expect(panel.exists()).toBe(true);
    expect(panel.props("showMetadata")).toBe(false);
  });

  it("does not render an actions column", async () => {
    const wrapper = mountPage();
    await flushPromises();

    expect(wrapper.text()).not.toContain("Enter Votes");
    expect(wrapper.text()).not.toContain("View Votes");
    expect(wrapper.text()).not.toContain("Import CDN Ballots");
  });
});
