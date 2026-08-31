import { flushPromises, mount } from "@vue/test-utils";
import { createPinia, setActivePinia } from "pinia";
import { createI18n } from "vue-i18n";
import { beforeEach, describe, expect, it, vi } from "vitest";
import {
  getActiveTellers,
  setActiveTeller1,
  setActiveTeller2,
} from "@/utils/activeTellerStorage";
import { useActiveTellers } from "@/composables/useActiveTellers";
import type { BallotDto } from "@/types/Ballot";
import type { ElectionDto } from "@/types";
import BallotEntryPanel from "../BallotEntryPanel.vue";

const mockBallot: BallotDto = {
  ballotGuid: "ballot-1",
  locationGuid: "loc-1",
  locationName: "Main Hall",
  computerCode: "AA",
  ballotCode: "A1",
  ballotNumAtComputer: 1,
  statusCode: "Ok",
  teller1: "StoredOnBallot",
  teller2: "AlsoStored",
  voteCount: 0,
  votes: [],
};

const mockElection = {
  electionGuid: "elec-1",
  numberToElect: 9,
} as ElectionDto;

const mockBallotStore = {
  currentBallot: mockBallot as BallotDto | null,
  fetchBallotById: vi.fn(),
  updateBallot: vi.fn(),
  initializeSignalR: vi.fn(),
  joinElection: vi.fn(),
  leaveElection: vi.fn(),
  createVote: vi.fn(),
  updateVote: vi.fn(),
  deleteVote: vi.fn(),
  reorderVotes: vi.fn(),
};

const mockElectionStore = {
  currentElection: mockElection,
  fetchElectionById: vi.fn(),
};

const mockPeopleStore = {
  initializeSignalR: vi.fn(),
  joinElection: vi.fn(),
  leaveElection: vi.fn(),
  onPersonUpdated: vi.fn(() => () => undefined),
};

vi.mock("@/stores/ballotStore", () => ({
  useBallotStore: () => mockBallotStore,
}));

vi.mock("@/stores/electionStore", () => ({
  useElectionStore: () => mockElectionStore,
}));

vi.mock("@/stores/peopleStore", () => ({
  usePeopleStore: () => mockPeopleStore,
}));

vi.mock("@/composables/useNotifications", () => ({
  useNotifications: () => ({
    showSuccessMessage: vi.fn(),
    showErrorMessage: vi.fn(),
    showInfoMessage: vi.fn(),
  }),
}));

vi.mock("../InlineBallotEntry.vue", () => ({
  default: {
    name: "InlineBallotEntry",
    template: '<div data-testid="inline-ballot-entry"></div>',
    props: ["electionGuid", "ballot", "requiredVotes", "hasKeyboardTeller"],
  },
}));

const { selectorChanges } = vi.hoisted(() => ({
  selectorChanges: {
    teller1: "",
    teller2: "",
  },
}));

vi.mock("@/components/tellers/ActiveTellerSelector.vue", async () => {
  const { setActiveTeller1, setActiveTeller2 } = await import(
    "@/utils/activeTellerStorage"
  );
  const { useActiveTellers } = await import("@/composables/useActiveTellers");
  const { defineComponent, h } = await import("vue");
  return {
    default: defineComponent({
      name: "ActiveTellerSelector",
      props: {
        electionGuid: { type: String, required: true },
        field: { type: String, default: "all" },
        highlightTeller1: { type: Boolean, default: false },
      },
      emits: ["tellersChanged"],
      setup(props, { emit }) {
        const { setTeller1, setTeller2, tellers } = useActiveTellers();
        function apply() {
          if (props.field === "teller2") {
            setActiveTeller2(selectorChanges.teller2);
            setTeller2(selectorChanges.teller2);
          } else {
            setActiveTeller1(selectorChanges.teller1);
            setTeller1(selectorChanges.teller1);
          }
          emit("tellersChanged", { ...tellers.value });
        }
        return () =>
          h(
            "button",
            {
              type: "button",
              class: `active-teller-selector-stub field-${props.field}`,
              "data-highlight": String(props.highlightTeller1),
              onClick: apply,
            },
            props.field,
          );
      },
    }),
  };
});

