import { describe, it, expect, vi, beforeEach } from "vitest";
import { mount, flushPromises } from "@vue/test-utils";
import ElementPlus from "element-plus";
import PersonForm from "../PersonForm.vue";
import { pinia, i18n } from "@/test/setup";
import type { PersonDetailDto, PersonListDto } from "@/types/Person";

const mockGetDetails = vi.fn();

vi.mock("@/services/peopleService", () => ({
  peopleService: {
    getDetails: (...args: unknown[]) => mockGetDetails(...args),
  },
}));

vi.mock("@/stores/peopleStore", () => ({
  usePeopleStore: () => ({
    peopleList: [],
    createPerson: vi.fn(),
    updatePerson: vi.fn(),
    deletePerson: vi.fn(),
  }),
}));

vi.mock("@/stores/electionStore", () => ({
  useElectionStore: () => ({
    currentElection: { votingMethods: "P" },
    fetchElectionById: vi.fn().mockResolvedValue(undefined),
  }),
}));

vi.mock("@/stores/eligibilityStore", () => ({
  useEligibilityStore: () => ({
    groupedReasons: {},
    getByCode: vi.fn(),
    fetchReasons: vi.fn().mockResolvedValue(undefined),
  }),
}));

vi.mock("@/composables/useNotifications", () => ({
  useNotifications: () => ({
    showSuccessMessage: vi.fn(),
    showErrorMessage: vi.fn(),
  }),
}));

vi.mock("@/composables/useApiErrorHandler", () => ({
  useApiErrorHandler: () => ({
    handleApiError: vi.fn(),
  }),
}));

const person: PersonListDto = {
  personGuid: "11111111-1111-1111-1111-111111111111",
  fullName: "Pat Smith",
  phone: "+14168972671",
};

function details(overrides: Partial<PersonDetailDto> = {}): PersonDetailDto {
  return {
    personGuid: person.personGuid,
    electionGuid: "22222222-2222-2222-2222-222222222222",
    lastName: "Smith",
    firstName: "Pat",
    fullName: "Pat Smith",
    phone: "+14168972671",
    voteCount: 0,
    canDelete: true,
    ...overrides,
  };
}

async function mountEditForm() {
  const wrapper = mount(PersonForm, {
    props: {
      electionGuid: "22222222-2222-2222-2222-222222222222",
      person,
      isEdit: true,
    },
    global: {
      plugins: [pinia, i18n, ElementPlus],
    },
  });
  await flushPromises();
  return wrapper;
}

describe("PersonForm phone OnlineVoter status", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("hides the block when the person has no phone", async () => {
    mockGetDetails.mockResolvedValue(
      details({ phone: undefined, phoneOnlineVoter: null }),
    );

    const wrapper = await mountEditForm();

    expect(wrapper.find(".phone-online-voter").exists()).toBe(false);
  });

  it("shows never seen and unchecked when there is no P row", async () => {
    mockGetDetails.mockResolvedValue(
      details({
        phoneOnlineVoter: {
          hasPhoneRow: false,
          whenRegistered: null,
          whenLastLogin: null,
          smsStatus: null,
        },
      }),
    );

    const wrapper = await mountEditForm();

    expect(wrapper.find(".phone-online-voter").text()).toContain("Never seen");
    expect(wrapper.find(".phone-online-voter").text()).toContain("Unchecked");
  });

  it("shows not yet used for auth when the P row has no WhenRegistered", async () => {
    mockGetDetails.mockResolvedValue(
      details({
        phoneOnlineVoter: {
          hasPhoneRow: true,
          whenRegistered: null,
          whenLastLogin: null,
          smsStatus: null,
        },
      }),
    );

    const wrapper = await mountEditForm();

    expect(wrapper.find(".phone-online-voter").text()).toContain(
      "Not yet used for authentication",
    );
  });

  it("shows first registered, last login, and OK SMS status", async () => {
    mockGetDetails.mockResolvedValue(
      details({
        phoneOnlineVoter: {
          hasPhoneRow: true,
          whenRegistered: "2026-04-01T12:00:00Z",
          whenLastLogin: "2026-04-02T08:00:00Z",
          smsStatus: "OK",
        },
      }),
    );

    const wrapper = await mountEditForm();
    const text = wrapper.find(".phone-online-voter").text();

    expect(text).toContain("First registered");
    expect(text).toContain("Last login");
    expect(text).toContain("OK");
    expect(wrapper.find(".phone-online-voter__row.is-blocked").exists()).toBe(
      false,
    );
  });

  it("shows blocked SMS status with the stored reason", async () => {
    mockGetDetails.mockResolvedValue(
      details({
        phoneOnlineVoter: {
          hasPhoneRow: true,
          whenRegistered: "2026-03-01T00:00:00Z",
          whenLastLogin: null,
          smsStatus: "landline",
        },
      }),
    );

    const wrapper = await mountEditForm();
    const text = wrapper.find(".phone-online-voter").text();

    expect(text).toContain("Blocked (landline)");
    expect(wrapper.find(".phone-online-voter__row.is-blocked").exists()).toBe(
      true,
    );
  });
});
