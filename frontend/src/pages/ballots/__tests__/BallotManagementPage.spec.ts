import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import { mount, flushPromises } from "@vue/test-utils";
import { createPinia, setActivePinia } from "pinia";
import { createI18n } from "vue-i18n";
import BallotManagementPage from "../BallotManagementPage.vue";
import { useBallotStore } from "@/stores/ballotStore";
import { useLocationStore } from "@/stores/locationStore";
import type { BallotSummaryDto } from "@/utils/ballotSummary";
import { computerFilterValue } from "@/utils/ballotViewFilter";
import { setComputerCode } from "@/utils/computerCodeStorage";
import { setActiveTeller1 } from "@/utils/activeTellerStorage";
import { REQUIRED_FIELD_FLASH_MS } from "@/composables/useRequiredFieldFlash";

const mockReplace = vi.fn();
const routeState = {
  params: { id: "test-election-guid" } as Record<string, string | undefined>,
  path: "/elections/test-election-guid/ballots",
};

vi.mock("vue-router", () => ({
  useRoute: () => routeState,
  useRouter: () => ({
    replace: mockReplace,
  }),
}));

vi.mock("@/components/ballots/BallotEntryPanel.vue", () => ({
  default: {
    name: "BallotEntryPanel",
    template: '<div data-testid="ballot-entry-panel"></div>',
    props: {
      electionGuid: { type: String, required: true },
      ballotGuid: { type: String, required: true },
      showMetadata: { type: Boolean, default: true },
      manageBallotSignalR: { type: Boolean, default: true },
      managePeopleSignalR: { type: Boolean, default: true },
      hasKeyboardTeller: { type: Boolean, default: true },
    },
  },
}));

vi.mock("@/components/tellers/ActiveTellerSelector.vue", () => ({
  default: {
    name: "ActiveTellerSelector",
    template:
      '<div class="active-teller-selector-stub" :class="{ \'required-field-flash\': highlightTeller1 }"></div>',
    props: {
      electionGuid: { type: String, required: true },
      highlightTeller1: { type: Boolean, default: false },
    },
  },
}));

const { mockShowErrorMessage, mockShowSuccessMessage } = vi.hoisted(() => ({
  mockShowErrorMessage: vi.fn(),
  mockShowSuccessMessage: vi.fn(),
}));

