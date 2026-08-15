import { mount } from "@vue/test-utils";
import { ElButton } from "element-plus";
import { nextTick } from "vue";
import { beforeEach, describe, expect, it, vi } from "vitest";
import BallotAddPersonPanel from "../BallotAddPersonPanel.vue";

const mockT = (key: string) => {
  const translations: Record<string, string> = {
    "ballots.rawVoteNameReference": "Name entered on the ballot",
    "ballots.voteEntryType": "Vote entry type",
    "ballots.voteEntryNormal": "Normal vote",
    "ballots.voteEntryUnidentifiable": "Unidentifiable (U01)",
    "ballots.voteEntryUnreadable": "Unreadable (U02)",
    "ballots.personLessVoteHint": "No person record will be created.",
    "common.cancel": "Cancel",
    "common.save": "Save",
  };
  return translations[key] || key;
};

vi.mock("vue-i18n", () => ({
  createI18n: vi.fn(),
  useI18n: () => ({ t: mockT }),
}));

vi.mock("@/composables/useApiErrorHandler", () => ({
  useApiErrorHandler: () => ({ handleApiError: vi.fn() }),
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
  });
});
