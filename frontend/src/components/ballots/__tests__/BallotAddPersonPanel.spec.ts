import { mount } from "@vue/test-utils";
import { ElButton } from "element-plus";
import { nextTick } from "vue";
import { beforeEach, describe, expect, it, vi } from "vitest";
import BallotAddPersonPanel from "../BallotAddPersonPanel.vue";

const mockT = (key: string) => {
  const translations: Record<string, string> = {
    "ballots.rawVoteNameReference": "Name entered on the ballot",
    "ballots.voteEntryType": "Vote entry type",
    "ballots.voteEntryNormal": "Full Name",
    "ballots.voteEntryNameNotInTheList": "Name not in the List",
    "ballots.addNewNameIncludingSpoiled": "Add new name (including spoiled)",
    "ballots.askHeadTellerToAddName": "(Ask head teller to add required name)",
    "ballots.voteEntryUnidentifiable": "Unidentifiable (U01)",
    "ballots.voteEntryUnreadable": "Unreadable (U02)",
    "ballots.personLessVoteHint": "No person record will be created.",
    "common.cancel": "Cancel",
    "common.save": "Save",
  };
  return translations[key] || key;
};

vi.mock("vue-i18n", async (importOriginal) => {
  const actual = await importOriginal<typeof import("vue-i18n")>();
  return {
    ...actual,
    useI18n: () => ({ t: mockT }),
  };
});

vi.mock("@/composables/useApiErrorHandler", () => ({
  useApiErrorHandler: () => ({ handleApiError: vi.fn() }),
}));

const { isGuestTellerMock, currentElection } = vi.hoisted(() => ({
  isGuestTellerMock: vi.fn(() => false),
  currentElection: { guestTellersCanAddPeople: false },
}));

vi.mock("@/domain/guestTellerAccess", () => ({
  isGuestTeller: () => isGuestTellerMock(),
}));

vi.mock("@/stores/electionStore", () => ({
  useElectionStore: () => ({
    currentElection,
  }),
}));

vi.mock("@/stores/peopleStore", () => ({
  usePeopleStore: () => ({
    createPerson: vi.fn(),
    enrichPersonForSearch: vi.fn(),
    isCacheInitialized: false,
    peopleCache: [],
  }),
}));

const PersonFormStub = {
  name: "PersonForm",
  template:
    '<div class="person-form-stub"><input class="first-name" v-model="form.firstName" /><input class="last-name" v-model="form.lastName" /></div>',
  data() {
    return {
      form: { firstName: "", lastName: "" },
    };
  },
};

describe("BallotAddPersonPanel", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    isGuestTellerMock.mockReturnValue(false);
    currentElection.guestTellersCanAddPeople = false;
  });

  function mountPanel() {
    return mount(BallotAddPersonPanel, {
      props: {
        electionGuid: "election-1",
        ballotGuid: "ballot-1",
        rawVote: {
          first: "Jon",
          last: "Smyth",
          otherInfo: "Jon Smyth",
        },
      },
      global: {
        components: {
          ElButton,
          PersonForm: PersonFormStub,
        },
        stubs: {
          PersonForm: PersonFormStub,
        },
        mocks: {
          $t: mockT,
        },
      },
    });
  }

  it("shows the raw name as a reference", () => {
    const wrapper = mountPanel();
    expect(wrapper.find(".ballot-add-person-panel__raw-label").text()).toBe(
      "Name entered on the ballot",
    );
    expect(wrapper.find(".ballot-add-person-panel__raw-value").text()).toBe(
      "Jon Smyth",
    );
  });

  it("copies first and last into the person form for a normal vote", async () => {
    const wrapper = mountPanel();
    (wrapper.vm as { voteEntryType: string }).voteEntryType = "normal";
    await nextTick();
    await nextTick();

    const form = (
      wrapper.findComponent({ name: "PersonForm" }).vm as {
        form: { firstName: string; lastName: string };
      }
    ).form;
    expect(form.firstName).toBe("Jon");
    expect(form.lastName).toBe("Smyth");
    expect(wrapper.text()).toContain("Name not in the List");
    expect(wrapper.text()).toContain("Add new name (including spoiled)");
  });

  it("asks a guest teller to wait when Can Add People is off", async () => {
    isGuestTellerMock.mockReturnValue(true);
    currentElection.guestTellersCanAddPeople = false;
    const wrapper = mountPanel();
    (wrapper.vm as { voteEntryType: string }).voteEntryType = "normal";
    await nextTick();

    expect(wrapper.text()).toContain("(Ask head teller to add required name)");
    expect(wrapper.findComponent({ name: "PersonForm" }).exists()).toBe(false);
    const save = wrapper
      .findAllComponents(ElButton)
      .find((button) => button.text() === "Save");
    expect(save?.props("disabled")).toBe(true);
  });

  it("lets a guest teller add a name when Can Add People is on", async () => {
    isGuestTellerMock.mockReturnValue(true);
    currentElection.guestTellersCanAddPeople = true;
    const wrapper = mountPanel();
    (wrapper.vm as { voteEntryType: string }).voteEntryType = "normal";
    await nextTick();

    expect(wrapper.findComponent({ name: "PersonForm" }).exists()).toBe(true);
    expect(wrapper.text()).toContain("Add new name (including spoiled)");
  });
});