vi.mock("@/composables/useNotifications", () => ({
  useNotifications: () => ({
    showErrorMessage: mockShowErrorMessage,
    showSuccessMessage: mockShowSuccessMessage,
    showWarningMessage: vi.fn(),
    showInfoMessage: vi.fn(),
  }),
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
          locationRequired: "Location is required",
          onlineLocationNotAllowed:
            "Cannot start a ballot at the Online location",
          tellerRequired: "Teller is required",
          "statusValue.Ok": "Ok",
        },
        common: {
          refresh: "Refresh",
        },
        locations: {
          locationSelected: "Location selected",
          selectLocation: "Select location",
          currentLocation: "Current location",
          typeOnline: "Online",
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
            emits: ["closed", "update:modelValue"],
            template:
              '<div v-if="modelValue" class="el-drawer" data-testid="ballot-drawer"><slot></slot><button type="button" class="drawer-close-btn" @click="$emit(\'closed\')">close</button></div>',
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

  async function clickAddBallot(wrapper: ReturnType<typeof mountPage>) {
    const addButton = wrapper
      .findAll(".el-button")
      .find((button) => button.text().includes("Add Ballot"));
    expect(addButton).toBeDefined();
    await addButton!.trigger("click");
    await flushPromises();
  }

  beforeEach(() => {
    localStorage.clear();
    mockReplace.mockReset();
    mockShowErrorMessage.mockReset();
    mockShowSuccessMessage.mockReset();
    routeState.params = { id: "test-election-guid" };
    routeState.path = "/elections/test-election-guid/ballots";
    setComputerCode("test-election-guid", "AA");
    setActiveTeller1("Alice");
    setActivePinia(createPinia());
    ballotStore = useBallotStore();
    locationStore = useLocationStore();
    locationStore.locations = [
      {
        locationGuid: "loc-1",
        name: "Main Hall",
        electionGuid: "test-election-guid",
        sortOrder: 1,
        locationType: "Manual",
      },
      {
        locationGuid: "loc-2",
        name: "Side Room",
        electionGuid: "test-election-guid",
        sortOrder: 2,
        locationType: "Manual",
      },
      {
        locationGuid: "loc-online",
        name: "Online",
        electionGuid: "test-election-guid",
        sortOrder: 999,
        locationType: "Online",
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

  afterEach(() => {
    vi.useRealTimers();
  });

  it("loads ballots and joins signalr on mount", async () => {
    mountPage();
    await flushPromises();

    expect(ballotStore.fetchBallots).toHaveBeenCalledWith("test-election-guid");
    expect(ballotStore.initializeSignalR).toHaveBeenCalled();
    expect(ballotStore.joinElection).toHaveBeenCalledWith("test-election-guid");
  });

  it("creates a new ballot and opens the drawer with the same header as an existing ballot", async () => {
    const wrapper = mountPage();
    await flushPromises();

    await clickAddBallot(wrapper);

    expect(ballotStore.createBallot).toHaveBeenCalledWith(
      expect.objectContaining({
        electionGuid: "test-election-guid",
        computerCode: "AA",
        locationGuid: "loc-1",
        teller1: "Alice",
      }),
    );
    const panel = wrapper.findComponent({ name: "BallotEntryPanel" });
    expect(panel.exists()).toBe(true);
    expect(panel.props("showMetadata")).toBe(true);
  });

  it("shows the metadata header when opening an existing ballot", async () => {
    routeState.params = {
      id: "test-election-guid",
      ballotId: "ballot-1",
    };
    routeState.path = "/elections/test-election-guid/ballot/ballot-1";
    const wrapper = mountPage();
    await flushPromises();

    const panel = wrapper.findComponent({ name: "BallotEntryPanel" });
    expect(panel.exists()).toBe(true);
    expect(panel.props("ballotGuid")).toBe("ballot-1");
    expect(panel.props("showMetadata")).toBe(true);
  });

  it("does not start a ballot when location is unset and flashes the location select", async () => {
    vi.useFakeTimers();
    locationStore.selectedLocationGuid = null;
    const wrapper = mountPage();
    await flushPromises();

    await clickAddBallot(wrapper);

    expect(ballotStore.createBallot).not.toHaveBeenCalled();
    expect(mockShowErrorMessage).toHaveBeenCalledWith("Location is required");
    expect(wrapper.find(".location-select").classes()).toContain(
      "required-field-flash",
    );

    vi.advanceTimersByTime(REQUIRED_FIELD_FLASH_MS);
    await flushPromises();
    expect(wrapper.find(".location-select").classes()).not.toContain(
      "required-field-flash",
    );

    wrapper.unmount();
    vi.useRealTimers();
  });

  it("does not start a ballot when the selected location is Online and flashes the location select", async () => {
    vi.useFakeTimers();
    locationStore.selectedLocationGuid = "loc-online";
    const wrapper = mountPage();
    await flushPromises();

    await clickAddBallot(wrapper);

    expect(ballotStore.createBallot).not.toHaveBeenCalled();
    expect(mockShowErrorMessage).toHaveBeenCalledWith(
      "Cannot start a ballot at the Online location",
    );
    expect(wrapper.find(".location-select").classes()).toContain(
      "required-field-flash",
    );

    vi.advanceTimersByTime(REQUIRED_FIELD_FLASH_MS);
    await flushPromises();
    expect(wrapper.find(".location-select").classes()).not.toContain(
      "required-field-flash",
    );

    wrapper.unmount();
    vi.useRealTimers();
  });

  it("does not start a ballot when the main teller is unset and flashes the teller select", async () => {
    vi.useFakeTimers();
    localStorage.clear();
    setComputerCode("test-election-guid", "AA");
    const wrapper = mountPage();
    await flushPromises();

    await clickAddBallot(wrapper);

    expect(ballotStore.createBallot).not.toHaveBeenCalled();
    expect(mockShowErrorMessage).toHaveBeenCalledWith("Teller is required");
    const tellerSelector = wrapper.findComponent({
      name: "ActiveTellerSelector",
    });
    expect(tellerSelector.props("highlightTeller1")).toBe(true);

    vi.advanceTimersByTime(REQUIRED_FIELD_FLASH_MS);
    await flushPromises();
    expect(tellerSelector.props("highlightTeller1")).toBe(false);

    wrapper.unmount();
    vi.useRealTimers();
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

    await clickAddBallot(wrapper);

    const panel = wrapper.findComponent({ name: "BallotEntryPanel" });
    expect(panel.props("hasKeyboardTeller")).toBe(true);
  });

  it("opens a ballot from the ballot route param on load", async () => {
    routeState.params = {
      id: "test-election-guid",
      ballotId: "ballot-1",
    };
    routeState.path = "/elections/test-election-guid/ballot/ballot-1";
    const wrapper = mountPage();
    await flushPromises();

    const panel = wrapper.findComponent({ name: "BallotEntryPanel" });
    expect(panel.exists()).toBe(true);
    expect(panel.props("ballotGuid")).toBe("ballot-1");
  });

  it("aligns the view filter to the bookmarked ballot location and computer", async () => {
    // Default workstation filter would be loc-2 / AA; bookmark is ballot-2 at loc-2 / BB
    // and we start with a different selected location so the default filter would not match.
    locationStore.selectedLocationGuid = "loc-1";
    setComputerCode("test-election-guid", "AA");

    routeState.params = {
      id: "test-election-guid",
      ballotId: "ballot-2",
    };
    routeState.path = "/elections/test-election-guid/ballot/ballot-2";

    const wrapper = mountPage();
    await flushPromises();

    expect(wrapper.text()).toContain(computerFilterValue("loc-2", "BB"));
    const panel = wrapper.findComponent({ name: "BallotEntryPanel" });
    expect(panel.props("ballotGuid")).toBe("ballot-2");
  });

  it("navigates to the ballot path when a ballot is created", async () => {
    const wrapper = mountPage();
    await flushPromises();

    await clickAddBallot(wrapper);

    expect(mockReplace).toHaveBeenCalledWith(
      "/elections/test-election-guid/ballot/new-ballot-guid",
    );
  });

  it("returns to the ballots list path when the drawer is closed", async () => {
    routeState.params = {
      id: "test-election-guid",
      ballotId: "ballot-1",
    };
    routeState.path = "/elections/test-election-guid/ballot/ballot-1";

    const wrapper = mountPage();
    await flushPromises();
    mockReplace.mockClear();

    // Element Plus emits `closed` before updating v-model to false.
    await wrapper.find(".drawer-close-btn").trigger("click");
    await flushPromises();

    expect(mockReplace).toHaveBeenCalledWith(
      "/elections/test-election-guid/ballots",
    );
  });
});