const i18n = createI18n({
  legacy: false,
  locale: "en",
  messages: {
    en: {
      ballots: {
        location: "Location",
        computer: "Computer",
        teller1: "Teller 1",
        teller2: "Teller 2",
        loadError: "Failed to load",
      },
    },
  },
});

function mountPanel() {
  return mount(BallotEntryPanel, {
    props: {
      electionGuid: "elec-1",
      ballotGuid: "ballot-1",
      highlightTeller1: true,
    },
    global: {
      plugins: [i18n, createPinia()],
      stubs: {
        ElSkeleton: { template: '<div class="el-skeleton"></div>' },
        ElDescriptions: { template: "<div class='el-descriptions'><slot /></div>" },
        ElDescriptionsItem: {
          props: ["label"],
          template:
            '<div class="el-descriptions-item"><span class="item-label">{{ label }}</span><slot /></div>',
        },
      },
    },
  });
}

describe("BallotEntryPanel session tellers", () => {
  beforeEach(() => {
    localStorage.clear();
    setActivePinia(createPinia());
    selectorChanges.teller1 = "Pat";
    selectorChanges.teller2 = "Sam";
    mockBallotStore.currentBallot = { ...mockBallot };
    mockBallotStore.fetchBallotById.mockResolvedValue(mockBallot);
    mockBallotStore.updateBallot.mockResolvedValue(mockBallot);
    mockBallotStore.initializeSignalR.mockResolvedValue(undefined);
    mockBallotStore.joinElection.mockResolvedValue(undefined);
    mockBallotStore.leaveElection.mockResolvedValue(undefined);
    mockElectionStore.fetchElectionById.mockResolvedValue(mockElection);
    mockPeopleStore.initializeSignalR.mockResolvedValue(undefined);
    mockPeopleStore.joinElection.mockResolvedValue(undefined);
    mockPeopleStore.leaveElection.mockResolvedValue(undefined);
    useActiveTellers().refreshActiveTellers();
  });

  it("shows Teller 1 and Teller 2 as session inputs, not the stored ballot names", async () => {
    setActiveTeller1("SessionOne");
    setActiveTeller2("SessionTwo");
    useActiveTellers().refreshActiveTellers();

    const wrapper = mountPanel();
    await flushPromises();

    expect(wrapper.text()).not.toContain("StoredOnBallot");
    expect(wrapper.text()).not.toContain("AlsoStored");
    expect(wrapper.find(".field-teller1").exists()).toBe(true);
    expect(wrapper.find(".field-teller2").exists()).toBe(true);
    expect(wrapper.text()).toContain("Location");
    expect(wrapper.text()).toContain("Main Hall");
  });

  it("changing Teller 1 on the open ballot sets the listing session global", async () => {
    const wrapper = mountPanel();
    await flushPromises();

    await wrapper.find(".field-teller1").trigger("click");
    await flushPromises();

    expect(getActiveTellers().teller1).toBe("Pat");
    expect(useActiveTellers().tellers.value.teller1).toBe("Pat");
  });

  it("changing Teller 2 on the open ballot sets the listing session global", async () => {
    const wrapper = mountPanel();
    await flushPromises();

    await wrapper.find(".field-teller2").trigger("click");
    await flushPromises();

    expect(getActiveTellers().teller2).toBe("Sam");
    expect(useActiveTellers().tellers.value.teller2).toBe("Sam");
  });

  it("forwards the teller-required highlight to Teller 1", async () => {
    const wrapper = mountPanel();
    await flushPromises();

    expect(wrapper.find(".field-teller1").attributes("data-highlight")).toBe(
      "true",
    );
  });
});
