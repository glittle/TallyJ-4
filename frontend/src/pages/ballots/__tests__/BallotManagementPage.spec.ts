import { describe, it, expect, vi, beforeEach } from "vitest";
import { mount, flushPromises } from "@vue/test-utils";
import { createPinia, setActivePinia } from "pinia";
import { createI18n } from "vue-i18n";
import BallotManagementPage from "../BallotManagementPage.vue";
import { useBallotStore } from "@/stores/ballotStore";
import { useLocationStore } from "@/stores/locationStore";
import type { BallotSummaryDto } from "@/utils/ballotSummary";
import { computerFilterValue } from "@/utils/ballotViewFilter";
import { setComputerCode } from "@/utils/computerCodeStorage";

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
      "hasKeyboardTeller",
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
  let locationStore: ReturnType<typeof useLocationStore>;

  const mockBallots: BallotSummaryDto[] = [
    {
      ballotGuid: "ballot-1",
      ballotCode: "A1",
      locationGuid: "loc-1",
      locationName: "Main Hall",
      ballotNumAtComputer: 1,
      computerCode: "AA",
      statusCode: "Ok",
      teller1: "Alice",
      teller2: "Bob",
      voteCount: 3,
    },
    {
      ballotGuid: "ballot-2",
      ballotCode: "B1",
      locationGuid: "loc-2",
      locationName: "Side Room",
      ballotNumAtComputer: 1,
      computerCode: "BB",
      statusCode: "Ok",
      teller1: "Alice",
      teller2: "Bob",
      voteCount: 1,
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
          allBallots: "All ballots",
          allAtLocation: "All at {name}",
          viewFilterLabel: "Ballots to show",
          viewFilterPlaceholder: "Search computers or locations",
          computerCodeRequired:
            "Set this computer's code before creating a ballot",
          "statusValue.Ok": "Ok",
        },
        common: {
          refresh: "Refresh",
        },
        locations: {
          locationSelected: "Location selected",
          selectLocation: "Select location",
          currentLocation: "Current location",
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
          ElTable: {
            props: ["data"],
            template:
              '<div class="el-table"><div v-for="row in data" :key="row.ballotGuid"><span class="ballot-code">{{ row.ballotCode }}</span><span class="location-name">{{ row.locationName }}</span></div></div>',
          },
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
          ElSelect: {
            props: ["modelValue"],
            emits: ["update:modelValue"],
            template:
              '<div class="el-select" @click="$emit(\'update:modelValue\', modelValue)">{{ modelValue }}</div>',
          },
          ElOption: true,
          ElOptionGroup: true,
        },
      },
    });
  }

  beforeEach(() => {
    localStorage.clear();
    setComputerCode("test-election-guid", "AA");
    setActivePinia(createPinia());
    ballotStore = useBallotStore();
    locationStore = useLocationStore();
    locationStore.locations = [
      {
        locationGuid: "loc-1",
        name: "Main Hall",
        electionGuid: "test-election-guid",
        sortOrder: 1,
      },
      {
        locationGuid: "loc-2",
        name: "Side Room",
        electionGuid: "test-election-guid",
        sortOrder: 2,
      },
    ];
    locationStore.selectedLocationGuid = "loc-1";
    ballotStore.ballots = mockBallots;
    ballotStore.loading = false;

    vi.spyOn(ballotStore, "fetchBallots").mockResolvedValue(undefined);
    vi.spyOn(locationStore, "fetchLocations").mockResolvedValue(undefined);
    vi.spyOn(ballotStore, "initializeSignalR").mockResolvedValue(undefined);
    vi.spyOn(ballotStore, "joinElection").mockResolvedValue(undefined);
    vi.spyOn(ballotStore, "leaveElection").mockResolvedValue(undefined);
    vi.spyOn(ballotStore, "createBallot").mockImplementation(async () => {
      const created = {
        ...mockBallots[0],
        ballotGuid: "new-ballot-guid",
        ballotCode: "A2",
        ballotNumAtComputer: 2,
        voteCount: 0,
        votes: [],
      };
      ballotStore.ballots.push({
        ballotGuid: created.ballotGuid,
        ballotCode: created.ballotCode,
        locationGuid: created.locationGuid,
        locationName: created.locationName,
        ballotNumAtComputer: created.ballotNumAtComputer,
        computerCode: created.computerCode,
        statusCode: created.statusCode,
        teller1: created.teller1,
        teller2: created.teller2,
        voteCount: created.voteCount,
      });
      ballotStore.currentBallot = created;
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

  it("defaults the ballot list to the current computer at the selected location", async () => {
    const wrapper = mountPage();
    await flushPromises();

    expect(wrapper.text()).toContain(computerFilterValue("loc-1", "AA"));
    expect(wrapper.text()).toContain("A1");
    expect(wrapper.text()).not.toContain("B1");
  });

  it("shows location names when the election has multiple locations", async () => {
    const wrapper = mountPage();
    await flushPromises();

    expect(wrapper.text()).toContain("Main Hall");
  });

  it("passes keyboard teller state to the entry panel", async () => {
    const wrapper = mountPage();
    await flushPromises();

    const addButton = wrapper
      .findAll(".el-button")
      .find((button) => button.text().includes("Add Ballot"));
    await addButton!.trigger("click");
    await flushPromises();

    const panel = wrapper.findComponent({ name: "BallotEntryPanel" });
    expect(panel.props("hasKeyboardTeller")).toBe(false);
  });
});
